using System;

using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AR
{
	[Serializable]
	public class ARDocumentEnqRetainage : PXGraphExtension<ARDocumentEnq>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.retainage>();
		}

		#region Cache Attached Events

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Currency Original Retainage")]
		protected virtual void ARDocumentResult_CuryRetainageTotal_CacheAttached(PXCache sender) { }
		
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Currency Unreleased Retainage")]
		protected virtual void ARDocumentResult_CuryRetainageUnreleasedAmt_CacheAttached(PXCache sender) { }
		
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Currency Total Amount")]
		protected virtual void ARDocumentResult_CuryOrigDocAmtWithRetainageTotal_CacheAttached(PXCache sender) { }

		#endregion
	}
}