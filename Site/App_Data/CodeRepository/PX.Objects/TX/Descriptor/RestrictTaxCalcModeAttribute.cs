﻿using PX.Data;
using PX.Objects.AP;
using System;

namespace PX.Objects.TX
{
	public abstract class RestrictTaxCalcModeAttribute : PXEventSubscriberAttribute, IPXRowSelectedSubscriber
	{
		protected Type _TaxZoneID;
		protected Type _TaxCalcMode;

		protected abstract string _restrictedCalcMode
		{
			get;
		}

		protected abstract Type _ConditionSelect
		{
			get;
		}

		protected RestrictTaxCalcModeAttribute(Type TaxZoneID, Type TaxCalcMode)
		{
			_TaxZoneID = TaxZoneID;
			_TaxCalcMode = TaxCalcMode;
		}

		protected virtual bool CheckCondition(PXCache sender, object row)
		{
			object[] selectParams = GetParams(sender, row);
			BqlCommand cmd = BqlCommand.CreateInstance(_ConditionSelect);
			PXView view = sender.Graph.TypedViews.GetView(cmd, true);
			object result = view.SelectSingleBound(new object[] { row }, selectParams);
			return result != null;
		}

		protected virtual object[] GetParams(PXCache sender, object row)
		{
			object taxZoneIDValue = sender.GetValue(row, _TaxZoneID.Name);
			return new object[] { taxZoneIDValue };
		}

		public override void CacheAttached(PXCache sender)
		{
			sender.Graph.FieldUpdated.AddHandler(sender.GetItemType(), _TaxZoneID.Name, TaxZoneID_FieldUpdated);
			sender.Graph.FieldSelecting.AddHandler(sender.GetItemType(), _TaxCalcMode.Name, TaxCalcMode_FieldSelecting);
		}

		public void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			bool? flagValue = (bool?)sender.GetValue(e.Row, _FieldOrdinal);
			if (flagValue == null)
			{
				bool newValue = sender.GetValue(e.Row, _TaxZoneID.Name) != null && CheckCondition(sender, e.Row);
				sender.SetValue(e.Row, _FieldOrdinal, newValue);
			}
		}

		public virtual void TaxZoneID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (e.Row == null)
				return;
			bool newValue = sender.GetValue(e.Row, _TaxZoneID.Name) != null && CheckCondition(sender, e.Row);
			sender.SetValue(e.Row, _FieldOrdinal, newValue);
			if (newValue == true)
			{
				sender.SetValueExt(e.Row, _TaxCalcMode.Name, _restrictedCalcMode);
			}
		}

		public void TaxCalcMode_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (e.Row == null)
				return;
			bool? flagValue = (bool?)sender.GetValue(e.Row, _FieldOrdinal);
			if (flagValue == true)
			{
				e.ReturnState = PXFieldState.CreateInstance(e.ReturnState, typeof(string), null, null, null, null, null, null, null,
					null, null, null, PXErrorLevel.Undefined, false, null, null, PXUIVisibility.Undefined, null, null, null);
			}
		}
	}

	public class RestrictWithholdingTaxCalcModeAttribute : RestrictTaxCalcModeAttribute
	{
		protected override string _restrictedCalcMode
		{
			get
			{
				return TaxCalculationMode.Gross;
			}
		}

		protected override Type _ConditionSelect
		{
			get
			{
				return typeof(
					Select2<TaxZoneDet, InnerJoin<Tax,
						On<TaxZoneDet.taxZoneID, Equal<Required<TaxZoneDet.taxZoneID>>,
							And<Tax.taxID, Equal<TaxZoneDet.taxID>>>>,
						Where<Tax.taxType, Equal<CSTaxType.withholding>>>);
			}
		}

		public RestrictWithholdingTaxCalcModeAttribute(Type TaxZoneID, Type TaxCalcMode)
			: base(TaxZoneID, TaxCalcMode)
		{
		}
	}

	public class RestrictWithholdingTaxCalcModeFromPOAttribute : RestrictWithholdingTaxCalcModeAttribute
	{
		protected Type _OrigModule;
		public RestrictWithholdingTaxCalcModeFromPOAttribute(Type TaxZoneID, Type TaxCalcMode, Type OrigModule)
			: base(TaxZoneID, TaxCalcMode)
		{
			_OrigModule = OrigModule;
		}
		protected override bool CheckCondition(PXCache sender, object row)
		{
			return base.CheckCondition(sender, row) && (string)sender.GetValue(row, _OrigModule.Name) != GL.BatchModule.PO;
		}
	}
	public class RestrictUseTaxCalcModeAttribute : RestrictTaxCalcModeAttribute
	{
		protected override string _restrictedCalcMode
		{
			get
			{
				return TaxCalculationMode.Net;
			}
		}

		protected override Type _ConditionSelect
		{
			get
			{
				return typeof(
					Select2<TaxZoneDet, InnerJoin<Tax,
						On<TaxZoneDet.taxZoneID, Equal<Required<TaxZoneDet.taxZoneID>>,
							And<Tax.taxID, Equal<TaxZoneDet.taxID>>>>,
						Where<Tax.taxType, Equal<CSTaxType.use>>>);
			}
		}

		public RestrictUseTaxCalcModeAttribute(Type TaxZoneID, Type TaxCalcMode)
			: base(TaxZoneID, TaxCalcMode)
		{

		}
	}

	public class RestrictManualVATAttribute : RestrictTaxCalcModeAttribute
	{
		protected override Type _ConditionSelect
		{
			get
			{
				return typeof(Select<TaxZone, Where<TaxZone.taxZoneID, Equal<Required<TaxZone.taxZoneID>>, And<TaxZone.isManualVATZone, Equal<True>>>>);
			}
		}

		protected override string _restrictedCalcMode
		{
			get
			{
				return TaxCalculationMode.Net;
			}
		}

		public RestrictManualVATAttribute(Type TaxZoneID, Type TaxCalcMode)
			: base(TaxZoneID, TaxCalcMode)
		{

		}
	}
}
