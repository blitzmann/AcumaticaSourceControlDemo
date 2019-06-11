using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.CS;
using PX.SM;

namespace PX.Objects.IN
{
	public interface IWarehouseManagementSystemGraph<TSelf, TGraph>
		where TSelf : IWarehouseManagementSystemGraph<TSelf, TGraph>
		where TGraph : PXGraph
	{
		TGraph ChangeModeTo(WMSModeOf<TSelf, TGraph> mode);
	}

	public abstract class WarehouseManagementSystemGraph<TSelf, TGraph, THeader, TWMSHeader> : PXGraphExtension<TGraph>, IWarehouseManagementSystemGraph<TSelf, TGraph>
		where TSelf : WarehouseManagementSystemGraph<TSelf, TGraph, THeader, TWMSHeader>
		where TGraph : PXGraph
		where THeader : class, IBqlTable, new()
		where TWMSHeader : WMSHeader, IBqlTable, new()
	{
		#region State
		private string _previousMode;
		private string _previousPrompt;
		private bool _saveRequested;
		private bool _bypassBarcodeFieldSelecting;
		#endregion

		public override void Initialize()
		{
			Base.FieldSelecting.AddHandler<WMSHeader.message>((s, e) => e.ReturnValue = s.Graph.FindImplementation<TSelf>().FullMessage);
			_previousMode = Info.Current.Mode;
			_previousPrompt = Info.Current.Prompt;
		}

		#region Views
		public abstract PXFilter<TWMSHeader> HeaderView { get; }
		public PXFilter<WMSInfo> Info;
		public PXFilter<WMSScanLog> Logs;
		protected IEnumerable logs()
		{
			var rs = new PXDelegateResult
			{
				IsResultFiltered = true,
				IsResultSorted = true,
				IsResultTruncated = true
			};
			rs.AddRange(Logs.Cache.Cached.RowCast<WMSScanLog>().Reverse());
			return rs;
		}
		#endregion

		#region Buttons
		public PXSave<TWMSHeader> Save;
		public PXCancel<TWMSHeader> Cancel;

		public PXAction<TWMSHeader> ClearBtn;
		[PXButton, PXUIField(DisplayName = "Reset")]
		protected virtual IEnumerable clearBtn(PXAdapter adapter) => scanBarcode(adapter, ScanCommands.Clear);

		public PXAction<TWMSHeader> Scan;
		[PXButton, PXUIField(DisplayName = "Scan", Visible = false)]
		protected virtual IEnumerable scan(PXAdapter adapter)
		{
			ProcessBarcode(HeaderView.Current);
			return adapter.Get();
		}

		protected virtual IEnumerable scanBarcode(PXAdapter adapter, string barcode)
		{
			HeaderView.Current.Barcode = barcode;
			return Scan.Press(adapter);
		}

		public PXAction<TWMSHeader> ScanModePick;
		[PXButton, PXUIField(DisplayName = "Pick")]
		protected virtual IEnumerable scanModePick(PXAdapter adapter) => scanBarcode(adapter, ScanRedirects.ModePick);

		public PXAction<TWMSHeader> ScanModePack;
		[PXButton, PXUIField(DisplayName = "Pack")]
		protected virtual IEnumerable scanModePack(PXAdapter adapter) => scanBarcode(adapter, ScanRedirects.ModePack);

		public PXAction<TWMSHeader> ScanModeShip;
		[PXButton, PXUIField(DisplayName = "Ship")]
		protected virtual IEnumerable scanModeShip(PXAdapter adapter) => scanBarcode(adapter, ScanRedirects.ModeShip);

		public PXAction<TWMSHeader> ScanModeReceive;
		[PXButton, PXUIField(DisplayName = "PO Receive")]
		protected virtual IEnumerable scanModeReceive(PXAdapter adapter) => scanBarcode(adapter, ScanRedirects.ModeReceive);

		public PXAction<TWMSHeader> ScanModePutAway;
		[PXButton, PXUIField(DisplayName = "Put Away")]
		protected virtual IEnumerable scanModePutAway(PXAdapter adapter) => scanBarcode(adapter, ScanRedirects.ModePutAway);

		public PXAction<TWMSHeader> ScanModeItemLookup;
		[PXButton, PXUIField(DisplayName = "Item Lookup")]
		protected virtual IEnumerable scanModeItemLookup(PXAdapter adapter) => scanBarcode(adapter, ScanRedirects.ModeItemLookup);

		public PXAction<TWMSHeader> ScanModeStorageLookup;
		[PXButton, PXUIField(DisplayName = "Storage Lookup")]
		protected virtual IEnumerable scanModeStorageLookup(PXAdapter adapter) => scanBarcode(adapter, ScanRedirects.ModeStorageLookup);

		public PXAction<TWMSHeader> ScanModeIssue;
		[PXButton, PXUIField(DisplayName = "IN Issue")]
		protected virtual IEnumerable scanModeIssue(PXAdapter adapter) => scanBarcode(adapter, ScanRedirects.ModeIssue);

		public PXAction<TWMSHeader> ScanModeInReceive;
		[PXButton, PXUIField(DisplayName = "IN Receive")]
		protected virtual IEnumerable scanModeInReceive(PXAdapter adapter) => scanBarcode(adapter, ScanRedirects.ModeInReceive);

		public PXAction<TWMSHeader> ScanModeInTransfer;
		[PXButton, PXUIField(DisplayName = "IN Transfer")]
		protected virtual IEnumerable scanModeInTransfer(PXAdapter adapter) => scanBarcode(adapter, ScanRedirects.ModeInTransfer);

		public PXAction<TWMSHeader> ScanModePhysicalCount;
		[PXButton, PXUIField(DisplayName = "PI Count")]
		protected virtual IEnumerable scanModePhysicalCount(PXAdapter adapter) => scanBarcode(adapter, ScanRedirects.ModePhysicalCount);

		public PXAction<TWMSHeader> ScanConfirm;
		[PXButton, PXUIField(DisplayName = "OK")]
		protected virtual IEnumerable scanConfirm(PXAdapter adapter) => scanBarcode(adapter, ScanCommands.Confirm);

		public PXAction<TWMSHeader> ScanRemove;
		[PXButton, PXUIField(DisplayName = "Remove")]
		protected virtual IEnumerable scanRemove(PXAdapter adapter) => scanBarcode(adapter, ScanCommands.Remove);

		public PXAction<TWMSHeader> ScanQty;
		[PXButton, PXUIField(DisplayName = "Set Qty")]
		protected virtual IEnumerable scanQty(PXAdapter adapter) => scanBarcode(adapter, ScanCommands.OverrideQty);
		#endregion

		#region Event Handlers
		#region WMSHeader
		protected virtual void _(Events.FieldDefaulting<TWMSHeader, WMSHeader.mode> e) => e.NewValue = DefaultMode.Value;
		protected virtual void _(Events.FieldUpdated<TWMSHeader, WMSHeader.mode> e)
		{
			if (e.Row.Mode == Modes.Free && e.Row.ManualView != true)
				e.Cache.SetValueExt<WMSHeader.manualView>(e.Row, true);
		}

		protected virtual void _(Events.FieldDefaulting<TWMSHeader, WMSHeader.scanState> e) => e.NewValue = GetDefaultState(e.Row);

		protected virtual void _(Events.FieldUpdated<TWMSHeader, WMSHeader.barcode> e)
		{
			// TODO: remove this handler when autocallbacks start to work on mobile
			if (Base.IsMobile)
			{
				try
				{
					Scan.Press();
				}
				catch (PXRedirectRequiredException)
				{
					ReportError(Msg.CommandUnknown);
				}
			}
		}
		protected virtual void _(Events.FieldSelecting<TWMSHeader, WMSHeader.barcode> e)
		{
			if (Base.IsMobile == false) return;
			if (_bypassBarcodeFieldSelecting) return;

			try
			{
				_bypassBarcodeFieldSelecting = true;
				PXFieldState state = (PXFieldState)e.Cache.GetStateExt<WMSHeader.barcode>(e.Row);
				if (state != null && e.Row != null)
					state.DisplayName = Info.Current.Prompt;
				e.ReturnState = state;
			}
			finally
			{
				_bypassBarcodeFieldSelecting = false;
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldUpdated<TWMSHeader, WMSHeader.manualView> e)
		{
			if (e.Row.ManualView == true)
			{
				e.Cache.SetValueExt<WMSHeader.mode>(e.Row, Modes.Free.Value);
				Info.Current.Mode = Localize(Msg.ModeIndicator, CurrentModeName);
				Info.Current.Prompt = Localize(Msg.UseCommandToContinue);
				SetScanState(ScanStates.Command, Msg.ModeChanged, CurrentModeName);
				Info.Update(Info.Current);
			}
		}

		protected virtual void _(Events.RowSelected<TWMSHeader> e)
		{
			e.Cache.IsDirty = false;

			ScanConfirm.SetVisible(ExplicitLineConfirmation || Info.Current?.MessageType == WMSMessageTypes.Warning);

			bool wmsFulfillment = PXAccess.FeatureInstalled<FeaturesSet.wMSFulfillment>();
			var ppsSetup = SO.SOPickPackShipSetup.PK.Find(Base, Base.Accessinfo.BranchID);
			ScanModePick.SetVisible(Base.IsMobile && wmsFulfillment && (Base is SO.PickPackShipHost || !OnlyLocalModeChange) && ppsSetup?.ShowPickTab != false);
			ScanModePack.SetVisible(Base.IsMobile && wmsFulfillment && (Base is SO.PickPackShipHost || !OnlyLocalModeChange) && ppsSetup?.ShowPackTab == true);
			ScanModeShip.SetVisible(Base.IsMobile && wmsFulfillment && (Base is SO.PickPackShipHost || !OnlyLocalModeChange) && ppsSetup?.ShowShipTab == true && false); // maybe later...

			bool wmsReceiving = PXAccess.FeatureInstalled<FeaturesSet.wMSReceiving>();
			var rpSetup = PO.POReceivePutAwaySetup.PK.Find(Base, Base.Accessinfo.BranchID);
			ScanModeReceive.SetVisible(Base.IsMobile && wmsReceiving && (Base is PO.ReceivePutAwayHost || !OnlyLocalModeChange) && rpSetup?.ShowReceivingTab != false);
			ScanModePutAway.SetVisible(Base.IsMobile && wmsReceiving && (Base is PO.ReceivePutAwayHost || !OnlyLocalModeChange) && rpSetup?.ShowPutAwayTab == true);

			bool wmsInventory = PXAccess.FeatureInstalled<FeaturesSet.wMSInventory>();
			ScanModeStorageLookup.SetVisible(Base.IsMobile && !OnlyLocalModeChange && wmsInventory);
			ScanModeItemLookup.SetVisible(Base.IsMobile && !OnlyLocalModeChange && wmsInventory);
			ScanModeIssue.SetVisible(Base.IsMobile && !OnlyLocalModeChange && wmsInventory);
			ScanModeInReceive.SetVisible(Base.IsMobile && !OnlyLocalModeChange && wmsInventory);
			ScanModeInTransfer.SetVisible(Base.IsMobile && !OnlyLocalModeChange && wmsInventory);
			ScanModePhysicalCount.SetVisible(Base.IsMobile && !OnlyLocalModeChange && wmsInventory);

			if (e.Row != null)
			{
				ScanQty.SetEnabled(UseQtyCorrectection.Implies(e.Row.IsQtyOverridable == true) && e.Row.IsQtyPrompted != true && DocumentLoaded);
			}
		}
		#endregion

		#region WMSInfo
		protected virtual void _(Events.FieldDefaulting<WMSInfo, WMSInfo.mode> e) => e.NewValue = Localize(Msg.ModeIndicator, CurrentModeName);
		protected virtual void _(Events.FieldDefaulting<WMSInfo, WMSInfo.prompt> e) => e.NewValue = GetModePrompt();
		protected virtual void _(Events.RowSelected<WMSInfo> e) => e.Cache.IsDirty = false;
		#endregion

		#region WMSScanLog
		protected virtual void _(Events.RowSelected<WMSScanLog> e) => e.Cache.IsDirty = false;
		#endregion
		#endregion

		#region Scan State logic
		protected virtual bool ProcessBarcode(TWMSHeader doc)
		{
			string[] barcodes = doc.Barcode?.Split(new[] { "\n", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
			if (barcodes.Length > 1)
			{
				RefreshState();
				MultiCommit = true;

				foreach (var barcode in barcodes)
				{
					doc.Barcode = barcode;
					ProcessSingleBarcode(doc);
					if (Info.Current.MessageType.IsIn(WMSMessageTypes.Error, WMSMessageTypes.Warning))
						break;
				}

				if (_saveRequested)
					Save.Press();

				return true;
			}
			else
			{
				RefreshState();
				return ProcessSingleBarcode(doc);
			}
		}

		protected virtual bool ProcessSingleBarcode(TWMSHeader doc)
		{
			bool processed = false;
			TWMSHeader headerBackup = PXCache<TWMSHeader>.CreateCopy(doc);
			WMSInfo infoBackup = PXCache<WMSInfo>.CreateCopy(Info.Current);

			if (String.IsNullOrEmpty(doc.Barcode))
			{
				ReportError(Msg.BarcodePrompt);
			}
			else
			{
				try
				{
					if (doc.Barcode.StartsWith(ScanCommands.Marker))
					{
						string command = doc.Barcode.ToUpper();
						if (command == ScanCommands.Save)
						{
							Save.Press();
							doc.Barcode = String.Empty;
							return true;
						}
						else if (command == ScanCommands.Cancel)
						{
							Base.Clear();
							Base.SelectTimeStamp();
							return true;
						}
						else if (command == ScanCommands.Clear)
						{
							ClearMode();
							processed = true;
						}
						else if (command == ScanCommands.OverrideQty)
						{
							if (UseQtyCorrectection && doc.IsQtyOverridable == false)
								processed = false;
							else
							{
								PromptQty(doc);
								processed = true;
							}
						}
						else
						{
							processed = ProcessCommand(command);
						}
					}
					else if (doc.Barcode.StartsWith(ScanRedirects.Marker))
						processed = ProcessRedirect(doc.Barcode.ToUpper());
					else if (doc.IsQtyPrompted == true || (UseQtyCorrectection == false || doc.IsQtyOverridable == true) && doc.Barcode.StartsWith("%"))
					{
						if (doc.IsQtyPrompted == true)
							return ProcessQtyBarcode(doc.Barcode);
						processed = ProcessQtyBarcode(doc.Barcode);
					}
					else
						processed = ProcessByState(doc);

					if (!processed)
						ReportError(Msg.CommandUnknown);
				}
				catch (PXRedirectRequiredException)
				{
					throw;
				}
				catch (Exception e)
				{
					PXTrace.WriteError(e);

					string errorMsg = e.Message;
					if (e is PXOuterException outerEx)
					{
						if (outerEx.InnerMessages.Length > 0)
							errorMsg += Environment.NewLine + string.Join(Environment.NewLine, outerEx.InnerMessages);
						else if (outerEx.Row != null)
							errorMsg += Environment.NewLine + string.Join(Environment.NewLine, PXUIFieldAttribute.GetErrors(Base.Caches[outerEx.Row.GetType()], outerEx.Row).Select(kvp => kvp.Value));
					}

					Base.Clear();
					headerBackup.Barcode = String.Empty;
					HeaderView.Update(headerBackup);
					Info.Update(infoBackup);
					ReportError(errorMsg);
					LogScan(headerBackup, headerBackup);
					return true;
				}
			}

			doc.Barcode = String.Empty;
			HeaderView.Update(doc);
			LogScan(headerBackup, PXCache<TWMSHeader>.CreateCopy(doc));

			return processed;
		}

		private void RefreshState()
		{
			foreach (var cache in Base.Caches)
			{
				if (cache.Key.IsIn(typeof(TWMSHeader), typeof(WMSHeader), typeof(WMSInfo), typeof(WMSScanLog)))
					continue;
				cache.Value.ClearQueryCache();
			}
			Base.SelectTimeStamp();
		}

		protected virtual bool ProcessByState(TWMSHeader doc)
		{
			switch (doc.ScanState)
			{
				case ScanStates.Command:    ProcessDocumentNumber(doc.Barcode); return true;
				case ScanStates.RefNbr:     ProcessDocumentNumber(doc.Barcode); return true;
				case ScanStates.Cart:       ProcessCartBarcode(doc.Barcode); return true;
				case ScanStates.Location:   ProcessLocationBarcode(doc.Barcode); return true;
				case ScanStates.Item:       ProcessItemBarcode(doc.Barcode); return true;
				case ScanStates.LotSerial:  ProcessLotSerialBarcode(doc.Barcode); return true;
				case ScanStates.ExpireDate: ProcessExpireDate(doc.Barcode); return true;
				case ScanStates.Wait:       return true;
				default:                    return false;
			}
		}

		protected abstract bool ProcessCommand(string barcode);
		protected abstract void ProcessDocumentNumber(string barcode);
		protected abstract void ProcessCartBarcode(string barcode);
		protected abstract void ProcessLocationBarcode(string barcode);
		protected abstract void ProcessItemBarcode(string barcode);
		protected abstract void ProcessLotSerialBarcode(string barcode);
		protected abstract void ProcessExpireDate(string barcode);
		protected virtual bool ProcessQtyBarcode(string barcode)
		{
			if (barcode.StartsWith("%"))
				barcode = barcode.Substring(1);

			if (decimal.TryParse(barcode, out decimal value))
			{
				value = Math.Abs(value);
				bool qtyWasPrompted = HeaderView.Current.IsQtyPrompted == true;
				bool success;

				if (UseQtyCorrectection)
				{
					success = PerformQtyCorrection(value - 1);
				}
				else
				{
					decimal? oldValue = HeaderView.Current.Qty;

					if (HeaderView.Current.IsQtyPrompted == true)
					{
						var oldHeader = RestoreHeaderWithQty(value);
						HeaderView.Current.Barcode = "";
						HeaderView.Current.IsQtyPrompted = false;
					}
					else
					{
						HeaderView.Current.Qty = value;
					}

					HeaderView.Cache.RaiseFieldUpdated<WMSHeader.qty>(HeaderView.Current, oldValue);
					Report(Msg.QtyReady, value, HeaderView.Current.UOM ?? Msg.Units);
					success = true;
				}

				if (qtyWasPrompted)
				{
					WMSScanLog lastLogEntry = Logs.Select();
					lastLogEntry.Scan = barcode;
					lastLogEntry.Prompt = Localize(Msg.QtyPrompt);
					Logs.Update(lastLogEntry);
				}

				return success;
			}
			else
			{
				ReportError(Msg.QtyBadFormat);
				return false;
			}
		}

		protected void PromptQty(TWMSHeader doc)
		{
			doc.IsQtyPrompted = true;
			Report(Msg.QtyEditMode);
			Prompt(Msg.QtyPrompt);
		}


		protected abstract string GetDefaultState(TWMSHeader header);

		protected void SetScanState(string state, string message = null, params object[] args)
		{
			HeaderView.Current.ScanState = state;
			if (message != null)
				Report(message, args);

			ApplyState(state);
		}

		protected abstract void ApplyState(string state);

		protected WMSFlowStatus ExecuteAndCompleteFlow(Func<WMSFlowStatus> func)
		{
			if (func == null) throw new ArgumentNullException(nameof(func));
			WMSFlowStatus flowStatus = func();
			if (flowStatus.IsError != false)
			{
				if (flowStatus.ClearHeader || (ExplicitLineConfirmation == false && flowStatus.IsError == true))
					ClearMode();
				if (flowStatus.IsError == true)
					ReportError(flowStatus.Message, flowStatus.MessageArgs);
				else
					ReportWarning(flowStatus.Message, flowStatus.MessageArgs);
			}
			else
			{
				ClearHeaderInfo();
				if (MultiCommit)
					_saveRequested = true;
				else
					Save.Press();
			}

			flowStatus.PostAction?.Invoke();
			return flowStatus;
		}

		protected void WaitFor(PXToggleAsyncDelegate method, string message = null, params object[] args)
		{
			HeaderView.Current.ScanState = ScanStates.Wait;
			if (message != null)
				Report(message, args);
			Prompt(Msg.Wait);
			PXLongOperation.StartOperation(Base, method);
		}
		#endregion

		#region Redirect logic
		private bool ProcessRedirect(string command)
		{
			if (PrepareRedirect(command))
				if (PerformRedirect(command))
					return CompleteRedirect(command);
			return false;
		}

		protected virtual bool PrepareRedirect(string command) => true;

		protected virtual bool PerformRedirect(string command)
		{
			Lazy<SO.SOPickPackShipSetup> ppsSetup = Lazy.By(() => new PXSetupOptional<SO.SOPickPackShipSetup, Where<SO.SOPickPackShipSetup.branchID, Equal<Current<AccessInfo.branchID>>>>(Base).Current);
			Lazy<PO.POReceivePutAwaySetup> rpaSetup = Lazy.By(() => new PXSetupOptional<PO.POReceivePutAwaySetup, Where<PO.POReceivePutAwaySetup.branchID, Equal<Current<AccessInfo.branchID>>>>(Base).Current);

			switch (command)
			{
				#region PickPackShip
				case ScanRedirects.ModePick:
					if (PXAccess.FeatureInstalled<FeaturesSet.wMSFulfillment>() && ppsSetup.Value.ShowPickTab == true)
					{
						RedirectToMode(SO.PickPackShip.Modes.Pick);
						return true;
					}
					else return false;

				case ScanRedirects.ModePack:
					if (PXAccess.FeatureInstalled<FeaturesSet.wMSFulfillment>() && ppsSetup.Value.ShowPackTab == true)
					{
						RedirectToMode(SO.PickPackShip.Modes.Pack);
						return true;
					}
					else return false;

				case ScanRedirects.ModeShip:
					if (PXAccess.FeatureInstalled<FeaturesSet.wMSFulfillment>() && ppsSetup.Value.ShowShipTab == true)
					{
						RedirectToMode(SO.PickPackShip.Modes.Ship);
						return true;
					}
					else return false;
				#endregion

				#region ReceivePutAway
				case ScanRedirects.ModeReceive:
					if (PXAccess.FeatureInstalled<FeaturesSet.wMSReceiving>() && rpaSetup.Value.ShowReceivingTab == true)
					{
						RedirectToMode(PO.ReceivePutAway.Modes.Receive);
						return true;
					}
					else return false;

				case ScanRedirects.ModePutAway:
					if (PXAccess.FeatureInstalled<FeaturesSet.wMSReceiving>() && rpaSetup.Value.ShowPutAwayTab == true)
					{
						RedirectToMode(PO.ReceivePutAway.Modes.PutAway);
						return true;
					}
					else return false;
				#endregion

				#region Inventory
				case ScanRedirects.ModeItemLookup:
					if (PXAccess.FeatureInstalled<FeaturesSet.wMSInventory>())
					{
						RedirectToMode(InventoryItemLookup.Modes.Lookup);
						return true;
					}
					else return false;

				case ScanRedirects.ModeStorageLookup:
					if (PXAccess.FeatureInstalled<FeaturesSet.wMSInventory>())
					{
						RedirectToMode(StoragePlaceLookup.Modes.Lookup);
						return true;
					}
					else return false;

				case ScanRedirects.ModeIssue:
					if (PXAccess.FeatureInstalled<FeaturesSet.wMSInventory>())
					{
						RedirectToMode(INScanIssue.Modes.ScanIssue);
						return true;
					}
					else return false;

				case ScanRedirects.ModeInReceive:
					if (PXAccess.FeatureInstalled<FeaturesSet.wMSInventory>())
					{
						RedirectToMode(INScanReceive.Modes.ScanInReceive);
						return true;
					}
					else return false;

				case ScanRedirects.ModeInTransfer:
					if (PXAccess.FeatureInstalled<FeaturesSet.wMSInventory>())
					{
						RedirectToMode(INScanTransfer.Modes.ScanINTransfer);
						return true;
					}
					else return false;

				case ScanRedirects.ModePhysicalCount:
					if (PXAccess.FeatureInstalled<FeaturesSet.wMSInventory>())
					{
						RedirectToMode(INScanCount.Modes.ScanInCount);
						return true;
					}
					else return false;
				#endregion

				default:
					return false;
			}
		}

		protected virtual bool CompleteRedirect(string command) => true;

		protected void RedirectToMode<TWMSExtension, TTargetGraph>(WMSModeOf<TWMSExtension, TTargetGraph> mode)
			where TWMSExtension : PXGraphExtension<TTargetGraph>, IWarehouseManagementSystemGraph<TWMSExtension, TTargetGraph>, new()
			where TTargetGraph : PXGraph, new()
		{
			if (this is TWMSExtension)
			{
				RedirectToSelf(mode);
			}
			else
			{
				throw new PXRedirectRequiredException(PXGraph.CreateInstance<TTargetGraph>().FindImplementation<TWMSExtension>().ChangeModeTo(mode), false, "");
			}
		}

		TGraph IWarehouseManagementSystemGraph<TSelf, TGraph>.ChangeModeTo(WMSModeOf<TSelf, TGraph> mode) => this.Apply(it => it.RedirectToSelf(mode)).Base;

		protected void RedirectToSelf(string mode)
		{
			HeaderView.Current.Mode = mode;
			HeaderView.Current.ManualView = false;
			ClearMode();
			HeaderView.Update(HeaderView.Current);

			Info.Current.Mode = Localize(Msg.ModeIndicator, CurrentModeName);
			Info.Current.Message = Localize(Msg.ModeChanged, CurrentModeName);
			Info.Current.MessageType = WMSMessageTypes.Information;
			Info.Current.MessageSoundFile = WMSMessageSoundFiles.Information;
			Info.Current.Prompt = GetModePrompt();
			Info.Update(Info.Current);
		} 
		#endregion

		#region Messaging logic
		protected virtual void ReportError(string errorMsg, params object[] args)
		{
			Info.Cache.SetValueExt<WMSInfo.message>(Info.Current, Localize(errorMsg, args));
			Info.Cache.SetValueExt<WMSInfo.messageType>(Info.Current, WMSMessageTypes.Error);
			if (Base.IsMobile)
				Info.Cache.RaiseExceptionHandling<WMSInfo.message>(Info.Current, Info.Current.Message, new PXSetPropertyException<WMSInfo.message>(ErrorMessages.ErrorHasOccurred, PXErrorLevel.Error));
			else
				HeaderView.Cache.RaiseExceptionHandling<WMSHeader.message>(HeaderView.Current, FullMessage, new PXSetPropertyException<WMSHeader.message>(ErrorMessages.ErrorHasOccurred, PXErrorLevel.Error));
		}

		protected virtual void ReportWarning(string warnMsg, params object[] args)
		{
			Info.Cache.SetValueExt<WMSInfo.message>(Info.Current, Localize(warnMsg, args));
			Info.Cache.SetValueExt<WMSInfo.messageType>(Info.Current, WMSMessageTypes.Warning);
			if (Base.IsMobile)
				Info.Cache.RaiseExceptionHandling<WMSInfo.message>(Info.Current, Info.Current.Message, new PXSetPropertyException<WMSInfo.message>(Msg.Warning, PXErrorLevel.Warning));
			else
				HeaderView.Cache.RaiseExceptionHandling<WMSHeader.message>(HeaderView.Current, FullMessage, new PXSetPropertyException<WMSHeader.message>(Msg.Warning, PXErrorLevel.Warning));
		}

		protected virtual void Report(string infoMsg, params object[] args)
		{
			Info.Cache.SetValueExt<WMSInfo.message>(Info.Current, Localize(infoMsg, args));
			Info.Cache.SetValueExt<WMSInfo.messageType>(Info.Current, WMSMessageTypes.Information);

			if (Base.IsMobile)
				Info.Cache.RaiseExceptionHandling<WMSInfo.message>(Info.Current, Info.Current.Message, RestoreFieldException<WMSInfo.message>(Info.Current));
			else
				HeaderView.Cache.RaiseExceptionHandling<WMSHeader.message>(HeaderView.Current, FullMessage, RestoreFieldException<WMSHeader.message>(HeaderView.Current));
		}

		protected virtual void Prompt(string promptMsg, params object[] args)
		{
			Info.Cache.SetValueExt<WMSInfo.prompt>(Info.Current, Localize(promptMsg, args));

			if (Base.IsMobile)
				Info.Cache.RaiseExceptionHandling<WMSInfo.message>(Info.Current, Info.Current.Message, RestoreFieldException<WMSInfo.message>(Info.Current));
			else
				HeaderView.Cache.RaiseExceptionHandling<WMSHeader.message>(HeaderView.Current, FullMessage, RestoreFieldException<WMSHeader.message>(HeaderView.Current));
		}

		private PXSetPropertyException RestoreFieldException<TField>(object row = null)
			where TField : IBqlField
		{
			var (errorMessage, errorLevel) = PXUIFieldAttribute.GetErrorWithLevel<TField>(Base.Caches[BqlCommand.GetItemType<TField>()], row);
			if (errorLevel == PXErrorLevel.Undefined || errorMessage == null)
				return null;

			return new PXSetPropertyException<TField>(errorMessage, errorLevel);
		}

		protected string Localize(string strMessage, params object[] args) => PXMessages.LocalizeFormatNoPrefix(strMessage, args);

		protected virtual void LogScan(TWMSHeader headerBefore, TWMSHeader headerAfter)
		{
			Logs.Cache.Insert(
				new WMSScanLog
				{
					ScanTime = PXTimeZoneInfo.Now,
					Scan = headerBefore.Barcode,
					Message = Info.Current.Message,
					MessageType = Info.Current.MessageType,
					NewPrompt = Info.Current.Prompt,
					Prompt = _previousPrompt,
					Mode = _previousMode,
					HeaderStateBefore = headerBefore,
					HeaderStateAfter = headerAfter,
				});
		}

		public abstract string CurrentModeName { get; }

		protected abstract string GetModePrompt();

		public string FullMessage => 
			Info.Current.Mode + Environment.NewLine +
			Info.Current.Message + Environment.NewLine +
			Info.Current.Prompt;
		#endregion

		#region Clearing logic
		protected virtual void ClearHeaderInfo(bool redirect = false)
		{
			HeaderView.Current.InventoryID = null;
			HeaderView.Current.SubItemID = null;
			HeaderView.Current.UOM = null;
			HeaderView.Current.Remove = false;
			HeaderView.Current.IsQtyPrompted = false;
			HeaderView.Current.IsQtyOverridable = false;
			if (UseQtyCorrectection)
				HeaderView.Current.Qty = 1;
		}
		protected virtual void ClearMode() { }

		protected void Clear(string message, params object[] args)
		{
			Base.Clear();
			Report(message, args);
		}

		[PXOverride]
		public virtual void Clear(Action baseMtd)
		{
			_clearHeader = true;
			baseMtd();
			_clearHeader = false;
		}
		private bool _clearHeader;

		[PXOverride]
		public virtual void Clear(PXClearOption option, Action<PXClearOption> baseMtd)
		{
			if (_clearHeader)
			{
				string mode = HeaderView.Current?.Mode;
				baseMtd(option);
				if (mode.IsNotIn(null, Modes.Free))
					RedirectToSelf(mode);
			}
			else
			{
				var header = HeaderView.Current;
				var info = Info.Current;
				var logs = Logs.Cache.Cached.RowCast<WMSScanLog>().ToArray();

				baseMtd(option);

				if (header != null && HeaderView.Cache.Locate(header) == null)
					HeaderView.Cache.SetStatus(header, PXEntryStatus.Inserted);
				if (info != null && Info.Cache.Locate(info) == null)
					Info.Cache.SetStatus(info, PXEntryStatus.Inserted);
				if (Logs.Cache.Cached.Count() == 0)
					foreach (var log in logs)
						Logs.Cache.Insert(log);
			}
		}
		#endregion

		protected abstract WMSModeOf<TSelf, TGraph> DefaultMode { get; }
		protected abstract bool UseQtyCorrectection { get; }
		protected abstract bool ExplicitLineConfirmation { get; }
		protected virtual bool DocumentLoaded => HeaderView.Current?.RefNbr != null;
		protected virtual bool OnlyLocalModeChange => true;

		protected virtual PXResult<INItemXRef, InventoryItem, INSubItem, INLotSerClass> ReadItemByBarcode(string barcode, INPrimaryAlternateType? additionalAlternateType = null)
		{
			var view = new
				PXSelectReadonly2<INItemXRef,
				InnerJoin<InventoryItem, 
					On2<INItemXRef.FK.InventoryItem,
					And<InventoryItem.itemStatus, NotIn3<InventoryItemStatus.inactive, InventoryItemStatus.noPurchases, InventoryItemStatus.markedForDeletion>>>,
				InnerJoin<INSubItem, 
					On<INItemXRef.FK.SubItem>,
				InnerJoin<INLotSerClass, 
					On<InventoryItem.FK.LotSerClass>>>>,
				Where<INItemXRef.alternateID, Equal<Required<WMSHeader.barcode>>>,
				OrderBy<Asc<INItemXRef.alternateType>>>(Base);

			if (additionalAlternateType == INPrimaryAlternateType.CPN)
				view.WhereAnd<Where<INItemXRef.alternateType, In3<INAlternateType.barcode, INAlternateType.cPN>>>();
			if (additionalAlternateType == INPrimaryAlternateType.VPN)
				view.WhereAnd<Where<INItemXRef.alternateType, In3<INAlternateType.barcode, INAlternateType.vPN>>>();
			else
				view.WhereAnd<Where<INItemXRef.alternateType, Equal<INAlternateType.barcode>>>();

			return view
				.Select(barcode).AsEnumerable()
				.OrderByDescending(r => r.GetItem<INItemXRef>().AlternateType == INAlternateType.Barcode) // TODO: rewrite using Linq2BQL
				.Cast<PXResult<INItemXRef, InventoryItem, INSubItem, INLotSerClass>>()
				.FirstOrDefault();
		}

		protected virtual INCart ReadCartByBarcode(string barcode)
		{
			return
				PXSelectReadonly2<INCart,
				InnerJoin<INSite, On<INCart.FK.Site>>,
				Where<INCart.cartCD, Equal<Required<INCart.cartCD>>,
					And<Match<INSite, Current<AccessInfo.userName>>>>>
				.Select(Base, barcode);
		}

		protected virtual INLocation ReadLocationByBarcode(int? siteID, string locationCD, bool skipErrorOnMissing = false)
		{
			INLocation location =
				PXSelectReadonly<INLocation,
				Where<INLocation.siteID, Equal<Required<INLocation.siteID>>,
					And<INLocation.locationCD, Equal<Required<INLocation.locationCD>>>>>
				.Select(Base, siteID, locationCD);

			if (location == null)
			{
				if (skipErrorOnMissing == false)
					ReportError(Msg.LocationMissing, locationCD);
				return null;
			}
			else if (location.Active != true)
			{
				ReportError(IN.Messages.InactiveLocation, location.LocationCD);
				return null;
			}
			return location;
		}

		protected virtual bool PerformQtyCorrection(decimal qtyDelta)
		{
			TWMSHeader headerBackup = RestoreHeaderWithQty(qtyDelta, beforeCommit: true);

			Scan.PressButton();

			if (Info.Current.MessageType == WMSMessageTypes.Error)
			{
				HeaderView.Cache.RestoreCopy(HeaderView.Current, headerBackup);
				return false;
			}
			else
			{
				HeaderView.Current.IsQtyOverridable = false;
				HeaderView.Current.IsQtyPrompted = false;
				HeaderView.UpdateCurrent();
				return true;
			}
		}

		private TWMSHeader RestoreHeaderWithQty(decimal newQty, bool beforeCommit = false)
		{
			WMSScanLog prelastLogEntry = Logs.SelectMain().First(log => log.HeaderStateAfter.IsQtyPrompted != true && log.HeaderStateBefore.IsQtyPrompted != true);
			TWMSHeader previouseHeader;
			if (beforeCommit)
			{
				previouseHeader = (TWMSHeader)prelastLogEntry.HeaderStateBefore;
			}
			else
			{
				previouseHeader = (TWMSHeader)prelastLogEntry.HeaderStateAfter;
				var previouseHeaderBefore = (TWMSHeader)prelastLogEntry.HeaderStateBefore;
				previouseHeader.Barcode = previouseHeaderBefore.Barcode;
			}
			previouseHeader.Qty = newQty;

			Info.Current.Message = prelastLogEntry.Message;
			Info.Current.Prompt = prelastLogEntry.NewPrompt;
			Info.Current.MessageType = prelastLogEntry.MessageType;

			var headerBackup = (TWMSHeader)HeaderView.Cache.CreateCopy(HeaderView.Current);
			HeaderView.Cache.RestoreCopy(HeaderView.Current, previouseHeader);
			return headerBackup;
		}

		protected bool IsValid<TField>(object value, out string error) where TField : IBqlField
		{
			return IsValid<TField, TWMSHeader>(HeaderView.Current, value, out error);
		}

		protected bool IsValid<TField, TTable>(TTable instance, object value, out string error)
			where TTable : class, IBqlTable, new()
			where TField : IBqlField
		{
			try
			{
				Base.Caches<TTable>().RaiseFieldVerifying<TField>(instance, ref value);
				error = null;
				return true;
			}
			catch (PXSetPropertyException ex)
			{
				error = ex.MessageNoPrefix;
				return false;
			}
		}

		protected virtual bool IsLocationRequired(TWMSHeader header) => PXAccess.FeatureInstalled<FeaturesSet.warehouseLocation>();
		protected virtual bool IsCartRequired(TWMSHeader header) => PXAccess.FeatureInstalled<FeaturesSet.wMSCartTracking>();

		protected virtual int? DefaultSiteID
		{
			get
			{
				var userPreferencesSelect = new PXSelectReadonly<UserPreferences, Where<UserPreferences.userID, Equal<Current<AccessInfo.userID>>>>(Base);
				var userPreferences = (UserPreferences)userPreferencesSelect.Select();
				var preferencesExt = userPreferences.GetExtension<UserPreferenceExt>();

				return preferencesExt.DefaultSite;
			}
		}

		protected bool MultiCommit { get; private set; }

		protected static WMSModeOf<TSelf, TGraph> WMSMode(string mode) => new WMSModeOf<TSelf, TGraph>(mode);

		protected static void PrintReportViaDeviceHub<TBAccount>(PXGraph graph, string reportID, Dictionary<string, string> reportParameters, string notificationSource, TBAccount baccount)
			where TBAccount : CR.BAccount
		{
			CR.NotificationUtility notificationUtility = new CR.NotificationUtility(graph);
			var reportsToPrint = PX.SM.SMPrintJobMaint.AssignPrintJobToPrinter(
				new Dictionary<string, PXReportRequiredException>(), 
				reportParameters,
				new PrinterParameters() { PrintWithDeviceHub = true, DefinePrinterManually = false },
				notificationUtility.SearchPrinter,
				notificationSource,
				reportID,
				baccount == null ? reportID : notificationUtility.SearchReport(notificationSource, baccount, reportID, graph.Accessinfo.BranchID),
				graph.Accessinfo.BranchID);

			PX.SM.SMPrintJobMaint.CreatePrintJobGroups(reportsToPrint);
		}

		#region Constants & Messages
		public abstract class Modes
		{
			public static readonly WMSModeOf<TSelf, TGraph> Free = WMSMode("FREE");
			public class free : PX.Data.BQL.BqlString.Constant<free> { public free() : base(Free) { } }
		}

		public abstract class ScanCommands
		{
			public const string Marker = "*";

			public const string Cancel = Marker + "CANCEL";
			public const string Save = Marker + "SAVE";
			public const string Clear = Marker + "CLEAR";

			public const string Confirm = Marker + "OK";
			public const string Remove = Marker + "REMOVE";
			public const string OverrideQty = Marker + "QTY";
		}

		public abstract class ScanRedirects
		{
			public const string Marker = "@";

			public const string ModePick = Marker + "PICK";
			public const string ModePack = Marker + "PACK";
			public const string ModeShip = Marker + "SHIP";

			public const string ModeReceive = Marker + "RECEIVE";
			public const string ModePutAway = Marker + "PUTAWAY";

			public const string ModeItemLookup = Marker + "ITEM";
			public const string ModeStorageLookup = Marker + "STORAGE";
			public const string ModeIssue = Marker + "INISSUE";
			public const string ModeInReceive = Marker + "INRECEIVE";
			public const string ModeInTransfer = Marker + "INTRANSFER";

			public const string ModePhysicalCount = Marker + "COUNT";
		}

		public abstract class ScanStates
		{
			public const string Command = "NONE";
			public const string RefNbr = "RNBR";
			public const string Item = "ITEM";
			public const string LotSerial = "LTSR";
			public const string ExpireDate = "EXPD";
			public const string Location = "LOCN";
			public const string Cart = "CART";
			public const string Wait = "WAIT";
		}

		[PXLocalizable]
		public abstract class Msg
		{
			public const string FreeMode = "MANUAL";

			public const string ScreenCleared = "The unconfirmed entries have been cleared.";
			public const string CommandUnknown = "The string is not a valid command or value.";
			public const string UseCommandToContinue = "Use any command or scan next document to continue.";
			public const string ModeIndicator = "{0} MODE IS IN USE";
			public const string ModeChanged = "The Active mode is set to {0}.";
			public const string RemoveMode = "Remove mode is activated.";
			public const string QtyEditMode = "The quantity editing mode has been activated.";
			public const string Wait = "Wait until the operation is completed.";

			public const string BarcodePrompt = "Scan a valid barcode.";

			public const string WarehousePrompt = "Scan the barcode of the warehouse.";
			public const string WarehouseReady = "The warehouse {0} is selected.";
			public const string WarehouseMissing = "The warehouse barcode {0} is not found.";

			public const string CartPrompt = "Scan the barcode of the cart.";
			public const string CartReady = "The cart {0} is selected.";
			public const string CartMissing = "The {0} cart is not found.";
			public const string CartIsEmpty = "Cart {0} is empty.";
			public const string CartInvalidSite = "Cart {0} has the warehouse that differs from the warehouse of the selected document.";
			public const string CartLoading = "The cart loading mode has been activated.";
			public const string CartUnloading = "The cart unloading mode has been activated.";
			public const string CartOverpicking = "The overall cart quantity cannot be greater than the difference between expected quantity and already picked quantity.";
			public const string CartUnderpicking = "The cart quantity cannot become negative.";
			public const string CartQty = "Cart Qty.";
			public const string CartOverallQty = "Overall Cart Qty.";

			public const string LocationPrompt = "Scan the barcode of the location.";
			public const string LocationReady = "The location {0} is selected.";
			public const string LocationMissing = "The location {0} is not found.";
			public const string LocationNotSet = "Location is not selected.";

			public const string InventoryPrompt = "Scan the barcode of the item.";
			public const string InventoryReady = "The item {0} is selected.";
			public const string InventoryMissing = "The item barcode {0} is not found.";
			public const string InventoryNotSet = "Item is not selected.";
			public const string InventoryAdded = "{0} x {1} has been added.";
			public const string InventoryRemoved = "{0} x {1} has been removed.";

			public const string LotSerialPrompt = "Scan lot/serial number.";
			public const string LotSerialReady = "The lot/serial number {0} is selected.";
			public const string LotSerialNotSet = "Lot/Serial Nbr. is not selected.";

			public const string LotSerialExpireDatePrompt = "Scan the lot/serial expiration date.";
			public const string LotSerialExpireDateReady = "Expiration date is set to {0:d}.";
			public const string LotSerialExpireDateBadFormat = "The date format does not fit the locale settings.";
			public const string LotSerialExpireDateNotSet = "Expiration Date is not selected.";

			public const string QtyPrompt = "Scan the quantity.";
			public const string QtyReady = "The quantity has been set to {0} {1}.";
			public const string QtyBadFormat = "The quantity format does not fit the locale settings.";
			public const string Units = "unit(s)";

			public const string SerialItemNotComplexQty = "Serialized items can be processed only with the base UOM and the 1.00 quantity.";

			public const string ConfirmQuestion = "OK?";
			public const string Warning = "Warning!";
			public const string Fits = "Matched";
			public const string NothingToRemove = "No items to remove.";
		}
		#endregion
	}

	public struct WMSModeOf<TWMSGraph, TGraph>
		where TWMSGraph : IWarehouseManagementSystemGraph<TWMSGraph, TGraph>
		where TGraph : PXGraph
	{
		public WMSModeOf(string value)
		{
			Value = value;
		}

		public string Value { get; }
		public static implicit operator string(WMSModeOf<TWMSGraph, TGraph> mode) => mode.Value;
	}

	public abstract class WMSHeader : IBqlTable
	{
		#region Barcode
		[PXString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Scan")]
		public virtual string Barcode { get; set; }
		public abstract class barcode : PX.Data.BQL.BqlString.Field<barcode> { }
		#endregion
		#region RefNbr
		public virtual string RefNbr { get; set; }
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		#endregion
		#region Message
		[PXUIField]
		public virtual string Message { get; set; }
		public abstract class message : PX.Data.BQL.BqlString.Field<message> { }
		#endregion
		#region ManualView
		[PXBool]
		[PXUnboundDefault(false)]
		[PXUIField(DisplayName = "Manual View")]
		[PXUIEnabled(typeof(Where<manualView, Equal<False>>))]
		public virtual bool? ManualView { get; set; }
		public abstract class manualView : PX.Data.BQL.BqlBool.Field<manualView> { }
		#endregion
		#region Status
		[PXString(3)]
		[PXUIField(DisplayName = "Status", Enabled = false, Visible = false)]
		public virtual string Status { get; set; }
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
		#endregion
		#region Mode
		[PXString(4)]
		[PXUIField(DisplayName = "Mode", Enabled = false, Visible = false)]
		public virtual string Mode { get; set; }
		public abstract class mode : PX.Data.BQL.BqlString.Field<mode> { }
		#endregion
		#region ScanState
		[PXString(4, IsFixed = true)]
		[PXUIField(DisplayName = "Scan State")]
		public virtual string ScanState { get; set; }
		public abstract class scanState : PX.Data.BQL.BqlString.Field<scanState> { } 
		#endregion
		#region Remove
		[PXBool, PXUnboundDefault(false)]
		[PXUIField(DisplayName = "Remove Mode", Enabled = false)]
		[PXUIVisible(typeof(remove))]
		public virtual bool? Remove { get; set; }
		public abstract class remove : PX.Data.BQL.BqlBool.Field<remove> { }
		#endregion

		#region InventoryID
		[StockItem(Enabled = false)]
		public virtual int? InventoryID { get; set; }
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		#endregion
		#region SubItemID
		[SubItem(Enabled = false)]
		public virtual int? SubItemID { get; set; }
		public abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		#endregion
		#region UOM
		[INUnit(typeof(inventoryID), Enabled = false)]
		[PXUnboundDefault(typeof(Search<InventoryItem.salesUnit, Where<InventoryItem.inventoryID, Equal<Current<inventoryID>>>>))]
		public virtual String UOM { get; set; }
		public abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }
		#endregion
		#region Qty
		[PXQuantity(typeof(uOM), typeof(baseQty), HandleEmptyKey = true)]
		[PXUnboundDefault(TypeCode.Decimal, "1")]
		public virtual decimal? Qty { get; set; }
		public abstract class qty : PX.Data.BQL.BqlDecimal.Field<qty> { }
		#endregion
		#region BaseQty
		[PXDecimal(6)]
		public virtual Decimal? BaseQty { get; set; }
		public abstract class baseQty : PX.Data.BQL.BqlDecimal.Field<baseQty> { }
		#endregion
		#region IsQtyOverridable
		[PXBool]
		public virtual Boolean? IsQtyOverridable { get; set; }
		public abstract class isQtyOverridable : PX.Data.BQL.BqlBool.Field<isQtyOverridable> { }
		#endregion
		#region IsQtyPrompted
		[PXBool]
		public virtual Boolean? IsQtyPrompted { get; set; }
		public abstract class isQtyPrompted : PX.Data.BQL.BqlBool.Field<isQtyPrompted> { }
		#endregion
	}

	public class WMSScanLog : IBqlTable
	{
		#region ScanTime
		[PXDateAndTime(InputMask = "dd-MM-yyyy HH:mm:ss", DisplayMask = "dd-MM-yyyy HH:mm:ss", IsKey = true)]
		[PXUIField(DisplayName = "Time", Enabled = false)]
		public virtual DateTime? ScanTime { get; set; }
		public abstract class scanTime : PX.Data.BQL.BqlDateTime.Field<scanTime> { }
		#endregion
		#region Mode
		[PXString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Mode", Enabled = false)]
		public virtual string Mode { get; set; }
		public abstract class mode : PX.Data.BQL.BqlString.Field<mode> { }
		#endregion
		#region Prompt
		[PXString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Prompt", Enabled = false)]
		public virtual string Prompt { get; set; }
		public abstract class prompt : PX.Data.BQL.BqlString.Field<prompt> { }
		#endregion
		#region NewPrompt
		[PXString(255, IsUnicode = true)]
		public virtual string NewPrompt { get; set; }
		public abstract class newPrompt : PX.Data.BQL.BqlString.Field<newPrompt> { }
		#endregion
		#region Scan
		[PXString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Scan", Enabled = false)]
		public virtual string Scan { get; set; }
		public abstract class scan : PX.Data.BQL.BqlString.Field<scan> { }
		#endregion
		#region Message
		[PXString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Message", Enabled = false)]
		public virtual string Message { get; set; }
		public abstract class message : PX.Data.BQL.BqlString.Field<message> { }
		#endregion
		#region MessageType
		[PXUnboundDefault(WMSMessageTypes.None)]
		[PXString(3, IsFixed = true)]
		public virtual string MessageType { get; set; }
		public abstract class messageType : PX.Data.BQL.BqlString.Field<messageType> { }
		#endregion
		#region HeaderStateBefore
		public WMSHeader HeaderStateBefore { get; set; }
		#endregion
		#region HeaderStateAfter
		public WMSHeader HeaderStateAfter { get; set; }
		#endregion
	}

	public class WMSInfo : IBqlTable
	{
		#region Mode
		[PXString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Mode", Enabled = false, Visible = false)]
		public virtual string Mode{ get; set; }
		public abstract class mode : PX.Data.BQL.BqlString.Field<mode> { }
		#endregion
		#region Message
		[PXString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Message", Enabled = false, Visible = false)]
		public virtual string Message { get; set; }
		public abstract class message : PX.Data.BQL.BqlString.Field<message> { }
		#endregion
		#region MessageType
		[PXUnboundDefault(WMSMessageTypes.None)]
		[PXString(3, IsFixed = true)]
		public virtual string MessageType { get; set; }
		public abstract class messageType : PX.Data.BQL.BqlString.Field<messageType> { }
		#endregion
		#region MessageSoundFile
		[PXString]
		[PXFormula(typeof(Switch<
			Case<Where<messageType, Equal<WMSMessageTypes.information>>, WMSMessageSoundFiles.information,
			Case<Where<messageType, Equal<WMSMessageTypes.warning>>, WMSMessageSoundFiles.warning,
			Case<Where<messageType, Equal<WMSMessageTypes.error>>, WMSMessageSoundFiles.error>>>,
			Empty>))]
		[PXUIField(DisplayName = "Message Sound", Enabled = false, Visible = false)]
		public virtual string MessageSoundFile { get; set; }
		public abstract class messageSoundFile : PX.Data.BQL.BqlString.Field<messageSoundFile> { }
		#endregion

		#region Prompt
		[PXString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Prompt", Enabled = false, Visible = false)]
		public virtual string Prompt { get; set; }
		public abstract class prompt : PX.Data.BQL.BqlString.Field<prompt> { }
		#endregion
	}

	public static class WMSMessageTypes
	{
		public const string None = "NON";
		public const string Error = "ERR";
		public const string Warning = "WRN";
		public const string Information = "INF";

		public class none : PX.Data.BQL.BqlString.Constant<none> { public none() : base(None) { } }
		public class error : PX.Data.BQL.BqlString.Constant<error> { public error() : base(Error) { } }
		public class warning : PX.Data.BQL.BqlString.Constant<warning> { public warning() : base(Warning) { } }
		public class information : PX.Data.BQL.BqlString.Constant<information> { public information() : base(Information) { } }
	}

	public static class WMSMessageSoundFiles
	{
		public const string Error = "wms_error";
		public const string Warning = "wms_warning";
		public const string Information = "wms_success";

		public class error : PX.Data.BQL.BqlString.Constant<error> { public error() : base(Error) { } }
		public class warning : PX.Data.BQL.BqlString.Constant<warning> { public warning() : base(Warning) { } }
		public class information : PX.Data.BQL.BqlString.Constant<information> { public information() : base(Information) { } }
	}

	public struct WMSFlowStatus
	{
		public string Message { get; }
		public object[] MessageArgs { get; }
		public bool? IsError { get; }
		public bool ClearHeader { get; }
		public Action PostAction { get; }
		private WMSFlowStatus(bool? isError, string errorMsg, object[] errorArgs, bool clearHeader, Action postAction)
		{
			IsError = isError;
			Message = errorMsg ?? "Unsupported input.";
			MessageArgs = errorArgs;
			ClearHeader = clearHeader;
			PostAction = postAction;
		}

		public WMSFlowStatus ClearIsNeeded => new WMSFlowStatus(IsError, Message, MessageArgs, true, PostAction);
		public WMSFlowStatus WithPostAction(Action postAction) => new WMSFlowStatus(IsError, Message, MessageArgs, ClearHeader, postAction);

		public static readonly WMSFlowStatus Ok = new WMSFlowStatus(false, null, Array.Empty<object>(), false, null);
		public static WMSFlowStatus Fail(string errorMsg, params object[] errorArgs) => new WMSFlowStatus(true, errorMsg, errorArgs, false, null);
		public static WMSFlowStatus Warning(string warnMsg, params object[] warnArgs) => new WMSFlowStatus(null, warnMsg, warnArgs, false, null);
	}
}