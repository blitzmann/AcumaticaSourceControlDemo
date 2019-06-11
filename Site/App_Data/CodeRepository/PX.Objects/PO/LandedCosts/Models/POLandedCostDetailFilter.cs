using PX.Data;
using PX.Objects.AP;
using System;

namespace PX.Objects.PO.LandedCosts
{
	[Serializable]
	public partial class POLandedCostDetailFilter : IBqlTable
	{
		#region LandedCostDocRefNbr
			public abstract class landedCostDocRefNbr : PX.Data.BQL.BqlString.Field<landedCostDocRefNbr>
		{
		}
		[PXString(15, IsUnicode = true, InputMask = "")]
		[PXDefault()]
		[PXSelector(typeof(Search<POLandedCostDoc.refNbr, Where<POLandedCostDoc.released, Equal<True>>>))]
		[PXUIField(DisplayName = "LC Nbr.", Visibility = PXUIVisibility.SelectorVisible, Required = false)]
		[PX.Data.EP.PXFieldDescription]
		public virtual String LandedCostDocRefNbr
		{
			get;
			set;
		}
		#endregion

		#region LandedCostCodeID
			public abstract class landedCostCodeID : PX.Data.BQL.BqlString.Field<landedCostCodeID>
		{
		}
		[PXString(15, IsUnicode = true, InputMask = "")]
		[PXDefault()]
		[PXSelector(typeof(Search<LandedCostCode.landedCostCodeID>))]
		[PXUIField(DisplayName = "LC Code", Visibility = PXUIVisibility.SelectorVisible, Required = false)]
		[PX.Data.EP.PXFieldDescription]
		public virtual String LandedCostCodeID
		{
			get;
			set;
		}
		#endregion

		#region ReceiptNbr
			public abstract class receiptNbr : PX.Data.BQL.BqlString.Field<receiptNbr>
		{
		}
		[PXString(15, IsUnicode = true, InputMask = "")]
		[PXDefault()]
		[POReceiptType.RefNbr(typeof(Search2<POReceipt.receiptNbr,
			LeftJoinSingleTable<Vendor, On<Vendor.bAccountID, Equal<POReceipt.vendorID>>>,
			Where<POReceipt.released, Equal<True>>,
			OrderBy<Desc<POReceipt.receiptNbr>>>), Filterable = true)]
		[PXUIField(DisplayName = "Receipt Nbr.", Visibility = PXUIVisibility.SelectorVisible, Required = false)]
		[PX.Data.EP.PXFieldDescription]
		public virtual String ReceiptNbr
		{
			get;
			set;
		}
		#endregion
	}
}
