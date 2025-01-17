using System;
using System.Collections;
using System.Collections.Generic;
using PX.CCProcessingBase;
using PX.Common;
using PX.Api;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.TX;
using PX.Objects.CA;
using ItemLotSerial = PX.Objects.IN.Overrides.INDocumentRelease.ItemLotSerial;
using SiteLotSerial = PX.Objects.IN.Overrides.INDocumentRelease.SiteLotSerial;
using LocationStatus = PX.Objects.IN.Overrides.INDocumentRelease.LocationStatus;
using LotSerialStatus = PX.Objects.IN.Overrides.INDocumentRelease.LotSerialStatus;
using POLineType = PX.Objects.PO.POLineType;
using POReceiptLine = PX.Objects.PO.POReceiptLine;
using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;
using System.Linq;
using CRLocation = PX.Objects.CR.Standalone.Location;
using PX.Objects.AR.CCPaymentProcessing;
using PX.Objects.AR.CCPaymentProcessing.Common;
using PX.Objects.AR.CCPaymentProcessing.Helpers;
using PX.Objects.AR.CCPaymentProcessing.Interfaces;
using PX.Objects.Common;
using PX.Objects.Common.Discount;
using PX.Objects.AR.MigrationMode;
using PX.Objects.Common.Bql;
using PX.Common.Collection;
using PX.Objects.GL.FinPeriods;
using PX.TaxProvider;
using PX.Objects.Extensions.PaymentTransaction;
namespace PX.Objects.SO
{
	public class SOInvoiceEntry : ARInvoiceEntry
	{
		private const string ClassName = "SOInvoiceEntry";
		public PXAction<ARInvoice> selectShipment;
		[PXUIField(DisplayName = "Add Order", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable SelectShipment(PXAdapter adapter)
		{
			if (this.Document.Cache.AllowDelete)
				shipmentlist.AskExt();
			return adapter.Get();
		}

		public PXAction<ARInvoice> addShipment;
		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXLookupButton]
		public virtual IEnumerable AddShipment(PXAdapter adapter)
		{
			var orders = shipmentlist
				.Cache.Updated.Cast<SOOrderShipment>()
				.Where(sho => sho.Selected == true)
				.SelectMany(sho =>
						PXSelectJoin<SOOrderShipment,
						InnerJoin<SOOrder, 
							On<SOOrderShipment.FK.Order>,
						InnerJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<SOOrder.curyInfoID>>,
						InnerJoin<SOAddress, On<SOAddress.addressID, Equal<SOOrder.billAddressID>>,
						InnerJoin<SOContact, On<SOContact.contactID, Equal<SOOrder.billContactID>>>>>>,
					Where<SOOrderShipment.shipmentNbr, Equal<Current<SOOrderShipment.shipmentNbr>>, 
						And<SOOrderShipment.shipmentType, Equal<Current<SOOrderShipment.shipmentType>>,
						And<SOOrderShipment.orderType, Equal<Current<SOOrderShipment.orderType>>,
						And<SOOrderShipment.orderNbr, Equal<Current<SOOrderShipment.orderNbr>>>>>>>
					.SelectMultiBound(this, new object[] {sho}).AsEnumerable()
					.Cast<PXResult<SOOrderShipment, SOOrder, CurrencyInfo, SOAddress, SOContact>>()
					.Select(row => new {Shipment = sho, Row = row}))
				.ToArray();
			
			var linkedOrdersKeys =
				PXSelect<SOOrderShipment,
				Where<SOOrderShipment.invoiceType, Equal<Current<ARInvoice.docType>>,
					And<SOOrderShipment.invoiceNbr, Equal<Current<ARInvoice.refNbr>>>>>
				.Select(this).AsEnumerable()
				.RowCast<SOOrderShipment>()
				.Select(r => new { Type = r.OrderType, Nbr = r.OrderNbr })
				.ToHashSet();
			var linkedOrders = linkedOrdersKeys.Any() // will fall if linked orders count is more than 1000
				? PXSelectReadonly<SOOrder, Where<SOOrder.orderType, In<Required<SOOrder.orderType>>, And<SOOrder.orderNbr, In<Required<SOOrder.orderNbr>>>>>
					.Select(this, linkedOrdersKeys.Select(k => k.Type).ToArray(), linkedOrdersKeys.Select(k => k.Nbr).ToArray()).AsEnumerable()
					.RowCast<SOOrder>()
					.Where(so => linkedOrdersKeys.Contains(new {Type = so.OrderType, Nbr = so.OrderNbr}))
					.ToArray()
				: Enumerable.Empty<SOOrder>();

			var ordersByTaxZone = orders.Select(r => r.Row.GetItem<SOOrder>()).Concat(linkedOrders).ToLookup(s => s.TaxZoneID);
			string theOnlyTaxZone = ordersByTaxZone.Any()
				? Document.Current?.TaxZoneID ?? linkedOrders.FirstOrDefault()?.TaxZoneID ?? ordersByTaxZone.First().Key
				: null;

			bool requireControlTotal = ARSetup.Current.RequireControlTotal == true;
			var excludedOrders = new List<SOOrder>();
			foreach (var order in orders)
			{
				if (order.Row.GetItem<SOOrder>().TaxZoneID == theOnlyTaxZone)
				{
					var details = new PXResultset<SOShipLine, SOLine>();
					details.AddRange(
						PXSelectJoin<POReceiptLine,
						InnerJoin<SOLineSplit, On<SOLineSplit.pOType, Equal<POReceiptLine.pOType>,
							And<SOLineSplit.pONbr, Equal<POReceiptLine.pONbr>,
							And<SOLineSplit.pOLineNbr, Equal<POReceiptLine.pOLineNbr>>>>,
						InnerJoin<SOLine, On<SOLine.orderType, Equal<SOLineSplit.orderType>,
							And<SOLine.orderNbr, Equal<SOLineSplit.orderNbr>,
							And<SOLine.lineNbr, Equal<SOLineSplit.lineNbr>>>>>>,
						Where<POReceiptLine.lineType, In3<POLineType.goodsForDropShip, POLineType.nonStockForDropShip>,
							And<SOShipmentType.dropShip, Equal<Current<SOOrderShipment.shipmentType>>,
							And<POReceiptLine.receiptNbr, Equal<Current<SOOrderShipment.shipmentNbr>>,
							And<SOLine.orderType, Equal<Current<SOOrderShipment.orderType>>,
							And<SOLine.orderNbr, Equal<Current<SOOrderShipment.orderNbr>>>>>>>>
						.SelectMultiBound(this, new object[] {order.Row.GetItem<SOOrderShipment>()}).AsEnumerable()
						.Cast<PXResult<POReceiptLine, SOLineSplit, SOLine>>()
						.Select(line => new PXResult<SOShipLine, SOLine>(line, line)));

						ARSetup.Current.RequireControlTotal = false;
					this.InvoiceOrder((DateTime) this.Accessinfo.BusinessDate, order.Row, details, null, null);
					ARSetup.Current.RequireControlTotal = requireControlTotal;
					order.Shipment.HasDetailDeleted = false;
					shipmentlist.Update(order.Shipment);
					}
				else
				{
					excludedOrders.Add(order.Row);
				}
			}

			shipmentlist.View.Clear();

			if (excludedOrders.Any())
				throw new PXInvalidOperationException(
					Messages.CannotAddOrderToInvoiceDueToTaxZoneConflict,
					theOnlyTaxZone,
					string.Join(",", excludedOrders.Select(s => s.OrderNbr)));

			return adapter.Get();
		}

		public PXAction<ARInvoice> addShipmentCancel;
		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXLookupButton]
		public virtual IEnumerable AddShipmentCancel(PXAdapter adapter)
		{
			foreach (SOOrderShipment shipment in shipmentlist.Cache.Updated)
			{
				if (shipment.InvoiceNbr == null)
				{
					shipment.Selected = false;
				}
			}

			shipmentlist.View.Clear();
			//shipmentlist.Cache.Clear();
			return adapter.Get();
		}
		public bool cancelUnitPriceCalculation
		{
			get;
			set;
		}
		private bool forceDiscountCalculation = false;
		public PXSelect<ARTran, Where<ARTran.tranType, Equal<Current<ARInvoice.docType>>, And<ARTran.refNbr, Equal<Current<ARInvoice.refNbr>>, And<ARTran.lineType, Equal<SOLineType.freight>>>>, OrderBy<Asc<ARTran.tranType, Asc<ARTran.refNbr, Asc<ARTran.lineNbr>>>>> Freight;
		public PXSelect<ARTran, Where<ARTran.tranType, Equal<Current<ARInvoice.docType>>, And<ARTran.refNbr, Equal<Current<ARInvoice.refNbr>>, And<ARTran.lineType, Equal<SOLineType.discount>>>>, OrderBy<Asc<ARTran.tranType, Asc<ARTran.refNbr, Asc<ARTran.lineNbr>>>>> Discount;
		public PXSelect<ARSalesPerTran, Where<ARSalesPerTran.docType, Equal<Current<ARInvoice.docType>>, And<ARSalesPerTran.refNbr, Equal<Current<ARInvoice.refNbr>>>>> commisionlist;
        public PXSelect<SOInvoice, Where<SOInvoice.docType, Equal<Optional<ARInvoice.docType>>, And<SOInvoice.refNbr, Equal<Optional<ARInvoice.refNbr>>>>> SODocument;
		[PXCopyPasteHiddenView]
        public PXSelectOrderBy<SOOrderShipment, OrderBy<Asc<SOOrderShipment.orderType, Asc<SOOrderShipment.orderNbr, Asc<SOOrderShipment.shipmentNbr, Asc<SOOrderShipment.shipmentType>>>>>> shipmentlist;
		public PXSelect<SOShipment> shipments;
		public PXSelect<ARInvoiceDiscountDetail, Where<ARInvoiceDiscountDetail.docType, Equal<Current<ARInvoice.docType>>, And<ARInvoiceDiscountDetail.refNbr, Equal<Current<ARInvoice.refNbr>>>>,
			OrderBy<Asc<ARInvoiceDiscountDetail.orderType, Asc<ARInvoiceDiscountDetail.orderNbr, Asc<ARInvoiceDiscountDetail.lineNbr>>>>> DiscountDetails;
		[PXCopyPasteHiddenView]
        public PXSelect<SOFreightDetail, Where<SOFreightDetail.docType, Equal<Current<ARInvoice.docType>>, And<SOFreightDetail.refNbr, Equal<Current<ARInvoice.refNbr>>>>> FreightDetails;
		

		public PXSelect<SOAdjust> soadjustments;
        public PXSelect<INTran> inTran;
		public PM.PMCommitmentSelect pmselect;
		public PXSetup<SOOrderType, Where<SOOrderType.orderType, Equal<Optional<SOOrder.orderType>>>> soordertype;

		public PXSelect<ARInvoice> invoiceview;

		[PXHidden]
        public new ARCustomerCreditHelper<ARInvoice, ARInvoice.customerID> CustomerCreditHelper;
		protected override void UpdateARBalances(PXCache cache, object newRow, object oldRow)
			=> CustomerCreditHelper.UpdateARBalances(cache, newRow, oldRow);

        #region Cache Attached
        #region ARTran
        [PXDBString(2, IsFixed = true)]
		[SOLineType.List()]
		[PXUIField(DisplayName = "Line Type", Visible = false, Enabled = false)]
		[PXDefault]
		[PXFormula(typeof(Switch<
			Case<Where<ARTran.inventoryID, IsNull>, SOLineType.nonInventory,
			Case<Where<ARTran.sOShipmentNbr, IsNull>,
				Selector<ARTran.inventoryID, Switch<
					Case<Where<InventoryItem.stkItem, Equal<True>>, SOLineType.inventory,
					Case<Where<InventoryItem.nonStockShip, Equal<True>>, SOLineType.nonInventory>>,
				SOLineType.miscCharge>>>>,
			ARTran.lineType>))]
		protected virtual void ARTran_LineType_CacheAttached(PXCache sender)
		{ 
		}

		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Category")]
		[SOInvoiceTax()]
        [PXSelector(typeof(TaxCategory.taxCategoryID), DescriptionField = typeof(TaxCategory.descr))]
        [PXRestrictor(typeof(Where<TaxCategory.active, Equal<True>>), TX.Messages.InactiveTaxCategory, typeof(TaxCategory.taxCategoryID))]
        [PXDefault(typeof(Search<InventoryItem.taxCategoryID,
			Where<InventoryItem.inventoryID, Equal<Current<ARTran.inventoryID>>>>),
			PersistingCheck = PXPersistingCheck.Nothing, SearchOnDefault = false)]
		protected override void ARTran_TaxCategoryID_CacheAttached(PXCache sender)
		{
		}

		[PXDBBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Manual Price", Visible = true)]
		protected virtual void ARTran_ManualPrice_CacheAttached(PXCache sender)
		{
		}

        [PopupMessage]
		[PXRemoveBaseAttribute(typeof(ARTranInventoryItemAttribute))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[NonStockNonKitCrossItem(INPrimaryAlternateType.CPN, Messages.CannotAddNonStockKitDirectly, typeof(ARTran.sOOrderNbr),
			typeof(FeaturesSet.advancedSOInvoices), Filterable = true)]
		protected override void ARTran_InventoryID_CacheAttached(PXCache sender) { }

		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[IN.SiteAvail(typeof(ARTran.inventoryID), typeof(ARTran.subItemID))]
		[InterBranchRestrictor(typeof(Where<SameOrganizationBranch<INSite.branchID, Current<ARInvoice.branchID>>>))]
		protected void ARTran_SiteID_CacheAttached(PXCache sender) { }

		//Returning original attributes from ARTran
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected override void ARTran_LocationID_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[ARTranPlanID(typeof(ARRegister.noteID), typeof(ARRegister.hold))]
		protected virtual void ARTran_PlanID_CacheAttached(PXCache sender) { }

		#endregion
		#region ARInvoice
		[PXDBString(3, IsKey = true, IsFixed = true)]
		[PXDefault()]
		[ARDocType.SOEntryList()]
		[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
		protected virtual void ARInvoice_DocType_CacheAttached(PXCache sender)
		{
		}
		[PXDBString(15, IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXDefault()]
		[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible, TabOrder = 1)]
		[ARInvoiceType.RefNbr(typeof(Search2<AR.Standalone.ARRegisterAlias.refNbr,
			InnerJoinSingleTable<ARInvoice, On<ARInvoice.docType, Equal<AR.Standalone.ARRegisterAlias.docType>,
				And<ARInvoice.refNbr, Equal<AR.Standalone.ARRegisterAlias.refNbr>>>,
			InnerJoinSingleTable<Customer, On<AR.Standalone.ARRegisterAlias.customerID, Equal<Customer.bAccountID>>>>,
			Where<AR.Standalone.ARRegisterAlias.docType, Equal<Optional<ARInvoice.docType>>,
				And<AR.Standalone.ARRegisterAlias.origModule, Equal<BatchModule.moduleSO>,
				And<Match<Customer, Current<AccessInfo.userName>>>>>, 
			OrderBy<Desc<AR.Standalone.ARRegisterAlias.refNbr>>>), Filterable = true, IsPrimaryViewCompatible = true)]
		[ARInvoiceType.Numbering()]
		[ARInvoiceNbr()]
		protected virtual void ARInvoice_RefNbr_CacheAttached(PXCache sender)
		{
		}
		[SOOpenPeriod(
			sourceType: typeof(ARRegister.docDate), 
			branchSourceType: typeof(ARRegister.branchID),
			masterFinPeriodIDType: typeof(ARRegister.tranPeriodID),
			IsHeader = true)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void ARInvoice_FinPeriodID_CacheAttached(PXCache sender)
		{
		}
		[PXDBString(10, IsUnicode = true)]
		[PXFormula(typeof(
			IIf<Where<ExternalCall, Equal<True>, Or<PendingValue<ARInvoice.termsID>, IsNull>>,
				IIf<Where<Current<ARInvoice.docType>, NotEqual<ARDocType.creditMemo>>,
					Selector<ARInvoice.customerID, Customer.termsID>,
					Null>,
				ARInvoice.termsID>))]
		[PXUIField(DisplayName = "Terms", Visibility = PXUIVisibility.Visible)]
		[PXSelector(typeof(Search<Terms.termsID, Where<Terms.visibleTo, Equal<TermsVisibleTo.all>, Or<Terms.visibleTo, Equal<TermsVisibleTo.customer>>>>), DescriptionField = typeof(Terms.descr), Filterable = true)]
		[SOInvoiceTerms()]
		protected override void ARInvoice_TermsID_CacheAttached(PXCache sender)
		{
		}
		[PXDBDate()]
		[PXUIField(DisplayName = "Due Date", Visibility = PXUIVisibility.SelectorVisible)]
		protected virtual void ARInvoice_DueDate_CacheAttached(PXCache sender)
		{
		}
		[PXDBDate()]
		[PXUIField(DisplayName = "Cash Discount Date", Visibility = PXUIVisibility.SelectorVisible)]
		protected virtual void ARInvoice_DiscDate_CacheAttached(PXCache sender)
		{
		}
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBCurrency(typeof(ARInvoice.curyInfoID), typeof(ARInvoice.origDocAmt))]
		[PXUIField(DisplayName = "Amount", Visibility = PXUIVisibility.SelectorVisible)]
		protected virtual void ARInvoice_CuryOrigDocAmt_CacheAttached(PXCache sender)
		{
		}
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBCurrency(typeof(ARInvoice.curyInfoID), typeof(ARInvoice.docBal), BaseCalc = false)]
		[PXUIField(DisplayName = "Balance", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		protected virtual void ARInvoice_CuryDocBal_CacheAttached(PXCache sender)
		{
		}
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBCurrency(typeof(ARInvoice.curyInfoID), typeof(ARInvoice.origDiscAmt))]
		[PXUIField(DisplayName = "Cash Discount", Visibility = PXUIVisibility.SelectorVisible)]
		protected virtual void ARInvoice_CuryOrigDiscAmt_CacheAttached(PXCache sender)
		{
		}
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Line Total")]
		protected virtual void ARInvoice_CuryGoodsTotal_CacheAttached(PXCache sender)
		{
		}
		#endregion
		#region SOAdjust
		[PXDBInt()]
		[PXDefault()]
		protected virtual void SOAdjust_CustomerID_CacheAttached(PXCache sender)
		{
		}
		[PXDBString(2, IsKey = true, IsFixed = true)]
		[PXDefault()]
		protected virtual void SOAdjust_AdjdOrderType_CacheAttached(PXCache sender)
		{
		}
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		[PXDefault()]
		protected virtual void SOAdjust_AdjdOrderNbr_CacheAttached(PXCache sender)
		{
		}
		[PXDBString(3, IsKey = true, IsFixed = true, InputMask = "")]
		[PXDefault()]
		protected virtual void SOAdjust_AdjgDocType_CacheAttached(PXCache sender)
		{
		}
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		protected virtual void SOAdjust_AdjgRefNbr_CacheAttached(PXCache sender)
		{
		}
		[PXDBCurrency(typeof(SOAdjust.adjdCuryInfoID), typeof(SOAdjust.adjAmt))]
		[PXFormula(typeof(Sub<SOAdjust.curyOrigAdjdAmt, SOAdjust.curyAdjdBilledAmt>))]
		[PXUIField(DisplayName = "Applied To Order")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		protected virtual void SOAdjust_CuryAdjdAmt_CacheAttached(PXCache sender)
		{
		}
		[PXDBDecimal(4)]
		[PXFormula(typeof(Sub<SOAdjust.origAdjAmt, SOAdjust.adjBilledAmt>))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		protected virtual void SOAdjust_AdjAmt_CacheAttached(PXCache sender)
		{
		}
		[PXDBDecimal(4)]
		[PXFormula(typeof(Sub<SOAdjust.curyOrigAdjgAmt, SOAdjust.curyAdjgBilledAmt>))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		protected virtual void SOAdjust_CuryAdjgAmt_CacheAttached(PXCache sender)
		{
		}
		[PXDBLong()]
		[PXDefault()]
		[CurrencyInfo(ModuleCode = BatchModule.SO, CuryIDField = "AdjdOrigCuryID")]
		protected virtual void SOAdjust_AdjdOrigCuryInfoID_CacheAttached(PXCache sender)
		{
		}
		[PXDBLong()]
		[PXDefault()]
		[CurrencyInfo(ModuleCode = BatchModule.SO, CuryIDField = "AdjgCuryID")]
		protected virtual void SOAdjust_AdjgCuryInfoID_CacheAttached(PXCache sender)
		{
		}
		[PXDBLong()]
		[PXDefault()]
		[CurrencyInfo(ModuleCode = BatchModule.SO, CuryIDField = "AdjdCuryID")]
		protected virtual void SOAdjust_AdjdCuryInfoID_CacheAttached(PXCache sender)
		{
		}
		#endregion
		#region ARInvoiceDiscountDetail
		[PXDBString(10, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXDefault()]
		[PXUIEnabled(typeof(Where<ARInvoiceDiscountDetail.type, NotEqual<DiscountType.ExternalDocumentDiscount>, And<ARInvoiceDiscountDetail.orderNbr, IsNull>>))]
		[PXUIField(DisplayName = "Discount Code", Required = false)]
		[PXSelector(typeof(Search<ARDiscount.discountID, Where<ARDiscount.type, NotEqual<DiscountType.LineDiscount>>>))]
		protected virtual void ARInvoiceDiscountDetail_DiscountID_CacheAttached(PXCache sender)
		{
		}
		#endregion
		#endregion

		public PXSelectReadonly<CCProcTran, Where<CCProcTran.tranNbr, Equal<Current<SOInvoice.cCAuthTranNbr>>>> ccLastTran;

		public PXSelect<CCProcTran, 
			Where<CCProcTran.refNbr, Equal<Current<SOInvoice.refNbr>>,
				And<CCProcTran.docType, Equal<Current<SOInvoice.docType>>>>,
			OrderBy<Desc<CCProcTran.tranNbr>>> ccProcTran;

		public virtual IEnumerable ccproctran()
		{
			Dictionary<int, CCProcTran> existsingTran = new Dictionary<int, CCProcTran>();
			foreach (CCProcTran iTran in PXSelectReadonly<CCProcTran,
				Where<CCProcTran.refNbr, Equal<Current<ARInvoice.refNbr>>,
					And<CCProcTran.docType, Equal<Current<ARInvoice.docType>>>>,
				OrderBy<Desc<CCProcTran.tranNbr>>>.SelectMultiBound(this, PXView.Currents))
			{
				if (existsingTran.ContainsKey(iTran.TranNbr.Value)) continue;
				existsingTran[iTran.TranNbr.Value] = iTran;
				yield return iTran;
			}

			foreach (CCProcTran iTran1 in PXSelectReadonly2<CCProcTran,
					InnerJoin<SOOrderShipment, On<SOOrderShipment.orderNbr, Equal<CCProcTran.origRefNbr>,
						And<SOOrderShipment.orderType, Equal<CCProcTran.origDocType>>>>,
					Where<SOOrderShipment.invoiceNbr, Equal<Current<ARInvoice.refNbr>>,
						And<SOOrderShipment.invoiceType, Equal<Current<ARInvoice.docType>>,
						And<CCProcTran.refNbr, IsNull>>>,
					OrderBy<Desc<CCProcTran.tranNbr>>>.SelectMultiBound(this, PXView.Currents))
			{
				if (existsingTran.ContainsKey(iTran1.TranNbr.Value)) continue;
				existsingTran[iTran1.TranNbr.Value] = iTran1;
				yield return iTran1;
			}
		}

		public PXSetup<SOSetup> sosetup;
        public PXSetup<ARSetup> arsetup;
		public PXSetup<Company> Company;

		public PXSelect<SOLine2> soline;
		public PXSelect<SOMiscLine2> somiscline;
		public PXSelect<SOTax> sotax;
		public PXSelect<SOTaxTran> sotaxtran;
		public PXSelect<SOOrder> soorder;
		public LSARTran lsselect;

		public PXAction<ARInvoice> hold;
		[PXUIField(DisplayName = "Hold")]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntryF)]
		protected virtual IEnumerable Hold(PXAdapter adapter)
		{
			return adapter.Get();
		}

		public PXAction<ARInvoice> creditHold;
		[PXUIField(DisplayName = "Credit Hold")]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntryF)]
		protected virtual IEnumerable CreditHold(PXAdapter adapter)
		{
			return adapter.Get();
		}

		public PXAction<ARInvoice> flow;
		[PXUIField(DisplayName = "Flow")]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.DataEntryF)]
		protected virtual IEnumerable Flow(PXAdapter adapter)
		{
			Save.Press();					
			return adapter.Get();
		}

		[PXUIField(DisplayName = "Release", Visible = false)]
		[PXButton()]
		public override IEnumerable Release(PXAdapter adapter)
		{
			List<ARRegister> list = new List<ARRegister>();
			foreach (ARInvoice ardoc in adapter.Get<ARInvoice>())
			{
				OnBeforeRelease(ardoc);
				Document.Cache.MarkUpdated(ardoc);
				list.Add(ardoc);
			}

			Save.Press();

			//ARInvoice should always come last in Persisted dictionary since it does have PostInvoice OnSuccess
			PXAutomation.RemovePersisted(this, typeof(ARInvoice), new List<object>(list));

			PXLongOperation.StartOperation(this, delegate ()
			{
				PXTimeStampScope.SetRecordComesFirst(typeof(ARInvoice), true);				

				SOInvoiceEntry ie = PXGraph.CreateInstance<SOInvoiceEntry>();
				SOOrderShipmentProcess docgraph = PXGraph.CreateInstance<SOOrderShipmentProcess>();
				var ingraph = new Lazy<INIssueEntry>(() =>
				{
					var g = PXGraph.CreateInstance<INIssueEntry>();
					g.FieldVerifying.AddHandler<INTran.inventoryID>((PXCache sender, PXFieldVerifyingEventArgs e) => { e.Cancel = true; });
					g.FieldVerifying.AddHandler<INTran.projectID>((PXCache sender, PXFieldVerifyingEventArgs e) => { e.Cancel = true; });
					g.FieldVerifying.AddHandler<INTran.taskID>((PXCache sender, PXFieldVerifyingEventArgs e) => { e.Cancel = true; });
					g.RowPersisting.AddHandler<INRegister>((PXCache sender, PXRowPersistingEventArgs e) =>
					{
						if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Delete)
						{
							return;
						}

						INRegister document = (INRegister) e.Row;

						PXResultset<INTran> trans = 
							PXSelectJoin<INTran, 
							InnerJoin<INSite,
								On<INTran.FK.Site>>,
							Where<INTran.docType, Equal<Required<INRegister.docType>>, 
								And<INTran.refNbr, Equal<Required<INRegister.refNbr>>>>>
							.Select(g, document.DocType, document.RefNbr);

						FinPeriodUtils.ValidateFinPeriod(
							trans.Cast<PXResult<INTran, INSite>>(),
							row => ((INTran) row).FinPeriodID,
							row => new[]
							{
								((INTran) row).BranchID,
								((INSite) row).BranchID,
							},
							typeof(OrganizationFinPeriod.iNClosed));
					});
					return g;
				});

				HashSet<object> processed = new HashSet<object>();
				try
				{
					var ccPTran = CCProcTranHelper.FindCCLastSuccessfulTran(ccProcTran);
					var lastTranType = ccPTran?.TranType;
					var soDocType = ccPTran?.OrigDocType;
					var soRefNbr = ccPTran?.OrigRefNbr;
				
					ARDocumentRelease.ReleaseDoc(list, adapter.MassProcess, null, null, delegate (ARRegister ardoc)
					{
						PXAutomation.RemovePersisted(ie, typeof(ARInvoice), new List<object> { ardoc });

						docgraph.Clear();
						var orderShipments = docgraph.Items.View.SelectMultiBound(new object[] { ardoc })
							.Cast<PXResult<SOOrderShipment, SOOrder>>()
							.ToList();
						foreach (var ordershipment in orderShipments)
						{
							SOOrderShipment copy = PXCache<SOOrderShipment>.CreateCopy(ordershipment);
							SOOrder order = ordershipment;
							SOOrderType otype = SOOrderType.PK.Find(this, order.OrderType);
							copy.InvoiceReleased = true;

							docgraph.Items.Update(copy);

							if ((order.Completed == true || otype.RequireShipping == false) && order.BilledCntr <= 1 && order.ShipmentCntr <= order.BilledCntr + order.ReleasedCntr)
							{
								foreach (SOAdjust adj in docgraph.Adjustments.Select(order.OrderType, order.OrderNbr))
								{
									SOAdjust adjcopy = PXCache<SOAdjust>.CreateCopy(adj);
									adjcopy.CuryAdjdAmt = 0m;
									adjcopy.CuryAdjgAmt = 0m;
									adjcopy.AdjAmt = 0m;
									docgraph.Adjustments.Update(adjcopy);
								}
							}
							processed.Add(ardoc);
						}

						if (lastTranType == CCTranTypeCode.PriorAuthorizedCapture)
						{
							SOOrder soOrder = PXSelect<SOOrder, Where<SOOrder.orderType, Equal<Required<CCProcTran.origDocType>>,
								And<SOOrder.orderNbr, Equal<Required<CCProcTran.origRefNbr>>>>>.Select(this, soDocType, soRefNbr);

							if (soOrder?.IsCCCaptured == true && soOrder?.CuryCCCapturedAmt != Decimal.Zero)
							{
								soOrder.IsCCCaptured = false;
								soOrder.IsCCAuthorized = false;
								soOrder.CuryCCCapturedAmt = 0m;
								docgraph.Orders.Update(soOrder);
							}
						}

						PXAutomation.CompleteSimple(docgraph.Orders.View);
						PXAutomation.CompleteSimple(docgraph.Shipments.View);

						docgraph.Save.Press();

						if (ie.SODocument.SelectSingle(ardoc.DocType, ardoc.RefNbr)?.CreateINDoc == true)
						{
							var inlist = new DocumentList<INRegister>(ingraph.Value);
							ie.PostInvoice(ingraph.Value, ardoc as ARInvoice, inlist);
							if (ie.sosetup.Current.AutoReleaseIN != true || (inlist.Count > 0 && inlist[0].Hold == true))
							{
								PXAutomation.RemovePersisted(ingraph.Value, typeof(INRegister), new List<object>(inlist));
							}
						}

						docgraph.CompleteSOLinesAndSplits(ardoc, orderShipments);
					});
				}
				finally
				{
					PXAutomation.StorePersisted(ie, typeof(ARInvoice), new List<object>(processed));
				}
			});
			return list;
		}

		public PXAction<ARInvoice> post;
		[PXUIField(DisplayName = "Post", Visible = false)]
		[PXButton()]
		[Obsolete("The action is obsolete as Posting to IN became a part of the Release action.")]
		protected virtual IEnumerable Post(PXAdapter adapter)
		{
			if (!PXAccess.FeatureInstalled<FeaturesSet.inventory>())
				return adapter.Get();

			List<ARRegister> list = new List<ARRegister>();
			foreach (ARInvoice order in adapter.Get<ARInvoice>())
			{
				list.Add(order);
			}

			Save.Press();

			PXLongOperation.StartOperation(this, delegate ()
			{
				SOInvoiceEntry ie = PXGraph.CreateInstance<SOInvoiceEntry>();
				INIssueEntry ingraph = PXGraph.CreateInstance<INIssueEntry>();
				ingraph.FieldVerifying.AddHandler<INTran.inventoryID>((PXCache sender, PXFieldVerifyingEventArgs e) => { e.Cancel = true; });
				ingraph.FieldVerifying.AddHandler<INTran.projectID>((PXCache sender, PXFieldVerifyingEventArgs e) => { e.Cancel = true; });
				ingraph.FieldVerifying.AddHandler<INTran.taskID>((PXCache sender, PXFieldVerifyingEventArgs e) => { e.Cancel = true; });
				DocumentList<INRegister> inlist = new DocumentList<INRegister>(ingraph);

				bool failed = false;

				foreach (ARInvoice ardoc in list)
				{
					try
					{
						ie.PostInvoice(ingraph, ardoc, inlist);

						if (adapter.MassProcess)
						{
							PXProcessing<ARInvoice>.SetInfo(list.IndexOf(ardoc), ActionsMessages.RecordProcessed);
						}
					}
					catch (Exception ex)
					{
						if (!adapter.MassProcess)
						{
							throw;
						}
						PXProcessing<ARInvoice>.SetError(list.IndexOf(ardoc), ex);
						failed = true;
					} 
				}

				if (ie.sosetup.Current.AutoReleaseIN == true && inlist.Count > 0 && inlist[0].Hold == false)
				{
					INDocumentRelease.ReleaseDoc(inlist, false);
				}

				if (failed)
				{
					throw new PXOperationCompletedWithErrorException(ErrorMessages.SeveralItemsFailed);
				}
			});

			return adapter.Get();
		}

		//throw new PXReportRequiredException(parameters, "SO642000", "Shipment Confirmation");
		[PXUIField(DisplayName = "Reports", MapEnableRights = PXCacheRights.Select)]
		[PXButton(SpecialType = PXSpecialButtonType.ReportsFolder)]
		protected override IEnumerable Report(PXAdapter adapter,
			[PXString(8, InputMask = "CC.CC.CC.CC")]
			string reportID
			)
		{
            List<ARInvoice> list = adapter.Get<ARInvoice>().ToList();
			if (!String.IsNullOrEmpty(reportID))
			{
				Save.Press();
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				string actualReportID = null;
				PXReportRequiredException ex = null;
				Dictionary<string, PXReportRequiredException> reportsToPrint = new Dictionary<string, PXReportRequiredException>();

				foreach (ARInvoice doc in list)
				{
					parameters = new Dictionary<string, string>();
					parameters["ARInvoice.DocType"] = doc.DocType;
					parameters["ARInvoice.RefNbr"] = doc.RefNbr;

					object cstmr = PXSelectorAttribute.Select<ARInvoice.customerID>(Document.Cache, doc);
					actualReportID = new NotificationUtility(this).SearchReport(ARNotificationSource.Customer, cstmr, reportID, doc.BranchID);
					ex = PXReportRequiredException.CombineReport(ex, actualReportID, parameters);

					reportsToPrint = PX.SM.SMPrintJobMaint.AssignPrintJobToPrinter(reportsToPrint, parameters, adapter, new NotificationUtility(this).SearchPrinter, ARNotificationSource.Customer, reportID, actualReportID, doc.BranchID);
				}

				if (ex != null)
				{
					PX.SM.SMPrintJobMaint.CreatePrintJobGroups(reportsToPrint);

					throw ex;
				}
			}
			return list;
		}

		public SOInvoiceEntry()
			: base()
		{
			{
				SOSetup record = sosetup.Current;
			}

			ARSetupNoMigrationMode.EnsureMigrationModeDisabled(this);

			Document.View = new PXView(this, false, new Select2<ARInvoice,
			LeftJoinSingleTable<Customer, On<ARInvoice.customerID, Equal<Customer.bAccountID>>>,
			Where<ARInvoice.docType, Equal<Optional<ARInvoice.docType>>,
			And<ARInvoice.origModule, Equal<BatchModule.moduleSO>,
			And<Where<Customer.bAccountID, IsNull,
			Or<Match<Customer, Current<AccessInfo.userName>>>>>>>>());

			this.Views["Document"] = Document.View;

			PXUIFieldAttribute.SetVisible<SOOrderShipment.orderType>(shipmentlist.Cache, null, true);
			PXUIFieldAttribute.SetVisible<SOOrderShipment.orderNbr>(shipmentlist.Cache, null, true);
			PXUIFieldAttribute.SetVisible<SOOrderShipment.shipmentNbr>(shipmentlist.Cache, null, true);

			PXDBLiteDefaultAttribute.SetDefaultForInsert<SOOrderShipment.invoiceNbr>(shipmentlist.Cache, null, true);
			PXDBLiteDefaultAttribute.SetDefaultForUpdate<SOOrderShipment.invoiceNbr>(shipmentlist.Cache, null, true);

			PXUIFieldAttribute.SetEnabled<ARAdjust2.curyAdjgDiscAmt>(Adjustments.Cache, null, false);

			reverseInvoiceAndApplyToMemo.SetVisible(false);  //A dirty workaround that hides inherited "Reverse and Aplly To Memo" button. Caused by a platform retrieving actions from base graph.

			TaxAttribute.SetTaxCalc<ARTran.taxCategoryID>(Transactions.Cache, null, TaxCalc.ManualLineCalc);
			this.ccLastTran.Cache.AllowInsert = false;
			this.ccLastTran.Cache.AllowUpdate = false;
			this.ccLastTran.Cache.AllowDelete = false;
			
			Transactions.CustomComparer = Comparer<PXResult>.Create((a, b) => {
				ARTran aTran = PXResult.Unwrap<ARTran>(a);
				ARTran bTran = PXResult.Unwrap<ARTran>(b);

				return string.Compare( string.Format("{0}.{1}.{2:D7}.{3}", aTran.SOOrderType, aTran.SOOrderNbr, aTran.SOOrderSortOrder, aTran.SOShipmentNbr),
					string.Format("{0}.{1}.{2:D7}.{3}", bTran.SOOrderType, bTran.SOOrderNbr, bTran.SOOrderSortOrder, bTran.SOShipmentNbr));
			});

			// the obsolete 'Post Invoice to IN' action should be hidden in SO Invoice Entry but available in Process Invoices and Memos
			PXButtonState bstate = Actions["action"]?.GetState(null) as PXButtonState;
			ButtonMenu postMenu = bstate?.Menus?.FirstOrDefault(m => m.Command == "Post Invoice to IN");
			if (postMenu != null)
			{
				postMenu.Visible = false;
			}
		}

		public override void Persist()
		{
			CopyFreightNotesAndFilesToARTran();

            if (this.Caches[typeof(SOOrderShipment)] != null)
			{
                foreach (SOOrderShipment sos in this.Caches[typeof(SOOrderShipment)].Cached)
				{
                    if (sos.HasDetailDeleted == true)
                    {
					throw new PXException(Messages.PartialInvoice);
				}
			}
            }

			PXCache solinecache = Caches[typeof(SOLine2)];
			foreach (SOLine2 soline in solinecache.Updated)
			{
				PXTimeStampScope.DuplicatePersisted(solinecache, soline, typeof(SOLine));
			}

			foreach (ARInvoice invoice in Document.Cache.Deleted)
			{
				foreach (SOInvoice ext in SODocument.Cache.Deleted)
				{
					if (string.Equals(ext.DocType, invoice.DocType) && string.Equals(ext.RefNbr, invoice.RefNbr) && 
						(invoice.IsCCPayment == true || ext.IsCCPayment == true) && ccProcTran.View.SelectMultiBound(new object[] { invoice, ext }).Count > 0)
					{
						ARPaymentEntry docgraph = PXGraph.CreateInstance<ARPaymentEntry>();
						docgraph.AutoPaymentApp = true;
						docgraph.arsetup.Current.HoldEntry = false;
						docgraph.arsetup.Current.RequireControlTotal = false;

						ARPayment payment = new ARPayment()
						{
							DocType = ARDocType.Payment,
							AdjDate = ext.AdjDate,
							AdjFinPeriodID = ext.AdjFinPeriodID
						};

						payment = PXCache<ARPayment>.CreateCopy(docgraph.Document.Insert(payment));
						payment.CustomerID = invoice.CustomerID;
						payment.CustomerLocationID = invoice.CustomerLocationID;
						payment.ARAccountID = invoice.ARAccountID;
						payment.ARSubID = invoice.ARSubID;

                        payment.PaymentMethodID = ext.PaymentMethodID;
                        payment.PMInstanceID = ext.PMInstanceID;
						payment.CashAccountID = ext.CashAccountID;
						payment.ExtRefNbr = ext.ExtRefNbr;
						payment.CuryOrigDocAmt = ext.CuryPaymentAmt;

						docgraph.Document.Update(payment);

						using (PXTransactionScope ts = new PXTransactionScope())
						{
							docgraph.Save.Press();

							ARReleaseProcess.SetPaymentReferenceOnCCTran(docgraph, ext, docgraph.Document.Current);
							ccProcTran.View.Clear();
							docgraph.Document.Cache.RaiseRowSelected(docgraph.Document.Current);

							PXFieldState voidState;
							if ((voidState = (PXFieldState)docgraph.voidCheck.GetState(Document.Current)) == null || voidState.Enabled == false)
							{
								throw new PXException(AR.Messages.ERR_CCTransactionMustBeVoided);
							}

							List<object> tovoid = new List<object>();
							tovoid.Add(docgraph.Document.Current);

							foreach (object item in docgraph.voidCheck.Press(new PXAdapter(new PXView.Dummy(docgraph, docgraph.Document.View.BqlSelect, tovoid)))) {; }

							base.Persist();

							ts.Complete();
						}

						return;
					}
				}
			}

			foreach (ARAdjust2 adj in Adjustments.Cache.Inserted)
			{
				if (adj.CuryAdjdAmt == 0m)
				{
					Adjustments.Cache.SetStatus(adj, PXEntryStatus.InsertedDeleted);
				}
			}

			foreach (ARAdjust2 adj in Adjustments.Cache.Updated
				.RowCast<ARAdjust2>()
				.Where(adj => adj.CuryAdjdAmt == 0m))
				{
					Adjustments.Cache.SetStatus(adj, PXEntryStatus.Deleted);
				}

			foreach (ARInvoice ardoc in Document.Cache.Cached
				.Cast<ARInvoice>()
				.Where(ardoc => (Document.Cache.GetStatus(ardoc) == PXEntryStatus.Inserted
						|| Document.Cache.GetStatus(ardoc) == PXEntryStatus.Updated)
					&& ardoc.DocType == ARDocType.Invoice
					&& ardoc.Released == false
					&& ardoc.ApplyPaymentWhenTaxAvailable != true)
)
				{
					SOInvoice ext = SODocument.Select(ardoc.DocType, ardoc.RefNbr);

                if (ardoc.CuryDocBal - ardoc.CuryBalanceWOTotal - ardoc.CuryOrigDiscAmt - ardoc.CuryPaymentTotal - ext.CuryCCCapturedAmt < 0m)
					{
					foreach (ARAdjust2 adj in Adjustments_Inv.View
						.SelectMultiBound(new object[] { ardoc })
						.RowCast<ARAdjust2>().Where(adj => Adjustments.Cache.GetStatus(adj) == PXEntryStatus.Updated 
							|| Adjustments.Cache.GetStatus(adj) == PXEntryStatus.Inserted 
							|| ((decimal?)Document.Cache.GetValueOriginal<ARInvoice.curyDocBal>(ardoc) != ardoc.CuryDocBal)))
						{
						Adjustments.Cache.MarkUpdated(adj);
							Adjustments.Cache.RaiseExceptionHandling<ARAdjust2.curyAdjdAmt>(adj, adj.CuryAdjdAmt, new PXSetPropertyException(AR.Messages.Application_Amount_Cannot_Exceed_Document_Amount));
							throw new PXException(AR.Messages.Application_Amount_Cannot_Exceed_Document_Amount);
						}
					}
				}

			base.Persist();
		}

		public override void RecalcUnbilledTax()
		{
			Dictionary<string, KeyValuePair<string, string>> orders =
																new Dictionary<string, KeyValuePair<string, string>>();

			foreach (ARTran line in Transactions.Select())
			{
				string key = string.Format("{0}.{1}", line.SOOrderType, line.SOOrderNbr);
				if (!orders.ContainsKey(key))
				{
					orders.Add(key, new KeyValuePair<string, string>(line.SOOrderType, line.SOOrderNbr));
				}

			}

			SOOrderEntry soOrderEntry = PXGraph.CreateInstance<SOOrderEntry>();
			soOrderEntry.RowSelecting.RemoveHandler<SOOrder>(soOrderEntry.SOOrder_RowSelecting);
			foreach (KeyValuePair<string, string> kv in orders.Values)
			{
				soOrderEntry.Clear(PXClearOption.ClearAll);
				soOrderEntry.Document.Current = soOrderEntry.Document.Search<SOOrder.orderNbr>(kv.Value, kv.Key);
				if(IsExternalTax(soOrderEntry.Document.Current.TaxZoneID))
					soOrderEntry.CalculateExternalTax(soOrderEntry.Document.Current);
				soOrderEntry.Persist();
			}
		}

		protected virtual void SOLine2_BaseShippedQty_CommandPreparing(PXCache sender, PXCommandPreparingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
			    e.ExcludeFromInsertUpdate();
			}
		}

		protected virtual void SOLine2_ShippedQty_CommandPreparing(PXCache sender, PXCommandPreparingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
			    e.ExcludeFromInsertUpdate();
			}
        }

		protected override void ARInvoice_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			var doc = (ARInvoice)e.Row;
			if (e.Operation != PXDBOperation.Delete && (doc.DocType == ARDocType.CashSale || doc.DocType == ARDocType.CashReturn))
			{
				ValidateTaxConfiguration(sender, doc);
			}
			
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert)
			{
				SOOrderShipment orderShipment = PXSelect<SOOrderShipment,
					Where<SOOrderShipment.invoiceType, Equal<Required<SOOrderShipment.invoiceType>>, 
						And<SOOrderShipment.invoiceNbr, Equal<Required<SOOrderShipment.invoiceNbr>>>>>
						.SelectSingleBound(this, null, doc.DocType, doc.RefNbr);

				if (orderShipment != null)
				{
					SOOrderType orderType = SOOrderType.PK.Find(this, orderShipment.OrderType);
					if (orderType != null)
					{
						if (string.IsNullOrEmpty(((ARInvoice)e.Row).RefNbr) && orderType.UserInvoiceNumbering == true)
						{
							throw new PXException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<SOOrder.invoiceNbr>(soorder.Cache));
						}

						if (orderType.MarkInvoicePrinted == true)
						{
							((ARInvoice)e.Row).Printed = true;
						}

						if (orderType.MarkInvoiceEmailed == true)
						{
							((ARInvoice)e.Row).Emailed = true;
						}

						AutoNumberAttribute.SetNumberingId<ARInvoice.refNbr>(Document.Cache, orderType.ARDocType, orderType.InvoiceNumberingID);
					}
				}
			}

			if (e.Operation == PXDBOperation.Insert || e.Operation == PXDBOperation.Update)
			{
				if ((doc.CuryDiscTot ?? 0m) > (Math.Abs((doc.CuryGoodsTotal ?? 0m) + (doc.CuryMiscTot ?? 0m))))
				{
					if (sender.RaiseExceptionHandling<ARInvoice.curyDiscTot>(e.Row, doc.CuryDiscTot,
						new PXSetPropertyException(AR.Messages.DiscountGreaterLineMiscTotal, PXErrorLevel.Error)))
					{
						throw new PXRowPersistingException(typeof(ARInvoice.curyDiscTot).Name, null,
							AR.Messages.DiscountGreaterLineMiscTotal);
					}
				}
			}



			base.ARInvoice_RowPersisting(sender, e);
		}

		protected virtual void ARInvoice_OrigModule_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			e.NewValue = GL.BatchModule.SO;
			e.Cancel = true;
		}

		protected override void ARInvoice_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			base.ARInvoice_RowInserted(sender, e);

			SODocument.Cache.Insert();
			SODocument.Cache.IsDirty = false;

			SODocument.Current.AdjDate = ((ARInvoice)e.Row).DocDate;
			SODocument.Current.AdjFinPeriodID = ((ARInvoice)e.Row).FinPeriodID;
			SODocument.Current.AdjTranPeriodID = ((ARInvoice)e.Row).TranPeriodID;
			SODocument.Current.NoteID = ((ARInvoice)e.Row).NoteID;

		}

        protected override void ARTran_CuryUnitPrice_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
        {
			if (!cancelUnitPriceCalculation)
                base.ARTran_CuryUnitPrice_FieldVerifying(sender, e);
        }

		protected override void ARTran_CuryUnitPrice_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
		{
            ARTran row = (ARTran)e.Row;

            if (row?.InventoryID != null && row.UOM != null && row.IsFree != true && row.ManualPrice != true && !cancelUnitPriceCalculation)
            {
                string customerPriceClass = ARPriceClass.EmptyPriceClass;
                Location c = location.Select();

                if (!string.IsNullOrEmpty(c?.CPriceClassID))
                    customerPriceClass = c.CPriceClassID;

                DateTime date = Document.Current.DocDate.Value;

                if (row.TranType == ARDocType.CreditMemo && row.OrigInvoiceDate != null)
                    date = row.OrigInvoiceDate.Value;

                decimal? price = ARSalesPriceMaint.CalculateSalesPrice(
                    cache, customerPriceClass, row.CustomerID, row.InventoryID, row.SiteID,
                    currencyinfo.Select(), row.UOM, row.Qty, date, row.CuryUnitPrice);

                e.NewValue = price;
                ARSalesPriceMaint.CheckNewUnitPrice<ARTran, ARTran.curyUnitPrice>(cache, row, price);
            }
            else
            {
                decimal? curyUnitPrice = row.CuryUnitPrice;
                e.NewValue = curyUnitPrice ?? 0m;
                e.Cancel = curyUnitPrice != null;
            }
		}

		protected override void ARInvoice_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			ARSetup.Current.RequireControlTotal = (((ARInvoice)e.Row).DocType == ARDocType.CashSale || ((ARInvoice)e.Row).DocType == ARDocType.CashReturn) ? true : ARSetup.Current.RequireControlTotal;

			base.ARInvoice_RowUpdated(sender, e);

			ARInvoice doc = e.Row as ARInvoice;

            if (doc != null && doc.RefNbr == null)
				return;

			if ((doc.DocType == ARDocType.CashSale || doc.DocType == ARDocType.CashReturn) && doc.Released != true)
			{
				if (sender.ObjectsEqual<ARInvoice.curyDocBal, ARInvoice.curyOrigDiscAmt>(e.Row, e.OldRow) == false && doc.CuryDocBal - doc.CuryOrigDiscAmt != doc.CuryOrigDocAmt)
				{
					if (doc.CuryDocBal != null && doc.CuryOrigDiscAmt != null && doc.CuryDocBal != 0)
						sender.SetValueExt<ARInvoice.curyOrigDocAmt>(doc, doc.CuryDocBal - doc.CuryOrigDiscAmt);
					else
						sender.SetValueExt<ARInvoice.curyOrigDocAmt>(doc, 0m);
				}
				else if (sender.ObjectsEqual<ARInvoice.curyOrigDocAmt>(e.Row, e.OldRow) == false)
				{
					if (doc.CuryDocBal != null && doc.CuryOrigDocAmt != null && doc.CuryDocBal != 0)
						sender.SetValueExt<ARInvoice.curyOrigDiscAmt>(doc, doc.CuryDocBal - doc.CuryOrigDocAmt);
					else
						sender.SetValueExt<ARInvoice.curyOrigDiscAmt>(doc, 0m);
				}
			}

			if (doc != null && doc.CuryDocBal != null && doc.Hold != true && doc.CuryDocBal < 0m && SODocument.Current != null && Document.Current.CuryPremiumFreightAmt < 0m && (doc.CuryDocBal - Document.Current.CuryPremiumFreightAmt) >= 0m)
			{
				sender.RaiseExceptionHandling<ARInvoice.curyDocBal>(doc, doc.CuryDocBal,
					new PXSetPropertyException(Messages.DocumentBalanceNegativePremiumFreight));
			}

			if ((doc.DocType == ARDocType.CashSale || doc.DocType == ARDocType.CashReturn) && doc.Released != true && doc.Hold != true)
			{
				if (doc.CuryDocBal < doc.CuryOrigDocAmt)
				{
					sender.RaiseExceptionHandling<ARInvoice.curyOrigDocAmt>(doc, doc.CuryOrigDocAmt, new PXSetPropertyException(AR.Messages.CashSaleOutOfBalance));
				}
				else
				{
					sender.RaiseExceptionHandling<ARInvoice.curyOrigDocAmt>(doc, doc.CuryOrigDocAmt, null);
				}
			}

			if (!sender.ObjectsEqual<ARInvoice.customerID, ARInvoice.docDate, ARInvoice.finPeriodID, ARInvoice.curyTaxTotal, ARInvoice.curyOrigDocAmt, ARInvoice.docDesc, ARInvoice.curyOrigDiscAmt, ARInvoice.hold>(e.Row, e.OldRow))
			{
				SOInvoice invoice = (SOInvoice)SODocument.Select();
				if (IsImport && invoice == null && SODocument.Current != null && sender.Current is ARInvoice)
				{
					if ((((ARInvoice)sender.Current).DocType != SODocument.Current.DocType
						|| ((ARInvoice)sender.Current).RefNbr != SODocument.Current.RefNbr)
						&& SODocument.Cache.GetStatus(SODocument.Current) == PXEntryStatus.Inserted
						&& sender.Locate(new ARInvoice { DocType = SODocument.Current.DocType, RefNbr = SODocument.Current.RefNbr }) == null)
					{
						SODocument.Cache.Delete(SODocument.Current);
					}
				}
				
				SODocument.Current = invoice ?? (SOInvoice)SODocument.Cache.Insert();
				SODocument.Current.CustomerID = ((ARInvoice)e.Row).CustomerID;

                if ((((ARInvoice)e.Row).DocType == ARDocType.CashSale 
                        || ((ARInvoice)e.Row).DocType == ARDocType.CashReturn 
                        || ((ARInvoice)e.Row).DocType == ARDocType.Invoice) && !sender.ObjectsEqual<ARInvoice.customerID>(e.Row, e.OldRow))
				{
                    SODocument.Cache.SetDefaultExt<SOInvoice.paymentMethodID>(SODocument.Current);
					SODocument.Cache.SetDefaultExt<SOInvoice.pMInstanceID>(SODocument.Current);
				}

				SODocument.Current.AdjDate = ((ARInvoice)e.Row).DocDate;
				SODocument.Current.DepositAfter = ((ARInvoice)e.Row).DocDate;
				SODocument.Current.AdjFinPeriodID = ((ARInvoice)e.Row).FinPeriodID;
				SODocument.Current.AdjTranPeriodID = ((ARInvoice)e.Row).TranPeriodID;
				SODocument.Current.CuryPaymentAmt = ((ARInvoice)e.Row).CuryOrigDocAmt - ((ARInvoice)e.Row).CuryOrigDiscAmt - ((ARInvoice)e.Row).CuryPaymentTotal;
				SODocument.Current.DocDesc = ((ARInvoice)e.Row).DocDesc;
				SODocument.Current.PaymentProjectID = PM.ProjectDefaultAttribute.NonProject();
				SODocument.Current.Hold = ((ARInvoice)e.Row).Hold;

                SODocument.Cache.MarkUpdated(SODocument.Current);
			}

            if (!sender.ObjectsEqual<ARInvoice.curyPaymentTotal>(e.OldRow, e.Row))
            {
                SOInvoice invoice = (SOInvoice)SODocument.Select();
                if (invoice != null)
                {
                    SODocument.Current = invoice;
                    SODocument.Current.CuryPaymentAmt = ((ARInvoice)e.Row).CuryOrigDocAmt - ((ARInvoice)e.Row).CuryOrigDiscAmt - ((ARInvoice)e.Row).CuryPaymentTotal;
                }
            }

			if (e.ExternalCall && sender.GetStatus(doc) != PXEntryStatus.Deleted && !sender.ObjectsEqual<SOOrder.curyDiscTot>(e.OldRow, e.Row))
			{
				ARDiscountEngine.SetTotalDocDiscount(Transactions.Cache, Transactions, DiscountDetails,
					Document.Current.CuryDiscTot, DiscountEngine.DiscountCalculationOptions.DisableAPDiscountsCalculation);
				RecalculateTotalDiscount();
			}
		}

		protected override void ARInvoice_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			base.ARInvoice_RowSelected(cache, e);

			PXUIFieldAttribute.SetVisible<ARTran.taskID>(Transactions.Cache, null, PM.ProjectAttribute.IsPMVisible(BatchModule.SO) || PM.ProjectAttribute.IsPMVisible(BatchModule.AR));

			selectShipment.SetEnabled(Document.AllowDelete);

			if (e.Row == null)
				return;

			ARInvoice doc = e.Row as ARInvoice;
			if (((ARInvoice)e.Row).DocType == ARDocType.CashSale || ((ARInvoice)e.Row).DocType == ARDocType.CashReturn)
			{
				PXUIFieldAttribute.SetVisible<ARInvoice.curyOrigDocAmt>(cache, e.Row);
			}

			SODocument.Cache.AllowUpdate = Document.Cache.AllowUpdate;
			FreightDetails.Cache.AllowUpdate = Document.Cache.AllowUpdate;
			PaymentState paymentState = new PaymentState(ccProcTran.Select());
			bool isCCCaptured = paymentState.isCCCaptured;
			bool isCCRefunded = paymentState.isCCRefunded;
			bool isCCPreAuthorized = paymentState.isCCPreAuthorized;
			bool isAuthorizedCashSale = (doc.DocType == ARDocType.CashSale && (isCCPreAuthorized || isCCCaptured));
			bool isRefundedCashReturn = doc.DocType == ARDocType.CashReturn && isCCRefunded;
			Transactions.Cache.AllowDelete = Transactions.Cache.AllowDelete && !isAuthorizedCashSale && !isRefundedCashReturn;
			Transactions.Cache.AllowUpdate = Transactions.Cache.AllowUpdate && !isAuthorizedCashSale && !isRefundedCashReturn;
			Transactions.Cache.AllowInsert = Transactions.Cache.AllowInsert && !isAuthorizedCashSale && !isRefundedCashReturn;
			PXUIFieldAttribute.SetEnabled<ARInvoice.curyOrigDocAmt>(cache, doc, ((ARInvoice)e.Row).Released == false && !isAuthorizedCashSale && !isRefundedCashReturn);
            PXUIFieldAttribute.SetEnabled<ARInvoice.curyOrigDiscAmt>(cache, doc, ((ARInvoice)e.Row).Released == false && doc.DocType != ARDocType.CreditMemo && !isAuthorizedCashSale && !isRefundedCashReturn);

			#region CCProcessing integrated with doc
			bool enableCCProcess = false;
			bool docTypePayment = doc.DocType == ARDocType.Invoice || doc.DocType == ARDocType.CashSale;
			doc.IsCCPayment = false;

			if (doc.PMInstanceID != null)
			{
				PXResult<CustomerPaymentMethodC, CA.PaymentMethod> pmInstance = (PXResult<CustomerPaymentMethodC, CA.PaymentMethod>)
								   PXSelectJoin<CustomerPaymentMethodC,
									InnerJoin<CA.PaymentMethod,
										On<CA.PaymentMethod.paymentMethodID, Equal<CustomerPaymentMethodC.paymentMethodID>>>,
								Where<CustomerPaymentMethodC.pMInstanceID, Equal<Optional<SOInvoice.pMInstanceID>>,
									And<CA.PaymentMethod.paymentType, Equal<CA.PaymentMethodType.creditCard>,
										And<CA.PaymentMethod.aRIsProcessingRequired, Equal<True>>>>>.Select(this, doc.PMInstanceID);
				if (pmInstance != null)
				{
					doc.IsCCPayment = true;
					enableCCProcess = IsDocTypeSuitableForCC(doc.DocType);
				}
			}

			enableCCProcess = enableCCProcess && !doc.Voided.Value;
			bool releaseActionEnabled = !enableCCProcess || arsetup.Current.IntegratedCCProcessing != true || (doc.DocType == ARDocType.CashReturn ? isCCRefunded : isCCCaptured) || paymentState.isNone;
			releaseActionEnabled &= !(arsetup.Current.IntegratedCCProcessing == true && doc.DocType == ARDocType.CashSale && paymentState.isOpenForReview);
			release.SetEnabled(releaseActionEnabled);
			#endregion
			Adjustments.Cache.AllowSelect = 
				doc.DocType != ARDocType.CashSale && 
				doc.DocType != ARDocType.CashReturn;
		}

		protected static bool IsDocTypeSuitableForCC(string docType)
		{
			return (docType == ARDocType.CashReturn || docType == ARDocType.Invoice || docType == ARDocType.CashSale || docType == ARDocType.Refund || docType == ARDocType.VoidPayment);
		}

        public override void SetDocTypeList(PXCache cache, PXRowSelectedEventArgs e)
        {
            //doctype list should not be updated in SOInvoiceEntry
        }

		protected override void ARInvoice_RowSelecting(PXCache sender, PXRowSelectingEventArgs e)
		{
			ARInvoice row = (ARInvoice)e.Row;
			
			if (row != null && e.IsReadOnly == false
				&& String.IsNullOrEmpty(row.DocType) == false && String.IsNullOrEmpty(row.RefNbr) == false)
			{
				row.IsCCPayment = false;
				using (new PXConnectionScope())
				{
					PXResult<CustomerPaymentMethodC, CA.PaymentMethod, SOInvoice> pmInstance = (PXResult<CustomerPaymentMethodC, CA.PaymentMethod, SOInvoice>)
										 PXSelectJoin<CustomerPaymentMethodC,
										InnerJoin<CA.PaymentMethod,
											On<CA.PaymentMethod.paymentMethodID, Equal<CustomerPaymentMethodC.paymentMethodID>>,
																			InnerJoin<SOInvoice, On<SOInvoice.pMInstanceID, Equal<CustomerPaymentMethodC.pMInstanceID>>>>,
									Where<SOInvoice.docType, Equal<Required<SOInvoice.docType>>,
										And<SOInvoice.refNbr, Equal<Required<SOInvoice.refNbr>>,
										And<CA.PaymentMethod.paymentType, Equal<CA.PaymentMethodType.creditCard>,
											And<CA.PaymentMethod.aRIsProcessingRequired, Equal<True>>>>>>.Select(this, row.DocType, row.RefNbr);
					if (pmInstance != null)
					{
						row.IsCCPayment = true;
					}
				}

                if (e.Row != null && e.IsReadOnly == false && (((ARInvoice)e.Row).CuryPaymentTotal == null || ((ARInvoice)e.Row).CuryBalanceWOTotal == null))
                {
                    using (new PXConnectionScope())
                    {
                        bool IsReadOnly = (sender.GetStatus(e.Row) == PXEntryStatus.Notchanged);

                        if (((ARInvoice)e.Row).CuryPaymentTotal == null)
                        {
                            PXFormulaAttribute.CalcAggregate<ARAdjust2.curyAdjdAmt>(Adjustments.Cache, e.Row, IsReadOnly);

                            sender.RaiseFieldUpdated<ARInvoice.curyPaymentTotal>(e.Row, null);
                            PXDBCurrencyAttribute.CalcBaseValues<ARInvoice.curyPaymentTotal>(sender, e.Row);
                        }

                        if (((ARInvoice)e.Row).CuryBalanceWOTotal == null)
                        {
                            PXFormulaAttribute.CalcAggregate<ARAdjust2.curyAdjdWOAmt>(Adjustments.Cache, e.Row, IsReadOnly);

                            sender.RaiseFieldUpdated<ARInvoice.curyBalanceWOTotal>(e.Row, null);
                            PXDBCurrencyAttribute.CalcBaseValues<ARInvoice.curyBalanceWOTotal>(sender, e.Row);
                        }
                    }
                }
            }
		}

		protected override void ARInvoice_CustomerID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			string CreditRule = customer.Current?.CreditRule;
			try
			{
				base.ARInvoice_CustomerID_FieldUpdated(sender, e);
			}
			finally
			{
				if (customer.Current != null)
				{
				customer.Current.CreditRule = CreditRule;
			}
		}
		}

		protected virtual void SOInvoice_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
            if (e.Row == null) return;

            SOInvoice doc = (SOInvoice)e.Row;
			ARInvoice arDoc = this.Document.Current;
			doc.PaymentProjectID = PM.ProjectDefaultAttribute.NonProject();

		
			PXUIFieldAttribute.SetEnabled(this.ccLastTran.Cache, null, false);

			
			PXUIFieldAttribute.SetEnabled<ARInvoice.curyDiscTot>(SODocument.Cache, e.Row, Document.Cache.AllowUpdate);
           
			PXUIFieldAttribute.SetEnabled<SOInvoice.cashAccountID>(SODocument.Cache, e.Row, Document.Cache.AllowUpdate && (((SOInvoice)e.Row).PMInstanceID != null || string.IsNullOrEmpty(doc.PaymentMethodID) == false));
			PXUIFieldAttribute.SetEnabled<SOInvoice.extRefNbr>(SODocument.Cache, e.Row, Document.Cache.AllowUpdate && (((SOInvoice)e.Row).PMInstanceID != null || string.IsNullOrEmpty(doc.PaymentMethodID) == false));
			PXUIFieldAttribute.SetEnabled<SOInvoice.cleared>(SODocument.Cache, e.Row, Document.Cache.AllowUpdate && (((SOInvoice)e.Row).PMInstanceID != null || string.IsNullOrEmpty(doc.PaymentMethodID) == false) && (((SOInvoice)e.Row).DocType == ARDocType.CashSale || ((SOInvoice)e.Row).DocType == ARDocType.CashReturn));
			PXUIFieldAttribute.SetEnabled<SOInvoice.clearDate>(SODocument.Cache, e.Row, Document.Cache.AllowUpdate && (((SOInvoice)e.Row).PMInstanceID != null || string.IsNullOrEmpty(doc.PaymentMethodID) == false) && (((SOInvoice)e.Row).DocType == ARDocType.CashSale || ((SOInvoice)e.Row).DocType == ARDocType.CashReturn));

            if (Document.Current == null)
            {
                DiscountDetails.Cache.AllowDelete = false;
                DiscountDetails.Cache.AllowUpdate = false;
                DiscountDetails.Cache.AllowInsert = false;
            }
            else
            {
                DiscountDetails.Cache.AllowDelete = Transactions.Cache.AllowDelete;
                DiscountDetails.Cache.AllowUpdate = Transactions.Cache.AllowUpdate;
                DiscountDetails.Cache.AllowInsert = Transactions.Cache.AllowInsert;
            }
		}

		protected virtual void SOInvoice_PaymentMethodID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            sender.SetDefaultExt<SOInvoice.pMInstanceID>(e.Row);
            sender.SetDefaultExt<SOInvoice.cashAccountID>(e.Row);
            sender.SetDefaultExt<SOInvoice.isCCCaptureFailed>(e.Row);
        }

		protected virtual void SOInvoice_PMInstanceID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			//sender.SetDefaultExt<SOInvoice.paymentMethodID>(e.Row);
			sender.SetDefaultExt<SOInvoice.cashAccountID>(e.Row);
			sender.SetDefaultExt<SOInvoice.isCCCaptureFailed>(e.Row);
			sender.SetValueExt<SOInvoice.refTranExtNbr>(e.Row, null);
		}

		protected virtual void SOInvoice_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (e.Operation == PXDBOperation.Insert || e.Operation == PXDBOperation.Update)
			{
				SOInvoice doc = (SOInvoice)e.Row;

				if ((doc.DocType == ARDocType.CashSale || doc.DocType == ARDocType.CashReturn))
				{
                    if (String.IsNullOrEmpty(doc.PaymentMethodID) == true)
                    {
                        if (sender.RaiseExceptionHandling<SOInvoice.pMInstanceID>(e.Row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, typeof(SOInvoice.pMInstanceID).Name)))
                        {
                            throw new PXRowPersistingException(typeof(SOInvoice.pMInstanceID).Name, null, ErrorMessages.FieldIsEmpty, typeof(SOInvoice.pMInstanceID).Name);
                        }
                    }
                    else
                    {
                        
                        CA.PaymentMethod pm = PXSelect<CA.PaymentMethod, Where<CA.PaymentMethod.paymentMethodID, Equal<Required<CA.PaymentMethod.paymentMethodID>>>>.Select(this, doc.PaymentMethodID);
                        bool pmInstanceRequired = (pm.IsAccountNumberRequired == true);
                        if (pmInstanceRequired && doc.PMInstanceID == null)
                        {
                            if (sender.RaiseExceptionHandling<SOInvoice.pMInstanceID>(e.Row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, typeof(SOInvoice.pMInstanceID).Name)))
                            {
                                throw new PXRowPersistingException(typeof(SOInvoice.pMInstanceID).Name, null, ErrorMessages.FieldIsEmpty, typeof(SOInvoice.pMInstanceID).Name);
                            }
                        }
                    }
				}

				bool isCashSale = (doc.DocType == AR.ARDocType.CashSale) || (doc.DocType == AR.ARDocType.CashReturn);
                if (isCashSale && SODocument.GetValueExt<SOInvoice.cashAccountID>((SOInvoice)e.Row) == null)
				{
					if (sender.RaiseExceptionHandling<SOInvoice.cashAccountID>(e.Row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, typeof(SOInvoice.cashAccountID).Name)))
					{
						throw new PXRowPersistingException(typeof(SOInvoice.cashAccountID).Name, null, ErrorMessages.FieldIsEmpty, typeof(SOInvoice.cashAccountID).Name);
					}
				}

				object acctcd;

				if ((acctcd = SODocument.GetValueExt<SOInvoice.cashAccountID>((SOInvoice)e.Row)) != null && sender.GetValue<SOInvoice.cashAccountID>(e.Row) == null)
				{
					sender.RaiseExceptionHandling<SOInvoice.cashAccountID>(e.Row, null, null);
					sender.SetValueExt<SOInvoice.cashAccountID>(e.Row, acctcd is PXFieldState ? ((PXFieldState)acctcd).Value : acctcd);
				}

				//if (doc.PMInstanceID != null && string.IsNullOrEmpty(doc.ExtRefNbr))
				//{
				//    if (sender.RaiseExceptionHandling<SOInvoice.extRefNbr>(e.Row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, typeof(SOInvoice.extRefNbr).Name)))
				//    {
				//        throw new PXRowPersistingException(typeof(SOInvoice.extRefNbr).Name, null, ErrorMessages.FieldIsEmpty, typeof(SOInvoice.extRefNbr).Name);
				//    }
				//}
			}
		}
		private void ValidateTaxConfiguration(PXCache cache, ARInvoice cashSale)
		{
			bool reduceOnEarlyPayments = false;
			bool reduceTaxableAmount = false;
			foreach (PXResult<ARTax, Tax> result in PXSelectJoin<ARTax,
				InnerJoin<Tax, On<Tax.taxID, Equal<ARTax.taxID>>>,
				Where<ARTax.tranType, Equal<Current<ARInvoice.docType>>,
				And<ARTax.refNbr, Equal<Current<ARInvoice.refNbr>>>>>.Select(this))
			{
				Tax tax = (Tax)result;
				if (tax.TaxApplyTermsDisc == CSTaxTermsDiscount.ToPromtPayment)
				{
					reduceOnEarlyPayments = true;
				}
				if (tax.TaxApplyTermsDisc == CSTaxTermsDiscount.ToTaxableAmount)
				{
					reduceTaxableAmount = true;
				}
				if (reduceOnEarlyPayments && reduceTaxableAmount)
				{
					cache.RaiseExceptionHandling<ARInvoice.taxZoneID>(cashSale, cashSale.TaxZoneID, new PXSetPropertyException(TX.Messages.InvalidTaxConfiguration));
				}
			}
		}

		protected virtual void SOInvoice_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (!sender.ObjectsEqual<SOInvoice.isCCCaptured>(e.Row, e.OldRow) && ((SOInvoice)e.Row).IsCCCaptured == true)
			{
				ARInvoice copy = (ARInvoice)Document.Cache.CreateCopy(Document.Current);

				copy.CreditHold = false;

				Document.Cache.Update(copy);
			}


            if (!sender.ObjectsEqual<SOInvoice.pMInstanceID, SOInvoice.paymentMethodID, SOInvoice.cashAccountID>(e.Row, e.OldRow))
			{ 
				ARInvoice ardoc = Document.Search<ARInvoice.refNbr>(((SOInvoice)e.Row).RefNbr, ((SOInvoice)e.Row).DocType);
				//is null on delete operation
				if (ardoc != null)
				{
					ardoc.PMInstanceID = ((SOInvoice)e.Row).PMInstanceID;
					ardoc.PaymentMethodID = ((SOInvoice)e.Row).PaymentMethodID;
					ardoc.CashAccountID = ((SOInvoice)e.Row).CashAccountID;
					
					Document.Cache.MarkUpdated(ardoc);
				}
			}
		}

		protected override void ARAdjust2_CuryAdjdAmt_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			ARAdjust2 adj = (ARAdjust2)e.Row;
			Terms terms = PXSelect<Terms, Where<Terms.termsID, Equal<Current<ARInvoice.termsID>>>>.Select(this);

			if (terms != null && terms.InstallmentType != TermsInstallmentType.Single && (decimal)e.NewValue > 0m)
			{
				throw new PXSetPropertyException(AR.Messages.PrepaymentAppliedToMultiplyInstallments);
			}

			if (adj.CuryDocBal == null)
			{
				PXResult<ARPayment, CurrencyInfo> res = (PXResult<ARPayment, CurrencyInfo>)PXSelectReadonly2<ARPayment, InnerJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<ARPayment.curyInfoID>>>, Where<ARPayment.docType, Equal<Required<ARPayment.docType>>, And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>.Select(this, adj.AdjgDocType, adj.AdjgRefNbr);

				ARPayment payment = PXCache<ARPayment>.CreateCopy(res);
				CurrencyInfo pay_info = (CurrencyInfo)res;
				CurrencyInfo inv_info = PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Current<ARInvoice.curyInfoID>>>>.Select(this);

				ARAdjust2 other = PXSelectGroupBy<ARAdjust2, Where<ARAdjust2.adjgDocType, Equal<Required<ARAdjust2.adjgDocType>>, And<ARAdjust2.adjgRefNbr, Equal<Required<ARAdjust2.adjgRefNbr>>, And<ARAdjust2.released, Equal<False>, And<Where<ARAdjust2.adjdDocType, NotEqual<Required<ARAdjust2.adjdDocType>>, Or<ARAdjust2.adjdRefNbr, NotEqual<Required<ARAdjust2.adjdRefNbr>>>>>>>>, Aggregate<GroupBy<ARAdjust2.adjgDocType, GroupBy<ARAdjust2.adjgRefNbr, Sum<ARAdjust2.curyAdjgAmt, Sum<ARAdjust2.adjAmt>>>>>>.Select(this, adj.AdjgDocType, adj.AdjgRefNbr, adj.AdjdDocType, adj.AdjdRefNbr);
				if (other != null && other.AdjdRefNbr != null)
				{
					payment.CuryDocBal -= other.CuryAdjgAmt;
					payment.DocBal -= other.AdjAmt;
				}

				decimal CuryDocBal;
				if (string.Equals(pay_info.CuryID, inv_info.CuryID))
				{
					CuryDocBal = (decimal)payment.CuryDocBal;
				}
				else
				{
					PXDBCurrencyAttribute.CuryConvCury(sender, inv_info, (decimal)payment.DocBal, out CuryDocBal);
				}

				adj.CuryDocBal = CuryDocBal - adj.CuryAdjdAmt;
			}

			if ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjdAmt - (decimal)e.NewValue < 0)
			{
				throw new PXSetPropertyException(AR.Messages.Entry_LE, ((decimal)adj.CuryDocBal + (decimal)adj.CuryAdjdAmt).ToString());
			}
		}

        protected override void ARTran_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
        {
			if (((ARTran)e.Row).SortOrder == null)
				((ARTran)e.Row).SortOrder = ((ARTran)e.Row).LineNbr;

            if (e.ExternalCall || forceDiscountCalculation)
                RecalculateDiscounts(sender, (ARTran)e.Row);
            TaxAttribute.Calculate<ARTran.taxCategoryID>(sender, e);

            if (SODocument.Current != null)
            {
                SODocument.Current.IsTaxValid = false;
                SODocument.Cache.MarkUpdated(SODocument.Current);
            }

            if (Document.Current != null)
            {
                Document.Current.IsTaxValid = false;
                SODocument.Cache.MarkUpdated(SODocument.Current);
            }
        }

		protected override void ARTran_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			base.ARTran_RowDeleted(sender, e);


			var row = (ARTran)e.Row;
			if (row.LineType == SOLineType.Freight)
				return;

			PXResultset<ARTran> siblings = PXSelect<ARTran, Where<ARTran.sOOrderType, Equal<Required<ARTran.sOOrderType>>,
				And<ARTran.sOOrderNbr, Equal<Required<ARTran.sOOrderNbr>>,
				And<ARTran.sOShipmentType, Equal<Required<ARTran.sOShipmentType>>,
				And<ARTran.sOShipmentNbr, Equal<Required<ARTran.sOShipmentNbr>>,
				And<ARTran.tranType, Equal<Required<ARTran.tranType>>,
				And<ARTran.refNbr, Equal<Required<ARTran.refNbr>>>>>>>>>.SelectWindowed(this, 0, 2,
				row.SOOrderType, row.SOOrderNbr, row.SOShipmentType, row.SOShipmentNbr, row.TranType, row.RefNbr);

			if (siblings.Count == 1 && ((ARTran)siblings).LineType == SOLineType.Freight)
			{
				Freight.Delete((ARTran)siblings);
				siblings.Clear();
			}

            SOOrderShipment ordershipment =
             PXSelect<SOOrderShipment, Where<SOOrderShipment.orderType, Equal<Required<SOOrderShipment.orderType>>,
					And<SOOrderShipment.orderNbr, Equal<Required<SOOrderShipment.orderNbr>>,
					And<SOOrderShipment.shipmentType, Equal<Required<SOOrderShipment.shipmentType>>,
					And<SOOrderShipment.shipmentNbr, Equal<Required<SOOrderShipment.shipmentNbr>>>>>>>.SelectWindowed(this, 0, 1,
				row.SOOrderType, row.SOOrderNbr, row.SOShipmentType, row.SOShipmentNbr);

            if (siblings.Count == 0)
			{
				if (ordershipment != null)
				{
                    ordershipment.HasDetailDeleted = false;
					shipmentlist.Delete(ordershipment);
				}

				SOFreightDetail freightDet = FreightDetails.Select().AsEnumerable()
					.RowCast<SOFreightDetail>()
					.Where(d => d.ShipmentType == row.SOShipmentType && d.ShipmentNbr == row.SOShipmentNbr && d.OrderType == row.SOOrderType && d.OrderNbr == row.SOOrderNbr)
					.FirstOrDefault();
				if (freightDet != null)
				{
					FreightDetails.Delete(freightDet);
				}
			}
            else
            {
				if (ordershipment != null)
                {
                    ordershipment.HasDetailDeleted = true;
                    shipmentlist.Update(ordershipment);
                }
			}

			if (SODocument.Current != null)
			{
				SODocument.Current.IsTaxValid = false;
				SODocument.Cache.MarkUpdated(SODocument.Current);
			}

			if (Document.Current != null)
			{
				Document.Current.IsTaxValid = false;
				Document.Cache.MarkUpdated(Document.Current);
			}

			if (row.LineType == SOLineType.Inventory && row.InvtMult != 0)
			{
				UpdateCreateINDocValue();
		}
		}

		protected override void ARTran_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			ARTran row = (ARTran)e.Row;
			ARTran oldRow = (ARTran)e.OldRow;

			if (row != null)
			{
                if (!sender.ObjectsEqual<ARTran.branchID>(e.Row, e.OldRow) || !sender.ObjectsEqual<ARTran.inventoryID>(e.Row, e.OldRow) ||
                    !sender.ObjectsEqual<ARTran.qty>(e.Row, e.OldRow) || !sender.ObjectsEqual<ARTran.curyUnitPrice>(e.Row, e.OldRow) || !sender.ObjectsEqual<ARTran.curyTranAmt>(e.Row, e.OldRow) ||
					!sender.ObjectsEqual<ARTran.curyExtPrice>(e.Row, e.OldRow) || !sender.ObjectsEqual<ARTran.curyDiscAmt>(e.Row, e.OldRow) ||
                    !sender.ObjectsEqual<ARTran.discPct>(e.Row, e.OldRow) || !sender.ObjectsEqual<ARTran.manualDisc>(e.Row, e.OldRow) ||
                    !sender.ObjectsEqual<ARTran.discountID>(e.Row, e.OldRow))
                    RecalculateDiscounts(sender, row);

				if (row.ManualDisc != true)
				{
					var discountCode = (ARDiscount)PXSelectorAttribute.Select<ARTran.discountID>(sender, row);
					row.DiscPctDR = (discountCode != null && discountCode.IsAppliedToDR == true) ? row.DiscPct : 0.0m;
				}

                if ((e.ExternalCall || sender.Graph.IsImport)
                    && sender.ObjectsEqual<ARTran.inventoryID>(e.Row, e.OldRow) && sender.ObjectsEqual<ARTran.uOM>(e.Row, e.OldRow)
                    && sender.ObjectsEqual<ARTran.qty>(e.Row, e.OldRow) && sender.ObjectsEqual<ARTran.branchID>(e.Row, e.OldRow)
					&& sender.ObjectsEqual<ARTran.siteID>(e.Row, e.OldRow) && sender.ObjectsEqual<ARTran.manualPrice>(e.Row, e.OldRow)
					&& (!sender.ObjectsEqual<ARTran.curyUnitPrice>(e.Row, e.OldRow) || !sender.ObjectsEqual<ARTran.curyExtPrice>(e.Row, e.OldRow))
					&& row.ManualPrice == oldRow.ManualPrice)
                    row.ManualPrice = true;

				if (row.ManualPrice != true)
				{
					row.CuryUnitPriceDR = row.CuryUnitPrice;
				}

				TaxAttribute.Calculate<ARTran.taxCategoryID>(sender, e);
			}
		}

		protected override void ARTran_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			base.ARTran_RowSelected(sender, e);

			ARTran row = e.Row as ARTran;
			if (row == null)
				return;

				PXUIFieldAttribute.SetEnabled<ARTran.inventoryID>(sender, row, row.SOShipmentNbr == null);
				PXUIFieldAttribute.SetEnabled<ARTran.qty>(sender, row, row.SOShipmentNbr == null);
				PXUIFieldAttribute.SetEnabled<ARTran.uOM>(sender, row, row.SOShipmentNbr == null);
			}

		protected override void ARTran_InventoryID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			var row = (ARTran)e.Row;
			if (row.SOShipmentNbr == null)
			{
				sender.SetDefaultExt<ARTran.invtMult>(e.Row);
			}
			if (SODocument.Current?.CreateINDoc == false && row.LineType == SOLineType.Inventory
				&& ((row.InvtMult ?? 0) != 0 || row.SOShipmentNbr == null))
			{
				SODocument.Current.CreateINDoc = true;
				if (SODocument.Cache.GetStatus(SODocument.Current) == PXEntryStatus.Notchanged)
				{
					SODocument.Cache.SetStatus(SODocument.Current, PXEntryStatus.Updated);
				}
			}

			base.ARTran_InventoryID_FieldUpdated(sender, e);
		}

		protected virtual void ARTran_InvtMult_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			ARTran row = (ARTran)e.Row;
			if (row == null) return;

			e.NewValue = (row.SOShipmentNbr != null) ? (short)0 : INTranType.InvtMultFromInvoiceType(row.TranType);
				}

		protected override void ARTran_UOM_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			sender.SetDefaultExt<ARTran.curyUnitPrice>(e.Row);
		}

		protected virtual void ARTran_SiteID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			sender.SetDefaultExt<ARTran.curyUnitPrice>(e.Row);
		}

		protected override void ARTran_Qty_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			base.ARTran_Qty_FieldUpdated(sender, e);
			ARTran row = e.Row as ARTran;
			if (row != null)
			{
				sender.SetDefaultExt<ARTran.tranDate>(row);
				sender.SetValueExt<ARTran.manualDisc>(row, false);
				sender.SetDefaultExt<ARTran.curyUnitPrice>(row);
			}
		}
				
		protected override void ARTran_TaxCategoryID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (e.Row != null && string.IsNullOrEmpty(((ARTran)e.Row).SOOrderNbr) == false)
			{
				//tax category is taken from invoice lines
				e.NewValue = null;
				e.Cancel = true;
			}
		}

		protected virtual void ARTran_SalesPersonID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (e.Row != null && string.IsNullOrEmpty(((ARTran)e.Row).SOOrderNbr) == false)
			{
				//salesperson is taken from invoice lines
				e.NewValue = null;
				e.Cancel = true;
			}
		}

		protected override void ARTran_AccountID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (e.Row != null && string.IsNullOrEmpty(((ARTran)e.Row).SOOrderType) == false)
			{
                ARTran tran = (ARTran)e.Row;

                if (tran != null)
                {
					InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<ARTran.inventoryID>(sender, e.Row);
					INSite site = (INSite)PXSelectorAttribute.Select<ARTran.siteID>(sender, e.Row);
					ReasonCode reasoncode = (ReasonCode)PXSelectorAttribute.Select<ARTran.reasonCode>(sender, e.Row);
					SOOrderType ordertype = (SOOrderType)PXSelectorAttribute.Select<ARTran.sOOrderType>(sender, e.Row);
					INPostClass postclass = new INPostClass();
					if (item != null)
					{
						postclass = PXSelectReadonly<INPostClass, Where<INPostClass.postClassID, Equal<Required<INPostClass.postClassID>>>>.Select(this, item.PostClassID);
					}

                    Location customerloc = location.Current;

                    if (item == null)
                    {
                        return;
                    }

                    switch (ordertype.SalesAcctDefault)
                    {
                        case SOSalesAcctSubDefault.MaskItem:
                            e.NewValue = GetValue<InventoryItem.salesAcctID>(item);
                            e.Cancel = true;
                            break;
                        case SOSalesAcctSubDefault.MaskSite:
                            e.NewValue = GetValue<INSite.salesAcctID>(site);
                            e.Cancel = true;
                            break;
                        case SOSalesAcctSubDefault.MaskClass:
                            e.NewValue = GetValue<INPostClass.salesAcctID>(postclass);
                            e.Cancel = true;
                            break;
                        case SOSalesAcctSubDefault.MaskLocation:
                            e.NewValue = GetValue<Location.cSalesAcctID>(customerloc);
                            e.Cancel = true;
                            break;
                        case SOSalesAcctSubDefault.MaskReasonCode:
                            e.NewValue = GetValue<ReasonCode.salesAcctID>(reasoncode);
                            e.Cancel = true;
                            break;
                    }
                }
			}
			else
			{
				base.ARTran_AccountID_FieldDefaulting(sender, e);
			}
		}

		protected override void ARTran_SubID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (e.Row != null && string.IsNullOrEmpty(((ARTran)e.Row).SOOrderType) == false)
			{
                ARTran tran = (ARTran)e.Row;

                if (tran != null && tran.AccountID != null)
                {
					InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<ARTran.inventoryID>(sender, e.Row);
					INSite site = (INSite)PXSelectorAttribute.Select<ARTran.siteID>(sender, e.Row);
					ReasonCode reasoncode = (ReasonCode)PXSelectorAttribute.Select<ARTran.reasonCode>(sender, e.Row);
					SOOrderType ordertype = (SOOrderType)PXSelectorAttribute.Select<ARTran.sOOrderType>(sender, e.Row);
					SalesPerson salesperson = (SalesPerson)PXSelectorAttribute.Select<ARTran.salesPersonID>(sender, e.Row);
					INPostClass postclass = new INPostClass();
					if (item != null)
					{
						postclass = PXSelectReadonly<INPostClass, Where<INPostClass.postClassID, Equal<Required<INPostClass.postClassID>>>>.Select(this, item.PostClassID);
					}

                    EPEmployee employee = (EPEmployee)PXSelectJoin<EPEmployee, InnerJoin<SOOrder, On<EPEmployee.userID, Equal<SOOrder.ownerID>>>, Where<SOOrder.orderType, Equal<Required<ARTran.sOOrderType>>, And<SOOrder.orderNbr, Equal<Required<ARTran.sOOrderNbr>>>>>.Select(this, tran.SOOrderType, tran.SOOrderNbr);
                    CRLocation companyloc =
                        PXSelectJoin<CRLocation, InnerJoin<BAccountR, On<CRLocation.bAccountID, Equal<BAccountR.bAccountID>, And<CRLocation.locationID, Equal<BAccountR.defLocationID>>>, InnerJoin<Branch, On<BAccountR.bAccountID, Equal<Branch.bAccountID>>>>, Where<Branch.branchID, Equal<Required<ARTran.branchID>>>>.Select(this, tran.BranchID);
                    Location customerloc = location.Current;

                    object item_SubID = GetValue<InventoryItem.salesSubID>(item);
                    object site_SubID = GetValue<INSite.salesSubID>(site);
                    object postclass_SubID = GetValue<INPostClass.salesSubID>(postclass);
                    object customer_SubID = GetValue<Location.cSalesSubID>(customerloc);
                    object employee_SubID = GetValue<EPEmployee.salesSubID>(employee);
                    object company_SubID = GetValue<CRLocation.cMPSalesSubID>(companyloc);
                    object salesperson_SubID = GetValue<SalesPerson.salesSubID>(salesperson);
                    object reasoncode_SubID = GetValue<ReasonCode.salesSubID>(reasoncode);

                    object value = null;

                    try
                    {
                        value = SOSalesSubAccountMaskAttribute.MakeSub<SOOrderType.salesSubMask>(this, ordertype.SalesSubMask,
                                                                                                 new object[] 
                                                                                             { 
                                                                                                 item_SubID, 
                                                                                                 site_SubID, 
                                                                                                 postclass_SubID, 
                                                                                                 customer_SubID, 
                                                                                                 employee_SubID, 
                                                                                                 company_SubID, 
                                                                                                 salesperson_SubID, 
                                                                                                 reasoncode_SubID 
                                                                                             },
                                                                                                 new Type[] 
                                                                                             { 
                                                                                                 typeof(InventoryItem.salesSubID), 
                                                                                                 typeof(INSite.salesSubID), 
                                                                                                 typeof(INPostClass.salesSubID), 
                                                                                                 typeof(Location.cSalesSubID), 
                                                                                                 typeof(EPEmployee.salesSubID), 
                                                                                                 typeof(Location.cMPSalesSubID), 
                                                                                                 typeof(SalesPerson.salesSubID), 
                                                                                                 typeof(ReasonCode.subID) 
                                                                                             });

                        sender.RaiseFieldUpdating<ARTran.subID>(tran, ref value);
                    }
                    catch (PXMaskArgumentException ex)
                    {
                        sender.RaiseExceptionHandling<ARTran.subID>(e.Row, null, new PXSetPropertyException(ex.Message));
                        value = null;
                    }
                    catch (PXSetPropertyException ex)
                    {
                        sender.RaiseExceptionHandling<ARTran.subID>(e.Row, value, ex);
                        value = null;
                    }

                    e.NewValue = (int?)value;
                    e.Cancel = true;
                }
			}
			else
			{
				base.ARTran_SubID_FieldDefaulting(sender, e);
			}
		}

		protected override void ARInvoiceDiscountDetail_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			base.ARInvoiceDiscountDetail_RowInserted(sender, e);

			ARInvoiceDiscountDetail discountDetail = (ARInvoiceDiscountDetail)e.Row;
			if (discountDetail.OrderNbr == null && discountDetail.DiscountID != null)
			{
				sender.RaiseExceptionHandling<ARInvoiceDiscountDetail.discountID>(discountDetail, discountDetail.DiscountID,
					new PXSetPropertyException(Messages.AutomaticDiscountInSOInvoice, PXErrorLevel.Warning));
			}
		}

		protected virtual void SOFreightDetail_AccountID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			SOFreightDetail row = e.Row as SOFreightDetail;
			if (row != null && row.TaskID == null)
			{
				sender.SetDefaultExt<SOFreightDetail.taskID>(e.Row);
			}
		}


		protected virtual void SOFreightDetail_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (Document.Current != null)
			{
				PXUIFieldAttribute.SetEnabled<SOFreightDetail.curyPremiumFreightAmt>(sender, e.Row, Document.Current.Released != true);
				PXUIFieldAttribute.SetEnabled<SOFreightDetail.curyFreightAmt>(sender, e.Row, Document.Current.Released != true);
			}
		}
				
		protected virtual void SOFreightDetail_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			var row = (SOFreightDetail)e.Row;
			if (row != null)
			{
				UpdateFreightTransaction(row, false);
			}
		}

		protected virtual void SOFreightDetail_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			var row = (SOFreightDetail)e.Row;
			if (row != null)
			{
				UpdateFreightTransaction(row, true);
			}
		}


		public override IEnumerable transactions()
		{
			BqlCommand cmd = Transactions.View.BqlSelect.WhereNew<
				Where<ARTran.tranType, Equal<Current<ARInvoice.docType>>,
				And<ARTran.refNbr, Equal<Current<ARInvoice.refNbr>>,
				And<ARTran.lineType, NotEqual<SOLineType.discount>,
				And<ARTran.lineType, NotEqual<SOLineType.freight>>>>>>();

			int startRow = PXView.StartRow;
			int totalRows = 0;

			foreach (object record in new PXView(this, false, cmd).Select(PXView.Currents, PXView.Parameters, PXView.Searches, PXView.SortColumns, PXView.Descendings, PXView.Filters, ref startRow, PXView.MaximumRows, ref totalRows))
			{
				yield return record;
			}

			PXView.StartRow = 0;
		}

		public override int ExecuteInsert(string viewName, IDictionary values, params object[] parameters)
		{
			switch (viewName)
			{
				case "Freight":
					values[PXDataUtils.FieldName<ARTran.lineType>()] = SOLineType.Freight;
					break;
				case "Discount":
					values[PXDataUtils.FieldName<ARTran.lineType>()] = SOLineType.Discount;
					break;
			}
			return base.ExecuteInsert(viewName, values, parameters);
		}

		public virtual IEnumerable sHipmentlist()
		{
			PXSelectBase<ARTran> cmd = new PXSelect<ARTran, 
				Where<ARTran.sOShipmentNbr, Equal<Current<SOOrderShipment.shipmentNbr>>, 
				And<ARTran.sOShipmentType, Equal<Current<SOOrderShipment.shipmentType>>, 
				And<ARTran.sOOrderType, Equal<Current<SOOrderShipment.orderType>>, 
				And<ARTran.sOOrderNbr, Equal<Current<SOOrderShipment.orderNbr>>>>>>>(this);

			DocumentList<ARInvoice, SOInvoice> list = new DocumentList<ARInvoice, SOInvoice>(this);
			list.Add(Document.Current, SODocument.Select());

			bool newInvoice = Transactions.Select().Count == 0;

			HashSet<SOOrderShipment> updated = new HashSet<SOOrderShipment>(shipmentlist.Cache.GetComparer());

			foreach (SOOrderShipment shipment in shipmentlist.Cache.Updated)
			{
				updated.Add(shipment);
				yield return shipment;
			}

			foreach (PXResult<SOOrderShipment, SOOrder, SOShipLine, SOOrderType> order in 
			PXSelectJoinGroupBy<SOOrderShipment,
			InnerJoin<SOOrder, On<SOOrderShipment.FK.Order>,
			InnerJoin<SOShipLine, On<SOShipLine.shipmentType, Equal<SOOrderShipment.shipmentType>, And<SOShipLine.shipmentNbr, Equal<SOOrderShipment.shipmentNbr>, And<SOShipLine.origOrderType, Equal<SOOrderShipment.orderType>, And<SOShipLine.origOrderNbr, Equal<SOOrderShipment.orderNbr>>>>>,
			InnerJoin<SOOrderType, 
				On<SOOrderShipment.FK.OrderType>>>>,
			Where<SOOrderShipment.customerID, Equal<Current<ARInvoice.customerID>>,
				And<SOOrderShipment.hold, Equal<boolFalse>,
				And<SOOrderShipment.confirmed, Equal<boolTrue>, 
				And<SOOrderType.aRDocType, Equal<Current<ARInvoice.docType>>,
				And<SOOrderShipment.invoiceNbr, IsNull>>>>>,
			Aggregate<GroupBy<SOOrderShipment.shippingRefNoteID,
				GroupBy<SOOrderShipment.shipmentType,
				GroupBy<SOOrderShipment.shipmentNbr,
				GroupBy<SOOrderShipment.orderType,
				GroupBy<SOOrderShipment.orderNbr>>>>>>>.Select(this))
			{
				if (!updated.Contains((SOOrderShipment)order) && cmd.View.SelectSingleBound(new object[] { (SOOrderShipment)order }) == null)
				{
					if (newInvoice || list.Find<ARInvoice.customerID, SOInvoice.billAddressID, SOInvoice.billContactID, ARInvoice.curyID, ARInvoice.termsID, ARInvoice.hidden>(((SOOrder)order).CustomerID, ((SOOrder)order).BillAddressID, ((SOOrder)order).BillContactID, ((SOOrder)order).CuryID, ((SOOrder)order).TermsID, false) != null)
					{
						yield return (SOOrderShipment)order;
					}
				}
			}

			foreach (PXResult<SOOrderShipment, SOOrder, POReceiptLine> order in PXSelectJoinGroupBy<SOOrderShipment,
					InnerJoin<SOOrder, On<SOOrder.orderType, Equal<SOOrderShipment.orderType>, And<SOOrder.orderNbr, Equal<SOOrderShipment.orderNbr>>>,				
					InnerJoin<POReceiptLine, On<POReceiptLine.receiptNbr, Equal<SOOrderShipment.shipmentNbr>>,
					InnerJoin<SOLineSplit, On<SOLineSplit.pOType, Equal<POReceiptLine.pOType>, And<SOLineSplit.pONbr, Equal<POReceiptLine.pONbr>, And<SOLineSplit.pOLineNbr, Equal<POReceiptLine.pOLineNbr>, And<SOLineSplit.orderType, Equal<SOOrder.orderType>, And<SOLineSplit.orderNbr, Equal<SOOrder.orderNbr>>>>>>,
					InnerJoin<SOLine, On<SOLine.orderType, Equal<SOLineSplit.orderType>, And<SOLine.orderNbr, Equal<SOLineSplit.orderNbr>, And<SOLine.lineNbr, Equal<SOLineSplit.lineNbr>>>>,
					InnerJoin<SOOrderType,
						On<SOLine.FK.OrderType>>>>>>,
					Where<SOOrderShipment.shipmentType, Equal<SOShipmentType.dropShip>, 
						And2<Where<POReceiptLine.lineType, Equal<POLineType.goodsForDropShip>, Or<POReceiptLine.lineType, Equal<POLineType.nonStockForDropShip>>>,
						And<SOOrder.customerID, Equal<Current<ARInvoice.customerID>>,
						And<SOOrderType.aRDocType, Equal<Current<ARInvoice.docType>>,
						And<SOOrderShipment.invoiceNbr, IsNull>>>>>,
					Aggregate<GroupBy<SOOrderShipment.shippingRefNoteID,
						GroupBy<SOOrderShipment.shipmentType, 
						GroupBy<SOOrderShipment.shipmentNbr, 
						GroupBy<SOOrderShipment.orderType, 
						GroupBy<SOOrderShipment.orderNbr,
						Sum<POReceiptLine.receiptQty,
						Sum<POReceiptLine.extWeight,
						Sum<POReceiptLine.extVolume>>>>>>>>>>.Select(this))
			{
				if (!updated.Contains((SOOrderShipment)order) && cmd.View.SelectSingleBound(new object[] { (SOOrderShipment)order }) == null)
				{
					if (newInvoice || list.Find<ARInvoice.customerID, SOInvoice.billAddressID, SOInvoice.billContactID, ARInvoice.curyID, ARInvoice.termsID, ARInvoice.hidden>(((SOOrder)order).CustomerID, ((SOOrder)order).BillAddressID, ((SOOrder)order).BillContactID, ((SOOrder)order).CuryID, ((SOOrder)order).TermsID, false) != null)
					{
						yield return (SOOrderShipment)order;
					}
				}
			}
		}

		protected virtual void SOOrderShipment_ShipmentNbr_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		protected virtual void SOOrderShipment_RowDeleting(PXCache sender, PXRowDeletingEventArgs e)
		{
			var row = (SOOrderShipment)e.Row;
			if (!string.Equals(row.ShipmentNbr, Constants.NoShipmentNbr) 
				&& row.ShipmentNbr != null && row.ShipmentType != null)
			{
				SOOrderShipment copy = PXCache<SOOrderShipment>.CreateCopy(row);

				row.InvoiceType = null;
				row.InvoiceNbr = null;
				row.OrderFreightAllocated = false;
				shipmentlist.Cache.SetStatus(row, PXEntryStatus.Updated);
				shipmentlist.Cache.RaiseRowUpdated(row, copy);

				//Probably not needed because of PXFormula referencing SOShipment
				SOShipment shipment = GetShipment(row);
				if (shipment != null)
				{
					//persist shipments to workflow
					shipments.Cache.SetStatus(shipment, PXEntryStatus.Updated);
				}

				e.Cancel = true;
			}

			if (row.CreateINDoc == true && row.InvtRefNbr == null)
			{
				UpdateCreateINDocValue();
			}
		}

		protected virtual void UpdateCreateINDocValue()
		{
			if (SODocument.Current == null)
				return;
			bool stockShipmentExists = PXSelect<SOOrderShipment,
					Where<SOOrderShipment.invoiceType, Equal<Current<ARInvoice.docType>>, And<SOOrderShipment.invoiceNbr, Equal<Current<ARInvoice.refNbr>>,
						And<SOOrderShipment.createINDoc, Equal<boolTrue>, And<SOOrderShipment.invtRefNbr, IsNull>>>>>
					.SelectWindowed(this, 0, 1).Count > 0;
			if (!stockShipmentExists)
				{
				bool directStockTranExists = PXSelect<ARTran,
					Where<ARTran.tranType, Equal<Current<ARInvoice.docType>>, And<ARTran.refNbr, Equal<Current<ARInvoice.refNbr>>,
						And<ARTran.lineType, Equal<SOLineType.inventory>, And<ARTran.invtMult, NotEqual<short0>>>>>>
					.SelectWindowed(this, 0, 1).Count > 0;
				if (!directStockTranExists)
				{
					SODocument.Current.CreateINDoc = false;
					if (SODocument.Cache.GetStatus(SODocument.Current) == PXEntryStatus.Notchanged)
					{
						SODocument.Cache.SetStatus(SODocument.Current, PXEntryStatus.Updated);
					}
				}
			}
		}

        protected virtual void SOOrderShipment_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
        {
            SOOrderShipment row = e.Row as SOOrderShipment;
            if (row != null && string.Equals(row.ShipmentNbr, Constants.NoShipmentNbr))
            {
                SOOrder cached = soorder.Locate(new SOOrder { OrderType = row.OrderType, OrderNbr = row.OrderNbr });
                if (cached != null)
                { 
                    cached.ShipmentCntr--;

	                if (cached.ShipmentCntr == 0 && (string.Equals(row.ShipmentNbr, Constants.NoShipmentNbr)))
	                {
		                cached.Completed = false;
		                cached.Status = SOOrderStatus.Open;
	                }

                    soorder.Update(cached);
                }
            }
        }

		protected virtual void SOOrderShipment_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			PXUIFieldAttribute.SetEnabled(sender, e.Row, false);
			PXUIFieldAttribute.SetEnabled<SOOrderShipment.selected>(sender, e.Row, true);
		}

        protected virtual void SOOrder_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
        {
            SOOrder doc = (SOOrder)e.Row;
            if (e.Operation == PXDBOperation.Update)
            {
                if (doc.ShipmentCntr < 0 || doc.OpenShipmentCntr < 0 || doc.ShipmentCntr < doc.BilledCntr + doc.ReleasedCntr && doc.Behavior == SOBehavior.SO)
                {
                    throw new PXSetPropertyException(Messages.InvalidShipmentCounters);
                }
            }
        }

        protected virtual void SOOrder_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			SOOrder row = e.Row as SOOrder;
			SOOrder oldRow = e.OldRow as SOOrder;
			if (row != null && oldRow != null && row.UnbilledOrderQty != oldRow.UnbilledOrderQty)
			{
				row.IsUnbilledTaxValid = false;
			}
			
			if (e.OldRow != null)
			{
				ARReleaseProcess.UpdateARBalances(this, (SOOrder)e.OldRow, -((SOOrder)e.OldRow).UnbilledOrderTotal, -((SOOrder)e.Row).OpenOrderTotal);
			}
			ARReleaseProcess.UpdateARBalances(this, (SOOrder)e.Row, ((SOOrder)e.Row).UnbilledOrderTotal, ((SOOrder)e.Row).OpenOrderTotal);
		}

		public bool TransferApplicationFromSalesOrder;
		public override IEnumerable adjustments()
		{
			CurrencyInfo inv_info = PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Current<ARInvoice.curyInfoID>>>>.Select(this);

			int applcount = 0;
			foreach (PXResult<ARAdjust2, ARPayment, AR.Standalone.ARRegisterAlias, CurrencyInfo> res in Adjustments_Inv.Select())
			{
				ARPayment payment = PXCache<ARPayment>.CreateCopy(res);
				ARAdjust2 adj = res;
				CurrencyInfo pay_info = res;

				PXCache<ARRegister>.RestoreCopy(payment, (AR.Standalone.ARRegisterAlias)res);
				ARPayment originalPayment = PXCache<ARPayment>.CreateCopy(payment);

				if (adj == null) continue;

				ARAdjust2 other = PXSelectGroupBy<ARAdjust2, Where<ARAdjust2.adjgDocType, Equal<Required<ARAdjust2.adjgDocType>>, And<ARAdjust2.adjgRefNbr, Equal<Required<ARAdjust2.adjgRefNbr>>, And<ARAdjust2.released, Equal<False>, And<Where<ARAdjust2.adjdDocType, NotEqual<Required<ARAdjust2.adjdDocType>>, Or<ARAdjust2.adjdRefNbr, NotEqual<Required<ARAdjust2.adjdRefNbr>>>>>>>>, Aggregate<GroupBy<ARAdjust2.adjgDocType, GroupBy<ARAdjust2.adjgRefNbr, Sum<ARAdjust2.curyAdjgAmt, Sum<ARAdjust2.adjAmt>>>>>>.Select(this, adj.AdjgDocType, adj.AdjgRefNbr, adj.AdjdDocType, adj.AdjdRefNbr);
				if (other != null && other.AdjdRefNbr != null)
				{
					payment.CuryDocBal -= other.CuryAdjgAmt;
					payment.DocBal -= other.AdjAmt;
				}

				BalanceCalculation.CalculateApplicationDocumentBalance(
					Adjustments.Cache, 
					payment, 
					adj, 
					pay_info, 
					inv_info);

                yield return new PXResult<ARAdjust2, ARPayment>(adj, originalPayment);
				applcount++;
			}
						
			//fix unattended grid load in InvoiceOrder when CurrencyRate is set
			if (this.UnattendedMode && !TransferApplicationFromSalesOrder)
			{
				yield break;
			}
						
			if (Document.Current != null && Document.Current.DocType.IsIn(ARDocType.Invoice, ARDocType.DebitMemo) && Document.Current.Released == false)
			{
				using (new ReadOnlyScope(Adjustments.Cache, Document.Cache, arbalances.Cache, SODocument.Cache))
				{
					List<PXResult<ARPayment, CurrencyInfo, ARAdjust2>> list = new List<PXResult<ARPayment, CurrencyInfo, ARAdjust2>>();

					//same as ARInvoiceEntry but without released constraint and with hold constraint
					PXSelectBase<ARPayment> cmd = new PXSelectReadonly2<ARPayment,
						InnerJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<ARPayment.curyInfoID>>,
						LeftJoin<ARAdjust2,
							On<ARAdjust2.adjgDocType, Equal<ARPayment.docType>,
							And<ARAdjust2.adjgRefNbr, Equal<ARPayment.refNbr>,
							And<ARAdjust2.adjNbr, Equal<ARPayment.adjCntr>,
							And<ARAdjust2.released, Equal<False>,
							And<ARAdjust2.hold, Equal<False>,
							And<ARAdjust2.voided, Equal<False>,
							And<Where<ARAdjust2.adjdDocType, NotEqual<Current<ARInvoice.docType>>,
								Or<ARAdjust2.adjdRefNbr, NotEqual<Current<ARInvoice.refNbr>>>>>>>>>>>>>,
						Where2<
						Where<ARPayment.customerID, Equal<Current<ARInvoice.customerID>>,
								Or<ARPayment.customerID, In2<Search<Customer.consolidatingBAccountID, Where<Customer.bAccountID, Equal<Current<ARInvoice.customerID>>>>>>>,
							And<ARPayment.docType, In3<ARDocType.payment, ARDocType.prepayment, ARDocType.creditMemo>,
							And<ARPayment.openDoc, Equal<True>
							,And<ARAdjust2.adjdRefNbr, IsNull>
							>>>,
                        OrderBy<Asc<ARPayment.docType, Asc<ARPayment.refNbr>>>>(this);

					//this delegate is invoked in processing to transfer applications from sales order
					//date and period constraints are not valid in this case
					if (!TransferApplicationFromSalesOrder)
					{
						cmd.Join<LeftJoin<SOAdjust,
						On<SOAdjust.adjgDocType, Equal<ARPayment.docType>,
							And<SOAdjust.adjgRefNbr, Equal<ARPayment.refNbr>,
							And<SOAdjust.adjAmt, Greater<decimal0>>>>>>();

						cmd.WhereAnd<Where<ARPayment.docDate, LessEqual<Current<ARInvoice.docDate>>,
							And<ARPayment.finPeriodID, LessEqual<Current<ARInvoice.finPeriodID>>,
							And<SOAdjust.adjgRefNbr, IsNull>>>>();

						int remaining = Constants.MaxNumberOfPaymentsAndMemos - applcount;
						if (remaining > 0)
						{
						foreach (PXResult<AR.ARPayment, CurrencyInfo, ARAdjust2> res in cmd.SelectWindowed(0, remaining))
						{
							list.Add(res);
						}
					}
					}
					else
					{
						cmd.Join<InnerJoin<SOAdjust,
						On<SOAdjust.adjgDocType, Equal<ARPayment.docType>,
							And<SOAdjust.adjgRefNbr, Equal<ARPayment.refNbr>,
							And<SOAdjust.adjAmt, Greater<decimal0>>>>>>();
						cmd.WhereAnd<Where<SOAdjust.adjdOrderType, Equal<Required<SOAdjust.adjdOrderType>>,
							And<SOAdjust.adjdOrderNbr, Equal<Required<SOAdjust.adjdOrderNbr>>>>>();

						HashSet<string> orderProcessed = new HashSet<string>();

						foreach (ARTran tran in Transactions.Select())
						{
							if (!string.IsNullOrEmpty(tran.SOOrderType) && !string.IsNullOrEmpty(tran.SOOrderNbr))
							{
								string key = string.Format("{0}.{1}", tran.SOOrderType, tran.SOOrderNbr);

								if (!orderProcessed.Contains(key))
								{
									orderProcessed.Add(key);
									foreach (PXResult<ARPayment, CurrencyInfo, ARAdjust2> res in cmd.Select(tran.SOOrderType, tran.SOOrderNbr))
									{
										list.Add(res);
									}
								}
							}
						}
					}

					foreach (PXResult<AR.ARPayment, CurrencyInfo, ARAdjust2> res in list)
					{
						ARPayment payment = PXCache<ARPayment>.CreateCopy(res);
						ARAdjust2 adj = new ARAdjust2();
						CurrencyInfo pay_info = PXResult.Unwrap<CurrencyInfo>(res);

						adj.CustomerID = payment.CustomerID;
						adj.AdjdCustomerID = Document.Current.CustomerID;
						adj.AdjdDocType = Document.Current.DocType;
						adj.AdjdRefNbr = Document.Current.RefNbr;
						adj.AdjdBranchID = Document.Current.BranchID;
						adj.AdjgDocType = payment.DocType;
						adj.AdjgRefNbr = payment.RefNbr;
						adj.AdjgBranchID = payment.BranchID;
						adj.AdjNbr = payment.AdjCntr;

						ARAdjust2 other = PXSelectGroupBy<ARAdjust2, Where<ARAdjust2.adjgDocType, Equal<Required<ARAdjust2.adjgDocType>>, And<ARAdjust2.adjgRefNbr, Equal<Required<ARAdjust2.adjgRefNbr>>, And<ARAdjust2.released, Equal<False>, And<Where<ARAdjust2.adjdDocType, NotEqual<Required<ARAdjust2.adjdDocType>>, Or<ARAdjust2.adjdRefNbr, NotEqual<Required<ARAdjust2.adjdRefNbr>>>>>>>>, Aggregate<GroupBy<ARAdjust2.adjgDocType, GroupBy<ARAdjust2.adjgRefNbr, Sum<ARAdjust2.curyAdjgAmt, Sum<ARAdjust2.adjAmt>>>>>>.Select(this, adj.AdjgDocType, adj.AdjgRefNbr, adj.AdjdDocType, adj.AdjdRefNbr);
						if (other != null && other.AdjdRefNbr != null)
						{
							payment.CuryDocBal -= other.CuryAdjgAmt;
							payment.DocBal -= other.AdjAmt;
						}

						if (Adjustments.Cache.Locate(adj) == null)
						{
							adj.AdjgCuryInfoID = payment.CuryInfoID;
							adj.AdjdOrigCuryInfoID = Document.Current.CuryInfoID;
							//if LE constraint is removed from payment selection this must be reconsidered
							adj.AdjdCuryInfoID = Document.Current.CuryInfoID;

							decimal CuryDocBal;
							if (string.Equals(pay_info.CuryID, inv_info.CuryID))
							{
								CuryDocBal = payment.CuryDocBal ?? 0m;
							}
							else
							{
								PXDBCurrencyAttribute.CuryConvCury(Adjustments.Cache, inv_info, payment.DocBal ?? 0m, out CuryDocBal);
							}
							adj.CuryDocBal = CuryDocBal;

							yield return new PXResult<ARAdjust2, ARPayment>(Adjustments.Insert(adj), payment);
						}
					}
				}
			}
		}

		public delegate void InvoiceCreatedDelegate(ARInvoice invoice, SOOrder source);
		protected virtual void InvoiceCreated(ARInvoice invoice, SOOrder source)
		{

		}

		public virtual string GetInvoiceDocType(SOOrderType soOrderType, string shipmentOperation)
		{
			string docType = soOrderType.ARDocType;
			if (shipmentOperation == soOrderType.DefaultOperation)
				return docType;
			//for RMA switch document type if previous shipment was not invoiced previously in the current run, i.e. list.Find() returned null
			return
				docType == ARDocType.Invoice ? ARDocType.CreditMemo :
				docType == ARDocType.DebitMemo ? ARDocType.CreditMemo :
				docType == ARDocType.CreditMemo ? ARDocType.Invoice :
				docType == ARDocType.CashSale ? ARDocType.CashReturn :
				docType == ARDocType.CashReturn ? ARDocType.CashSale :
				null;
		}

		public virtual void InvoiceOrder(DateTime invoiceDate, PXResult<SOOrderShipment, SOOrder, CurrencyInfo, SOAddress, SOContact> order, Customer customer, DocumentList<ARInvoice, SOInvoice> list)
			=> InvoiceOrder(invoiceDate, order, customer, list, PXQuickProcess.ActionFlow.NoFlow);
		public virtual void InvoiceOrder(DateTime invoiceDate, PXResult<SOOrderShipment, SOOrder, CurrencyInfo, SOAddress, SOContact> order, Customer customer, DocumentList<ARInvoice, SOInvoice> list, PXQuickProcess.ActionFlow quickProcessFlow)
		{
			InvoiceOrder(invoiceDate, order, null, customer, list, quickProcessFlow);
		}

		public virtual void InvoiceOrder(DateTime invoiceDate, PXResult<SOOrderShipment, SOOrder, CurrencyInfo, SOAddress, SOContact> order, PXResultset<SOShipLine, SOLine> details, Customer customer, DocumentList<ARInvoice, SOInvoice> list)
			=> InvoiceOrder(invoiceDate, order, details, customer, list, PXQuickProcess.ActionFlow.NoFlow);
		public virtual void InvoiceOrder(DateTime invoiceDate, PXResult<SOOrderShipment, SOOrder, CurrencyInfo, SOAddress, SOContact> order, PXResultset<SOShipLine, SOLine> details, Customer customer, DocumentList<ARInvoice, SOInvoice> list, PXQuickProcess.ActionFlow quickProcessFlow)
		{
			ARInvoice newdoc;

			SOOrderShipment orderShipment = order;
			SOOrder soOrder = order;
			CurrencyInfo currencyInfo = order;
			SOAddress soBillAddress = order;
			SOContact soBillContact = order;
			SOOrderType soOrderType = SOOrderType.PK.Find(this, soOrder.OrderType);

			//TODO: Temporary solution. Review when AC-80210 is fixed
			if (orderShipment.ShipmentNbr != Constants.NoShipmentNbr && orderShipment.ShipmentType != SOShipmentType.DropShip && orderShipment.Confirmed != true)
			{
				throw new PXException(Messages.UnableToProcessUnconfirmedShipment, orderShipment.ShipmentNbr);
			}

			decimal ApprovedBalance = 0;
			HashSet<SOOrder> accountedForOrders = new HashSet<SOOrder>(new LSSOLine.Comparer<SOOrder>(this));
            
            PXRowUpdated ApprovedBalanceCollector = delegate (PXCache sender, PXRowUpdatedEventArgs e)
            {
                ARInvoice ARDoc = (ARInvoice)e.Row;

				//Discounts can reduce the balance - adjust the creditHold if it was wrongly set:
				if ((decimal)ARDoc.DocBal <= ApprovedBalance && ARDoc.CreditHold == true)
				{
					object OldRow = sender.CreateCopy(ARDoc);
					sender.SetValueExt<ARInvoice.creditHold>(ARDoc, false);
					sender.RaiseRowUpdated(ARDoc, OldRow);
				}

				//Maximum approved balance for an invoice is the sum of all approved order amounts:
				if ((bool)soOrder.ApprovedCredit)
                {
					if (!accountedForOrders.Contains(soOrder))
                    {
						ApprovedBalance += soOrder.ApprovedCreditAmt.GetValueOrDefault();
						accountedForOrders.Add(soOrder);
                    }

					ARDoc.ApprovedCreditAmt = ApprovedBalance;
					ARDoc.ApprovedCredit = true;
                }
            };
            CustomerCreditHelper.AppendPreUpdatedEvent(typeof(ARInvoice), ApprovedBalanceCollector);
			SOOpenPeriodAttribute.SetValidatePeriod<ARInvoice.finPeriodID>(Document.Cache, null, PeriodValidation.Nothing);

            if (list != null)
			{
				DateTime orderInvoiceDate = GetOrderInvoiceDate(invoiceDate, soOrder, orderShipment);
				
				newdoc = FindOrCreateInvoice(orderInvoiceDate, order, list);

				if (newdoc.RefNbr != null)
				{
					Document.Current = newdoc = this.Document.Search<ARInvoice.refNbr>(newdoc.RefNbr, newdoc.DocType);
				}
				else
				{
					this.Clear();

					newdoc.DocType = GetInvoiceDocType(soOrderType, orderShipment.Operation);

					newdoc.DocDate = orderInvoiceDate;
					newdoc.BranchID = soOrder.BranchID;

					if (string.IsNullOrEmpty(soOrder.FinPeriodID) == false)
					{
						newdoc.FinPeriodID = soOrder.FinPeriodID;
					}

					if (soOrder.InvoiceNbr != null)
					{
						newdoc.RefNbr = soOrder.InvoiceNbr;
						newdoc.RefNoteID = soOrder.NoteID;
					}

					if (soOrderType.UserInvoiceNumbering == true && string.IsNullOrEmpty(newdoc.RefNbr))
					{
						throw new PXException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<SOOrder.invoiceNbr>(soorder.Cache));
					}

					if (soOrderType.UserInvoiceNumbering == false && !string.IsNullOrEmpty(newdoc.RefNbr))
					{
						throw new PXException(Messages.MustBeUserNumbering, soOrderType.InvoiceNumberingID);
					}

					AutoNumberAttribute.SetNumberingId<ARInvoice.refNbr>(Document.Cache, soOrderType.ARDocType, soOrderType.InvoiceNumberingID);

					newdoc = (ARInvoice)Document.Cache.CreateCopy(this.Document.Insert(newdoc));

					newdoc.CustomerID = soOrder.CustomerID;
					newdoc.CustomerLocationID = soOrder.CustomerLocationID;
					
					if (newdoc.DocType != ARDocType.CreditMemo)
					{
					newdoc.TermsID = soOrder.TermsID;
					newdoc.DiscDate = soOrder.DiscDate;
					newdoc.DueDate = soOrder.DueDate;
					}

					newdoc.TaxZoneID = soOrder.TaxZoneID;
					newdoc.AvalaraCustomerUsageType = soOrder.AvalaraCustomerUsageType;
					newdoc.SalesPersonID = soOrder.SalesPersonID;
					newdoc.DocDesc = soOrder.OrderDesc;
					newdoc.InvoiceNbr = soOrder.CustomerOrderNbr;
					newdoc.CuryID = soOrder.CuryID;
					newdoc.ProjectID = soOrder.ProjectID ?? PM.ProjectDefaultAttribute.NonProject();
					newdoc.Hold = quickProcessFlow != PXQuickProcess.ActionFlow.HasNextInFlow && soOrderType.InvoiceHoldEntry == true;

					if (soOrderType.MarkInvoicePrinted == true)
					{
						newdoc.Printed = true;
					}

					if (soOrderType.MarkInvoiceEmailed == true)
					{
						newdoc.Emailed = true;
					}

					if (soOrder.PMInstanceID != null || string.IsNullOrEmpty(soOrder.PaymentMethodID) == false)
					{
						newdoc.PMInstanceID = soOrder.PMInstanceID;
						newdoc.PaymentMethodID = soOrder.PaymentMethodID;
						newdoc.CashAccountID = soOrder.CashAccountID;
					}

					var cancel_defaulting = new PXFieldDefaulting((cache, e) =>
					{
						e.NewValue = cache.GetValue<ARInvoice.branchID>(e.Row);
						e.Cancel = true;
					});
					this.FieldDefaulting.AddHandler<ARInvoice.branchID>(cancel_defaulting);

					try
					{
						using (new PXReadDeletedScope())
						{
							newdoc = this.Document.Update(newdoc);
						}
					}
					finally
					{
						this.FieldDefaulting.RemoveHandler<ARInvoice.branchID>(cancel_defaulting);
					}

					if (soOrder.PMInstanceID != null || string.IsNullOrEmpty(soOrder.PaymentMethodID) == false)
					{
						if (SODocument.Current.DocType != ARDocType.CreditMemo)
						{
						SODocument.Current.PMInstanceID = soOrder.PMInstanceID;
						SODocument.Current.PaymentMethodID = soOrder.PaymentMethodID;
						if (SODocument.Current.CashAccountID != soOrder.CashAccountID)
							SODocument.SetValueExt<SOInvoice.cashAccountID>(SODocument.Current, soOrder.CashAccountID);
						if (SODocument.Current.CashAccountID == null)
							SODocument.Cache.SetDefaultExt<SOInvoice.cashAccountID>(SODocument.Current);
						if (SODocument.Current.ARPaymentDepositAsBatch == true && SODocument.Current.DepositAfter == null)
							SODocument.Current.DepositAfter = SODocument.Current.AdjDate;
						SODocument.Current.ExtRefNbr = soOrder.ExtRefNbr;
						}
						//clear error in case invoice currency different from default cash account for customer
						SODocument.Cache.RaiseExceptionHandling<SOInvoice.cashAccountID>(SODocument.Current, null, null);
					}

					foreach (CurrencyInfo info in this.currencyinfo.Select())
					{
						if (soOrder.InvoiceDate != null)
						{
							PXCache<CurrencyInfo>.RestoreCopy(info, currencyInfo);
							info.CuryInfoID = newdoc.CuryInfoID;
						}
						else
						{
							info.CuryRateTypeID = currencyInfo.CuryRateTypeID;
                            currencyinfo.Update(info);
						}
					}
					AddressAttribute.CopyRecord<ARInvoice.billAddressID>(this.Document.Cache, newdoc, soBillAddress, true);
					ContactAttribute.CopyRecord<ARInvoice.billContactID>(this.Document.Cache, newdoc, soBillContact, true);
					var soShipContact = SOContact.PK.Find(this, orderShipment.ShipContactID);
					ARShippingContactAttribute.CopyRecord<ARInvoice.shipContactID>(this.Document.Cache, newdoc, soShipContact, true);
				}
			}
			else
			{
				newdoc = (ARInvoice)Document.Cache.CreateCopy(Document.Current);

                if (Transactions.SelectSingle() == null)
                {
                    newdoc.CustomerID = soOrder.CustomerID;
                    newdoc.ProjectID = soOrder.ProjectID;
                    newdoc.CustomerLocationID = soOrder.CustomerLocationID;
                    newdoc.SalesPersonID = soOrder.SalesPersonID;
                    newdoc.TaxZoneID = soOrder.TaxZoneID;
                    newdoc.AvalaraCustomerUsageType = soOrder.AvalaraCustomerUsageType;
                    newdoc.DocDesc = soOrder.OrderDesc;
                    newdoc.InvoiceNbr = soOrder.CustomerOrderNbr;
                    newdoc.TermsID = soOrder.TermsID;

					foreach (CurrencyInfo info in this.currencyinfo.Select())
					{
						PXCache<CurrencyInfo>.RestoreCopy(info, currencyInfo);
						info.CuryInfoID = newdoc.CuryInfoID;
						this.currencyinfo.Update(info);
						newdoc.CuryID = info.CuryID;
					}
                }

                newdoc = this.Document.Update(newdoc);

				AddressAttribute.CopyRecord<ARInvoice.billAddressID>(this.Document.Cache, newdoc, soBillAddress, true);
				ContactAttribute.CopyRecord<ARInvoice.billContactID>(this.Document.Cache, newdoc, soBillContact, true);
				var soShipContact = SOContact.PK.Find(this, orderShipment.ShipContactID);
				ARShippingContactAttribute.CopyRecord<ARInvoice.shipContactID>(this.Document.Cache, newdoc, soShipContact, true);
			}

			SODocument.Current = (SOInvoice)SODocument.Select() ?? (SOInvoice)SODocument.Cache.Insert();
			if (SODocument.Current.ShipAddressID == null)
			{
				var soShipAddress = SOAddress.PK.Find(this, orderShipment.ShipAddressID);
				ARShippingAddressAttribute.CopyRecord<ARInvoice.shipAddressID>(this.Document.Cache, newdoc, soShipAddress, true);
			}
			else if (SODocument.Current.ShipAddressID != orderShipment.ShipAddressID && newdoc.MultiShipAddress != true)
			{
				newdoc.MultiShipAddress = true;
				ARShippingAddressAttribute.DefaultRecord<ARInvoice.shipAddressID>(this.Document.Cache, newdoc);
			}

			bool prevHoldState = newdoc.Hold == true;
			if (newdoc.Hold != true)
			{
				newdoc.Hold = true;
				newdoc = this.Document.Update(newdoc);
			}
			InvoiceCreated(newdoc, soOrder);

			PXSelectBase<ARInvoiceDiscountDetail> selectInvoiceDiscounts = new PXSelect<ARInvoiceDiscountDetail,
			Where<ARInvoiceDiscountDetail.docType, Equal<Current<SOInvoice.docType>>,
			And<ARInvoiceDiscountDetail.refNbr, Equal<Current<SOInvoice.refNbr>>,
			And<ARInvoiceDiscountDetail.orderType, Equal<Required<ARInvoiceDiscountDetail.orderType>>,
			And<ARInvoiceDiscountDetail.orderNbr, Equal<Required<ARInvoiceDiscountDetail.orderNbr>>>>>>>(this);

			foreach (ARInvoiceDiscountDetail detail in selectInvoiceDiscounts.Select(orderShipment.OrderType, orderShipment.OrderNbr))
			{
				ARDiscountEngine.DeleteDiscountDetail(this.DiscountDetails.Cache, DiscountDetails, detail);
			}

			TaxAttribute.SetTaxCalc<ARTran.taxCategoryID>(this.Transactions.Cache, null, TaxCalc.ManualCalc);

			if (details != null)
			{
                PXCache cache = this.Caches[typeof(SOShipLine)];
				foreach (PXResult<SOShipLine, SOLine> det in details)
				{
                    SOShipLine shipline = det;
                    SOLine soline = det;
                    //there should be no parent record of SOLineSplit2 type.
                    var insertedshipline = (SOShipLine)cache.Insert(shipline);

                    if (insertedshipline == null)
                        continue;

                    if (insertedshipline.LineType == SOLineType.Inventory)
                    {
                        var ii = (InventoryItem)PXSelectorAttribute.Select<SOShipLine.inventoryID>(cache, insertedshipline);
                        if (ii.StkItem == false && ii.KitItem == true)
                        {
                            insertedshipline.RequireINUpdate = ((SOLineSplit)PXSelectJoin<SOLineSplit,
                                InnerJoin<IN.InventoryItem, 
									On2<SOLineSplit.FK.InventoryItem, 
									And<IN.InventoryItem.stkItem, Equal<True>>>>,
                                Where<SOLineSplit.orderType, Equal<Current<SOLine.orderType>>, And<SOLineSplit.orderNbr, Equal<Current<SOLine.orderNbr>>, And<SOLineSplit.lineNbr, Equal<Current<SOLine.lineNbr>>, And<SOLineSplit.qty, Greater<Zero>>>>>>.SelectSingleBound(this, new object[] { soline })) != null;
                        }
                        else
                        {
                            insertedshipline.RequireINUpdate = ii.StkItem;
                        }
                    }
                    else
                    {
                        insertedshipline.RequireINUpdate = false;
                    }
				}
			}

			//DropShip Receipt/Shipment cannot be invoiced twice thats why we have to be sure that all SOPO links at this point in that Receipt are valid:

			if (orderShipment.ShipmentType == SOShipmentType.DropShip)
			{
			PXSelectBase<POReceiptLine> selectUnlinkedDropShips = new PXSelectJoin<POReceiptLine,
				InnerJoin<PO.POLine, On<PO.POLine.orderType, Equal<POReceiptLine.pOType>, And<PO.POLine.orderNbr, Equal<POReceiptLine.pONbr>, And<PO.POLine.lineNbr, Equal<POReceiptLine.pOLineNbr>>>>,
				LeftJoin<SOLineSplit, On<SOLineSplit.pOType, Equal<POReceiptLine.pOType>, And<SOLineSplit.pONbr, Equal<POReceiptLine.pONbr>, And<SOLineSplit.pOLineNbr, Equal<POReceiptLine.pOLineNbr>>>>>>,
				Where<POReceiptLine.receiptType, Equal<PO.POReceiptType.poreceipt>, 
				And<POReceiptLine.receiptNbr, Equal<Required<POReceiptLine.receiptNbr>>,
				And<SOLineSplit.pOLineNbr, IsNull,
				And<Where<POReceiptLine.lineType, Equal<POLineType.goodsForDropShip>, Or<POReceiptLine.lineType, Equal<POLineType.nonStockForDropShip>>>>>>>>(this);

			var rs = selectUnlinkedDropShips.Select(orderShipment.ShipmentNbr);
			if (rs.Count > 0)
			{
				foreach (POReceiptLine line in rs)
				{
					InventoryItem item = IN.InventoryItem.PK.Find(this, line.InventoryID);
					PXTrace.WriteError(Messages.SOPOLinkIsIvalidInPOOrder, line.PONbr, item?.InventoryCD);
				}

				throw new PXException(Messages.SOPOLinkIsIvalid);
			}
			}

			DateTime? origInvoiceDate = null;
			bool updateINRequired = (orderShipment.ShipmentType == SOShipmentType.DropShip);

			HashSet<ARTran> set = new HashSet<ARTran>(new LSSOLine.Comparer<ARTran>(this));
			Dictionary<int, SOSalesPerTran> dctcommisions = new Dictionary<int, SOSalesPerTran>();

			foreach (PXResult<SOShipLine, SOLine, SOSalesPerTran, ARTran> res in 
				PXSelectJoin<SOShipLine, 
					InnerJoin<SOLine, On<SOLine.orderType, Equal<SOShipLine.origOrderType>, 
						And<SOLine.orderNbr, Equal<SOShipLine.origOrderNbr>, 
						And<SOLine.lineNbr, Equal<SOShipLine.origLineNbr>>>>, 
					LeftJoin<SOSalesPerTran, On<SOLine.orderType, Equal<SOSalesPerTran.orderType>,
						And<SOLine.orderNbr, Equal<SOSalesPerTran.orderNbr>,
						And<SOLine.salesPersonID, Equal<SOSalesPerTran.salespersonID>>>>, 
					LeftJoin<ARTran, On<ARTran.sOShipmentNbr, Equal<SOShipLine.shipmentNbr>, 
						And<ARTran.sOShipmentType, Equal<SOShipLine.shipmentType>, 
						And<ARTran.sOOrderType, Equal<SOShipLine.origOrderType>, 
						And<ARTran.sOOrderNbr, Equal<SOShipLine.origOrderNbr>, 
						And<ARTran.sOOrderLineNbr, Equal<SOShipLine.origLineNbr>>>>>>>>>, 
					Where<SOShipLine.shipmentNbr, Equal<Required<SOShipLine.shipmentNbr>>, 
						And<SOShipLine.origOrderType, Equal<Required<SOShipLine.origOrderType>>, 
						And<SOShipLine.origOrderNbr, Equal<Required<SOShipLine.origOrderNbr>>>>>>
					.Select(this, orderShipment.ShipmentNbr, orderShipment.OrderType, orderShipment.OrderNbr))
            {
                ARTran artran = (ARTran)res;
				SOSalesPerTran sspt = (SOSalesPerTran)res;

				if (sspt != null && sspt.SalespersonID != null && !dctcommisions.ContainsKey(sspt.SalespersonID.Value))
				{
					dctcommisions[sspt.SalespersonID.Value] = sspt;
				}
                if (artran.RefNbr == null || (artran.RefNbr != null && this.Transactions.Cache.GetStatus(artran) == PXEntryStatus.Deleted))

			{
				SOLine orderline = (SOLine)res;
				SOShipLine shipline = (SOShipLine)res;

					//TODO: Temporary solution. Review when AC-80210 is fixed
					if (shipline.ShipmentNbr != null && orderShipment.ShipmentType != SOShipmentType.DropShip && orderShipment.ShipmentNbr != Constants.NoShipmentNbr && (shipline.Confirmed != true || shipline.UnassignedQty != 0))
					{
						throw new PXException(Messages.UnableToProcessUnconfirmedShipment, shipline.ShipmentNbr);
					}

                if (Math.Abs((decimal)shipline.BaseShippedQty) < 0.0000005m && !string.Equals(shipline.ShipmentNbr, Constants.NoShipmentNbr))
                {
                    continue;
                }

				if (origInvoiceDate == null && orderline.InvoiceDate != null)
					origInvoiceDate = orderline.InvoiceDate;

					ARTran newtran = CreateTranFromShipLine(newdoc, soOrderType, orderline.Operation, orderline, ref shipline);
					foreach (ARTran existing in Transactions.Cache.Inserted)
					{
						if (Transactions.Cache.ObjectsEqual<ARTran.sOShipmentNbr, ARTran.sOShipmentType, ARTran.sOOrderType, ARTran.sOOrderNbr, ARTran.sOOrderLineNbr>(newtran, existing))
						{
							Transactions.Cache.RestoreCopy(newtran, existing);
							break;
						}
					}

                    foreach (ARTran existing in Transactions.Cache.Updated)
                    {
                        if (Transactions.Cache.ObjectsEqual<ARTran.sOShipmentNbr, ARTran.sOShipmentType, ARTran.sOOrderType, ARTran.sOOrderNbr, ARTran.sOOrderLineNbr>(newtran, existing))
                        {
                            Transactions.Cache.RestoreCopy(newtran, existing);
                            break;
                        }
                    }

					if (newtran.LineNbr == null)
					{
                        try
                        {
                            cancelUnitPriceCalculation = true;
                            newtran = this.Transactions.Insert(newtran);
							set.Add(newtran);
                        }
						catch (PXSetPropertyException e)
                        {
                            throw new PXErrorContextProcessingException(this, PXParentAttribute.SelectParent(this.Caches[typeof(ARTran)], newtran, typeof(SOLine2)), e);
                        }
                        finally
                        {
                            cancelUnitPriceCalculation = false;
						}
						
						PXNoteAttribute.CopyNoteAndFiles(Caches[typeof(SOLine)], orderline, Caches[typeof(ARTran)], newtran,
							soOrderType.CopyLineNotesToInvoice == true && (soOrderType.CopyLineNotesToInvoiceOnlyNS == false || orderline.LineType == SOLineType.NonInventory),
							soOrderType.CopyLineFilesToInvoice == true && (soOrderType.CopyLineFilesToInvoiceOnlyNS == false || orderline.LineType == SOLineType.NonInventory));
				}
					else
					{
						newtran = this.Transactions.Update(newtran);
						TaxAttribute.Calculate<ARTran.taxCategoryID>(Transactions.Cache, new PXRowUpdatedEventArgs(newtran, null, true));
			}

					if (newtran.RequireINUpdate == true && newtran.Qty != 0m)
                    {
						updateINRequired = true;
				}
						
				}
			}
			PXSelectBase<ARTran> cmd = new PXSelect<ARTran, 
				Where<ARTran.tranType, Equal<Current<ARInvoice.docType>>, 
					And<ARTran.refNbr, Equal<Current<ARInvoice.refNbr>>, 
					And<ARTran.sOOrderType, Equal<Current<SOMiscLine2.orderType>>, 
					And<ARTran.sOOrderNbr, Equal<Current<SOMiscLine2.orderNbr>>, 
					And<ARTran.sOOrderLineNbr, Equal<Current<SOMiscLine2.lineNbr>>>>>>>>(this);
				

			foreach (PXResult<SOMiscLine2, SOSalesPerTran> res in PXSelectJoin<SOMiscLine2, 
																LeftJoin<SOSalesPerTran, On<SOMiscLine2.orderType, Equal<SOSalesPerTran.orderType>,
																	And<SOMiscLine2.orderNbr, Equal<SOSalesPerTran.orderNbr>,
																	And<SOMiscLine2.salesPersonID, Equal<SOSalesPerTran.salespersonID>>>>>,
				Where<SOMiscLine2.orderType, Equal<Required<SOMiscLine2.orderType>>, 
					And<SOMiscLine2.orderNbr, Equal<Required<SOMiscLine2.orderNbr>>, 
																	And<
																		Where2<
																			Where<SOMiscLine2.curyUnbilledAmt, Greater<decimal0>,   //direct billing process with positive amount
																			And<SOMiscLine2.curyLineAmt, Greater<decimal0>>>,  
																		Or2<
																			Where<SOMiscLine2.curyUnbilledAmt, Less<decimal0>,      //billing process with negative amount
																			And<SOMiscLine2.curyLineAmt, Less<decimal0>>>,
																		Or<
																			Where<SOMiscLine2.curyLineAmt, Equal<decimal0>,         //special case with zero line amount, e.g. discount = 100% or unit price=0
																			And<SOMiscLine2.unbilledQty, Greater<decimal0>>>>>>>>>>
																.Select(this, orderShipment.OrderType, orderShipment.OrderNbr))
			{
				SOMiscLine2 orderline = res;
				SOSalesPerTran sspt = res;
				if (sspt != null && sspt.SalespersonID != null && !dctcommisions.ContainsKey(sspt.SalespersonID.Value))
				{
					dctcommisions[sspt.SalespersonID.Value] = sspt;
				}
				if (cmd.View.SelectSingleBound(new object[] { Document.Current, orderline }) == null)
				{
					ARTran newtran = CreateTranFromMiscLine(orderShipment, orderline);
					if (this.Document.Current != null && ((this.Document.Current.CuryLineTotal ?? 0m) + (newtran.CuryTranAmt ?? 0m)) < 0m)
						continue;

					ChangeBalanceSign(newtran, soOrderType, newdoc.DocType, orderline.Operation);
					newtran = this.Transactions.Insert(newtran);
					set.Add(newtran);
					PXNoteAttribute.CopyNoteAndFiles(Caches[typeof(SOMiscLine2)], orderline, Caches[typeof(ARTran)], newtran,
						soOrderType.CopyLineNotesToInvoice, soOrderType.CopyLineFilesToInvoice);
				}
			}

			foreach (SOSalesPerTran sspt in dctcommisions.Values)
			{
				ARSalesPerTran aspt = new ARSalesPerTran();
				aspt.DocType = newdoc.DocType;
				aspt.RefNbr = newdoc.RefNbr;
				aspt.SalespersonID = sspt.SalespersonID;
				commisionlist.Cache.SetDefaultExt<ARSalesPerTran.adjNbr>(aspt);
				commisionlist.Cache.SetDefaultExt<ARSalesPerTran.adjdRefNbr>(aspt);
				commisionlist.Cache.SetDefaultExt<ARSalesPerTran.adjdDocType>(aspt);
				aspt = commisionlist.Locate(aspt);
				if (aspt != null && aspt.CommnPct != sspt.CommnPct)
				{
					aspt.CommnPct = sspt.CommnPct;
					commisionlist.Update(aspt);
				}
			}
			
			if (this.UnattendedMode == true)
			{
				//Total resort and orderNumber assignments:

				List<Tuple<string, ARTran>> invoiceLines = new List<Tuple<string, ARTran>>();
				foreach (PXResult<ARTran> res in Transactions.Select())
				{
					ARTran tran = res;

					string sortkey = string.Format("{0}.{1}.{2:D7}.{3}", tran.SOOrderType, tran.SOOrderNbr, tran.SOOrderSortOrder, tran.SOShipmentNbr);
					invoiceLines.Add(new Tuple<string, ARTran>(sortkey, tran));
				}

				invoiceLines.Sort((x, y) => x.Item1.CompareTo(y.Item1));

				for (int i = 0; i < invoiceLines.Count; i++)
				{
					if (invoiceLines[i].Item2.SortOrder != i + 1)
					{
					invoiceLines[i].Item2.SortOrder = i + 1;
						if (this.Transactions.Cache.GetStatus(invoiceLines[i].Item2) != PXEntryStatus.Inserted)
						{
							this.Transactions.Cache.SetStatus(invoiceLines[i].Item2, PXEntryStatus.Updated);
				}
			}
				}
			}
			else
			{
				//Appending to the end sorted soordershipment transactions.

				int lastSortOrderNbr = 0;
				List<Tuple<string, ARTran>> tail = new List<Tuple<string, ARTran>>();
				foreach (PXResult<ARTran> res in Transactions.Select())
				{
					ARTran tran = res;

					if (set.Contains(tran))
					{
						string sortkey = string.Format("{0}.{1:D7}.{2}", tran.SOOrderNbr, tran.SOOrderSortOrder, tran.SOShipmentNbr);
						tail.Add(new Tuple<string, ARTran>(sortkey, tran));
					}
					else
					{
						lastSortOrderNbr = Math.Max(lastSortOrderNbr, tran.SortOrder.GetValueOrDefault());
					}
				}

				tail.Sort((x, y) => x.Item1.CompareTo(y.Item1));

				for (int i = 0; i < tail.Count; i++)
				{
					lastSortOrderNbr++;
					if (tail[i].Item2.SortOrder != lastSortOrderNbr)
					{
					tail[i].Item2.SortOrder = lastSortOrderNbr;

						if (this.Transactions.Cache.GetStatus(tail[i].Item2) != PXEntryStatus.Inserted)
						{
							this.Transactions.Cache.SetStatus(tail[i].Item2, PXEntryStatus.Updated);
						}
					}
				}
			}

			SODocument.Current.BillAddressID = soOrder.BillAddressID;
			SODocument.Current.BillContactID = soOrder.BillContactID;

			SODocument.Current.ShipAddressID = orderShipment.ShipAddressID;
			SODocument.Current.ShipContactID = orderShipment.ShipContactID;

			SODocument.Current.IsCCCaptured = soOrder.IsCCCaptured;
			SODocument.Current.IsCCCaptureFailed = soOrder.IsCCCaptureFailed;
			SODocument.Current.PaymentProjectID = PM.ProjectDefaultAttribute.NonProject();
			
			if (soOrder.IsCCCaptured == true)
			{
				SODocument.Current.CuryCCCapturedAmt = soOrder.CuryCCCapturedAmt;
				SODocument.Current.CCCapturedAmt = soOrder.CCCapturedAmt;
			}

			SODocument.Current.RefTranExtNbr = soOrder.RefTranExtNbr;
			SODocument.Current.CreateINDoc |= (updateINRequired && orderShipment.InvtRefNbr == null);

			SOOrderShipment shipment = PXCache<SOOrderShipment>.CreateCopy(orderShipment);
			shipment.InvoiceType = SODocument.Current.DocType;
			shipment.InvoiceNbr = SODocument.Current.RefNbr;
			if (string.Equals(shipment.ShipmentNbr, Constants.NoShipmentNbr))
			{
				shipment.ShippingRefNoteID = SODocument.Current.NoteID;
			}
			shipment.CreateINDoc = updateINRequired;

			SOFreightDetail fd = FillFreightDetails(soOrder, shipment);

			shipment = shipmentlist.Update(shipment);

            if (string.Equals(shipment.ShipmentNbr, Constants.NoShipmentNbr))
            {
                SOOrder cached = soorder.Locate(soOrder);
                if (cached != null)
                {
                    if ((cached.Behavior == SOBehavior.SO || cached.Behavior == SOBehavior.RM) && cached.OpenLineCntr == 0)
                    {
                        cached.Completed = true;
                        cached.Status = SOOrderStatus.Completed;
                    }
                    cached.ShipmentCntr++;
                    soorder.Update(cached);
                }
            }

            /*In case Discounts were not recalculated add prorated discounts */
            if (soOrderType.RecalculateDiscOnPartialShipment != true)
            {
				PXCache cache = this.Caches[typeof(SOLine)];
				PXSelectBase<SOLine> transactions = new PXSelect<SOLine, Where<SOLine.orderType, Equal<Current<SOOrder.orderType>>, And<SOLine.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>(this);
				PXSelectBase<SOOrderDiscountDetail> discountdetail = new PXSelect<SOOrderDiscountDetail, 
				Where<SOOrderDiscountDetail.orderType, Equal<Current<SOOrder.orderType>>, 
				And<SOOrderDiscountDetail.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>(this);

				bool fullOrderInvoicing = false;
				if (PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>() && soOrderType.RequireShipping == true && soOrder.OpenLineCntr == 0)
				{
					if (transactions.Select().AsEnumerable().RowCast<SOLine>()
						.All(l => l.ShippedQty == l.OrderQty || l.LineType == SOLineType.MiscCharge))
					{
						SOOrderShipment notInvoicedOrderShipment = PXSelect<SOOrderShipment,
							Where<SOOrderShipment.orderType, Equal<Current<SOOrder.orderType>>, And<SOOrderShipment.orderNbr, Equal<Current<SOOrder.orderNbr>>,
								And<Where<SOOrderShipment.invoiceNbr, IsNull,
									Or<SOOrderShipment.invoiceType, NotEqual<Current<ARInvoice.docType>>, Or<SOOrderShipment.invoiceNbr, NotEqual<Current<ARInvoice.refNbr>>>>>>>>>
							.SelectWindowed(this, 0, 1);
						fullOrderInvoicing = (notInvoicedOrderShipment == null);
					}
				}
				decimal? defaultRate = 1m;
				if (soOrder.LineTotal > 0m)
					defaultRate = shipment.LineTotal / soOrder.LineTotal;

				TwoWayLookup<SOOrderDiscountDetail, SOLine> discountCodesWithApplicableSOLines = DiscountEngineProvider.GetEngineFor<SOLine, SOOrderDiscountDetail>()
					.GetListOfLinksBetweenDiscountsAndDocumentLines(cache, transactions, discountdetail);

				TwoWayLookup<ARInvoiceDiscountDetail, ARTran> discountCodesWithApplicableARLines = new TwoWayLookup<ARInvoiceDiscountDetail, ARTran>(leftComparer: new ARInvoiceDiscountDetail.ARInvoiceDiscountDetailComparer());

				foreach (SOOrderDiscountDetail docGroupDisc in discountCodesWithApplicableSOLines.LeftValues)
                {
	                var dd = new ARInvoiceDiscountDetail
							{
								SkipDiscount = docGroupDisc.SkipDiscount,
								Type = docGroupDisc.Type,
								DiscountID = docGroupDisc.DiscountID,
								DiscountSequenceID = docGroupDisc.DiscountSequenceID,
								OrderType = PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>() ? docGroupDisc.OrderType : null,
								OrderNbr = PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>() ? docGroupDisc.OrderNbr : null,
								DocType = newdoc.DocType,
								RefNbr = newdoc.RefNbr,
								IsManual = docGroupDisc.IsManual,
								DiscountPct = docGroupDisc.DiscountPct,
								FreeItemID = docGroupDisc.FreeItemID,
								FreeItemQty = docGroupDisc.FreeItemQty,
								ExtDiscCode = docGroupDisc.ExtDiscCode,
								Description = docGroupDisc.Description
							};

					decimal? rate = defaultRate;
					decimal invoicedCuryGroupAmt = 0m;
					decimal invoicedMiscAmt = 0m;
					foreach (SOLine soLine in discountCodesWithApplicableSOLines.RightsFor(docGroupDisc))
					{
						foreach (ARTran tran in Transactions.Select())
						{
							if ((soLine.OrderType == tran.SOOrderType && soLine.OrderNbr == tran.SOOrderNbr && soLine.LineNbr == tran.SOOrderLineNbr) ||
								!PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>())
                                        {
								if (docGroupDisc.Type == DiscountType.Group)
									invoicedCuryGroupAmt += (tran.CuryTranAmt ?? 0m);
								else if (tran.LineType == SOLineType.MiscCharge && tran.OrigDocumentDiscountRate == 1m)
                                            {
									invoicedMiscAmt += (tran.TranAmt ?? 0m);
                                            }

								discountCodesWithApplicableARLines.Link(dd, tran);
							}
						}
					}

					bool fullOrderDiscAllocation = (fullOrderInvoicing && docGroupDisc.Type == DiscountType.Document);
					if (fullOrderDiscAllocation)
					{
						rate = 1m;
					}
					else if (docGroupDisc.CuryDiscountableAmt > 0m)
					{
						if (docGroupDisc.Type == DiscountType.Group)
							rate = invoicedCuryGroupAmt / docGroupDisc.CuryDiscountableAmt;
						else if (soOrder.LineTotal != 0m || soOrder.MiscTot != 0)
							rate = (shipment.LineTotal + invoicedMiscAmt) / (soOrder.LineTotal + soOrder.MiscTot);
                    }

                    ARInvoiceDiscountDetail located = DiscountDetails.Locate(dd);
                    //RecordID prevents Locate() from work as intended. To review.
                    if (located == null)
                    {
						List<ARInvoiceDiscountDetail> discountDetails = new List<ARInvoiceDiscountDetail>();

						//TODO: review this part
						if (PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>())
						{
                        foreach (ARInvoiceDiscountDetail detail in DiscountDetails.Cache.Cached)
                        {
								discountDetails.Add(detail);
							}
						}
						else
						{
							foreach (ARInvoiceDiscountDetail detail in DiscountDetails.Select())
							{
								discountDetails.Add(detail);
							}
						}

						foreach (ARInvoiceDiscountDetail detail in discountDetails)
						{
                            if (detail.DiscountID == dd.DiscountID && detail.DiscountSequenceID == dd.DiscountSequenceID && detail.OrderType == dd.OrderType 
                                && detail.OrderNbr == dd.OrderNbr && detail.DocType == dd.DocType && detail.RefNbr == dd.RefNbr && detail.Type == dd.Type)
                                located = detail;
                        }
                    }
                    if (located != null)
                    {
                        if (docGroupDisc.Type == DiscountType.Group || fullOrderDiscAllocation)
                        {
                            located.DiscountAmt = docGroupDisc.DiscountAmt * rate;
                            located.CuryDiscountAmt = docGroupDisc.CuryDiscountAmt * rate;
                            located.DiscountableAmt = docGroupDisc.DiscountableAmt * rate;
                            located.CuryDiscountableAmt = docGroupDisc.CuryDiscountableAmt * rate;
                            located.DiscountableQty = docGroupDisc.DiscountableQty * rate;
                        }
                        else
                        {
                            located.DiscountAmt += docGroupDisc.DiscountAmt * rate;
                            located.CuryDiscountAmt += docGroupDisc.CuryDiscountAmt * rate;
                            located.DiscountableAmt += docGroupDisc.DiscountableAmt * rate;
                            located.CuryDiscountableAmt += docGroupDisc.CuryDiscountableAmt * rate;
                            located.DiscountableQty += docGroupDisc.DiscountableQty * rate;
                        }
						if (DiscountDetails.Cache.GetStatus(located) == PXEntryStatus.Deleted)
							located = ARDiscountEngine.InsertDiscountDetail(this.DiscountDetails.Cache, DiscountDetails, located);
						else
							located = ARDiscountEngine.UpdateDiscountDetail(this.DiscountDetails.Cache, DiscountDetails, located);
                    }
                    else
                    {
                        dd.DiscountAmt = docGroupDisc.DiscountAmt * rate;
                        dd.CuryDiscountAmt = docGroupDisc.CuryDiscountAmt * rate;
                        dd.DiscountableAmt = docGroupDisc.DiscountableAmt * rate;
                        dd.CuryDiscountableAmt = docGroupDisc.CuryDiscountableAmt * rate;
                        dd.DiscountableQty = docGroupDisc.DiscountableQty * rate;

						located = ARDiscountEngine.InsertDiscountDetail(this.DiscountDetails.Cache, DiscountDetails, dd);
                    }

					ARInvoiceDiscountDetail.ARInvoiceDiscountDetailComparer discountDetailComparer = new ARInvoiceDiscountDetail.ARInvoiceDiscountDetailComparer();
					foreach (ARInvoiceDiscountDetail newDiscount in discountCodesWithApplicableARLines.LeftValues)
					{
						if (discountDetailComparer.Equals(newDiscount, located))
						{
							newDiscount.DiscountAmt = located.DiscountAmt;
							newDiscount.CuryDiscountableAmt = located.CuryDiscountableAmt;
							newDiscount.CuryDiscountAmt = located.CuryDiscountAmt;
							newDiscount.DiscountableQty = located.DiscountableQty;
							newDiscount.DiscountableAmt = located.DiscountableAmt;
							newDiscount.IsOrigDocDiscount = located.IsOrigDocDiscount;
							newDiscount.LineNbr = located.LineNbr;
						}
					}
                }

				if (PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>())
				{
                RecalculateTotalDiscount();
				}

                PXSelectBase<ARTran> orderLinesSelect = new PXSelectJoin<ARTran, LeftJoin<SOLine,
                On<SOLine.orderType, Equal<ARTran.sOOrderType>,
                And<SOLine.orderNbr, Equal<ARTran.sOOrderNbr>,
                And<SOLine.lineNbr, Equal<ARTran.sOOrderLineNbr>>>>>,
                Where<ARTran.tranType, Equal<Current<ARInvoice.docType>>,
                And<ARTran.refNbr, Equal<Current<ARInvoice.refNbr>>,
				And<ARTran.sOOrderType, Equal<Current<SOOrder.orderType>>,
				And<ARTran.sOOrderNbr, Equal<Current<SOOrder.orderNbr>>>>>>,
                OrderBy<Asc<ARTran.tranType, Asc<ARTran.refNbr, Asc<ARTran.lineNbr>>>>>(this);

                PXSelectBase<ARInvoiceDiscountDetail> orderDiscountDetailsSelect = new PXSelect<ARInvoiceDiscountDetail, Where<ARInvoiceDiscountDetail.docType, Equal<Current<SOInvoice.docType>>, And<ARInvoiceDiscountDetail.refNbr, Equal<Current<SOInvoice.refNbr>>,
                    And<ARInvoiceDiscountDetail.orderType, Equal<Current<SOOrder.orderType>>, And<ARInvoiceDiscountDetail.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>>>(this);

				ARDiscountEngine.CalculateGroupDiscountRate(Transactions.Cache, orderLinesSelect, null, discountCodesWithApplicableARLines, false, forceFormulaCalculation: true);

				ARDiscountEngine.CalculateDocumentDiscountRate(Transactions.Cache, discountCodesWithApplicableARLines, null, documentDetails: Transactions, forceFormulaCalculation: true);

				if (!PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>())
				{
					RecalculateTotalDiscount();
				}
            }
            else
            {
                //Recalculate all discounts
                foreach (ARTran tran in Transactions.Select())
                {
                    RecalculateDiscounts(this.Transactions.Cache, tran);
                    this.Transactions.Update(tran);
                }
            }

			AddOrderTaxes(shipment);

			if (!IsExternalTax(Document.Current.TaxZoneID))
			{
				SOShipment soShipment = GetShipment(shipment);
				if (soShipment != null && soShipment.TaxCategoryID != soOrder.FreightTaxCategoryID)
				{
					// if freight tax category is changed on the shipment we need to recalculate freight tax
					// because the tax added in the shipment may be absent in the sales order
					TaxAttribute.SetTaxCalc<ARTran.taxCategoryID>(this.Transactions.Cache, null, TaxCalc.ManualLineCalc);
					try
					{
						fd.TaxCategoryID = soShipment.TaxCategoryID;
						FreightDetails.Update(fd);
					}
					finally
					{
						TaxAttribute.SetTaxCalc<ARTran.taxCategoryID>(this.Transactions.Cache, null, TaxCalc.ManualCalc);
					}
				}
			}

			decimal? CuryApplAmt = 0m;
			bool Calculated = false;

            foreach (SOAdjust soadj in PXSelectJoin<SOAdjust, InnerJoin<AR.ARPayment, On<AR.ARPayment.docType, Equal<SOAdjust.adjgDocType>, And<AR.ARPayment.refNbr, Equal<SOAdjust.adjgRefNbr>>>>, Where<SOAdjust.adjdOrderType, Equal<Required<SOAdjust.adjdOrderType>>, And<SOAdjust.adjdOrderNbr, Equal<Required<SOAdjust.adjdOrderNbr>>, And<AR.ARPayment.openDoc, Equal<True>>>>>.Select(this, orderShipment.OrderType, orderShipment.OrderNbr))
            {
                ARAdjust2 prev_adj = null;
                bool found = false;

                PXResultset<ARAdjust2> resultset = null;

                try
                {
                    TransferApplicationFromSalesOrder = true;
                    resultset = Adjustments.Select();
                }
                finally
                {
                    TransferApplicationFromSalesOrder = false;
                }

                foreach (ARAdjust2 adj in resultset)
                {
                    if (Calculated)
                    {
                        CuryApplAmt -= adj.CuryAdjdAmt;
                    }

                    if (string.Equals(adj.AdjgDocType, soadj.AdjgDocType) && string.Equals(adj.AdjgRefNbr, soadj.AdjgRefNbr))
                    {
                        if (soadj.CuryAdjdAmt > 0m)
                        {
                            ARAdjust2 copy = PXCache<ARAdjust2>.CreateCopy(adj);
                            copy.CuryAdjdAmt += (soadj.CuryAdjdAmt > adj.CuryDocBal) ? adj.CuryDocBal : soadj.CuryAdjdAmt;
                            copy.CuryAdjdOrigAmt = copy.CuryAdjdAmt;
                            copy.AdjdOrderType = soadj.AdjdOrderType;
                            copy.AdjdOrderNbr = soadj.AdjdOrderNbr;
                            prev_adj = Adjustments.Update(copy);
                        }

                        found = true;

                        if (Calculated)
                        {
                            CuryApplAmt += adj.CuryAdjdAmt;
                            break;
                        }
                    }

                    CuryApplAmt += adj.CuryAdjdAmt;
                }

                //if soadjust is not available in adjustments mark as billed
                if (!found)
                {
                    /*
                        soadj.Billed = true;
                        soadjustments.Cache.SetStatus(soadj, PXEntryStatus.Updated);
                    */
                }

                Calculated = true;

                if (!IsExternalTax(Document.Current.TaxZoneID) && prev_adj != null)
                {
                    prev_adj = PXCache<ARAdjust2>.CreateCopy(prev_adj);

                    decimal curyDocBalance = (Document.Current.CuryDocBal ?? 0m) - (Document.Current.CuryOrigDiscAmt ?? 0m);
                    decimal curyApplDifference = (CuryApplAmt ?? 0m) - curyDocBalance;

                    if (CuryApplAmt > curyDocBalance)
                    {
                        if (prev_adj.CuryAdjdAmt > curyApplDifference)
                        {
                            prev_adj.CuryAdjdAmt -= curyApplDifference;
                            CuryApplAmt = curyDocBalance;
                        }
                        else
                        {
                            CuryApplAmt -= prev_adj.CuryAdjdAmt;
                            prev_adj.CuryAdjdAmt = 0m;
                        }
                        prev_adj = Adjustments.Update(prev_adj);
                    }
                }
            }

			newdoc = (ARInvoice)Document.Cache.CreateCopy(Document.Current);
			newdoc.OrigDocDate = origInvoiceDate;
			SOInvoice socopy = (SOInvoice)SODocument.Cache.CreateCopy(SODocument.Current);
            
			PXFormulaAttribute.CalcAggregate<ARAdjust2.curyAdjdAmt>(Adjustments.Cache, SODocument.Current, false);

			Document.Cache.RaiseFieldUpdated<ARInvoice.curyPaymentTotal>(Document.Current, null);
			PXDBCurrencyAttribute.CalcBaseValues<ARInvoice.curyPaymentTotal>(Document.Cache, Document.Current);

            Document.Cache.RaiseFieldUpdated<ARInvoice.curyBalanceWOTotal>(Document.Current, null);
            PXDBCurrencyAttribute.CalcBaseValues<ARInvoice.curyBalanceWOTotal>(Document.Cache, Document.Current);

            SODocument.Cache.RaiseRowUpdated(SODocument.Current, socopy);

			List<string> ordersdistinct = new List<string>();
			foreach (SOOrderShipment shipments in PXSelect<SOOrderShipment, Where<SOOrderShipment.invoiceType, Equal<Current<ARInvoice.docType>>, And<SOOrderShipment.invoiceNbr, Equal<Current<ARInvoice.refNbr>>>>>.Select(this))
			{
				string key = string.Format("{0}|{1}", shipments.OrderType, shipments.OrderNbr);
				if (!ordersdistinct.Contains(key))
				{
					ordersdistinct.Add(key);
				}

				if (list != null && ordersdistinct.Count > 1)
				{
					newdoc.InvoiceNbr = null;
					newdoc.SalesPersonID = null;
					newdoc.DocDesc = null;
					break;
				}

				#region Update FreeItemQty for DiscountDetails based on shipments
				
				PXSelectBase<SOShipmentDiscountDetail> selectShipmentDiscounts = new PXSelect<SOShipmentDiscountDetail,
						Where<SOShipmentDiscountDetail.orderType, Equal<Required<SOShipmentDiscountDetail.orderType>>,
						And<SOShipmentDiscountDetail.orderNbr, Equal<Required<SOShipmentDiscountDetail.orderNbr>>,
						And<SOShipmentDiscountDetail.shipmentNbr, Equal<Required<SOShipmentDiscountDetail.shipmentNbr>>>>>>(this);

				foreach (SOShipmentDiscountDetail sdd in selectShipmentDiscounts.Select(shipments.OrderType, shipments.OrderNbr, shipments.ShipmentNbr))
				{
                    bool discountDetailLineExist = false;

                    foreach (ARInvoiceDiscountDetail idd in DiscountDetails.Select())
                    {
                        if (idd.DocType == newdoc.DocType && idd.RefNbr == newdoc.RefNbr
                            && idd.OrderType == shipments.OrderType && idd.OrderNbr == shipments.OrderNbr
                            && idd.DiscountID == sdd.DiscountID && idd.DiscountSequenceID == sdd.DiscountSequenceID)
					{
                            discountDetailLineExist = true;
						if (idd.FreeItemID == null)
						{
							idd.FreeItemID = sdd.FreeItemID;
							idd.FreeItemQty = sdd.FreeItemQty;
						}
						else
							idd.FreeItemQty = sdd.FreeItemQty;
					}
                    }

                    if (!discountDetailLineExist)
					{
						var idd = new ARInvoiceDiscountDetail
						{
									Type = DiscountType.Group,
									DocType = newdoc.DocType,
									RefNbr = newdoc.RefNbr,
									OrderType = sdd.OrderType,
									OrderNbr = sdd.OrderNbr,
									DiscountID = sdd.DiscountID,
									DiscountSequenceID = sdd.DiscountSequenceID,
									FreeItemID = sdd.FreeItemID,
									FreeItemQty = sdd.FreeItemQty
								};

						ARDiscountEngine.InsertDiscountDetail(this.DiscountDetails.Cache, DiscountDetails, idd);
					}
				} 

				#endregion
			}

			if (newdoc.CuryDocBal >= 0)
				newdoc.Hold = prevHoldState;

			this.Document.Update(newdoc);
			SOOpenPeriodAttribute.SetValidatePeriod<ARInvoice.finPeriodID>(Document.Cache, null, PeriodValidation.DefaultSelectUpdate);

			{
				SOOrder cached = soorder.Locate(soOrder);
				if (cached != null && cached.Behavior != SOBehavior.RM && cached.Status == SOOrderStatus.Open && cached.Approved == true)
				{
					cached.Status = SOOrderStatus.Invoiced;
					soorder.Update(cached);
				}
			}

			if (list != null)
			{
				if (Transactions.Search<ARTran.sOOrderType, ARTran.sOOrderNbr, ARTran.sOShipmentType, ARTran.sOShipmentNbr>(shipment.OrderType, shipment.OrderNbr, shipment.ShipmentType, shipment.ShipmentNbr).Count > 0)
				{
                    try
                    {
                        this.Document.Current.ApplyPaymentWhenTaxAvailable = true;
                        this.Save.Press();

                        if (soOrderType.AutoWriteOff == true)
                        {
                            AutoWriteOffBalance(customer);
                        }

                    }
                    finally
                    {
                        this.Document.Current.ApplyPaymentWhenTaxAvailable = false;
                    }


					if (list.Find(this.Document.Current) == null)
					{
						list.Add(this.Document.Current, this.SODocument.Current);
					}
				}
				else
				{
					this.Clear();
				}
			}
            CustomerCreditHelper.RemovePreUpdatedEvent(typeof(ARInvoice), ApprovedBalanceCollector);
			TaxAttribute.SetTaxCalc<ARTran.taxCategoryID>(this.Transactions.Cache, null, TaxCalc.ManualLineCalc);
		}

        /// <summary>
        /// Automatically writes-off the difference between original Amount Paid in Sales Order and Amount Paid in SO Invoice 
        /// </summary>
        /// <param name="customer"></param>
        private void AutoWriteOffBalance(Customer customer)
        {
            bool adjustmentModified = false;

            foreach (ARAdjust2 adjustment in Adjustments_Inv.Select())
            {
                decimal applDifference = (adjustment.CuryAdjdAmt ?? 0m) - (adjustment.CuryAdjdOrigAmt ?? 0m);
                if (customer != null && customer.SmallBalanceAllow == true && applDifference != 0m && Math.Abs(customer.SmallBalanceLimit ?? 0m) >= Math.Abs(applDifference))
                {
                    ARAdjust2 upd_adj = PXCache<ARAdjust2>.CreateCopy(adjustment);
                    upd_adj.CuryAdjdAmt = upd_adj.CuryAdjdOrigAmt;
                    upd_adj.CuryAdjdWOAmt = applDifference;
                    upd_adj = Adjustments.Update(upd_adj);
                    adjustmentModified = true;
                }
            }

            if (this.Document.Current.CuryApplicationBalance != 0m)
            {
                ARAdjust2 firstAdjustment = Adjustments_Inv.SelectSingle();
                if (firstAdjustment != null)
                {
                    ARAdjust2 upd_adj = PXCache<ARAdjust2>.CreateCopy(firstAdjustment);

                    decimal applDifference = this.Document.Current.CuryApplicationBalance ?? 0m;

                    if (customer != null && customer.SmallBalanceAllow == true && Math.Abs(customer.SmallBalanceLimit ?? 0m) >= Math.Abs(applDifference))
                    {
                        upd_adj.CuryAdjdWOAmt = -applDifference;
                        upd_adj = Adjustments.Update(upd_adj);
                        adjustmentModified = true;
                    }
                }
            }
            if (adjustmentModified)
                this.Save.Press();
        }

        public virtual void AddOrderTaxes(SOOrderShipment orderShipment)
		{
			if (Document.Current == null || IsExternalTax(Document.Current.TaxZoneID))
				return;

			// scope of the taxes recalculation is limited with the current SOOrderShipment
			// necessary to set proper current because the method SOInvoiceTaxAttribute.FilterParent depends on it
			shipmentlist.Current = orderShipment;

			foreach (PXResult<SOTaxTran, Tax> res in PXSelectJoin<SOTaxTran,
				InnerJoin<Tax, On<SOTaxTran.taxID, Equal<Tax.taxID>>>,
				Where<SOTaxTran.orderType, Equal<Required<SOTaxTran.orderType>>, And<SOTaxTran.orderNbr, Equal<Required<SOTaxTran.orderNbr>>>>>
				.Select(this, orderShipment.OrderType, orderShipment.OrderNbr))
			{
				SOTaxTran tax = (SOTaxTran)res;
				ARTaxTran newtax = new ARTaxTran();
				newtax.Module = BatchModule.AR;
				Taxes.Cache.SetDefaultExt<ARTaxTran.origTranType>(newtax);
				Taxes.Cache.SetDefaultExt<ARTaxTran.origRefNbr>(newtax);
				Taxes.Cache.SetDefaultExt<ARTaxTran.lineRefNbr>(newtax);
				newtax.TranType = Document.Current.DocType;
				newtax.RefNbr = Document.Current.RefNbr;
				newtax.TaxID = tax.TaxID;
				newtax.TaxRate = 0m;

				foreach (ARTaxTran existingTaxTran in this.Taxes.Cache.Cached.RowCast<ARTaxTran>().Where(a =>
					!this.Taxes.Cache.GetStatus(a).IsIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted)
					&& this.Taxes.Cache.ObjectsEqual<ARTaxTran.module, ARTaxTran.refNbr, ARTaxTran.tranType, ARTaxTran.taxID>(newtax, a)))
				{
					this.Taxes.Delete(existingTaxTran);
				}

				newtax = this.Taxes.Insert(newtax);
			}
		}

		public virtual DateTime GetOrderInvoiceDate(DateTime invoiceDate, SOOrder soOrder, SOOrderShipment orderShipment)
		{
			return (sosetup.Current.UseShipDateForInvoiceDate == true && soOrder.InvoiceDate == null ? orderShipment.ShipDate : soOrder.InvoiceDate) ?? invoiceDate;
		}

		public virtual bool IsCreditCardProcessing(SOOrder soOrder)
		{
			return PXSelectReadonly<CCProcTran,
				Where<CCProcTran.origDocType, Equal<Required<CCProcTran.origDocType>>, And<CCProcTran.origRefNbr, Equal<Required<CCProcTran.origRefNbr>>,
					And<CCProcTran.refNbr, IsNull>>>>
				.SelectWindowed(this, 0, 1, soOrder.OrderType, soOrder.OrderNbr).Count > 0;
		}

		public virtual ARInvoice FindOrCreateInvoice(DateTime orderInvoiceDate, PXResult<SOOrderShipment, SOOrder, CurrencyInfo, SOAddress, SOContact> order, DocumentList<ARInvoice, SOInvoice> list)
		{
			SOOrderShipment orderShipment = order;
			SOOrder soOrder = order;
			SOOrderType soOrderType = SOOrderType.PK.Find(this, soOrder.OrderType);
			string orderTermsID = soOrderType.ARDocType == ARDocType.CreditMemo ? null : soOrder.TermsID;

			if (orderShipment.BillShipmentSeparately == true)
			{
				ARInvoice newdoc = list.Find<ARInvoice.hidden, ARInvoice.hiddenByShipment, ARInvoice.hiddenShipmentType, ARInvoice.hiddenShipmentNbr>(
					false, true, orderShipment.ShipmentType, orderShipment.ShipmentNbr);
				return newdoc ?? new ARInvoice()
				{
					HiddenShipmentType = orderShipment.ShipmentType,
					HiddenShipmentNbr = orderShipment.ShipmentNbr,
					HiddenByShipment = true
				};
			}
			else if (soOrder.PaymentCntr != 0 || soOrder.BillSeparately == true || IsCreditCardProcessing(soOrder))
			{
				ARInvoice newdoc = list.Find<ARInvoice.hidden, ARInvoice.hiddenByShipment, ARInvoice.hiddenOrderType, ARInvoice.hiddenOrderNbr>(
					true, false, soOrder.OrderType, soOrder.OrderNbr);
				return newdoc ?? new ARInvoice()
				{
					HiddenOrderType = soOrder.OrderType,
					HiddenOrderNbr = soOrder.OrderNbr,
					Hidden = true
				};
			}
			else
			{
				ARInvoice newdoc;
				if (soOrder.PaymentMethodID == null && soOrder.CashAccountID == null)
				{
					newdoc = list.Find<ARInvoice.hidden, ARInvoice.hiddenByShipment, ARInvoice.docType, ARInvoice.docDate, ARInvoice.branchID, ARInvoice.customerID, ARInvoice.customerLocationID, SOInvoice.billAddressID, SOInvoice.billContactID, ARInvoice.taxZoneID, ARInvoice.curyID, ARInvoice.termsID, SOInvoice.extRefNbr>(
							false, false, soOrderType.ARDocType, orderInvoiceDate, soOrder.BranchID, soOrder.CustomerID, soOrder.CustomerLocationID, soOrder.BillAddressID, soOrder.BillContactID, soOrder.TaxZoneID, soOrder.CuryID, orderTermsID, soOrder.ExtRefNbr);
				}
				else if (soOrder.CashAccountID == null)
				{
					newdoc = list.Find<ARInvoice.hidden, ARInvoice.hiddenByShipment, ARInvoice.docType, ARInvoice.docDate, ARInvoice.branchID, ARInvoice.customerID, ARInvoice.customerLocationID, SOInvoice.billAddressID, SOInvoice.billContactID, ARInvoice.taxZoneID, ARInvoice.curyID, ARInvoice.termsID, SOInvoice.extRefNbr, SOInvoice.pMInstanceID>(
							false, false, soOrderType.ARDocType, orderInvoiceDate, soOrder.BranchID, soOrder.CustomerID, soOrder.CustomerLocationID, soOrder.BillAddressID, soOrder.BillContactID, soOrder.TaxZoneID, soOrder.CuryID, orderTermsID, soOrder.ExtRefNbr, soOrder.PMInstanceID);
				}
				else
				{
					newdoc = list.Find<ARInvoice.hidden, ARInvoice.hiddenByShipment, ARInvoice.docType, ARInvoice.docDate, ARInvoice.branchID, ARInvoice.customerID, ARInvoice.customerLocationID, SOInvoice.billAddressID, SOInvoice.billContactID, ARInvoice.taxZoneID, ARInvoice.curyID, ARInvoice.termsID, SOInvoice.extRefNbr, SOInvoice.pMInstanceID, SOInvoice.cashAccountID>(
							false, false, soOrderType.ARDocType, orderInvoiceDate, soOrder.BranchID, soOrder.CustomerID, soOrder.CustomerLocationID, soOrder.BillAddressID, soOrder.BillContactID, soOrder.TaxZoneID, soOrder.CuryID, orderTermsID, soOrder.ExtRefNbr, soOrder.PMInstanceID, soOrder.CashAccountID);
				}
				return newdoc ?? new ARInvoice();
			}
		}

		public virtual ARTran CreateTranFromMiscLine(SOOrderShipment orderShipment, SOMiscLine2 orderline)
		{
			ARTran newtran = new ARTran();
			newtran.BranchID = orderline.BranchID;
			newtran.AccountID = orderline.SalesAcctID;
			newtran.SubID = orderline.SalesSubID;
			newtran.SOOrderType = orderline.OrderType;
			newtran.SOOrderNbr = orderline.OrderNbr;
			newtran.SOOrderLineNbr = orderline.LineNbr;
			newtran.SOOrderLineOperation = orderline.Operation;
			newtran.SOOrderSortOrder = orderline.SortOrder;
			newtran.SOShipmentNbr = orderShipment.ShipmentNbr;
			newtran.SOShipmentType = orderShipment.ShipmentType;
			newtran.SOShipmentLineNbr = null;

			newtran.LineType = SOLineType.MiscCharge;
			newtran.InventoryID = orderline.InventoryID;
			newtran.TaskID = orderline.TaskID;
			newtran.CommitmentID = orderline.CommitmentID;
			newtran.SalesPersonID = orderline.SalesPersonID;
			newtran.Commissionable = orderline.Commissionable;
			newtran.UOM = orderline.UOM;
			newtran.Qty = orderline.UnbilledQty;
			newtran.BaseQty = orderline.BaseUnbilledQty;
			newtran.CuryUnitPrice = orderline.CuryUnitPrice;
			newtran.CuryExtPrice = orderline.CuryExtPrice;
			newtran.CuryDiscAmt = orderline.CuryDiscAmt;
			newtran.CuryTranAmt = orderline.CuryUnbilledAmt;
			newtran.TranDesc = orderline.TranDesc;
			newtran.TaxCategoryID = orderline.TaxCategoryID;
			newtran.DiscPct = orderline.DiscPct;

			newtran.IsFree = orderline.IsFree;
			newtran.ManualPrice = true;
			newtran.ManualDisc = orderline.ManualDisc == true || orderline.IsFree == true;
			newtran.FreezeManualDisc = true;

			newtran.DiscountID = orderline.DiscountID;
			newtran.DiscountSequenceID = orderline.DiscountSequenceID;

			newtran.DRTermStartDate = orderline.DRTermStartDate;
			newtran.DRTermEndDate = orderline.DRTermEndDate;
			newtran.CuryUnitPriceDR = orderline.CuryUnitPriceDR;
			newtran.DiscPctDR = orderline.DiscPctDR;
			newtran.DefScheduleID = orderline.DefScheduleID;
			newtran.SortOrder = orderline.SortOrder;
			newtran.OrigInvoiceType = orderline.InvoiceType;
			newtran.OrigInvoiceNbr = orderline.InvoiceNbr;
			newtran.OrigInvoiceLineNbr = orderline.InvoiceLineNbr;
			newtran.OrigInvoiceDate = orderline.InvoiceDate;

			return newtran;
		}

		public virtual ARTran CreateTranFromShipLine(ARInvoice newdoc, SOOrderType ordertype, string operation, SOLine orderline, ref SOShipLine shipline)
		{
			ARTran newtran = new ARTran();
			newtran.SOOrderType = shipline.OrigOrderType;
			newtran.SOOrderNbr = shipline.OrigOrderNbr;
			newtran.SOOrderLineNbr = shipline.OrigLineNbr;
			newtran.SOShipmentNbr = shipline.ShipmentNbr;
			newtran.SOShipmentType = shipline.ShipmentType;
			newtran.SOShipmentLineNbr = shipline.LineNbr;
            newtran.RequireINUpdate = shipline.RequireINUpdate;

			newtran.LineType = orderline.LineType;
			newtran.InventoryID = shipline.InventoryID;
			newtran.SiteID = orderline.SiteID;
			newtran.UOM = shipline.UOM;
			newtran.SubItemID = shipline.SubItemID;
			newtran.LocationID = (shipline.ShipmentType == SOShipmentType.DropShip || shipline.ShipmentNbr == Constants.NoShipmentNbr) ? orderline.LocationID : shipline.LocationID;
			newtran.LotSerialNbr = shipline.LotSerialNbr;
			newtran.ExpireDate = shipline.ExpireDate;

			bool useLineDiscPct;
			CopyTranFieldsFromSOLine(newtran, ordertype, orderline, out useLineDiscPct);

			foreach (SOShipLine other in this.Caches[typeof(SOShipLine)].Cached)
			{
				if (this.Caches[typeof(SOShipLine)].ObjectsEqual<SOShipLine.shipmentNbr, SOShipLine.shipmentType, SOShipLine.origOrderType, SOShipLine.origOrderNbr, SOShipLine.origLineNbr>(shipline, other) && shipline.LineNbr != other.LineNbr)
				{
					shipline = PXCache<SOShipLine>.CreateCopy(shipline);
					shipline.Qty += other.ShippedQty;
					shipline.BaseShippedQty += other.BaseShippedQty;

					newtran.SOShipmentLineNbr = null;
					if (newtran.LotSerialNbr != other.LotSerialNbr)
					{
						newtran.LotSerialNbr = null;
						newtran.ExpireDate = null;
					}
				}
			}

			newtran.Qty = shipline.ShippedQty;
			newtran.BaseQty = shipline.BaseShippedQty;

			decimal shippedQtyInBaseUnits = 0m;
			decimal shippedQtyInOrderUnits = 0m;

			try
			{
				shippedQtyInBaseUnits = INUnitAttribute.ConvertToBase(Transactions.Cache, newtran.InventoryID, shipline.UOM, shipline.ShippedQty.Value, INPrecision.QUANTITY);
				shippedQtyInOrderUnits = INUnitAttribute.ConvertFromBase(Transactions.Cache, newtran.InventoryID, orderline.UOM, shippedQtyInBaseUnits, INPrecision.QUANTITY);
			}
			catch (PXSetPropertyException e)
			{
				throw new PXErrorContextProcessingException(this, orderline, e);
			}

			if (shippedQtyInOrderUnits != orderline.OrderQty || shipline.UOM != orderline.UOM)
			{
				if (shippedQtyInOrderUnits != orderline.OrderQty && orderline.OrderQty != 0m)
				{
					decimal? curyExtPrice = orderline.CuryExtPrice * shippedQtyInOrderUnits / orderline.OrderQty;
					newtran.CuryExtPrice = PXCurrencyAttribute.Round(Transactions.Cache, newtran, curyExtPrice ?? 0m, CMPrecision.TRANCURY);
				}
				else
				{
					newtran.CuryExtPrice = orderline.CuryExtPrice;
				}

				decimal curyUnitPriceInOrderUnits = orderline.CuryUnitPrice.Value;
				if (orderline.CuryExtPrice != 0 && orderline.OrderQty != 0)
				{
					curyUnitPriceInOrderUnits = PXPriceCostAttribute.Round(orderline.CuryExtPrice.Value / orderline.OrderQty.Value);
				}

				decimal curyUnitPriceInBaseUnits = INUnitAttribute.ConvertFromBase(Transactions.Cache, newtran.InventoryID, orderline.UOM, curyUnitPriceInOrderUnits, INPrecision.UNITCOST);
				decimal curyUnitPriceInShippedUnits = INUnitAttribute.ConvertToBase(Transactions.Cache, newtran.InventoryID, shipline.UOM, curyUnitPriceInBaseUnits, INPrecision.UNITCOST);

				decimal? salesPriceAfterDiscount = curyUnitPriceInShippedUnits * (useLineDiscPct ? (1m - orderline.DiscPct / 100m) : 1m);
				if (arsetup.Current.LineDiscountTarget == LineDiscountTargetType.SalesPrice)
				{
					salesPriceAfterDiscount = PXPriceCostAttribute.Round(salesPriceAfterDiscount ?? 0m);
				}
				decimal? curyTranAmt = shipline.ShippedQty * salesPriceAfterDiscount;
				newtran.CuryTranAmt = PXCurrencyAttribute.Round(Transactions.Cache, newtran, curyTranAmt ?? 0m, CMPrecision.TRANCURY);

				if (orderline.CuryUnitPrice != 0)
					newtran.CuryUnitPrice = curyUnitPriceInShippedUnits;

				if (orderline.DiscPct != 0 || orderline.DocumentDiscountRate != 1 || orderline.GroupDiscountRate != 1)
					newtran.CuryDiscAmt = (shipline.ShippedQty * curyUnitPriceInShippedUnits) - newtran.CuryTranAmt;
				else
					newtran.CuryDiscAmt = orderline.CuryDiscAmt;
			}
			else
			{
				newtran.CuryUnitPrice = orderline.CuryUnitPrice;
				newtran.CuryExtPrice = orderline.CuryExtPrice;
				newtran.CuryTranAmt = orderline.CuryLineAmt;
				newtran.CuryDiscAmt = orderline.CuryDiscAmt;
			}

			ChangeBalanceSign(newtran, ordertype, newdoc.DocType, operation);

			return newtran;
		}

		protected virtual void ChangeBalanceSign(ARTran tran, SOOrderType orderType, string docType, string operation)
		{
			if (docType == orderType.ARDocType && operation != orderType.DefaultOperation
				|| docType != orderType.ARDocType && operation == orderType.DefaultOperation)
			{
				//keep BaseQty positive for PXFormula
				tran.Qty = -tran.Qty;
				tran.CuryDiscAmt = -tran.CuryDiscAmt;
				tran.CuryTranAmt = -tran.CuryTranAmt;
			}
		}

		protected virtual void CopyTranFieldsFromOrigTran(ARTran newtran, ARTran origTran)
		{
			newtran.IsFree = origTran.IsFree;
			newtran.ManualPrice = true;
			newtran.ManualDisc = (origTran.ManualDisc == true || origTran.IsFree == true);
			if (origTran.ManualDisc == true)
			{
				newtran.DiscPct = origTran.DiscPct;
			}
		}

		protected virtual void CopyTranFieldsFromSOLine(ARTran newtran, SOOrderType ordertype, SOLine orderline, out bool useLineDiscPct)
		{
			useLineDiscPct = ordertype?.RecalculateDiscOnPartialShipment != true || orderline.ManualDisc == true;
			newtran.BranchID = orderline.BranchID;
			newtran.AccountID = orderline.SalesAcctID;
			newtran.SubID = orderline.SalesSubID;
			newtran.ReasonCode = orderline.ReasonCode;

			newtran.DRTermStartDate = orderline.DRTermStartDate;
			newtran.DRTermEndDate = orderline.DRTermEndDate;
			newtran.CuryUnitPriceDR = orderline.CuryUnitPriceDR;
			newtran.DiscPctDR = orderline.DiscPctDR;
			newtran.DefScheduleID = orderline.DefScheduleID;

			newtran.Commissionable = orderline.Commissionable;

			newtran.ProjectID = orderline.ProjectID;
			newtran.TaskID = orderline.TaskID;
			newtran.CostCodeID = orderline.CostCodeID;
			newtran.CommitmentID = orderline.CommitmentID;
			newtran.TranDesc = orderline.TranDesc;
			newtran.SalesPersonID = orderline.SalesPersonID;
			newtran.TaxCategoryID = orderline.TaxCategoryID;
			newtran.DiscPct = (useLineDiscPct ? orderline.DiscPct : 0m);

			newtran.IsFree = orderline.IsFree;
			newtran.ManualPrice = true;
			newtran.ManualDisc = orderline.ManualDisc == true || orderline.IsFree == true;
			newtran.FreezeManualDisc = true;

			newtran.DiscountID = orderline.DiscountID;
			newtran.DiscountSequenceID = orderline.DiscountSequenceID;

			newtran.SortOrder = orderline.SortOrder;
			newtran.OrigInvoiceType = orderline.InvoiceType;
			newtran.OrigInvoiceNbr = orderline.InvoiceNbr;
			newtran.OrigInvoiceLineNbr = orderline.InvoiceLineNbr;
			newtran.OrigInvoiceDate = orderline.InvoiceDate;

			newtran.SOOrderLineOperation = orderline.Operation;
			newtran.SOOrderSortOrder = orderline.SortOrder;
		}

		public virtual void PostInvoice(INIssueEntry docgraph, ARInvoice invoice, DocumentList<INRegister> list)
		{
			SOOrderEntry oe = null;
			SOShipmentEntry se = null;

			foreach (PXResult<SOOrderShipment, SOOrder> res in PXSelectJoin<SOOrderShipment, 
				InnerJoin<SOOrder, On<SOOrder.orderType, Equal<SOOrderShipment.orderType>, And<SOOrder.orderNbr, Equal<SOOrderShipment.orderNbr>>>>, 
				Where<SOOrderShipment.invoiceType, Equal<Current<ARInvoice.docType>>, And<SOOrderShipment.invoiceNbr, Equal<Current<ARInvoice.refNbr>>, 
				And<SOOrderShipment.invtRefNbr, IsNull>>>>.SelectMultiBound(this, new object[] { invoice }))
			{
				if (((SOOrderShipment)res).ShipmentType == SOShipmentType.DropShip)
				{
					if (se == null)
					{
						se = PXGraph.CreateInstance<SOShipmentEntry>();
					}
					else
					{
						se.Clear();
					}
                    se.PostReceipt(docgraph, res, invoice, list);
				}
				else if (string.Equals(((SOOrderShipment)res).ShipmentNbr, Constants.NoShipmentNbr))
				{
					if (oe == null)
					{
						oe = PXGraph.CreateInstance<SOOrderEntry>();
					}
					else
					{
						oe.Clear();
					}
					oe.PostOrder(docgraph, (SOOrder)res, list, (SOOrderShipment)res);
				}
				else
				{
					if (se == null)
					{
						se = PXGraph.CreateInstance<SOShipmentEntry>();
						se.MergeCachesWithINRegisterEntry(docgraph);
					}
					else
					{
						se.Clear();
					}
					se.PostShipment(docgraph, res, list, invoice);
				}
			}		

			PostInvoiceDirectLines(docgraph, invoice, list);
		}

		public virtual ARTran InsertInvoiceDirectLine(ARTran tran)
		{
			if (Document.Current == null)
				return null;

			if (tran.SOOrderLineNbr != null)
			{
				SOLine line = SOLine.PK.Find(this, tran.SOOrderType, tran.SOOrderNbr, tran.SOOrderLineNbr);
				if (line != null)
				{
					tran.InventoryID = tran.InventoryID ?? line.InventoryID;
					tran.SubItemID = tran.SubItemID ?? line.SubItemID;
					tran.SiteID = tran.SiteID ?? line.SiteID;
					tran.LocationID = tran.LocationID ?? line.LocationID;
					tran.UOM = tran.UOM ?? line.UOM;
					tran.LotSerialNbr = tran.LotSerialNbr ?? line.LotSerialNbr;
					tran.ExpireDate = tran.ExpireDate ?? line.ExpireDate;
					tran.CuryUnitPrice = tran.CuryUnitPrice ?? line.CuryUnitPrice;
					if (tran.Qty == null)
					{
						short lineSign = (short)((line.Operation == SOOperation.Receipt) ? 1 : -1);
						short? tranMult = INTranType.InvtMultFromInvoiceType(Document.Current.DocType);
						tran.Qty = lineSign * tranMult * (line.OrderQty - line.ShippedQty);
					}

					bool useLineDiscPct;
					var orderType = SOOrderType.PK.Find(this, line.OrderType);
					CopyTranFieldsFromSOLine(tran, orderType, line, out useLineDiscPct);
				}
			}
			else if (tran.OrigInvoiceNbr != null && tran.OrigInvoiceLineNbr != null)
			{
				ARTran origTran = PXSelectReadonly<ARTran,
					Where<ARTran.tranType, Equal<Required<ARTran.tranType>>,
						And<ARTran.refNbr, Equal<Required<ARTran.refNbr>>, And<ARTran.lineNbr, Equal<Required<ARTran.lineNbr>>>>>>
					.Select(this, tran.OrigInvoiceType, tran.OrigInvoiceNbr, tran.OrigInvoiceLineNbr);
				if (origTran != null)
				{
					tran.InventoryID = tran.InventoryID ?? origTran.InventoryID;
					tran.SubItemID = tran.SubItemID ?? origTran.SubItemID;
					tran.SiteID = tran.SiteID ?? origTran.SiteID;
					tran.LocationID = tran.LocationID ?? origTran.LocationID;
					tran.UOM = tran.UOM ?? origTran.UOM;
					tran.LotSerialNbr = tran.LotSerialNbr ?? origTran.LotSerialNbr;
					tran.ExpireDate = tran.ExpireDate ?? origTran.ExpireDate;
					tran.CuryUnitPrice = tran.CuryUnitPrice ?? origTran.CuryUnitPrice;
					if (tran.Qty == null)
					{
						short? tranMult = INTranType.InvtMultFromInvoiceType(Document.Current.DocType);
						tran.Qty = tranMult * Math.Abs(origTran.Qty ?? 0m);
					}

					CopyTranFieldsFromOrigTran(tran, origTran);
				}
			}

			if (tran.CuryUnitPrice != null)
			{
				cancelUnitPriceCalculation = true;
			}
			forceDiscountCalculation = true;

			try
			{
				return Transactions.Insert(tran);
			}
			finally
			{
				cancelUnitPriceCalculation = false;
				forceDiscountCalculation = false;
			}
		}

		protected virtual void PostInvoiceDirectLines(INIssueEntry docgraph, ARInvoice invoice, DocumentList<INRegister> list)
		{
			List<PXResult<ARTran, SOLine, INItemPlan>> directLines = PXSelectJoin<ARTran,
				LeftJoin<SOLine, On<SOLine.orderType, Equal<ARTran.sOOrderType>,
					And<SOLine.orderNbr, Equal<ARTran.sOOrderNbr>, And<SOLine.lineNbr, Equal<ARTran.sOOrderLineNbr>>>>,
				LeftJoin<INItemPlan, On<INItemPlan.planID, Equal<ARTran.planID>>>>,
				Where<ARTran.tranType, Equal<Current<ARInvoice.docType>>, And<ARTran.refNbr, Equal<Current<ARInvoice.refNbr>>,
					And<ARTran.invtRefNbr, IsNull, And<ARTran.invtMult, NotEqual<short0>>>>>,
				OrderBy<
					Desc<ARTran.sOOrderType, Asc<ARTran.sOOrderNbr, Asc<ARTran.sOOrderLineNbr>>>>>
				.SelectMultiBound(this, new object[] { invoice }).AsEnumerable()
				.Select(r => (PXResult<ARTran, SOLine, INItemPlan>)r).ToList();
			if (!directLines.Any())
				return;

			using (PXTransactionScope ts = new PXTransactionScope())
			{
				Document.Current = Document.Search<ARInvoice.refNbr>(invoice.RefNbr, invoice.DocType);

				var postedARTrans = new List<ARTran>();
				var orderARTrans = new PXResultset<ARTran, SOLine>();
				var orderEntry = new Lazy<SOOrderEntry>(() => PXGraph.CreateInstance<SOOrderEntry>());
				var persistedOrders = new List<object>();
				var createdOrderShipments = new List<SOOrderShipment>();
				for (int i = 0; i < directLines.Count; i++)
				{
					PXResult<ARTran, SOLine, INItemPlan> directLine = directLines[i];
					ARTran tran = directLine;
					INItemPlan plan = directLine;

					//avoid ReadItem()
					if (plan.PlanID != null)
					{
						Caches[typeof(INItemPlan)].SetStatus(plan, PXEntryStatus.Notchanged);
		}

					Transactions.Cache.SetStatus(tran, PXEntryStatus.Updated);
					tran = (ARTran)Transactions.Cache.Locate(tran);
					tran.PlanID = null;
					Transactions.Cache.IsDirty = true;

					if (tran.Qty == decimal.Zero)
					{
						if (plan.PlanID != null)
						{
							Caches[typeof(INItemPlan)].Delete(plan);
						}
						continue;
					}

					if (tran.LineType == SOLineType.Inventory)
					{
						if (!postedARTrans.Any())
						{
							docgraph.insetup.Current.HoldEntry = false;
							docgraph.insetup.Current.RequireControlTotal = false;

							INRegister newdoc = new INRegister()
							{
								BranchID = invoice.BranchID,
								DocType = INDocType.Issue,
								TranDate = invoice.DocDate,
								OrigModule = BatchModule.SO,
								SrcDocType = invoice.DocType,
								SrcRefNbr = invoice.RefNbr,
							};

							docgraph.issue.Insert(newdoc);
						}

						INTran newline = new INTran()
						{
							BranchID = tran.BranchID,
							DocType = INDocType.Issue,
							TranType = INTranType.TranTypeFromInvoiceType(tran.TranType, tran.Qty),
							SOShipmentNbr = tran.SOShipmentNbr,
							SOShipmentType = tran.SOShipmentType,
							SOShipmentLineNbr = tran.SOShipmentLineNbr,
							SOOrderType = tran.SOOrderType,
							SOOrderNbr = tran.SOOrderNbr,
							SOOrderLineNbr = tran.SOOrderLineNbr,
							SOLineType = SOLineType.Inventory,
							ARDocType = tran.TranType,
							ARRefNbr = tran.RefNbr,
							ARLineNbr = tran.LineNbr,
							BAccountID = tran.CustomerID,
							ProjectID = tran.ProjectID,
							TaskID = tran.TaskID,
							InventoryID = tran.InventoryID,
							SiteID = tran.SiteID,
							Qty = 0m,
							SubItemID = tran.SubItemID,
							UOM = tran.UOM,
							UnitPrice = tran.UnitPrice,
							TranDesc = tran.TranDesc,
							ReasonCode = tran.ReasonCode,
						};
						if (tran.OrigInvoiceNbr != null && tran.InvtMult * tran.Qty > 0m)
						{
							newline.UnitCost = CalculateUnitCostForReturnDirectLine(tran);
						}
						newline.InvtMult = INTranType.InvtMult(newline.TranType);
						newline = docgraph.lsselect.Insert(newline);

						INTranSplit newsplit = (INTranSplit)newline;
						newsplit.SplitLineNbr = null;
						newsplit.SubItemID = tran.SubItemID;
						newsplit.LocationID = tran.LocationID;
						newsplit.LotSerialNbr = tran.LotSerialNbr;
						newsplit.ExpireDate = tran.ExpireDate;
						newsplit.UOM = tran.UOM;
						newsplit.Qty = Math.Abs(tran.Qty ?? 0m);
						newsplit.BaseQty = null;
						newsplit.PlanID = plan.PlanID;
						newsplit = docgraph.splits.Insert(newsplit);
						postedARTrans.Add(tran);
					}
					if (tran.SOOrderNbr != null)
					{
						orderARTrans.Add(directLine);
						if (i + 1 >= directLines.Count
							|| !Transactions.Cache.ObjectsEqual<ARTran.sOOrderType, ARTran.sOOrderNbr>(tran, (ARTran)directLines[i + 1]))
						{
							PXResult<SOOrder, SOOrderShipment> order = UpdateSalesOrderInvoicedDirectly(orderEntry.Value, orderARTrans);
							if (order != null)
							{
								persistedOrders.Add((SOOrder)order);
								createdOrderShipments.Add(order);
							}
							orderARTrans.Clear();
						}
					}
				}

				bool updatedIN = postedARTrans.Any();
				if (updatedIN)
				{
					INRegister copy = PXCache<INRegister>.CreateCopy(docgraph.issue.Current);
					PXFormulaAttribute.CalcAggregate<INTran.qty>(docgraph.transactions.Cache, copy);
					PXFormulaAttribute.CalcAggregate<INTran.tranAmt>(docgraph.transactions.Cache, copy);
					PXFormulaAttribute.CalcAggregate<INTran.tranCost>(docgraph.transactions.Cache, copy);
					docgraph.issue.Update(copy);
				}
				PXAutomation.StorePersisted(this, typeof(SOOrder), persistedOrders);
				try
				{
					if (updatedIN)
					{
						docgraph.Save.Press();

						foreach (ARTran tran in postedARTrans)
						{
							tran.InvtDocType = docgraph.issue.Current.DocType;
							tran.InvtRefNbr = docgraph.issue.Current.RefNbr;
						}
						foreach (SOOrderShipment orderShip in createdOrderShipments)
						{
							orderShip.InvtDocType = docgraph.issue.Current.DocType;
							orderShip.InvtRefNbr = docgraph.issue.Current.RefNbr;
							orderShip.InvtNoteID = docgraph.issue.Current.NoteID;
							shipmentlist.Cache.Update(orderShip);
						}
					}
					this.Save.Press();
				}
				catch
				{
					PXAutomation.RemovePersisted(this, typeof(SOOrder), persistedOrders);
					throw;
				}
				if (updatedIN)
				{
					list.Add(docgraph.issue.Current);
				}

				ts.Complete();
			}
		}

		protected virtual PXResult<SOOrder, SOOrderShipment> UpdateSalesOrderInvoicedDirectly(SOOrderEntry orderEntry, PXResultset<ARTran, SOLine> orderARTranSet)
		{
			var orderARTrans = orderARTranSet.Select(r => (PXResult<ARTran, SOLine>)r).ToList();
			if (orderARTrans.Any(r => ((SOLine)r).LineNbr == null))
				throw new PXException(Messages.SOLineNotFound);
			int orderCount = orderARTrans.GroupBy(r => new { ((SOLine)r).OrderType, ((SOLine)r).OrderNbr }).Count();
			if (orderCount > 1)
				throw new PXArgumentException(nameof(orderARTrans));
			else if (orderCount == 0)
				return null;

			PXCache cache = Transactions.Cache;
			SOLine first = orderARTrans.First();
			orderEntry.Clear();
			orderEntry.Document.Current = orderEntry.Document.Search<SOOrder.orderNbr>(first.OrderNbr, first.OrderType);
			orderEntry.Document.Cache.SetStatus(orderEntry.Document.Current, PXEntryStatus.Updated);

			orderEntry.soordertype.Current.RequireControlTotal = false;

			decimal orderInvoicedQty = 0m;
			decimal orderInvoicedAmt = 0m;
			bool updateIN = false;
			foreach (var groupBySOLine in orderARTrans.GroupBy(r => ((SOLine)r).LineNbr))
			{
				SOLine line = groupBySOLine.First();

				IEnumerable<ARTran> trans = groupBySOLine.Select(r => (ARTran)r);
				ARTran firstTran = trans.First();
				decimal sumQty = trans.Sum(t => Math.Abs(t.Qty ?? 0m)),
					sumBaseQty = trans.Sum(t => Math.Abs(t.BaseQty ?? 0m));
				orderInvoicedQty += sumQty;
				orderInvoicedAmt += trans.Sum(t => t.TranAmt) ?? 0m;
				updateIN |= trans.Any(t => t.LineType == SOLineType.Inventory);

				foreach (ARTran tran in trans)
				{
					lsselect.OrderAvailabilityCheck(cache, tran);
				}
				bool completeLineByQty = (PXDBQuantityAttribute.Round((decimal)(line.BaseOrderQty * line.CompleteQtyMin / 100m - line.BaseShippedQty - sumBaseQty)) <= 0m);
				if (line.ShipComplete == SOShipComplete.ShipComplete && !completeLineByQty)
				{
					throw new PXException(Messages.CannotShipComplete_Line, cache.GetValueExt<ARTran.inventoryID>(firstTran));
				}
				bool completeLine = completeLineByQty || (line.ShipComplete == SOShipComplete.CancelRemainder);
				if (PXDBQuantityAttribute.Round((decimal)(line.BaseOrderQty * line.CompleteQtyMax / 100m - line.BaseShippedQty - sumBaseQty)) < 0m)
				{
					throw new PXException(Messages.OrderCheck_QtyNegative,
						cache.GetValueExt<ARTran.inventoryID>(firstTran), cache.GetValueExt<ARTran.subItemID>(firstTran),
						cache.GetValueExt<ARTran.sOOrderType>(firstTran), cache.GetValueExt<ARTran.sOOrderNbr>(firstTran));
				}

				line = (SOLine)orderEntry.Transactions.Cache.CreateCopy(line);
				orderEntry.Transactions.Current = line;
				var splitsWithPlans = PXSelectJoin<SOLineSplit,
					InnerJoin<INItemPlan, On<INItemPlan.planID, Equal<SOLineSplit.planID>>>,
					Where<SOLineSplit.orderType, Equal<Required<SOLineSplit.orderType>>,
						And<SOLineSplit.orderNbr, Equal<Required<SOLineSplit.orderNbr>>,
						And<SOLineSplit.lineNbr, Equal<Required<SOLineSplit.lineNbr>>,
						And<SOLineSplit.completed, Equal<boolFalse>>>>>>
					.Select(orderEntry, line.OrderType, line.OrderNbr, line.LineNbr)
					.Select(r => (PXResult<SOLineSplit, INItemPlan>)r)
					.ToList();
				var splits = splitsWithPlans.Select(s => (SOLineSplit)s).ToList();
				var updatedSplits = new HashSet<int?>();
				SOLineSplit lastUpdatedSplit = null;

				foreach (ARTran tran in trans)
				{
					var splitsCopy = splits.Where(s => s.Completed != true).ToList();
					// sort SOLineSplits by their proximity to the current ARTran (by Lot/Serial Nbr and Location)
					splitsCopy.Sort((s1, s2) =>
					{
						if (!string.IsNullOrEmpty(tran.LotSerialNbr)
							&& !string.Equals(s1.LotSerialNbr, s2.LotSerialNbr, StringComparison.InvariantCultureIgnoreCase))
						{
							if (string.Equals(s1.LotSerialNbr, tran.LotSerialNbr, StringComparison.InvariantCultureIgnoreCase))
								return -1;
							else if (string.Equals(s2.LotSerialNbr, tran.LotSerialNbr, StringComparison.InvariantCultureIgnoreCase))
								return 1;
						}

						if (s1.LocationID != s2.LocationID)
						{
							if (tran.LocationID == s1.LocationID)
								return -1;
							else if (tran.LocationID == s2.LocationID)
								return 1;
						}

						return s1.SplitLineNbr.GetValueOrDefault().CompareTo(
							s2.SplitLineNbr.GetValueOrDefault());
					});
					decimal qtyToWriteOff = Math.Abs(tran.BaseQty ?? 0m);

					for (int j = 0; j < splits.Count; j++)
					{
						if (qtyToWriteOff <= 0m) break;
						SOLineSplit split = splits[j];
						bool lastSplit = (j + 1 >= splits.Count);

						decimal splitQty = (decimal)(split.BaseQty - split.BaseShippedQty);
						if (lastSplit || splitQty >= qtyToWriteOff)
						{
							split.BaseShippedQty += qtyToWriteOff;
							split.ShippedQty = INUnitAttribute.ConvertFromBase(orderEntry.splits.Cache, split.InventoryID, split.UOM, (decimal)split.BaseShippedQty, INPrecision.QUANTITY);
							qtyToWriteOff = 0m;
						}
						else
						{
							split.BaseShippedQty = split.BaseQty;
							split.ShippedQty = split.Qty;
							qtyToWriteOff -= splitQty;
							split.Completed = true;
						}
						updatedSplits.Add(split.SplitLineNbr);
						lastUpdatedSplit = split;
					}
				}

				PXRowUpdating cancelSOLineUpdatingHandler = new PXRowUpdating((sender, e) => { e.Cancel = true; });
				orderEntry.RowUpdating.AddHandler<SOLine>(cancelSOLineUpdatingHandler);
				foreach (PXResult<SOLineSplit, INItemPlan> splitWithPlan in splitsWithPlans)
				{
					SOLineSplit split = splitWithPlan;
					if (updatedSplits.Contains(split.SplitLineNbr))
					{
						split.Completed = true;
						split.ShipComplete = line.ShipComplete;
						split.PlanID = null;
						split.RefNoteID = Document.Current.NoteID;
						orderEntry.splits.Cache.Update(split);
						orderEntry.Caches[typeof(INItemPlan)].Delete((INItemPlan)splitWithPlan);
					}
				}

				if (!completeLine)
				{
					SOLineSplit split = PXCache<SOLineSplit>.CreateCopy(lastUpdatedSplit);
					split.PlanID = null;
					split.PlanType = split.BackOrderPlanType;
					split.ParentSplitLineNbr = split.SplitLineNbr;
					split.SplitLineNbr = null;
					split.IsAllocated = false;
					split.Completed = false;
					split.ShipmentNbr = null;
					split.LotSerialNbr = null;
					split.VendorID = null;
					split.ClearPOFlags();
					split.ClearPOReferences();
					split.ClearSOReferences();
					
					split.RefNoteID = null;
					split.BaseReceivedQty = 0m;
					split.ReceivedQty = 0m;
					split.BaseShippedQty = 0m;
					split.ShippedQty = 0m;
					split.BaseQty = line.BaseOrderQty - line.BaseShippedQty - sumBaseQty;
					split.Qty = INUnitAttribute.ConvertFromBase(orderEntry.splits.Cache, split.InventoryID, split.UOM, (decimal)split.BaseQty, INPrecision.QUANTITY);

					orderEntry.splits.Insert(split);
				}
				orderEntry.RowUpdating.RemoveHandler<SOLine>(cancelSOLineUpdatingHandler);

				orderEntry.lsselect.SuppressedMode = true;
				line.ShippedQty += sumQty;
				line.BaseShippedQty += sumBaseQty;
				if (completeLine)
				{
					line.OpenQty = 0m;
					line.ClosedQty = line.OrderQty;
					line.BaseClosedQty = line.BaseOrderQty;
					line.OpenLine = false;
					line.Completed = true;
					line.UnbilledQty -= (line.OrderQty - line.ShippedQty);
				}
				else
				{
					line.OpenQty = line.OrderQty - line.ShippedQty;
					line.BaseOpenQty = line.BaseOrderQty - line.BaseShippedQty;
					line.ClosedQty = line.ShippedQty;
					line.BaseClosedQty = line.BaseShippedQty;
				}
				orderEntry.Transactions.Cache.Update(line);
				orderEntry.lsselect.SuppressedMode = false;
			}
			var orderShipment = new SOOrderShipment
			{
				OrderType = orderEntry.Document.Current.OrderType,
				OrderNbr = orderEntry.Document.Current.OrderNbr,
				ShippingRefNoteID = Document.Current.NoteID,
				ShipmentType = INDocType.Invoice,
				Operation = orderEntry.Document.Current.DefaultOperation,
				Confirmed = true,
				CustomerID = Document.Current.CustomerID,
				CustomerLocationID = Document.Current.CustomerLocationID,
				SiteID = null,
				ShipDate = Document.Current.DocDate,
				LineCntr = orderARTrans.Count,
				ShipmentQty = orderInvoicedQty,
				LineTotal = orderInvoicedAmt,
				CreateINDoc = updateIN,
				NoteID = orderEntry.Document.Current.NoteID,
				InvoiceType = Document.Current.DocType,
				InvoiceNbr = Document.Current.RefNbr,
				InvoiceReleased = true,
			};
			orderShipment = orderEntry.shipmentlist.Insert(orderShipment);
			orderEntry.Document.Current.ShipmentCntr++;
			if (orderEntry.Document.Current.OpenLineCntr <= 0)
			{
				orderEntry.Document.Current.Completed = true;
			}

			PXAutomation.CompleteSimple(orderEntry.Document.View);
			orderEntry.Save.Press();
			return new PXResult<SOOrder, SOOrderShipment>(orderEntry.Document.Current, orderEntry.shipmentlist.Current);
		}

		protected virtual decimal? CalculateUnitCostForReturnDirectLine(ARTran tran)
		{
			PXSelectBase cmd = new PXSelectReadonly<ARTran,
				Where<ARTran.tranType, Equal<Current<ARTran.origInvoiceType>>, And<ARTran.refNbr, Equal<Current<ARTran.origInvoiceNbr>>,
					And<ARTran.inventoryID, Equal<Current<ARTran.inventoryID>>, And<ARTran.subItemID, Equal<Current<ARTran.subItemID>>,
					And<Mult<ARTran.invtMult, ARTran.qty>, LessEqual<decimal0>>>>>>>(this);

			decimal baseQtySum = 0m, tranCostSum = 0m;
			foreach (ARTran t in cmd.View.SelectMultiBound(new[] { tran }))
			{
				if (INTranType.InvtMultFromInvoiceType(t.TranType) * t.Qty < 0m)
				{
					baseQtySum += Math.Abs(t.BaseQty ?? 0m);
					tranCostSum += Math.Abs(t.TranCost ?? 0m);
				}
			}
			return (baseQtySum == 0m) ? null : (decimal?)PXPriceCostAttribute.Round(tranCostSum / baseQtySum);
		}

		public override void DefaultDiscountAccountAndSubAccount(ARTran tran)
		{
			ARTran firstTranWithOrderType = PXSelect<ARTran, Where<ARTran.tranType, Equal<Current<SOInvoice.docType>>,
				And<ARTran.refNbr, Equal<Current<SOInvoice.refNbr>>,
				And<ARTran.sOOrderType, IsNotNull>>>>.Select(this);

			if (firstTranWithOrderType != null)
			{
				SOOrderType type = soordertype.SelectWindowed(0, 1, firstTranWithOrderType.SOOrderType);

				if (type != null)
				{
					Location customerloc = location.Current;
					CRLocation companyloc =
						PXSelectJoin<CRLocation, InnerJoin<BAccountR, On<CRLocation.bAccountID, Equal<BAccountR.bAccountID>, And<CRLocation.locationID, Equal<BAccountR.defLocationID>>>, InnerJoin<Branch, On<Branch.bAccountID, Equal<BAccountR.bAccountID>>>>, Where<Branch.branchID, Equal<Current<ARRegister.branchID>>>>.Select(this);

					switch (type.DiscAcctDefault)
					{
						case SODiscAcctSubDefault.OrderType:
							tran.AccountID = (int?)GetValue<SOOrderType.discountAcctID>(type);
							break;
						case SODiscAcctSubDefault.MaskLocation:
							tran.AccountID = (int?)GetValue<Location.cDiscountAcctID>(customerloc);
							break;
					}


					if (tran.AccountID == null)
					{
						tran.AccountID = type.DiscountAcctID;
					}

					Discount.Cache.RaiseFieldUpdated<ARTran.accountID>(tran, null);

					if (tran.AccountID != null)
					{
						object ordertype_SubID = GetValue<SOOrderType.discountSubID>(type);
						object customer_Location = GetValue<Location.cDiscountSubID>(customerloc);
						object company_Location = GetValue<CRLocation.cMPDiscountSubID>(companyloc);

						object value = SODiscSubAccountMaskAttribute.MakeSub<SOOrderType.discSubMask>(this, type.DiscSubMask,
								new object[] { ordertype_SubID, customer_Location, company_Location },
								new Type[] { typeof(SOOrderType.discountSubID), typeof(Location.cDiscountSubID), typeof(Location.cMPDiscountSubID) });

						Discount.Cache.RaiseFieldUpdating<ARTran.subID>(tran, ref value);

						tran.SubID = (int?)value;
					}
				}
			}

		}

		#region Freight
		public virtual SOFreightDetail FillFreightDetails(SOOrder order, SOOrderShipment ordershipment)
		{
			return string.Equals(ordershipment.ShipmentNbr, Constants.NoShipmentNbr)
				? FillFreightDetailsForNonShipment(order, ordershipment)
				: FillFreightDetailsForShipment(order, ordershipment);
		}

		public virtual SOFreightDetail FillFreightDetailsForNonShipment(SOOrder order, SOOrderShipment orderShipment)
		{
			var freightDet = new SOFreightDetail()
			{
				CuryInfoID = Document.Current.CuryInfoID,
				ShipmentNbr = orderShipment.ShipmentNbr,
				ShipmentType = orderShipment.ShipmentType,
				OrderType = orderShipment.OrderType,
				OrderNbr = orderShipment.OrderNbr,
				ProjectID = order.ProjectID,
				ShipTermsID = order.ShipTermsID,
				ShipVia = order.ShipVia,
				ShipZoneID = order.ShipZoneID,
				TaxCategoryID = order.FreightTaxCategoryID,

				Weight = order.OrderWeight,
				Volume = order.OrderVolume,
				CuryLineTotal = order.CuryLineTotal,
				CuryFreightCost = order.CuryFreightCost,
				CuryFreightAmt = order.CuryFreightAmt,
				CuryPremiumFreightAmt = order.CuryPremiumFreightAmt,
			};

			PopulateFreightAccountAndSubAccount(freightDet, order, orderShipment);

			return FreightDetails.Insert(freightDet);
		}

		public virtual SOShipment GetShipment(SOOrderShipment orderShipment)
		{
			if (string.Equals(orderShipment.ShipmentNbr, Constants.NoShipmentNbr) || orderShipment.ShipmentType == SOShipmentType.DropShip)
				return null;

			return (SOShipment)PXSelect<SOShipment,
				Where<SOShipment.shipmentType, Equal<Required<SOShipment.shipmentType>>, And<SOShipment.shipmentNbr, Equal<Required<SOShipment.shipmentNbr>>>>>
				.Select(this, orderShipment.ShipmentType, orderShipment.ShipmentNbr);
		}

		public virtual SOFreightDetail FillFreightDetailsForShipment(SOOrder order, SOOrderShipment orderShipment)
		{
			bool isDropship = (orderShipment.ShipmentType == SOShipmentType.DropShip);
			var shipment = GetShipment(orderShipment);
			if (!isDropship && shipment == null)
				return null;

			bool isOrderBased = ((shipment?.FreightAmountSource ?? order.FreightAmountSource) == FreightAmountSourceAttribute.OrderBased);

			var freightDet = new SOFreightDetail()
			{
				CuryInfoID = Document.Current.CuryInfoID,
				ShipmentNbr = orderShipment.ShipmentNbr,
				ShipmentType = orderShipment.ShipmentType,
				OrderType = orderShipment.OrderType,
				OrderNbr = orderShipment.OrderNbr,
				ProjectID = order.ProjectID,

				ShipTermsID = (isDropship || isOrderBased) ? order.ShipTermsID : shipment.ShipTermsID,
				ShipVia = isDropship ? order.ShipVia : shipment.ShipVia,
				ShipZoneID = isDropship ? order.ShipZoneID : shipment.ShipZoneID,
				// set freight tax category from order unconditionally to update it later from shipment for correct tax calculation
				TaxCategoryID = order.FreightTaxCategoryID,
				Weight = orderShipment.ShipmentWeight,
				Volume = orderShipment.ShipmentVolume,
				LineTotal = orderShipment.LineTotal,
				CuryFreightCost = 0m,
				CuryFreightAmt = 0m,
				CuryPremiumFreightAmt = 0m,
			};
			PXCurrencyAttribute.CuryConvCury<SOFreightDetail.curyLineTotal>(FreightDetails.Cache, freightDet);

			bool fullOrderAllocation = IsFullOrderFreightAmountFirstTime(order);
			CalcOrderBasedFreight(freightDet, order, orderShipment, isOrderBased, fullOrderAllocation, isDropship);
			CalcShipmentBasedFreight(freightDet, orderShipment, shipment, isOrderBased, isDropship);

			PopulateFreightAccountAndSubAccount(freightDet, order, orderShipment);

			freightDet = FreightDetails.Insert(freightDet);

			freightDet = FillFreightDetailRoundingDiffByOrder(freightDet, order, orderShipment, isOrderBased, fullOrderAllocation);
			freightDet = FillFreightDetailRoundingDiffByShipment(freightDet, orderShipment, shipment, isOrderBased, isDropship);

			return freightDet;
		}

		public virtual void CalcOrderBasedFreight(SOFreightDetail freightDet, SOOrder order, SOOrderShipment orderShipment, bool isOrderBased, bool fullOrderAllocation, bool isDropship)
		{
			if (order.DefaultOperation != orderShipment.Operation)
				return;

			var orderRatio = new Lazy<decimal>(() => CalcOrderFreightRatio(order, orderShipment));
			if (fullOrderAllocation)
			{
				SOOrderShipment allocated = PXSelect<SOOrderShipment,
					Where<SOOrderShipment.orderType, Equal<Current<SOOrderShipment.orderType>>,
						And<SOOrderShipment.orderNbr, Equal<Current<SOOrderShipment.orderNbr>>,
						And<SOOrderShipment.invoiceNbr, IsNotNull,
						And<SOOrderShipment.orderFreightAllocated, Equal<True>>>>>>
					.SelectSingleBound(this, new object[] { orderShipment });
				if (allocated == null)
				{
					freightDet.CuryPremiumFreightAmt = order.CuryPremiumFreightAmt;
					if (isOrderBased)
					{
						freightDet.CuryFreightAmt = order.CuryFreightAmt;
					}
					orderShipment.OrderFreightAllocated = true;
				}
			}
			else if (sosetup.Current.FreightAllocation == FreightAllocationList.Prorate)
			{
				freightDet.CuryPremiumFreightAmt = PXDBCurrencyAttribute.Round(FreightDetails.Cache, freightDet, orderRatio.Value * (order.CuryPremiumFreightAmt ?? 0m), CMPrecision.TRANCURY);
				if (isOrderBased)
				{
					freightDet.CuryFreightAmt = PXDBCurrencyAttribute.Round(FreightDetails.Cache, freightDet, orderRatio.Value * (order.CuryFreightAmt ?? 0m), CMPrecision.TRANCURY);
				}
			}

			if (isDropship)
			{
				freightDet.CuryFreightCost = PXDBCurrencyAttribute.Round(FreightDetails.Cache, freightDet, orderRatio.Value * (order.CuryFreightCost ?? 0m), CMPrecision.TRANCURY);
			}
		}

		public virtual void CalcShipmentBasedFreight(SOFreightDetail freightDet, SOOrderShipment orderShipment, SOShipment shipment, bool isOrderBased, bool isDropship)
		{
			if (isDropship)
				return;

			decimal shipmentRatio = CalcShipmentFreightRatio(orderShipment, shipment);
			bool sameCurrency = string.Equals(shipment.CuryID, Document.Current.CuryID, StringComparison.OrdinalIgnoreCase);
			if (sameCurrency)
			{
			freightDet.CuryFreightCost = PXDBCurrencyAttribute.Round(FreightDetails.Cache, freightDet, shipmentRatio * (shipment.CuryFreightCost ?? 0m), CMPrecision.TRANCURY);
			if (!isOrderBased)
			{
				freightDet.CuryFreightAmt = PXDBCurrencyAttribute.Round(FreightDetails.Cache, freightDet, shipmentRatio * (shipment.CuryFreightAmt ?? 0m), CMPrecision.TRANCURY);
			}
		}
			else
			{
				freightDet.FreightCost = shipmentRatio * (shipment.FreightCost ?? 0m);
				PXCurrencyAttribute.CuryConvCury<SOFreightDetail.curyFreightCost>(FreightDetails.Cache, freightDet);
				if (!isOrderBased)
				{
					freightDet.FreightAmt = shipmentRatio * (shipment.FreightAmt ?? 0m);
					PXCurrencyAttribute.CuryConvCury<SOFreightDetail.curyFreightAmt>(FreightDetails.Cache, freightDet);
				}
			}
		}

		public virtual bool IsFullOrderFreightAmountFirstTime(SOOrder order)
		{
			if (sosetup.Current.FreightAllocation == FreightAllocationList.FullAmount || order.LineTotal <= 0m)
				return true;

			if (order.Behavior != SOBehavior.RM)
				return false;

			SOOrderTypeOperation nonDefaultOperation = PXSelectReadonly<SOOrderTypeOperation,
				Where<SOOrderTypeOperation.orderType, Equal<Required<SOOrderTypeOperation.orderType>>,
					And<SOOrderTypeOperation.operation, NotEqual<Required<SOOrderTypeOperation.operation>>, 
					And<SOOrderTypeOperation.active, Equal<True>>>>>
				.SelectWindowed(this, 0, 1, order.OrderType, order.DefaultOperation);
			return nonDefaultOperation != null;
		}

		public virtual decimal CalcOrderFreightRatio(SOOrder order, SOOrderShipment orderShipment)
		{
			if (orderShipment.ShipmentType == SOShipmentType.DropShip && orderShipment.LineTotal == 0m)
			{
				// this block is obsolete and will be used only for drop-shipments created before
				// now SOOrderShipment.LineTotal is properly populated on PO Receipt releasing
				
				// prorate by base receipted qty and then by amount
				if (order.CuryLineTotal == 0m)
				{
					return 1m;
				}
				decimal curyDropShipLineAmt = 0m;
				foreach (PXResult<SOLine, SOLineSplit, PO.POLine, POReceiptLine> res in PXSelectJoin<SOLine,
					InnerJoin<SOLineSplit, On<SOLineSplit.orderType, Equal<SOLine.orderType>, And<SOLineSplit.orderNbr, Equal<SOLine.orderNbr>, And<SOLineSplit.lineNbr, Equal<SOLine.lineNbr>>>>,
					InnerJoin<PO.POLine, On<PO.POLine.orderType, Equal<SOLineSplit.pOType>, And<PO.POLine.orderNbr, Equal<SOLineSplit.pONbr>, And<PO.POLine.lineNbr, Equal<SOLineSplit.pOLineNbr>>>>,
					InnerJoin<POReceiptLine, On<POReceiptLine.pOLineNbr, Equal<PO.POLine.lineNbr>, And<POReceiptLine.pONbr, Equal<PO.POLine.orderNbr>, And<POReceiptLine.pOType, Equal<PO.POLine.orderType>>>>>>>,
					Where<POReceiptLine.receiptNbr, Equal<Required<POReceiptLine.receiptNbr>>,
						And<SOLine.orderType, Equal<Required<SOLine.orderType>>, And<SOLine.orderNbr, Equal<Required<SOLine.orderNbr>>>>>>
					.Select(this, orderShipment.ShipmentNbr, orderShipment.OrderType, orderShipment.OrderNbr))
				{
					SOLine soline = (SOLine)res;
					POReceiptLine pOReceiptline = (POReceiptLine)res;

					decimal baseQtyRcpRate = ((soline.BaseOrderQty ?? 0m) > 0m) ? (decimal)(pOReceiptline.BaseReceiptQty / soline.BaseOrderQty) : 1m;
					curyDropShipLineAmt += (soline.CuryLineAmt ?? 0m) * baseQtyRcpRate;
				}
				return Math.Min(1m, curyDropShipLineAmt / (decimal)order.CuryLineTotal);
			}
			else
			{
				return (order.LineTotal == 0m) ? 1m : Math.Min(1m, (decimal)(orderShipment.LineTotal / order.LineTotal));
			}
		}

		public virtual decimal CalcShipmentFreightRatio(SOOrderShipment orderShipment, SOShipment shipment)
		{
			return (shipment.LineTotal == 0m) ? 1m : Math.Min(1m, (decimal)(orderShipment.LineTotal / shipment.LineTotal));
		}

		public virtual SOFreightDetail FillFreightDetailRoundingDiffByShipment(SOFreightDetail freightDet, SOOrderShipment orderShipment, SOShipment shipment, bool isOrderBased, bool isDropship)
		{
			if (isDropship)
				return freightDet;

			bool sameCurrency = string.Equals(shipment.CuryID, Document.Current.CuryID, StringComparison.OrdinalIgnoreCase);
			if (!sameCurrency) return freightDet;

			PXResultset<SOFreightDetail> freightDetails = PXSelect<SOFreightDetail,
				Where<SOFreightDetail.docType, Equal<Current<ARInvoice.docType>>, And<SOFreightDetail.refNbr, Equal<Current<ARInvoice.refNbr>>,
					And<SOFreightDetail.shipmentType, Equal<Required<SOFreightDetail.shipmentType>>, And<SOFreightDetail.shipmentNbr, Equal<Required<SOFreightDetail.shipmentNbr>>>>>>>
				.Select(this, orderShipment.ShipmentType, orderShipment.ShipmentNbr);

			if (freightDetails.Count <= 1)
				return freightDet;

			PXResultset<SOOrderShipment> orderShipments = PXSelect<SOOrderShipment,
				Where<SOOrderShipment.shipmentType, Equal<Required<SOOrderShipment.shipmentType>>, And<SOOrderShipment.shipmentNbr, Equal<Required<SOOrderShipment.shipmentNbr>>>>>
				.Select(this, orderShipment.ShipmentType, orderShipment.ShipmentNbr);

			if (freightDetails.Count != orderShipments.Count)
				return freightDet;

			decimal totalInvoicedFreightCost = 0m,
				totalInvoicedFreightPrice = 0m;
			foreach (SOFreightDetail freightDetail in freightDetails)
			{
				totalInvoicedFreightCost += freightDetail.CuryFreightCost ?? 0m;
				totalInvoicedFreightPrice += freightDetail.CuryFreightAmt ?? 0m;
			}

			decimal freightCostDiff = (shipment.CuryFreightCost ?? 0m) - totalInvoicedFreightCost,
				freightPriceDiff = (shipment.CuryFreightAmt ?? 0m) - totalInvoicedFreightPrice;
			if (freightCostDiff != 0m || !isOrderBased && freightPriceDiff != 0m)
			{
				freightDet.CuryFreightCost += freightCostDiff;
				if (freightDet.CuryFreightCost < 0m)
				{
					freightDet.CuryFreightCost = 0m;
				}
				if (!isOrderBased)
				{
					freightDet.CuryFreightAmt += freightPriceDiff;
					if (freightDet.CuryFreightAmt < 0m)
					{
						freightDet.CuryFreightAmt = 0m;
					}
				}

				return FreightDetails.Update(freightDet);
			}

			return freightDet;
		}

		public virtual SOFreightDetail FillFreightDetailRoundingDiffByOrder(SOFreightDetail freightDet, SOOrder order, SOOrderShipment orderShipment, bool isOrderBased, bool fullOrderAllocation)
		{
			if (order.OpenLineCntr != 0 || fullOrderAllocation)
				return freightDet;

			PXResultset<SOFreightDetail> freightDetails = PXSelect<SOFreightDetail,
				Where<SOFreightDetail.docType, Equal<Current<ARInvoice.docType>>, And<SOFreightDetail.refNbr, Equal<Current<ARInvoice.refNbr>>,
					And<SOFreightDetail.orderType, Equal<Required<SOFreightDetail.orderType>>, And<SOFreightDetail.orderNbr, Equal<Required<SOFreightDetail.orderNbr>>>>>>>
				.Select(this, order.OrderType, order.OrderNbr);

			if (freightDetails.Count <= 1)
				return freightDet;

			PXResultset<SOOrderShipment> orderShipments = PXSelect<SOOrderShipment,
				Where<SOOrderShipment.orderType, Equal<Required<SOOrderShipment.orderType>>, And<SOOrderShipment.orderNbr, Equal<Required<SOOrderShipment.orderNbr>>>>>
				.Select(this, order.OrderType, order.OrderNbr);

			if (freightDetails.Count != orderShipments.Count)
				return freightDet;

			decimal totalInvoicedFreight = 0m,
				totalInvoicedPremiumFreight = 0m;
			foreach (SOFreightDetail freightDetail in freightDetails)
			{
				totalInvoicedFreight += freightDetail.CuryFreightAmt ?? 0m;
				totalInvoicedPremiumFreight += freightDetail.CuryPremiumFreightAmt ?? 0m;
			}

			decimal freightDiff = (order.CuryFreightAmt ?? 0m) - totalInvoicedFreight,
				premiumFreightDiff = (order.CuryPremiumFreightAmt ?? 0m) - totalInvoicedPremiumFreight;
			if (isOrderBased && freightDiff != 0m || premiumFreightDiff != 0m)
			{
				if (isOrderBased)
				{
					freightDet.CuryFreightAmt += freightDiff;
					if (freightDet.CuryFreightAmt < 0m)
					{
						freightDet.CuryFreightAmt = 0m;
					}
				}
				freightDet.CuryPremiumFreightAmt += premiumFreightDiff;

				return FreightDetails.Update(freightDet);
			}
			return freightDet;
		}

		public virtual ARTran UpdateFreightTransaction(SOFreightDetail fd, bool newFreightDetail)
		{
			ARTran freightTran = null;
			if (!newFreightDetail)
			{
				freightTran = PXSelect<ARTran,
					Where<ARTran.lineType, Equal<SOLineType.freight>,
						And<ARTran.tranType, Equal<Current<ARInvoice.docType>>, And<ARTran.refNbr, Equal<Current<ARInvoice.refNbr>>,
						And<ARTran.sOShipmentType, Equal<Required<ARTran.sOShipmentType>>, And<ARTran.sOShipmentNbr, Equal<Required<ARTran.sOShipmentNbr>>,
						And<ARTran.sOOrderType, Equal<Required<ARTran.sOOrderType>>, And<ARTran.sOOrderNbr, Equal<Required<ARTran.sOOrderNbr>>>>>>>>>>
					.Select(this, fd.ShipmentType, fd.ShipmentNbr, fd.OrderType, fd.OrderNbr);
			}

			if (fd.CuryFreightAmt == 0m && fd.CuryPremiumFreightAmt == 0m && fd.CuryFreightCost == 0m && fd.TaxCategoryID == null)
			{
				if (freightTran != null)
				{
					Freight.Delete(freightTran);
				}
				return null;
			}

			bool newFreightTran = (freightTran == null);
			freightTran = freightTran ?? new ARTran();
			freightTran.SOShipmentNbr = fd.ShipmentNbr;
			freightTran.SOShipmentType = fd.ShipmentType ?? SOShipmentType.Issue;
			freightTran.SOOrderType = fd.OrderType;
			freightTran.SOOrderNbr = fd.OrderNbr;
			freightTran.LineType = SOLineType.Freight;
			freightTran.CuryTranAmt = fd.CuryTotalFreightAmt;
			freightTran.TranCostOrig = fd.FreightCost;
			freightTran.TaxCategoryID = fd.TaxCategoryID;
			freightTran.AccountID = fd.AccountID;
			freightTran.SubID = fd.SubID;
			freightTran.ProjectID = fd.ProjectID;
			freightTran.TaskID = fd.TaskID;
			using (new PXLocaleScope(customer.Current.LocaleName))
			{
				freightTran.TranDesc = PXMessages.LocalizeFormatNoPrefix(Messages.FreightDescr, fd.ShipVia);
			}
			
			freightTran = newFreightTran
				? Freight.Insert(freightTran)
				: Freight.Update(freightTran);

			if (freightTran.TaskID == null && !PM.ProjectDefaultAttribute.IsNonProject(freightTran.ProjectID))
			{
				Account ac = PXSelect<Account, Where<Account.accountID, Equal<Required<Account.accountID>>>>.Select(this, freightTran.AccountID);
				throw new PXException(Messages.TaskWasNotAssigned, ac.AccountCD);
			}
			return freightTran;
		}

		public virtual void CopyFreightNotesAndFilesToARTran()
		{
			foreach (SOFreightDetail fd in FreightDetails.Select())
			{
				foreach (ARTran tran in PXSelect<ARTran,
					Where<ARTran.lineType, Equal<SOLineType.freight>,
						And<ARTran.tranType, Equal<Current<ARInvoice.docType>>,
						And<ARTran.refNbr, Equal<Current<ARInvoice.refNbr>>,
						And<ARTran.sOShipmentNbr, Equal<Required<ARTran.sOShipmentNbr>>,
						And<ARTran.sOShipmentType, NotEqual<SOShipmentType.dropShip>>>>>>>
					.Select(this, fd.ShipmentNbr))
				{
					PXNoteAttribute.CopyNoteAndFiles(FreightDetails.Cache, fd, Freight.Cache, tran);
				}
			}
		}

		public virtual void PopulateFreightAccountAndSubAccount(SOFreightDetail freightDet, SOOrder order, SOOrderShipment orderShipment)
		{
			int? accountID;
			object subID;
			GetFreightAccountAndSubAccount(order, freightDet.ShipVia, order.OwnerID, out accountID, out subID);
			freightDet.AccountID = accountID;
			FreightDetails.Cache.RaiseFieldUpdating<SOFreightDetail.subID>(freightDet, ref subID);
			freightDet.SubID = (int?)subID;
		}

		public virtual void GetFreightAccountAndSubAccount(SOOrder order, string ShipVia, Guid? ownerGuid, out int? accountID, out object subID)
		{
			accountID = null;
			subID = null;
			SOOrderType type = soordertype.SelectWindowed(0, 1, order.OrderType);

			if (type != null)
			{
				Location customerloc = location.Current;
				Carrier carrier = Carrier.PK.Find(this, ShipVia);

                CRLocation companyloc =
                        PXSelectJoin<CRLocation, InnerJoin<BAccountR, On<CRLocation.bAccountID, Equal<BAccountR.bAccountID>, And<CRLocation.locationID, Equal<BAccountR.defLocationID>>>, InnerJoin<Branch, On<Branch.bAccountID, Equal<BAccountR.bAccountID>>>>, Where<Branch.branchID, Equal<Current<ARRegister.branchID>>>>.Select(this);
				EPEmployee employee = (EPEmployee)PXSelect<EPEmployee, Where<EPEmployee.userID, Equal<Required<SOOrder.ownerID>>>>.Select(this, ownerGuid);

				switch (type.FreightAcctDefault)
				{
					case SOFreightAcctSubDefault.OrderType:
						accountID = (int?)GetValue<SOOrderType.freightAcctID>(type);
						break;
					case SOFreightAcctSubDefault.MaskLocation:
						accountID = (int?)GetValue<Location.cFreightAcctID>(customerloc);
						break;
					case SOFreightAcctSubDefault.MaskShipVia:
						accountID = (int?)GetValue<Carrier.freightSalesAcctID>(carrier);
						break;
				}

				if (accountID == null)
				{
					accountID = type.FreightAcctID;

					if (accountID == null)
					{
						throw new PXException(Messages.FreightAccountIsRequired);
					}

				}

				if (accountID != null)
				{
					object orderType_SubID = GetValue<SOOrderType.freightSubID>(type);
					object customer_Location_SubID = GetValue<Location.cFreightSubID>(customerloc);
					object carrier_SubID = GetValue<Carrier.freightSalesSubID>(carrier);
                    object branch_SubID = GetValue<CRLocation.cMPFreightSubID>(companyloc);
					object employee_SubID = GetValue<EPEmployee.salesSubID>(employee);

					if (employee_SubID != null)
					subID = SOFreightSubAccountMaskAttribute.MakeSub<SOOrderType.freightSubMask>(this, type.FreightSubMask,
								new object[] { orderType_SubID, customer_Location_SubID, carrier_SubID, branch_SubID, employee_SubID },
								new Type[] { typeof(SOOrderType.freightSubID), typeof(Location.cFreightSubID), typeof(Carrier.freightSalesSubID), typeof(Location.cMPFreightSubID), typeof(EPEmployee.salesSubID) });
					else
						subID = SOFreightSubAccountMaskAttribute.MakeSub<SOOrderType.freightSubMask>(this, type.FreightSubMask,
							new object[] { orderType_SubID, customer_Location_SubID, carrier_SubID, branch_SubID, customer_Location_SubID },
							new Type[] { typeof(SOOrderType.freightSubID), typeof(Location.cFreightSubID), typeof(Carrier.freightSalesSubID), typeof(Location.cMPFreightSubID), typeof(Location.cFreightSubID) });
				}
			}
		}
		#endregion

		#region Discount

        public override void RecalculateDiscounts(PXCache sender, ARTran line)
        {
            if (PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>() && line.InventoryID != null && line.Qty != null && line.CuryTranAmt != null && line.IsFree != true)
            {
				object origCurrent = sender.CreateCopy(sender.Current);
                DateTime? docDate = Document.Current.DocDate;
                int? customerLocationID = Document.Current.CustomerLocationID;

				//Recalculate discounts on Sales Order date
				/*SOLine soline = PXSelect<SOLine, Where<SOLine.orderType, Equal<Required<SOLine.orderType>>,
				And<SOLine.orderNbr, Equal<Required<SOLine.orderNbr>>,
				And<SOLine.lineNbr, Equal<Required<SOLine.lineNbr>>>>>>.Select(this, line.SOOrderType, line.SOOrderNbr, line.SOOrderLineNbr);
				if (soline != null)
				{
					docDate = soline.OrderDate;
				}*/

				DiscountEngine.DiscountCalculationOptions discountCalculationOptions = DiscountEngine.DefaultARDiscountCalculationParameters | DiscountEngine.DiscountCalculationOptions.DisableFreeItemDiscountsCalculation;
				if (line.CalculateDiscountsOnImport == true)
					discountCalculationOptions = discountCalculationOptions | DiscountEngine.DiscountCalculationOptions.CalculateDiscountsFromImport;

				ARDiscountEngine.SetDiscounts(
					sender, 
					Transactions, 
					line, 
					DiscountDetails, 
					Document.Current.BranchID, 
					customerLocationID, 
					Document.Current.CuryID, 
					docDate, 
					recalcdiscountsfilter.Current, 
					discountCalculationOptions);

                RecalculateTotalDiscount();

				if (sender.Graph.IsMobile || sender.Graph.IsContractBasedAPI)
				{
					sender.Current = origCurrent;
				}
			}
			else if (!PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>() && Document.Current != null)
			{
				ARDiscountEngine.CalculateDocumentDiscountRate(Transactions.Cache, Transactions, line, DiscountDetails);
			}
		}

		public override void RecalculateTotalDiscount()
		{
			if (Document.Current != null)
			{
				ARInvoice old_row = PXCache<ARInvoice>.CreateCopy(Document.Current);
                Document.Cache.SetValueExt<ARInvoice.curyDiscTot>(Document.Current, ARDiscountEngine.GetTotalGroupAndDocumentDiscount(DiscountDetails));
				Document.Cache.RaiseRowUpdated(Document.Current, old_row);
			}
		}

		public bool ProrateDiscount
		{
			get
			{
				SOSetup sosetup = PXSelect<SOSetup>.Select(this);

				if (sosetup == null)
				{
					return true;//default true
				}
				else
				{
					if (sosetup.ProrateDiscounts == null)
						return true;
					else
						return sosetup.ProrateDiscounts == true;
				}

			}
		}
		#endregion

		private void UpdateRelatedSOOrderPreAuthAmount(CCTranType lastOperation, CCProcTran tran, IEnumerable<PXResult<CCProcTran>> ccProcTran)
		{
			var hasOrigDocRef = tran.OrigDocType != null && tran.OrigRefNbr != null;

			if ((lastOperation != CCTranType.PriorAuthorizedCapture || tran.TranType != CCTranTypeCode.Authorize) && hasOrigDocRef == true)
			{
				return;
			}

			UpdateRelatedSOOrder(tran, ccProcTran);
		}

		private void UpdateRelatedSOOrderCapturedAmount(CCTranType lastOperation, CCProcTran tran, IEnumerable<PXResult<CCProcTran>> ccProcTran)
		{
			var hasOrigDocRef = tran.OrigDocType != null && tran.OrigRefNbr != null;
			if ((lastOperation != CCTranType.VoidOrCredit || tran.TranType != CCTranTypeCode.VoidTran) && hasOrigDocRef == true)
			{
				return;
			}

			UpdateRelatedSOOrder(tran, ccProcTran);
		}

		private void UpdateRelatedSOOrder(CCProcTran tran, IEnumerable<PXResult<CCProcTran>> ccProcTran)
		{
			SOOrder soOrder = PXSelect<SOOrder, Where<SOOrder.orderType, Equal<Required<CCProcTran.origDocType>>,
					And<SOOrder.orderNbr, Equal<Required<CCProcTran.origRefNbr>>>>>.Select(this, tran.OrigDocType, tran.OrigRefNbr);

			if (soOrder == null)
			{
				return;
			}

			var orderState = CCProcTranHelper.UpdateCCPaymentState<SOOrder>(soOrder, ccProcTran);

			if (orderState.NeedUpdate == true)
			{
				soorder.Update(soOrder);
			}
		}

		public class PaymentTransaction : PaymentTransactionGraph<ARInvoiceEntry, ARInvoice>
		{
			protected override PaymentTransactionDetailMapping GetPaymentTransactionMapping()
			{
				return new PaymentTransactionDetailMapping(typeof(CCProcTran));
			}

			protected override PaymentMapping GetPaymentMapping()
			{
				return new PaymentMapping(typeof(SOInvoice));
			}

			protected void SOInvoiceRowSelectedHandler(PXCache cache ,PXRowSelectedEventArgs arg)
			{
				SOInvoice doc = arg.Row as SOInvoice;
				if (doc == null)
					return;
				SOInvoiceEntry graph = cache.Graph as SOInvoiceEntry;
				if (graph == null)
					return;
				ARInvoice arDoc = Base.Document.Current;
				bool validDocState = (arDoc != null) && (arDoc.OpenDoc == true && arDoc.Released == false);
				bool enableCCProcess = false;
				bool docTypePayment = (doc.DocType == ARDocType.Invoice || doc.DocType == ARDocType.CashSale);
				bool isCashReturn = doc.DocType == ARDocType.CashReturn;
				doc.IsCCPayment = false;
				if (doc.PMInstanceID != null)
				{
					PXResult<CustomerPaymentMethodC, CA.PaymentMethod> pmInstance = (PXResult<CustomerPaymentMethodC, CA.PaymentMethod>)
									   PXSelectJoin<CustomerPaymentMethodC,
										InnerJoin<CA.PaymentMethod,
											On<CA.PaymentMethod.paymentMethodID, Equal<CustomerPaymentMethodC.paymentMethodID>>>,
									Where<CustomerPaymentMethodC.pMInstanceID, Equal<Optional<SOInvoice.pMInstanceID>>,
										And<CA.PaymentMethod.paymentType, Equal<CA.PaymentMethodType.creditCard>,
											And<CA.PaymentMethod.aRIsProcessingRequired, Equal<True>>>>>.Select(this.Base, doc.PMInstanceID);
					if (pmInstance != null)
					{
						doc.IsCCPayment = true;
						enableCCProcess = IsDocTypeSuitableForCC(doc.DocType);
					}
				}
				if (arDoc != null)
					arDoc.IsCCPayment = doc.IsCCPayment;
				
				PaymentState paymentState = GetPaymentState();
				doc.CCPaymentStateDescr = paymentState.Description;
				doc.CCAuthTranNbr = paymentState.lastTran?.TranNbr;
				decimal? docBalance = ((ICCPayment)doc).CuryDocBal;
				bool isNotNullAmt = docBalance != null && docBalance > 0;
				bool canAuthorize = validDocState && docTypePayment && !(paymentState.isCCPreAuthorized || paymentState.isCCCaptured) && isNotNullAmt;
				bool canCapture = validDocState && docTypePayment && !(paymentState.isCCCaptured) && isNotNullAmt;
				bool canVoid = validDocState && ((paymentState.isCCCaptured || paymentState.isCCPreAuthorized) && docTypePayment);
				bool canValidate = doc.Hold == false && docTypePayment && paymentState.isOpenForReview && doc.DocType == ARDocType.CashSale;
				this.captureCCPayment.SetEnabled(enableCCProcess && canCapture);
				Base.action.SetVisible("Capture CC Payment", false);
				this.authorizeCCPayment.SetEnabled(enableCCProcess && canAuthorize);
				this.voidCCPayment.SetEnabled(enableCCProcess && canVoid);
				this.creditCCPayment.SetEnabled(enableCCProcess && isCashReturn && !paymentState.isCCRefunded && doc.RefTranExtNbr != null);
				this.recordCCPayment.SetVisible(false);
				this.recordCCPayment.SetEnabled(false);
				this.captureOnlyCCPayment.SetVisible(false);
				this.captureOnlyCCPayment.SetEnabled(false);

				bool getTranSupported = false;
				if (enableCCProcess && canValidate)
				{
					CCProcessingCenter procCenter = Base.ProcessingCenter.SelectSingle();
					getTranSupported = CCProcessingFeatureHelper.IsFeatureSupported(procCenter, CCProcessingFeature.TransactionGetter);
				}
				this.validateCCPayment.SetEnabled(enableCCProcess && canValidate && getTranSupported);
				this.validateCCPayment.SetVisible(doc.DocType == ARDocType.CashSale);

				PXUIFieldAttribute.SetEnabled<SOInvoice.refTranExtNbr>(cache, doc, doc.PMInstanceID.HasValue && doc.IsCCPayment == true && isCashReturn && !paymentState.isCCRefunded);
				PXUIFieldAttribute.SetVisible<SOInvoice.cCPaymentStateDescr>(cache, doc, enableCCProcess);
				bool allowPaymentInfo = Base.Document.Cache.AllowUpdate && (doc.DocType == ARDocType.CashSale || doc.DocType == ARDocType.CashReturn || doc.DocType == ARDocType.Invoice)
				                            && !paymentState.isCCPreAuthorized && !paymentState.isCCCaptured;
				bool isPMInstanceRequired = false;

				if (allowPaymentInfo && (String.IsNullOrEmpty(doc.PaymentMethodID) == false))
				{
					CA.PaymentMethod pm = PXSelect<CA.PaymentMethod, Where<CA.PaymentMethod.paymentMethodID, Equal<Required<CA.PaymentMethod.paymentMethodID>>>>.Select(this.Base, doc.PaymentMethodID);
					isPMInstanceRequired = (pm.IsAccountNumberRequired == true);
				}

				PXUIFieldAttribute.SetEnabled<SOInvoice.paymentMethodID>(graph.SODocument.Cache, doc, allowPaymentInfo);
				PXUIFieldAttribute.SetEnabled<SOInvoice.pMInstanceID>(graph.SODocument.Cache, doc, allowPaymentInfo && isPMInstanceRequired);

				bool isAuthorizedCashSale = (doc.DocType == ARDocType.CashSale && (paymentState.isCCPreAuthorized || paymentState.isCCCaptured));
				PXUIFieldAttribute.SetEnabled<ARInvoice.curyDiscTot>(cache, doc, !PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>() && !isAuthorizedCashSale);
			}

			public static void UpdateSOInvoiceState(IBqlTable aDoc, CCTranType lastOperation, bool success)
			{
				SOInvoice doc = aDoc as SOInvoice;
				SOInvoiceEntry graph = PXGraph.CreateInstance<SOInvoiceEntry>();
				graph.Document.Current = graph.Document.Search<ARInvoice.refNbr>(doc.RefNbr, doc.DocType);

				bool needUpdate = CCProcTranHelper.UpdateCapturedState<SOInvoice>(doc, graph.ccProcTran.Select());

				if (string.IsNullOrEmpty(doc.ExtRefNbr) && doc.IsCCCaptured == true)
				{
					CCProcTran currTran = CCProcTranHelper.FindCCLastSuccessfulTran(graph.ccProcTran);
					if (currTran != null)
					{
						doc.ExtRefNbr = currTran.PCTranNumber;
					}
				}
				if (doc.IsCCCaptured == false)
				{
					CCProcTran currTran = CCProcTranHelper.FindCCLastSuccessfulTran(graph.ccProcTran);
					graph.UpdateRelatedSOOrderCapturedAmount(lastOperation, currTran, graph.ccProcTran.Select());
				}

				if (needUpdate)
				{
					doc = graph.SODocument.Update(doc);
					graph.Document.Search<ARInvoice.refNbr>(doc.RefNbr, doc.DocType);
					if (doc.IsCCCaptured == true)
					{
						foreach (CCProcTran tran in graph.ccProcTran.Select())
						{
							if (String.IsNullOrEmpty(tran.RefNbr) || String.IsNullOrEmpty(tran.DocType))
							{
								tran.DocType = doc.DocType;
								tran.RefNbr = doc.RefNbr;
								graph.ccProcTran.Update(tran);
							}

							graph.UpdateRelatedSOOrderPreAuthAmount(lastOperation, tran, graph.ccProcTran.Select());
						}
					}
					graph.Save.Press();
				}
			}

			public override void Initialize()
			{
				base.Initialize();
				Type noCustType = CustomizedTypeManager.GetTypeNotCustomized(Base.GetType());
				if (noCustType != typeof(SOInvoiceEntry))
				{
					authorizeCCPayment.SetVisible(false);
					authorizeCCPayment.SetEnabled(false);
					captureCCPayment.SetVisible(false);
					captureCCPayment.SetEnabled(false);
					voidCCPayment.SetVisible(false);
					voidCCPayment.SetEnabled(false);
					creditCCPayment.SetVisible(false);
					creditCCPayment.SetEnabled(false);
					captureOnlyCCPayment.SetVisible(false);
					captureOnlyCCPayment.SetEnabled(false);
					recordCCPayment.SetVisible(false);
					recordCCPayment.SetEnabled(false);
					validateCCPayment.SetVisible(false);
					validateCCPayment.SetEnabled(false);
				}
				this.Base.RowSelected.AddHandler<SOInvoice>(SOInvoiceRowSelectedHandler);
			}
			protected override void MapViews(ARInvoiceEntry graph)
			{
				base.MapViews(graph);
				SOInvoiceEntry soInvoiceEntry = graph as SOInvoiceEntry;
				if (soInvoiceEntry != null)
				{
					PaymentTransaction = new PXSelectExtension<PaymentTransactionDetail>(soInvoiceEntry.ccProcTran);
				}
			}

			protected override IEnumerable<AfterTranProcDelegate> GetAfterAuthorizeActions()
			{
				yield return UpdateSOInvoiceState;
			}

			protected override IEnumerable<AfterTranProcDelegate> GetAfterCaptureActions()
			{
				yield return UpdateSOInvoiceState;
			}

			protected override IEnumerable<AfterTranProcDelegate> GetAfterVoidActions()
			{
				yield return UpdateSOInvoiceState;
			}

			protected override IEnumerable<AfterTranProcDelegate> GetAfterCreditActions()
			{
				yield return UpdateSOInvoiceState;
			}
		}
	}

    public class SOInvoiceEntryProjectFieldVisibilityGraphExtension : PXGraphExtension<SOInvoiceEntry>
    {
        protected virtual void _(Events.RowSelected<ARInvoice> e)
        {
            if (e.Row == null) return;

            PXUIFieldAttribute.SetVisible<ARInvoice.projectID>(e.Cache, e.Row,
                PXAccess.FeatureInstalled<FeaturesSet.contractManagement>() || PM.ProjectAttribute.IsPMVisible(BatchModule.SO) || PM.ProjectAttribute.IsPMVisible(BatchModule.AR));
            PXUIFieldAttribute.SetDisplayName<ARInvoice.projectID>(e.Cache, GL.Messages.ProjectContract);
        }
    }
}
