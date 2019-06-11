using PX.Common;
using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WMSBase = PX.Objects.IN.WarehouseManagementSystemGraph<PX.Objects.IN.INScanReceive, PX.Objects.IN.INScanReceiveHost, PX.Objects.IN.INRegister, PX.Objects.IN.INScanReceive.Header>;

namespace PX.Objects.IN
{
	public class INScanReceiveHost : INReceiptEntry
	{
		public override Type PrimaryItemType => typeof(INScanReceive.Header);
		public PXFilter<INScanReceive.Header> HeaderView;
	}

	public class INScanReceive : WMSBase
	{
		public class UserSetup : PXUserSetupPerMode<UserSetup, INScanReceiveHost, Header, INScanUserSetup, INScanUserSetup.userID, INScanUserSetup.mode, Modes.scanReceipt> { }

		#region DACs
		public class Header : WMSHeader, ILSMaster
		{
			#region ReceiptNbr
			[PXUnboundDefault(typeof(INRegister.refNbr))]
			[PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
			[PXUIField(DisplayName = "Reference Nbr.", Enabled = false)]
			[PXSelector(typeof(Search<INRegister.refNbr, Where<INRegister.docType, Equal<INDocType.receipt>>>))]
			public override string RefNbr { get; set; }
			public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
			#endregion
			#region TranDate
			[PXDate]
			[PXUnboundDefault(typeof(AccessInfo.businessDate))]
			public virtual DateTime? TranDate { get; set; }
			public abstract class tranDate : PX.Data.BQL.BqlDateTime.Field<tranDate> { }
			#endregion
			#region SiteID
			[Site]
			public virtual int? SiteID { get; set; }
			public abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
			#endregion
			#region LocationID
			[Location]
			public virtual int? LocationID { get; set; }
			public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
			#endregion
			#region InventoryID
			public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
			#endregion
			#region SubItemID
			public new abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
			#endregion
			#region LotSerialNbr
			[INLotSerialNbr(typeof(inventoryID), typeof(subItemID), typeof(locationID), PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual string LotSerialNbr { get; set; }
			public abstract class lotSerialNbr : PX.Data.BQL.BqlString.Field<lotSerialNbr> { }
			#endregion
			#region LotSerTrack
			[PXString(1, IsFixed = true)]
			public virtual String LotSerTrack { get; set; }
			public abstract class lotSerTrack : PX.Data.BQL.BqlString.Field<lotSerTrack> { }
			#endregion
			#region LotSerTrackExpiration
			[PXBool]
			public virtual Boolean? LotSerTrackExpiration { get; set; }
			public abstract class lotSerTrackExpiration : PX.Data.BQL.BqlBool.Field<lotSerTrackExpiration> { }
			#endregion
			#region LotSerAssign
			[PXString(1, IsFixed = true)]
			public virtual String LotSerAssign { get; set; }
			public abstract class lotSerAssign : PX.Data.BQL.BqlString.Field<lotSerAssign> { } 
			#endregion
			#region ExpirationDate
			[INExpireDate(typeof(inventoryID), PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual DateTime? ExpireDate { get; set; }
			public abstract class expireDate : PX.Data.BQL.BqlDateTime.Field<expireDate> { }
			#endregion
			#region ILSMaster implementation
			public string TranType => string.Empty;
			public short? InvtMult { get => -1; set { } }
			public int? ProjectID { get; set; }
			public int? TaskID { get; set; }
			#endregion
		}
		#endregion

		#region Views
		public override PXFilter<Header> HeaderView => Base.HeaderView;
		public PXSetupOptional<INScanSetup, Where<INScanSetup.branchID, Equal<Current<AccessInfo.branchID>>>> Setup;
		#endregion

		#region Buttons
		public PXAction<Header> ScanRelease;
		[PXButton, PXUIField(DisplayName = "Release")]
		protected virtual IEnumerable scanRelease(PXAdapter adapter) => scanBarcode(adapter, ScanCommands.Release);

		public PXAction<Header> Review;
		[PXButton, PXUIField(DisplayName = "Review")]
		protected virtual IEnumerable review(PXAdapter adapter) => adapter.Get();
		#endregion

		#region Event Handlers
		protected override void _(Events.RowSelected<Header> e)
		{
			base._(e);

			ScanModeInReceive.SetEnabled(e.Row != null && e.Row.Mode != Modes.ScanInReceive);

			Review.SetVisible(Base.IsMobile);

			new[] {
				ScanRemove,
				ScanRelease
			}.Modify(b => b.SetEnabled(Receipt?.Released != true && e.Row?.ScanState == ScanStates.Item &&
				Base.transactions.SelectMain().Any())).Consume();

			ScanConfirm.SetEnabled(Receipt?.Released != true && e.Row?.ScanState == ScanStates.Confirm);

			Logs.AllowInsert = Logs.AllowDelete = Logs.AllowUpdate = false;
			Base.transactions.AllowInsert = false;
			Base.transactions.AllowDelete = Base.transactions.AllowUpdate = (Receipt == null || Receipt.Released != true);
		}

		protected virtual void _(Events.RowSelected<INTran> e)
		{
			bool isMobileAndNotReleased = Base.IsMobile && (Receipt == null || Receipt.Released != true);

			Base.transactions.Cache
			.Adjust<PXUIFieldAttribute>()
			.For<INTran.inventoryID>(ui => ui.Enabled = false)
			.SameFor<INTran.tranDesc>()
			.SameFor<INTran.locationID>()
			.SameFor<INTran.qty>()
			.SameFor<INTran.uOM>()
			.For<INTran.lotSerialNbr>(ui => ui.Enabled = isMobileAndNotReleased)
			.SameFor<INTran.expireDate>()
			.SameFor<INTran.reasonCode>();
		}

		protected virtual void _(Events.FieldDefaulting<Header, Header.siteID> e) => e.NewValue = IsWarehouseRequired() ? null : DefaultSiteID;

		protected virtual void _(Events.RowPersisted<Header> e)
		{
			e.Row.RefNbr = Receipt?.RefNbr;
			e.Row.TranDate = Receipt?.TranDate;

			Base.transactions.Cache.Clear();
			Base.transactions.Cache.ClearQueryCache();
		}

		protected virtual void _(Events.FieldUpdated<Header, Header.refNbr> e)
		{
			Base.receipt.Current = Base.receipt.Search<INRegister.refNbr>(e.Row.RefNbr, INDocType.Receipt);
		}

		protected virtual void _(Events.RowUpdated<INScanUserSetup> e) => e.Row.IsOverridden = !e.Row.SameAs(Setup.Current);
		protected virtual void _(Events.RowInserted<INScanUserSetup> e) => e.Row.IsOverridden = !e.Row.SameAs(Setup.Current);

		protected virtual void _(Events.RowSelected<INRegister> e)
		{
			if (HeaderView.Current.ScanState == ScanStates.Wait)
			{
				if (e.Row?.Released == true)
				{
					if (Base.IsMobile)
						Clear(Msg.DocumentIsReleased);
					else
						SetScanState(GetDefaultState(), Msg.DocumentIsReleased);
				}
			}
		}
		#endregion

		private INRegister Receipt => Base.CurrentDocument.Current;

		protected virtual bool IsWarehouseRequired() => UserSetup.For(Base).DefaultWarehouse != true || DefaultSiteID == null;
		protected virtual bool IsLotSerialRequired() => UserSetup.For(Base).DefaultLotSerialNumber != true;
		protected virtual bool IsExpirationDateRequired() => UserSetup.For(Base).DefaultExpireDate != true || EnsureExpireDateDefault() == null;

		protected override WMSModeOf<INScanReceive, INScanReceiveHost> DefaultMode => Modes.ScanInReceive;
		public override string CurrentModeName =>
			HeaderView.Current.Mode == Modes.ScanInReceive ? Msg.ScanInReceiveMode :
			Msg.FreeMode;
		protected override string GetModePrompt()
		{
			if (HeaderView.Current.Mode == Modes.ScanInReceive)
			{
				if (HeaderView.Current.SiteID == null)
					return Localize(Msg.WarehousePrompt);
				if (HeaderView.Current.InventoryID == null)
					return Localize(Msg.InventoryPrompt);
				if (HeaderView.Current.LotSerialNbr == null && IsLotSerialRequired())
					return Localize(Msg.LotSerialPrompt);
				if (HeaderView.Current.LocationID == null)
					return Localize(Msg.LocationPrompt);
				return Localize(Msg.ConfirmationPrompt);
			}
			return null;
		}

		protected override bool ProcessCommand(string barcode)
		{
			switch (barcode)
			{
				case ScanCommands.Confirm:
					if (HeaderView.Current.Remove != true) ProcessConfirm();
					else ProcessConfirmRemove();
					return true;

				case ScanCommands.Remove:
					HeaderView.Current.Remove = true;
					SetScanState(ScanStates.Item, Msg.RemoveMode);
					return true;

				case ScanCommands.Release:
					ProcessRelease();
					return true;
			}
			return false;
		}

		protected override bool ProcessByState(Header doc)
		{
			if (Receipt?.Released == true)
			{
				ClearHeaderInfo();
				HeaderView.Current.RefNbr = null;
				Base.CurrentDocument.Insert();
			}

			switch (doc.ScanState)
			{
				case ScanStates.Warehouse:
					ProcessWarehouse(doc.Barcode);
					return true;
				default:
					return base.ProcessByState(doc);
			}
		}

		protected virtual void ProcessWarehouse(string barcode)
		{
			INSite site =
				PXSelectReadonly<INSite,
				Where<INSite.siteCD, Equal<Required<Header.barcode>>>>
				.Select(Base, barcode);

			if (site == null)
			{
				ReportError(Msg.WarehouseMissing, barcode);
			}
			else if (IsValid<Header.siteID>(site.SiteID, out string error) == false)
			{
				ReportError(error);
				return;
			}
			else
			{
				HeaderView.Current.SiteID = site.SiteID;
				SetScanState(ScanStates.Item, Msg.WarehouseReady, site.SiteCD);
			}
		}

		protected override void ProcessItemBarcode(string barcode)
		{
			var item = ReadItemByBarcode(barcode, INPrimaryAlternateType.CPN);
			if (item == null)
			{
				ReportError(Msg.InventoryMissing, barcode);
				return;
			}

			INItemXRef xref = item;
			InventoryItem inventoryItem = item;
			INLotSerClass lsclass = item;
			var uom = xref.UOM ?? inventoryItem.PurchaseUnit;

			if (lsclass.LotSerTrack == INLotSerTrack.SerialNumbered &&
				HeaderView.Current.LotSerAssign != INLotSerAssign.WhenUsed &&
				uom != inventoryItem.BaseUnit)
			{
				ReportError(Msg.SerialItemNotComplexQty);
				return;
			}

			HeaderView.Current.InventoryID = xref.InventoryID;
			HeaderView.Current.SubItemID = xref.SubItemID;
			HeaderView.Current.UOM = uom;
			HeaderView.Current.LotSerTrack = lsclass.LotSerTrack;
			HeaderView.Current.LotSerTrackExpiration = lsclass.LotSerTrackExpiration;
			HeaderView.Current.LotSerAssign = lsclass.LotSerAssign;

			if (IsLotSerialRequired() && lsclass.LotSerTrack != INLotSerTrack.NotNumbered && lsclass.LotSerAssign == INLotSerAssign.WhenReceived)
			{
				SetScanState(ScanStates.LotSerial, Msg.InventoryReady, inventoryItem.InventoryCD);
			}
			else
			{
				SetScanState(ScanStates.Location, Msg.InventoryReady, inventoryItem.InventoryCD);
			}
		}

		protected override void ProcessLotSerialBarcode(string barcode)
		{
			if (IsValid<Header.lotSerialNbr>(barcode, out string error) == false)
			{
				ReportError(error);
				return;
			}

			HeaderView.Current.LotSerialNbr = barcode;
			SetScanState((HeaderView.Current.LotSerTrackExpiration == true && IsExpirationDateRequired()) ?
				ScanStates.ExpireDate : ScanStates.Location, Msg.LotSerialReady, barcode);
		}

		protected override void ProcessExpireDate(string barcode)
		{
			if (DateTime.TryParse(barcode.Trim(), out DateTime value) == false)
			{
				ReportError(Msg.LotSerialExpireDateBadFormat);
				return;
			}

			if (IsValid<Header.expireDate>(value, out string error) == false)
			{
				ReportError(error);
				return;
			}

			HeaderView.Current.ExpireDate = value;
			SetScanState(ScanStates.Location, Msg.LotSerialExpireDateReady, barcode);
		}

		protected override void ProcessLocationBarcode(string barcode)
		{
			INLocation location = ReadLocationByBarcode(HeaderView.Current.SiteID, barcode);
			if (location == null)
				return;

			HeaderView.Current.LocationID = location.LocationID;
			SetScanState(ScanStates.Confirm, Msg.LocationReady, location.LocationCD);
		}

		protected override bool ProcessQtyBarcode(string barcode)
		{
			var result = base.ProcessQtyBarcode(barcode);

			if (HeaderView.Current.LotSerTrack == INLotSerTrack.SerialNumbered &&
				HeaderView.Current.LotSerAssign != INLotSerAssign.WhenUsed &&
				HeaderView.Current.Qty != 1)
			{
				HeaderView.Current.Qty = 1;
				ReportError(Msg.SerialItemNotComplexQty);
			}

			return result;
		}

		protected virtual void ProcessConfirm()
		{
			if (!ValidateConfirmation())
			{
				if (ExplicitLineConfirmation == false)
					ClearHeaderInfo();
				return;
			}

			var header = HeaderView.Current;
			bool isSerialItem = HeaderView.Current.LotSerTrack == INLotSerTrack.SerialNumbered;

			if (Receipt == null) Base.CurrentDocument.Insert();

			INTran existTransaction = FindReceiptRow(header);

			if (existTransaction != null)
			{
				var newQty = existTransaction.Qty + header.Qty;

				if (HeaderView.Current.LotSerTrack == INLotSerTrack.SerialNumbered &&
					HeaderView.Current.LotSerAssign == INLotSerAssign.WhenReceived &&
					newQty != 1)
				{
					if (ExplicitLineConfirmation == false)
						ClearHeaderInfo();
					ReportError(Msg.SerialItemNotComplexQty);
					return;
				}

				Base.transactions.Cache.SetValueExt<INTran.qty>(existTransaction, newQty);
				existTransaction = Base.transactions.Update(existTransaction);
			}
			else
			{
				INTran tran = Base.transactions.Insert();
				Base.transactions.Cache.SetValueExt<INTran.inventoryID>(tran, header.InventoryID);
				tran = existTransaction = Base.transactions.Update(tran);

				Base.transactions.Cache.SetValueExt<INTran.siteID>(tran, header.SiteID);
				Base.transactions.Cache.SetValueExt<INTran.locationID>(tran, header.LocationID);
				Base.transactions.Cache.SetValueExt<INTran.uOM>(tran, header.UOM);
				Base.transactions.Cache.SetValueExt<INTran.qty>(tran, header.Qty);
				Base.transactions.Cache.SetValueExt<INTran.expireDate>(tran, header.ExpireDate);
				Base.transactions.Cache.SetValueExt<INTran.lotSerialNbr>(tran, header.LotSerialNbr);
				existTransaction = Base.transactions.Update(tran);
			}

			if (!string.IsNullOrEmpty(header.LotSerialNbr))
			{
				foreach (INTranSplit split in Base.splits.Select())
				{
					Base.splits.Cache.SetValueExt<INTranSplit.expireDate>(split, header.ExpireDate ?? existTransaction.ExpireDate);
					Base.splits.Cache.SetValueExt<INTranSplit.lotSerialNbr>(split, header.LotSerialNbr);
					Base.splits.Update(split);
				}
			}

			ClearHeaderInfo();
			SetScanState(ScanStates.Item, Msg.InventoryAdded, Base.transactions.Cache.GetValueExt<INTran.inventoryID>(existTransaction), existTransaction.Qty, header.UOM);
			if (!isSerialItem)
				HeaderView.Current.IsQtyOverridable = true;
		}

		protected virtual bool ValidateConfirmation()
		{
			var needLotSerialNbr = IsLotSerialRequired() && HeaderView.Current.LotSerTrack != INLotSerTrack.NotNumbered &&
				HeaderView.Current.LotSerAssign != INLotSerAssign.WhenUsed;

			if (needLotSerialNbr && HeaderView.Current.LotSerialNbr == null)
			{
				ReportError(Msg.LotSerialNotSet);
				return false;
			}
			if (needLotSerialNbr && HeaderView.Current.LotSerTrackExpiration == true &&
				!HeaderView.Current.ExpireDate.HasValue && IsExpirationDateRequired())
			{
				ReportError(Msg.LotSerialExpireDateNotSet);
				return false;
			}
			if (HeaderView.Current.LotSerTrack == INLotSerTrack.SerialNumbered &&
				HeaderView.Current.LotSerAssign != INLotSerAssign.WhenUsed &&
				HeaderView.Current.Qty != 1)
			{
				ReportError(Msg.SerialItemNotComplexQty);
				return false;
			}


			return true;
		}

		protected virtual void ProcessConfirmRemove()
		{
			if (!ValidateConfirmation())
			{
				if (ExplicitLineConfirmation == false)
					ClearHeaderInfo();
				return;
			}

			var header = HeaderView.Current;
			bool isSerialItem = HeaderView.Current.LotSerTrack == INLotSerTrack.SerialNumbered;

			INTran existTransaction = FindReceiptRow(header);

			if (existTransaction != null)
			{
				if (existTransaction.Qty == header.Qty)
				{
					Base.transactions.Delete(existTransaction);
				}
				else
				{
					var newQty = existTransaction.Qty - header.Qty;

					if (!IsValid<INTran.qty, INTran>(existTransaction, newQty, out string error))
					{
						if (ExplicitLineConfirmation == false)
							ClearHeaderInfo();
						ReportError(error);
						return;
					}

					Base.transactions.Cache.SetValueExt<INTran.qty>(existTransaction, newQty);
					Base.transactions.Update(existTransaction);
				}

				SetScanState(ScanStates.Item, Msg.InventoryRemoved, Base.transactions.Cache.GetValueExt<INTran.inventoryID>(existTransaction), header.Qty, header.UOM);
				ClearHeaderInfo();
				if (!isSerialItem)
					HeaderView.Current.IsQtyOverridable = true;
			}
			else
			{
				ClearHeaderInfo();
				ReportError(Msg.ReceiptLineMissing, Base.transactions.Cache.GetValueExt<INTran.inventoryID>(existTransaction));
				SetScanState(ScanStates.Item);
			}
		}

		protected virtual void ProcessRelease()
		{
			if (Receipt != null)
			{
				if (Receipt.Released == true)
				{
					ReportError(Messages.Document_Status_Invalid);
					return;
				}

				if (Receipt.Hold != false) Base.CurrentDocument.Cache.SetValueExt<INRegister.hold>(Receipt, false);

				Save.Press();

				INScanReceiveHost clone = Base.Clone();

				bool printInventory = UserSetup.For(Base).PrintInventoryLabelsAutomatically == true;
				string printLabelsReportID = UserSetup.For(Base).InventoryLabelsReportID;
				INRegister receipt = Receipt;

				WaitFor(() =>
				{
					INDocumentRelease.ReleaseDoc(new List<INRegister>() { receipt }, false);
					if (PXAccess.FeatureInstalled<FeaturesSet.deviceHub>() && printInventory && receipt.RefNbr != null && !string.IsNullOrEmpty(printLabelsReportID))
					{
						var reportParameters = new Dictionary<string, string>()
						{
							[nameof(INRegister.RefNbr)] = receipt.RefNbr
						};

						PrintReportViaDeviceHub<BAccount>(Base, printLabelsReportID, reportParameters, INNotificationSource.None, null);
					}
					PXLongOperation.SetCustomInfo(clone); // Redirect
				}, Msg.DocumentReleasing, Base.receipt.Current.RefNbr);
			}
		}

		protected virtual INTran FindReceiptRow(Header header)
		{
			var existTransactions = Base.transactions.SelectMain().Where(t =>
				t.InventoryID == header.InventoryID &&
				t.SiteID == header.SiteID &&
				t.LocationID == (header.LocationID ?? t.LocationID) &&
				t.UOM == header.UOM);

			INTran existTransaction = null;

			if (IsLotSerialRequired())
			{
				foreach (var tran in existTransactions)
				{
					Base.transactions.Current = tran;
					if (Base.splits.SelectMain().Any(t => (t.LotSerialNbr ?? "") == (header.LotSerialNbr ?? "")))
					{
						existTransaction = tran;
						break;
					}
				}
			}
			else
			{
				existTransaction = existTransactions.FirstOrDefault();
			}

			return existTransaction;
		}

		protected override void ClearHeaderInfo(bool redirect = false)
		{
			base.ClearHeaderInfo(redirect);

			if (redirect)
			{
				HeaderView.Current.SiteID = null;
			}
			HeaderView.Current.LotSerialNbr = null;
			HeaderView.Current.LotSerTrack = null;
			HeaderView.Current.LotSerTrackExpiration = null;
			HeaderView.Current.LotSerAssign = null;
			HeaderView.Current.ExpireDate = null;
			HeaderView.Current.LocationID = null;
		}

		protected override void ApplyState(string state)
		{
			switch (state)
			{
				case ScanStates.Warehouse:
					Prompt(Msg.WarehousePrompt);
					break;
				case ScanStates.Item:
					Prompt(Msg.InventoryPrompt);
					break;
				case ScanStates.Location:
					if (!PXAccess.FeatureInstalled<FeaturesSet.warehouseLocation>())
					{
						SetScanState(ScanStates.Confirm);
					}
					else
					{
						Prompt(Msg.LocationPrompt);
					}
					break;
				case ScanStates.LotSerial:
					Prompt(Msg.LotSerialPrompt);
					break;
				case ScanStates.ExpireDate:
					Prompt(Msg.LotSerialExpireDatePrompt);
					break;
				case ScanStates.Confirm:
					if (ExplicitLineConfirmation)
						Prompt(Msg.ConfirmationPrompt);
					else if (HeaderView.Current.Remove == false)
						ProcessConfirm();
					else
						ProcessConfirmRemove();
					break;
			}
		}

		protected override string GetDefaultState(Header header = null) => IsWarehouseRequired() ? ScanStates.Warehouse : ScanStates.Item;

		protected override void ClearMode()
		{
			ClearHeaderInfo();
			SetScanState(HeaderView.Current.SiteID == null ? ScanStates.Warehouse : ScanStates.Item, Msg.ScreenCleared);
		}

		protected override void ProcessDocumentNumber(string barcode) => throw new NotImplementedException();
		protected override void ProcessCartBarcode(string barcode) => throw new NotImplementedException();

		private DateTime? EnsureExpireDateDefault() => LSSelect.ExpireDateByLot(Base, HeaderView.Current, null);

		protected override bool UseQtyCorrectection => Setup.Current.UseDefaultQtyInReceipt != true;
		protected override bool ExplicitLineConfirmation => Setup.Current.ExplicitLineConfirmation == true;
		protected override bool DocumentLoaded => Receipt != null;

		#region Constants & Messages
		public new abstract class Modes : WMSBase.Modes
		{
			public static WMSModeOf<INScanReceive, INScanReceiveHost> ScanInReceive { get; } = WMSMode("INRE");

			public class scanReceipt : PX.Data.BQL.BqlString.Constant<scanReceipt> { public scanReceipt() : base(ScanInReceive) { } }
		}

		public new abstract class ScanStates : WMSBase.ScanStates
		{
			public const string Warehouse = "SITE";
			public const string Confirm = "CONF";
		}

		public new abstract class ScanCommands : WMSBase.ScanCommands
		{
			public const string Release = Marker + "RELEASE*RECEIPT";
		}

		[PXLocalizable]
		public new abstract class Msg : WMSBase.Msg
		{
			public const string ScanInReceiveMode = "Scan and Receive";

			public const string ConfirmationPrompt = "Confirm the line, or scan or enter the line quantity.";

			public const string DocumentReleasing = "The {0} receipt is being released.";
			public const string DocumentIsReleased = "The receipt is successfully released.";

			public const string ReceiptLineMissing = "Line {0} is not found in the receipt.";
		}
		#endregion
	}
}