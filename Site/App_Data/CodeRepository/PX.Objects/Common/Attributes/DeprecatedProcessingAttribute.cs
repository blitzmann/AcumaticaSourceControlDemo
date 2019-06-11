using PX.Data;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.AR.CCPaymentProcessing.Specific;
using System.Linq;
namespace PX.Objects.Common.Attributes
{

	public class DeprecatedProcessingAttribute : PXEventSubscriberAttribute, IPXRowSelectedSubscriber
	{
		public enum CheckVal
		{
			PmInstanceId,
			ProcessingCenterId,
			ProcessingCenterType
		}

		public CheckVal ChckVal { get; set; } = CheckVal.PmInstanceId;

		private static string[] deprecatedProcCenterNames = new string[] {
			AuthnetConstants.AIMPluginFullName,
			AuthnetConstants.CIMPluginFullName
		};

		public DeprecatedProcessingAttribute() : base()
		{

		}

		public void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (e.Row == null)
			{
				return;
			}

			string name = this.FieldName;
			object val = sender.GetValue(e.Row, name);

			if (ChckVal == CheckVal.PmInstanceId)
			{
				int? id = val as int?;
				if (id != null && IsProcessingCenterDeprecated(sender.Graph, id))
				{
					sender.RaiseExceptionHandling(name, e.Row, id, new PXSetPropertyException(AR.Messages.PaymentProfileDiscontinuedProcCenter, PXErrorLevel.Warning));
				}
			}

			if (ChckVal == CheckVal.ProcessingCenterId)
			{
				string procCenterId = val as string;
				if (procCenterId != null && IsProcessingCenterDeprecated(sender.Graph, procCenterId))
				{
					sender.RaiseExceptionHandling(name, e.Row, procCenterId, new PXSetPropertyException(AR.Messages.PaymentProfileDiscontinuedProcCenter, PXErrorLevel.Warning));
				}
			}

			if (ChckVal == CheckVal.ProcessingCenterType)
			{
				string typeStr = val as string;
				if (typeStr != null && IsProcessingCenterPlugunTypeDeprecated(typeStr))
				{
					sender.RaiseExceptionHandling(name, e.Row, typeStr, new PXSetPropertyException(CA.Messages.DiscontinuedProcCenter, PXErrorLevel.Warning));
				}
			}
		}

		public static bool IsProcessingCenterPlugunTypeDeprecated(string typeStr)
		{
			if (typeStr == null)
			{
				return false;
			}
			bool ret = deprecatedProcCenterNames.Any(i => i == typeStr);
			return ret;
		}

		public static bool IsProcessingCenterDeprecated(PXGraph graph, string procCenterId)
		{
			if (procCenterId == null)
			{
				return false;
			}
			CCProcessingCenter processingCenter = PXSelect<CCProcessingCenter,
				Where<CCProcessingCenter.processingCenterID, Equal<Required<CCProcessingCenter.processingCenterID>>,
					And<CCProcessingCenter.isActive, Equal<True>>>>
				.Select(graph, procCenterId);

			bool ret = deprecatedProcCenterNames.Any(i => i == processingCenter?.ProcessingTypeName);
			return ret;
		}

		public static bool IsProcessingCenterDeprecated(PXGraph graph, int? pmInstanceID)
		{

			if (pmInstanceID == null)
			{
				return false;
			}
			CCProcessingCenter processingCenter = (CCProcessingCenter)PXSelectJoin<CCProcessingCenter, 
				InnerJoin<CustomerPaymentMethod, On<CCProcessingCenter.processingCenterID, Equal<CustomerPaymentMethod.cCProcessingCenterID>>>,
				Where<CustomerPaymentMethod.pMInstanceID, Equal<Required<CustomerPaymentMethod.pMInstanceID>>, 
					And<CCProcessingCenter.isActive, Equal<True>>>>
				.Select(graph, pmInstanceID);

			bool ret = deprecatedProcCenterNames.Any(i => i == processingCenter?.ProcessingTypeName);
			return ret;
		}
	}
}
