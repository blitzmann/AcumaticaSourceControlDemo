﻿using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL.FinPeriods;
using PX.Objects.IN;
using System;

namespace PX.Objects.PM
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	[PXCacheName(Messages.PMForecastHistory)]
	[Serializable]
	public class PMForecastHistory : PX.Data.IBqlTable
	{
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID>
		{
		}
		[PXForeignReference(typeof(Field<projectID>.IsRelatedTo<PMProject.contractID>))]
		[PXDBInt(IsKey = true)]
		public virtual Int32? ProjectID
		{
			get;
			set;
		}
		#endregion
		#region ProjectTaskID
		public abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID>
		{
		}

		[PXForeignReference(typeof(Field<projectTaskID>.IsRelatedTo<PMTask.taskID>))]
		[PXDBInt(IsKey = true)]
		public virtual Int32? ProjectTaskID
		{
			get;
			set;
		}
		#endregion

		#region AccountGroupID
		public abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID>
		{
		}
		[PXForeignReference(typeof(Field<accountGroupID>.IsRelatedTo<PMAccountGroup.groupID>))]
		[PXDBInt(IsKey = true)]
		public virtual Int32? AccountGroupID
		{
			get;
			set;
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID>
		{
		}
		[PXDBInt(IsKey = true)]
		[PXForeignReference(typeof(Field<inventoryID>.IsRelatedTo<InventoryItem.inventoryID>))]
		public virtual Int32? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region CostCodeID
		public abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID>
		{
		}
		[PXForeignReference(typeof(Field<costCodeID>.IsRelatedTo<PMCostCode.costCodeID>))]
		[PXDBInt(IsKey = true)]
		public virtual Int32? CostCodeID
		{
			get;
			set;
		}
		#endregion
		#region PeriodID
		public abstract class periodID : PX.Data.BQL.BqlString.Field<periodID>
		{
		}

		[GL.FinPeriodID(IsKey = true)]
		public virtual String PeriodID
		{
			get;
			set;
		}
		#endregion
				
		#region ActualQty
		public abstract class actualQty : PX.Data.BQL.BqlDecimal.Field<actualQty>
		{
		}
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Actual Quantity", Enabled = false)]
		public virtual Decimal? ActualQty
		{
			get;
			set;
		}
		#endregion
		#region CuryActualAmount
		public abstract class curyActualAmount : PX.Data.BQL.BqlDecimal.Field<curyActualAmount>
		{
		}
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Actual Amount", Enabled = false)]
		public virtual Decimal? CuryActualAmount
		{
			get;
			set;
		}
		#endregion
		#region ActualAmount
		public abstract class actualAmount : PX.Data.BQL.BqlDecimal.Field<actualAmount>
		{
		}
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Actual Amount in Base Currency", Enabled = false)]
		public virtual Decimal? ActualAmount
		{
			get;
			set;
		}
		#endregion
		#region ChangeOrderQty
		public abstract class changeOrderQty : PX.Data.BQL.BqlDecimal.Field<changeOrderQty>
		{
		}
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Budgeted CO Quantity", Enabled = false, FieldClass = PMChangeOrder.FieldClass)]
		public virtual Decimal? ChangeOrderQty
		{
			get;
			set;
		}
		#endregion
		#region CuryChangeOrderAmount
		public abstract class curyChangeOrderAmount : PX.Data.BQL.BqlDecimal.Field<curyChangeOrderAmount>
		{
		}
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Budgeted CO Amount", Enabled = false, FieldClass = PMChangeOrder.FieldClass)]
		public virtual Decimal? CuryChangeOrderAmount
		{
			get;
			set;
		}
		#endregion
		
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp>
		{
		}
		protected Byte[] _tstamp;
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}
		#endregion

		public virtual BudgetKeyTuple GetBudgetKey()
		{
			return new BudgetKeyTuple(ProjectID.GetValueOrDefault(),
				ProjectTaskID.GetValueOrDefault(),
				AccountGroupID.GetValueOrDefault(),
				InventoryID.GetValueOrDefault(PMInventorySelectorAttribute.EmptyInventoryID),
				CostCodeID.GetValueOrDefault(CostCodeAttribute.GetDefaultCostCode()));
		}
	}

	[PXBreakInheritance]
	[PMForecastHistoryAccum]
	[Serializable]
	[PXHidden]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class PMForecastHistoryAccum : PMForecastHistory
	{
		#region ProjectID
		public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID>
		{
		}

		[PXDBInt(IsKey = true)]
		public override Int32? ProjectID
		{
			get;
			set;
		}
		#endregion
		#region ProjectTaskID
		public new abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID>
		{
		}

		[PXDBInt(IsKey = true)]
		public override Int32? ProjectTaskID
		{
			get;
			set;
		}
		#endregion

		#region AccountGroupID
		public new abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID>
		{
		}

		[PXDBInt(IsKey = true)]
		public override Int32? AccountGroupID
		{
			get;
			set;
		}
		#endregion
		#region InventoryID
		public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID>
		{
		}
		[PXDBInt(IsKey = true)]
		public override Int32? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region CostCodeID
		public new abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID>
		{
		}
		[PXDBInt(IsKey = true)]
		public override Int32? CostCodeID
		{
			get;
			set;
		}
		#endregion
		#region PeriodID
		public new abstract class periodID : PX.Data.BQL.BqlString.Field<periodID>
		{
		}

		[GL.FinPeriodID(IsKey = true)]
		public override String PeriodID
		{
			get;
			set;
		}
		#endregion
		
	}
}
