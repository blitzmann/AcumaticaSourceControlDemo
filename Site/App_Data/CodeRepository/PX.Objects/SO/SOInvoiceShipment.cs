using System;
using System.Collections;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.AR.MigrationMode;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.SM;
using POReceipt = PX.Objects.PO.POReceipt;

namespace PX.Objects.SO
{
	[PX.Objects.GL.TableAndChartDashboardType]
	public class SOInvoiceShipment : PXGraph<SOInvoiceShipment>
	{
		public PXCancel<SOShipmentFilter> Cancel;
		public PXAction<SOShipmentFilter> viewDocument;
		public PXFilter<SOShipmentFilter> Filter;
		[PXFilterable]
		public PXFilteredProcessing<SOShipment, SOShipmentFilter> Orders;
		public PXSelect<SOShipLine> dummy_select_to_bind_events;
		public PXSetup<SOSetup> sosetup;

		
		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXEditDetailButton]
		public virtual IEnumerable ViewDocument(PXAdapter adapter)
		{
			if (Orders.Current != null)
			{
				if (Orders.Current.ShipmentType == INDocType.DropShip)
				{
					PO.POReceiptEntry docgraph = PXGraph.CreateInstance<PO.POReceiptEntry>();
					docgraph.Document.Current = docgraph.Document.Search<POReceipt.receiptNbr>(Orders.Current.ShipmentNbr);

					throw new PXRedirectRequiredException(docgraph, true, PO.Messages.POReceipt) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				}
				else
				{
					SOShipmentEntry docgraph = PXGraph.CreateInstance<SOShipmentEntry>();
					docgraph.Document.Current = docgraph.Document.Search<SOShipment.shipmentNbr>(Orders.Current.ShipmentNbr);

					throw new PXRedirectRequiredException(docgraph, true, Messages.SOShipment) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				}
			}
			return adapter.Get();
		}

        public PXSelect<INSite> INSites;
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = Messages.SiteDescr, Visibility = PXUIVisibility.SelectorVisible)]
        protected virtual void INSite_Descr_CacheAttached(PXCache sender)
        {
        }

        public PXSelect<Carrier> Carriers;
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXUIField(DisplayName = Messages.CarrierDescr, Visibility = PXUIVisibility.SelectorVisible)]
        protected virtual void Carrier_Description_CacheAttached(PXCache sender)
        {
        }

        public SOInvoiceShipment()
		{
			ARSetupNoMigrationMode.EnsureMigrationModeDisabled(this);

			Orders.SetSelected<SOShipment.selected>();
            object item = sosetup.Current;
		}

		public virtual void SOShipmentFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
            if (e.Row == null) return;

            SOShipmentFilter filter = e.Row as SOShipmentFilter;
			if (filter != null && !String.IsNullOrEmpty(filter.Action))
			{
				Dictionary<string, object> parameters = Filter.Cache.ToDictionary(filter);
				Orders.SetProcessTarget(null, null, null, filter.Action, parameters);
			}
			int? action = GetActionIDByName(filter.Action);

			PXUIFieldAttribute.SetEnabled<SOShipmentFilter.invoiceDate>(sender, filter, action == SOShipmentEntryActionsAttribute.CreateInvoice || action == SOShipmentEntryActionsAttribute.CreateDropshipInvoice);
			PXUIFieldAttribute.SetEnabled<SOShipmentFilter.packagingType>(sender, filter, action != SOShipmentEntryActionsAttribute.CreateDropshipInvoice);
			PXUIFieldAttribute.SetEnabled<SOShipmentFilter.siteID>(sender, filter, action != SOShipmentEntryActionsAttribute.CreateDropshipInvoice);
			PXUIFieldAttribute.SetVisible<SOShipmentFilter.showPrinted>(sender, filter, action == SOShipmentEntryActionsAttribute.PrintLabels || action == SOShipmentEntryActionsAttribute.PrintPickList);

			PXUIFieldAttribute.SetDisplayName<SOShipment.shipmentNbr>(Orders.Cache, action == SOShipmentEntryActionsAttribute.CreateDropshipInvoice ? Messages.ReceiptNbr : Messages.ShipmentNbr);
			PXUIFieldAttribute.SetDisplayName<SOShipment.shipDate>(Orders.Cache, action == SOShipmentEntryActionsAttribute.CreateDropshipInvoice ? Messages.ReceiptDate : Messages.ShipmentDate);

			if (sosetup.Current.UseShipDateForInvoiceDate == true)
			{
				sender.RaiseExceptionHandling<SOShipmentFilter.invoiceDate>(filter, null, new PXSetPropertyException(Messages.UseInvoiceDateFromShipmentDateWarning, PXErrorLevel.Warning));
				PXUIFieldAttribute.SetEnabled<SOShipmentFilter.invoiceDate>(sender, filter, false);
			}

			bool warnShipNotInvoiced = (action == SOShipmentEntryActionsAttribute.PostInvoiceToIN
				&& (string)Orders.GetTargetFill(null, null, null, filter.Action, nameof(SOShipment.status)) != SOShipmentStatus.Completed
				&& sosetup.Current.UseShippedNotInvoiced != true && sosetup.Current.UseShipDateForInvoiceDate != true);
			Exception warnShipNotInvoicedExc = warnShipNotInvoiced ? new PXSetPropertyException(Messages.ShipNotInvoicedWarning, PXErrorLevel.Warning) : null;
			sender.RaiseExceptionHandling<SOShipmentFilter.action>(filter, null, warnShipNotInvoicedExc);

			bool showPrintSettings = PXAccess.FeatureInstalled<FeaturesSet.deviceHub>() &&
				(action == SOShipmentEntryActionsAttribute.PrintLabels ||
				action == SOShipmentEntryActionsAttribute.PrintPickList ||
				(filter != null && !String.IsNullOrEmpty(filter.Action) && SOReports.GetReportIDByName(Orders, filter.Action) == SOReports.PrintShipmentConfirmation));

			PXUIFieldAttribute.SetVisible<SOShipmentFilter.printWithDeviceHub>(sender, filter, showPrintSettings);
			PXUIFieldAttribute.SetVisible<SOShipmentFilter.definePrinterManually>(sender, filter, showPrintSettings);
			PXUIFieldAttribute.SetVisible<SOShipmentFilter.printerName>(sender, filter, showPrintSettings);

			PXUIFieldAttribute.SetEnabled<SOShipmentFilter.definePrinterManually>(sender, filter, filter.PrintWithDeviceHub == true);
			PXUIFieldAttribute.SetEnabled<SOShipmentFilter.printerName>(sender, filter, filter.PrintWithDeviceHub == true && filter.DefinePrinterManually == true);

			if (filter.PrintWithDeviceHub != true || filter.DefinePrinterManually != true)
			{
				filter.PrinterName = null;
			}

			bool showInvoiceSeparately = action.IsIn(SOShipmentEntryActionsAttribute.CreateInvoice, SOShipmentEntryActionsAttribute.CreateDropshipInvoice);
			PXUIFieldAttribute.SetEnabled<SOShipment.billSeparately>(Orders.Cache, null, showInvoiceSeparately
				&& PXLongOperation.GetStatus(this.UID) == PXLongRunStatus.NotExists);
			PXUIFieldAttribute.SetVisible<SOShipment.billSeparately>(Orders.Cache, null, showInvoiceSeparately);
		}

		protected bool _ActionChanged = false;

		public virtual void SOShipmentFilter_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			_ActionChanged = !sender.ObjectsEqual<SOShipmentFilter.action>(e.Row, e.OldRow);
			if (_ActionChanged && e.Row != null)
			{
				var row = ((SOShipmentFilter)e.Row);

				row.PackagingType = SOShipmentFilter.Both;

				if (GetActionIDByName(row.Action) == SOShipmentEntryActionsAttribute.CreateDropshipInvoice)
					row.SiteID = null;
			}

			if ((_ActionChanged || !sender.ObjectsEqual<SOShipmentFilter.definePrinterManually>(e.Row, e.OldRow) || !sender.ObjectsEqual<SOShipmentFilter.printWithDeviceHub>(e.Row, e.OldRow))
				&& Filter.Current != null && PXAccess.FeatureInstalled<FeaturesSet.deviceHub>() && Filter.Current.PrintWithDeviceHub == true && Filter.Current.DefinePrinterManually == true)
			{
				string actualReportID = SOReports.GetReportID(GetActionIDByName(Filter.Current.Action), null) ?? SOReports.GetReportIDByName(Orders, Filter.Current.Action);
				Filter.Current.PrinterName = new NotificationUtility(this).SearchPrinter(ARNotificationSource.Customer, actualReportID, Accessinfo.BranchID);
			}
		}
				
		public virtual IEnumerable orders()
		{
			PXUIFieldAttribute.SetDisplayName<SOShipment.customerID>(Caches[typeof(SOShipment)], Messages.CustomerID);

			SOShipmentFilter filter = Filter.Current;
			int? action = GetActionIDByName(filter.Action);
			if (action == null)
			{
				yield break;
			}

			if (_ActionChanged)
			{
				Orders.Cache.Clear();
			}

			PXSelectBase cmd;
			
			switch (action)
			{
				case SOShipmentEntryActionsAttribute.CreateInvoice:
					cmd = new PXSelectJoinGroupBy<SOShipment,
						InnerJoin<SOOrderShipment, On<SOOrderShipment.shipmentNbr, Equal<SOShipment.shipmentNbr>, And<SOOrderShipment.shipmentType, Equal<SOShipment.shipmentType>>>,
						InnerJoin<SOOrder, On<SOOrder.orderType, Equal<SOOrderShipment.orderType>, And<SOOrder.orderNbr, Equal<SOOrderShipment.orderNbr>>>,
						InnerJoin<SOOrderType, On<SOOrderShipment.FK.OrderType>,
						InnerJoin<INSite, 
							On<SOShipment.FK.Site>,
						InnerJoinSingleTable<Customer, On<SOOrder.customerID, Equal<Customer.bAccountID>>,
						LeftJoin<Carrier, On<SOShipment.FK.Carrier>>>>>>>,
						Where<SOShipment.confirmed, Equal<boolTrue>,
						And<SOOrderType.aRDocType, NotEqual<ARDocType.noUpdate>,
						And<SOOrderShipment.invoiceNbr, IsNull,
                        And2<Match<Customer, Current<AccessInfo.userName>>,
						And<Match<INSite, Current<AccessInfo.userName>>>>>>>,
						Aggregate<GroupBy<SOShipment.shipmentNbr,
						GroupBy<SOShipment.createdByID,
						GroupBy<SOShipment.lastModifiedByID,
						GroupBy<SOShipment.confirmed,
						GroupBy<SOShipment.ownerID,
						GroupBy<SOShipment.released,
						GroupBy<SOShipment.hold,
						GroupBy<SOShipment.resedential,
						GroupBy<SOShipment.saturdayDelivery,
						GroupBy<SOShipment.groundCollect,
						GroupBy<SOShipment.insurance,
						GroupBy<SOShipment.shippedViaCarrier,
						GroupBy<SOShipment.labelsPrinted>>>>>>>>>>>>>>>(this);
					break;
				case SOShipmentEntryActionsAttribute.PostInvoiceToIN:
					cmd = new PXSelectJoinGroupBy<SOShipment, 
					LeftJoin<Carrier, 
						On<SOShipment.FK.Carrier>,
					InnerJoin<INSite, 
						On<SOShipment.FK.Site>,
					InnerJoin<SOOrderShipment, On<SOOrderShipment.shipmentType, Equal<SOShipment.shipmentType>, And<SOOrderShipment.shipmentNbr, Equal<SOShipment.shipmentNbr>>>,
					InnerJoin<SOShipLine, On<SOShipLine.shipmentType, Equal<SOOrderShipment.shipmentType>, And<SOShipLine.shipmentNbr, Equal<SOOrderShipment.shipmentNbr>, And<SOShipLine.origOrderType, Equal<SOOrderShipment.orderType>, And<SOShipLine.origOrderNbr, Equal<SOOrderShipment.orderNbr>>>>>,
					InnerJoin<SOOrder, On<SOOrder.orderType, Equal<SOShipLine.origOrderType>, And<SOOrder.orderNbr, Equal<SOShipLine.origOrderNbr>>>,
					LeftJoinSingleTable<Customer, On<SOOrder.customerID, Equal<Customer.bAccountID>>,
					InnerJoin<SOOrderType, On<SOOrderShipment.FK.OrderType>>>>>>>>,
					Where<SOShipment.confirmed, Equal<boolTrue>, 
						And<SOOrderShipment.invtRefNbr, IsNull,
						And<SOShipLine.requireINUpdate, Equal<True>,
						And<SOOrderType.iNDocType, NotEqual<INTranType.noUpdate>,
                        And2<Where<Customer.bAccountID, IsNull, Or<Match<Customer, Current<AccessInfo.userName>>>>,
						And<Match<INSite, Current<AccessInfo.userName>>>>>>>>,
					Aggregate<GroupBy<SOShipment.shipmentNbr,
						GroupBy<SOShipment.createdByID,
						GroupBy<SOShipment.lastModifiedByID,
						GroupBy<SOShipment.confirmed,
						GroupBy<SOShipment.ownerID,
						GroupBy<SOShipment.released,
						GroupBy<SOShipment.hold,
						GroupBy<SOShipment.resedential,
						GroupBy<SOShipment.saturdayDelivery,
						GroupBy<SOShipment.groundCollect,
						GroupBy<SOShipment.saturdayDelivery,
						GroupBy<SOShipment.shippedViaCarrier,
						GroupBy<SOShipment.labelsPrinted>>>>>>>>>>>>>>>(this);
					break;
				case SOShipmentEntryActionsAttribute.CreateDropshipInvoice:
					cmd = new PXSelectJoinGroupBy<POReceipt,
					InnerJoin<SOOrderShipment, On<SOOrderShipment.shipmentNbr, Equal<POReceipt.receiptNbr>, And<SOOrderShipment.shipmentType, Equal<SOShipmentType.dropShip>>>,
					InnerJoin<SOOrder, On<SOOrder.orderType, Equal<SOOrderShipment.orderType>, And<SOOrder.orderNbr, Equal<SOOrderShipment.orderNbr>>>,
					InnerJoin<SOOrderType, On<SOOrderType.orderType, Equal<SOOrderShipment.orderType>>,
					InnerJoinSingleTable<Customer, On<SOOrder.customerID, Equal<Customer.bAccountID>>>>>>,
					Where<POReceipt.released, Equal<True>,
						//And2<Where<POReceiptLine.lineType, Equal<POLineType.goodsForDropShip>, Or<POReceiptLine.lineType, Equal<POLineType.nonStockForDropShip>>>,
						And<SOOrderType.aRDocType, NotEqual<ARDocType.noUpdate>,
						And<SOOrderShipment.invoiceNbr, IsNull,
                        And<Match<Customer, Current<AccessInfo.userName>>>>>>,
					Aggregate<GroupBy<POReceipt.receiptNbr,
						GroupBy<POReceipt.createdByID,
						GroupBy<POReceipt.lastModifiedByID,
						GroupBy<POReceipt.released,
						GroupBy<POReceipt.ownerID,
						GroupBy<POReceipt.hold>>>>>>>>(this);
					break;
				case SOShipmentEntryActionsAttribute.CancelReturn:
					cmd = new PXSelectJoinGroupBy<SOShipment, 
						InnerJoin<INSite, On<SOShipment.FK.Site>,
						LeftJoin<Carrier, On<SOShipment.FK.Carrier>,
						InnerJoin<SOOrderShipment, On<SOOrderShipment.shipmentType, Equal<SOShipment.shipmentType>, And<SOOrderShipment.shipmentNbr, Equal<SOShipment.shipmentNbr>>>,
						InnerJoin<SOOrder, On<SOOrder.orderType, Equal<SOOrderShipment.orderType>, And<SOOrder.orderNbr, Equal<SOOrderShipment.orderNbr>>>,
						InnerJoinSingleTable<Customer, On<SOOrder.customerID, Equal<Customer.bAccountID>>>>>>>,
					Where<SOShipment.labelsPrinted, Equal<False>,
                    And2<Match<Customer, Current<AccessInfo.userName>>,
                    And<Match<INSite, Current<AccessInfo.userName>>>>>,
					Aggregate<GroupBy<SOShipment.shipmentNbr,
						GroupBy<SOShipment.createdByID,
						GroupBy<SOShipment.lastModifiedByID,
						GroupBy<SOShipment.confirmed,
						GroupBy<SOShipment.ownerID,
						GroupBy<SOShipment.released,
						GroupBy<SOShipment.hold,
						GroupBy<SOShipment.resedential,
						GroupBy<SOShipment.saturdayDelivery,
						GroupBy<SOShipment.groundCollect,
						GroupBy<SOShipment.saturdayDelivery,
						GroupBy<SOShipment.shippedViaCarrier,
						GroupBy<SOShipment.labelsPrinted>>>>>>>>>>>>>>>(this);
					break;
				default:
					cmd = new PXSelectJoinGroupBy<SOShipment,
						LeftJoin<Carrier, On<SOShipment.FK.Carrier>,
						InnerJoin<INSite, 
							On<SOShipment.FK.Site>,
						InnerJoin<SOOrderShipment, On<SOOrderShipment.shipmentType, Equal<SOShipment.shipmentType>, And<SOOrderShipment.shipmentNbr, Equal<SOShipment.shipmentNbr>>>,
						InnerJoin<SOOrder, On<SOOrder.orderType, Equal<SOOrderShipment.orderType>, And<SOOrder.orderNbr, Equal<SOOrderShipment.orderNbr>>>,
						LeftJoinSingleTable<Customer, On<SOOrder.customerID, Equal<Customer.bAccountID>>>>>>>,
					Where2<Match<INSite, Current<AccessInfo.userName>>,
					And<Where<Customer.bAccountID, IsNull, Or<Match<Customer, Current<AccessInfo.userName>>>>>>,
					Aggregate<GroupBy<SOShipment.shipmentNbr,
						GroupBy<SOShipment.createdByID,
						GroupBy<SOShipment.lastModifiedByID,
						GroupBy<SOShipment.confirmed,
						GroupBy<SOShipment.ownerID,
						GroupBy<SOShipment.released,
						GroupBy<SOShipment.hold,
						GroupBy<SOShipment.resedential,
						GroupBy<SOShipment.saturdayDelivery,
						GroupBy<SOShipment.groundCollect,
						GroupBy<SOShipment.saturdayDelivery,
						GroupBy<SOShipment.shippedViaCarrier,
						GroupBy<SOShipment.labelsPrinted>>>>>>>>>>>>>>>(this);
					break;
			}

			if (typeof(PXSelectBase<SOShipment>).IsAssignableFrom(cmd.GetType()))
			{
				((PXSelectBase<SOShipment>)cmd).WhereAnd<Where<SOShipment.shipDate, LessEqual<Current<SOShipmentFilter.endDate>>>>();

				if (filter.SiteID != null)
				{
					((PXSelectBase<SOShipment>)cmd).WhereAnd<Where<SOShipment.siteID, Equal<Current<SOShipmentFilter.siteID>>>>();
				}

				if (!string.IsNullOrEmpty(filter.CarrierPluginID))
				{
					((PXSelectBase<SOShipment>)cmd).WhereAnd<Where<Carrier.carrierPluginID, Equal<Current<SOShipmentFilter.carrierPluginID>>>>();
				}

				if (!string.IsNullOrEmpty(filter.ShipVia))
				{
					((PXSelectBase<SOShipment>)cmd).WhereAnd<Where<SOShipment.shipVia, Equal<Current<SOShipmentFilter.shipVia>>>>();
				}

				if (filter.StartDate != null)
				{
					((PXSelectBase<SOShipment>)cmd).WhereAnd<Where<SOShipment.shipDate, GreaterEqual<Current<SOShipmentFilter.startDate>>>>();
				}

				if (filter.CustomerID != null)
				{
					((PXSelectBase<SOShipment>)cmd).WhereAnd<Where<SOShipment.customerID, Equal<Current<SOShipmentFilter.customerID>>>>();
				}

				if ( action == SOShipmentEntryActionsAttribute.PrintLabels && filter.ShowPrinted == false)
				{
					((PXSelectBase<SOShipment>)cmd).WhereAnd<Where<SOShipment.labelsPrinted, Equal<False>>>();
				}

				if (action == SOShipmentEntryActionsAttribute.PrintPickList && filter.ShowPrinted == false)
				{
					((PXSelectBase<SOShipment>)cmd).WhereAnd<Where<SOShipment.pickListPrinted, Equal<False>>>();
				}
								
				if (filter.PackagingType == SOShipmentFilter.Manual)
				{
					((PXSelectBase<SOShipment>)cmd).WhereAnd<Where<SOOrder.isManualPackage, Equal<True>>>();
				}
				else if (filter.PackagingType == SOShipmentFilter.Auto)
				{
					((PXSelectBase<SOShipment>)cmd).WhereAnd<Where<SOOrder.isManualPackage, NotEqual<True>>>();
				}

				int startRow = PXView.StartRow;
				int totalRows = 0;

				List<PXFilterRow> newFilters = new List<PXFilterRow>();
				foreach (PXFilterRow f in PXView.Filters)
				{
					if (string.Compare(f.DataField, "CustomerOrderNbr", StringComparison.OrdinalIgnoreCase) == 0)
					{
						f.DataField = "SOOrder__CustomerOrderNbr";
					}
					newFilters.Add(f);
				}

				foreach (object res in ((PXSelectBase<SOShipment>)cmd).View.Select(null, null, PXView.Searches, PXView.SortColumns, PXView.Descendings, newFilters.ToArray(), ref startRow, PXView.MaximumRows, ref totalRows))
				{
					SOShipment order = PXResult.Unwrap<SOShipment>(res);
					SOOrder so = PXResult.Unwrap<SOOrder>(res);

					if (order.BilledOrderCntr + order.UnbilledOrderCntr + order.ReleasedOrderCntr == 1)
					{
						order.CustomerOrderNbr = so.CustomerOrderNbr;
					}

					SOShipment cached = (SOShipment)Orders.Cache.Locate(order);
					if (cached != null)
					{
						order.Selected = cached.Selected;
						order.BillSeparately = cached.BillSeparately;
					}
					yield return order;
				}
				PXView.StartRow = 0;
			}

			if (typeof(PXSelectBase<POReceipt>).IsAssignableFrom(cmd.GetType()))
			{
				((PXSelectBase<POReceipt>)cmd).WhereAnd<Where<POReceipt.receiptDate, LessEqual<Current<SOShipmentFilter.endDate>>>>();

				if (filter.CustomerID != null)
				{
					((PXSelectBase<POReceipt>)cmd).WhereAnd<Where<SOOrderShipment.customerID, Equal<Current<SOShipmentFilter.customerID>>>>();
				}

				if (filter.ShipVia != null)
				{
					((PXSelectBase<POReceipt>)cmd).WhereAnd<Where<SOOrder.shipVia, Equal<Current<SOShipmentFilter.shipVia>>>>();
				}

				if (filter.CarrierPluginID != null)
				{
					((PXSelectBase<POReceipt>)cmd).Join<InnerJoin<Carrier, On<Carrier.carrierID, Equal<SOOrder.shipVia>>>>();
					((PXSelectBase<POReceipt>)cmd).WhereAnd<Where<Carrier.carrierPluginID, Equal<Current<SOShipmentFilter.carrierPluginID>>>>();
				}

				if (filter.StartDate != null)
				{
					((PXSelectBase<POReceipt>)cmd).WhereAnd<Where<POReceipt.receiptDate, GreaterEqual<Current<SOShipmentFilter.startDate>>>>();
				}

				foreach (PXResult<POReceipt, SOOrderShipment, SOOrder> res in ((PXSelectBase<POReceipt>)cmd).Select())
				{
					SOShipment order = res;
					SOShipment cached = (SOShipment)Orders.Cache.Locate(order);

					if (cached == null)
						Orders.Cache.SetStatus(order, PXEntryStatus.Held);
					else
					{
						order.Selected = cached.Selected;
						order.BillSeparately = cached.BillSeparately;
					}
					yield return order;
				}
			}
			Orders.Cache.IsDirty = false;
		}

		protected virtual int? GetActionIDByName(string actionName)
		{
			if (actionName == "<SELECT>")
			{
				return null;
			}
			string actionID = (string)Orders.GetTargetFill(null, null, null, actionName, "@actionID");
			int action = 0;
			int.TryParse(actionID, out action);
			return action;
		}
	}

    [Serializable]
	public partial class SOShipmentFilter : IBqlTable, PX.SM.IPrintable
	{
		#region Action
		public abstract class action : PX.Data.BQL.BqlString.Field<action> { }
		protected string _Action;
		[PXAutomationMenu]
		public virtual string Action
		{
			get
			{
				return this._Action;
			}
			set
			{
				this._Action = value;
			}
		}
		#endregion
		#region SiteID
		public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		protected Int32? _SiteID;
		[IN.Site(DisplayName = "Warehouse", DescriptionField = typeof(INSite.descr))]
		public virtual Int32? SiteID
		{
			get
			{
				return this._SiteID;
			}
			set
			{
				this._SiteID = value;
			}
		}
		#endregion
		#region StartDate
		public abstract class startDate : PX.Data.BQL.BqlDateTime.Field<startDate> { }
		protected DateTime? _StartDate;
		[PXDBDate()]
		[PXUIField(DisplayName = "Start Date", Visibility = PXUIVisibility.SelectorVisible, Required = false)]
		[PXDefault()]
		public virtual DateTime? StartDate
		{
			get
			{
				return this._StartDate;
			}
			set
			{
				this._StartDate = value;
			}
		}
		#endregion
		#region EndDate
		public abstract class endDate : PX.Data.BQL.BqlDateTime.Field<endDate> { }
		protected DateTime? _EndDate;
		[PXDBDate()]
		[PXUIField(DisplayName = "End Date", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDefault(typeof(AccessInfo.businessDate))]
		public virtual DateTime? EndDate
		{
			get
			{
				return this._EndDate;
			}
			set
			{
				this._EndDate = value;
			}
		}
		#endregion
		#region CarrierPluginID
		public abstract class carrierPluginID : PX.Data.BQL.BqlString.Field<carrierPluginID> { }
		protected String _CarrierPluginID;
		[PXDBString(15, IsUnicode = true, InputMask = ">aaaaaaaaaaaaaaa")]
		[PXUIField(DisplayName = "Carrier", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(Search<CarrierPlugin.carrierPluginID>))]
		public virtual String CarrierPluginID
		{
			get
			{
				return this._CarrierPluginID;
			}
			set
			{
				this._CarrierPluginID = value;
			}
		}
		#endregion
		#region ShipVia
		public abstract class shipVia : PX.Data.BQL.BqlString.Field<shipVia> { }
		protected String _ShipVia;
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Ship Via")]
		[PXSelector(typeof(Search<Carrier.carrierID>), DescriptionField = typeof(Carrier.description), CacheGlobal = true)]
		public virtual String ShipVia
		{
			get
			{
				return this._ShipVia;
			}
			set
			{
				this._ShipVia = value;
			}
		}
		#endregion
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		protected int? _CustomerID;
		[PXUIField(DisplayName = "Customer")]
		[Customer(DescriptionField = typeof(Customer.acctName))]
		public virtual int? CustomerID
		{
			get
			{
				return _CustomerID;
			}
			set
			{
				_CustomerID = value;
			}
		}
		#endregion
		#region InvoiceDate
		public abstract class invoiceDate : PX.Data.BQL.BqlDateTime.Field<invoiceDate> { }
		protected DateTime? _InvoiceDate;
		[PXDBDate]
		[PXUIField(DisplayName = "Invoice Date")]
		[PXDefault(typeof(AccessInfo.businessDate))]
		public virtual DateTime? InvoiceDate
		{
			get
			{
				return _InvoiceDate;
			}
			set
			{
				_InvoiceDate = value;
			}
		}
		#endregion
		#region PackagingType
		public abstract class packagingType : PX.Data.BQL.BqlString.Field<packagingType> { }

		public const string Auto = "A";
		public const string Manual = "M";
		public const string Both = "B";
		protected String _PackagingType;
		[PXDBString(1, IsFixed = true)]
		[PXDefault("B")]
		[PackagingTypeList]
		[PXUIField(DisplayName = "Packaging Type")]
		public virtual String PackagingType
		{
			get
			{
				return this._PackagingType;
			}
			set
			{
				this._PackagingType = value;
			}
		}

	    public class PackagingTypeListAttribute : PXStringListAttribute
	    {
		    public PackagingTypeListAttribute() : base(
			    new[]
				{
					Pair(Both, Messages.PackagingType_Both),
					Pair(Auto, Messages.PackagingType_Auto),
					Pair(Manual, Messages.PackagingType_Manual),
				}) {}
	    }
		#endregion
		#region ShowPrinted
		public abstract class showPrinted : PX.Data.BQL.BqlBool.Field<showPrinted> { }
		protected bool? _ShowPrinted;
		[PXBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Show Printed")]
		public virtual bool? ShowPrinted
		{
			get
			{
				return this._ShowPrinted;
			}
			set
			{
				this._ShowPrinted = value;
			}
		}
		#endregion
		#region PrintWithDeviceHub
		public abstract class printWithDeviceHub : PX.Data.BQL.BqlBool.Field<printWithDeviceHub> { }
		protected bool? _PrintWithDeviceHub;
		[PXDBBool]
		[PXDefault(typeof(FeatureInstalled<FeaturesSet.deviceHub>))]
		[PXUIField(DisplayName = "Print with DeviceHub")]
		public virtual bool? PrintWithDeviceHub
		{
			get
			{
				return _PrintWithDeviceHub;
			}
			set
			{
				_PrintWithDeviceHub = value;
			}
		}
		#endregion
		#region DefinePrinterManually
		public abstract class definePrinterManually : PX.Data.BQL.BqlBool.Field<definePrinterManually> { }
		protected bool? _DefinePrinterManually = false;
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Define Printer Manually")]
		public virtual bool? DefinePrinterManually
		{
			get
			{
				return _DefinePrinterManually;
			}
			set
			{
				_DefinePrinterManually = value;
			}
		}
		#endregion
		#region Printer
		public abstract class printerName : PX.Data.BQL.BqlString.Field<printerName> { }
		protected string _PrinterName;
		[PXPrinterSelector]
		public virtual string PrinterName
		{
			get
			{
				return this._PrinterName;
			}
			set
			{
				this._PrinterName = value;
			}
		}
		#endregion
	}
}
