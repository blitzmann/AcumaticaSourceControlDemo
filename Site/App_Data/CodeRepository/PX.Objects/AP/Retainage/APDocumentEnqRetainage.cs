using System;

using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	[Serializable]
	public class APDocumentEnqRetainage : PXGraphExtension<APDocumentEnq>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.retainage>();
		}

		#region Cache Attached Events

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Currency Original Retainage")]
		protected virtual void APDocumentResult_CuryRetainageTotal_CacheAttached(PXCache sender) { }
		
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Currency Unreleased Retainage")]
		protected virtual void APDocumentResult_CuryRetainageUnreleasedAmt_CacheAttached(PXCache sender) { }
		
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Currency Total Amount")]
		protected virtual void APDocumentResult_CuryOrigDocAmtWithRetainageTotal_CacheAttached(PXCache sender) { }

		#endregion
	}
}