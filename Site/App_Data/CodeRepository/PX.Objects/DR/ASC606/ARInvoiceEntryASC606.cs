using PX.Data;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.TX;
using System.Collections;

namespace PX.Objects.DR
{
	public class ARInvoiceEntryASC606 : PXGraphExtension<ARInvoiceEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.aSC606>();
		}

		public PXAction<ARInvoice> viewSchedule;
		[PXUIField(DisplayName = "View Schedule", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable ViewSchedule(PXAdapter adapter)
		{
			ARTran currentLine = Base.Transactions.Current;

			if (currentLine != null &&
				Base.Transactions.Cache.GetStatus(currentLine) == PXEntryStatus.Notchanged)
			{
				Base.Save.Press();
				ViewScheduleForDocument(Base, Base.Document.Current);
			}

			return adapter.Get();
		}

		public static void ViewScheduleForDocument(PXGraph graph, ARInvoice document)
		{
			PXSelectBase<DRSchedule> correspondingScheduleView = new PXSelect<
				DRSchedule,
				Where<
				DRSchedule.module, Equal<BatchModule.moduleAR>,
					And<DRSchedule.docType, Equal<Required<ARTran.tranType>>,
					And<DRSchedule.refNbr, Equal<Required<ARTran.refNbr>>>>>>
				(graph);

			DRSchedule correspondingSchedule = correspondingScheduleView.Select(document.DocType, document.RefNbr);

			if (correspondingSchedule?.IsOverridden != true && document.Released == false)
			{
				var netLinesAmount = ASC606Helper.CalculateNetAmount(graph, document);
				int? defScheduleID = null;

				if (netLinesAmount.Cury != 0m)
				{
					DRSingleProcess process = PXGraph.CreateInstance<DRSingleProcess>();

					process.CreateSingleSchedule(document, netLinesAmount, defScheduleID, true);
					process.Actions.PressSave();

					correspondingScheduleView.Cache.Clear();
					correspondingScheduleView.Cache.ClearQueryCache();

					correspondingSchedule = correspondingScheduleView.Select(document.DocType, document.RefNbr);
				}
			}

			if (correspondingSchedule != null)
			{
				PXRedirectHelper.TryRedirect(
					graph.Caches[typeof(DRSchedule)],
					correspondingSchedule,
					"View Schedule",
					PXRedirectHelper.WindowMode.NewWindow);
			}
		}

		public delegate void ReverseDRScheduleDelegate(ARRegister doc, ARTran tran);
		[PXOverride]
		public virtual void ReverseDRSchedule(ARRegister doc, ARTran tran, ReverseDRScheduleDelegate baseDelegate)
		{
			if (string.IsNullOrEmpty(tran.DeferredCode))
			{
				return;
			}

			DRSchedule schedule = PXSelect<DRSchedule,
				Where<DRSchedule.module, Equal<BatchModule.moduleAR>,
				And<DRSchedule.docType, Equal<Required<DRSchedule.docType>>,
				And<DRSchedule.refNbr, Equal<Required<DRSchedule.refNbr>>>>>>.
									Select(Base, doc.DocType, doc.RefNbr, tran.LineNbr);

			if (schedule != null)
			{
				tran.DefScheduleID = schedule.ScheduleID;
			}
		}

		protected virtual void _(Events.FieldVerifying<ARTran, ARTran.deferredCode> e)
		{
			if (e.Row == null)
				return;

			if (e.Row.InventoryID == null && e.NewValue != null)
			{
				throw new PXSetPropertyException(AR.Messages.InventoryIDCouldNotBeEmpty);
			}
		}
	}
}
