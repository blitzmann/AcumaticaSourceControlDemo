﻿using System;
using PX.Data;
using PX.Objects.GL;
using PX.Objects.CM;

namespace PX.Objects.CA
{
	public class CAChargeSelect<DocumentTable, AdjDate, AdjFinPeriodID,
			ChargeTable, EntryTypeID, ChargeRefNbr, WhereSelect> : PXSelect<ChargeTable, WhereSelect>
		where DocumentTable : class, ICADocument, IBqlTable, new()
		where AdjDate : IBqlField
		where AdjFinPeriodID : IBqlField
		where ChargeTable : class, IBqlTable, AP.IPaymentCharge, new()
		where EntryTypeID : IBqlField
		where ChargeRefNbr : IBqlField
		where WhereSelect : IBqlWhere, new()
	{
		#region Ctor
		public CAChargeSelect(PXGraph graph)
			: base(graph)
		{
			graph.RowUpdated.AddHandler<DocumentTable>(PaymentRowUpdated);
		}
		#endregion

		#region Implementation
		protected virtual void PaymentRowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (!sender.ObjectsEqual<AdjDate, AdjFinPeriodID>(e.Row, e.OldRow))
			{
				foreach (ChargeTable charge in this.View.SelectMulti())
				{
					this.View.Cache.MarkUpdated(charge);
				}
			}
		}

		#endregion

		public void ReverseExpenses(ICADocument oldDoc, ICADocument newDoc)
		{
			ReverseCharges(oldDoc, newDoc, true);
		}

		protected virtual void ReverseCharges(ICADocument oldDoc, ICADocument newDoc, bool reverseSign)
		{
			foreach (PXResult<ChargeTable> paycharge in PXSelect<ChargeTable,
				Where<ChargeRefNbr, Equal<Required<ChargeRefNbr>>>>.Select(this._Graph, oldDoc.RefNbr))
			{
				ChargeTable charge = PXCache<ChargeTable>.CreateCopy((ChargeTable)paycharge);
				CurrencyInfo chargeInfo = CreateCuryInfo(charge);

				charge.DocType = CATranType.CATransferExp;
				charge.CuryInfoID = chargeInfo.CuryInfoID;
				charge.RefNbr = null;
				charge.CuryTranAmt = (reverseSign ? -1 : 1) * charge.CuryTranAmt;
				charge.Released = false;
				charge.CashTranID = null;
				this.Insert(charge);
			}
		}

		private CurrencyInfo CreateCuryInfo(ChargeTable charge)
		{
			CurrencyInfo currencyInfo = PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Required<CurrencyInfo.curyInfoID>>>>.Select(this._Graph, charge.CuryInfoID);
			CurrencyInfo chargeInfo = PXCache<CurrencyInfo>.CreateCopy(currencyInfo);
			chargeInfo.CuryInfoID = null;
			chargeInfo = (CurrencyInfo)this._Graph.Caches[typeof(CurrencyInfo)].Insert(chargeInfo);
			return chargeInfo;
		}
	}
}