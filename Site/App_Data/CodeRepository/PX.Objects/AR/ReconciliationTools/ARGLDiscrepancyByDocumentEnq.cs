using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Data;
using PX.Objects.AR;
using PX.Objects.GL;
using PX.Objects.CM;
using static PX.Objects.AR.ARDocumentEnq;

namespace ReconciliationTools
{
	#region Internal Types

	[Serializable]
	public partial class ARGLDiscrepancyByDocumentEnqResult : ARDocumentResult, IDiscrepancyEnqResult
	{
		#region GLTurnover
		public abstract class gLTurnover : PX.Data.BQL.BqlDecimal.Field<gLTurnover> { }

		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "GL Turnover")]
		public virtual decimal? GLTurnover
		{
			get;
			set;
		}
		#endregion
		#region XXTurnover
		public abstract class xXTurnover : PX.Data.BQL.BqlDecimal.Field<xXTurnover> { }

		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "AR Turnover")]
		public virtual decimal? XXTurnover
		{
			get;
			set;
		}
		#endregion
		#region Discrepancy
		public abstract class discrepancy : PX.Data.BQL.BqlDecimal.Field<discrepancy> { }

		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Discrepancy")]
		public virtual decimal? Discrepancy
		{
			get
			{
				return GLTurnover - XXTurnover;
			}
		}
		#endregion
	}

	#endregion

	[TableAndChartDashboardType]
	public class ARGLDiscrepancyByDocumentEnq : ARGLDiscrepancyEnqGraphBase<ARGLDiscrepancyByAccountEnq, ARGLDiscrepancyByCustomerEnqFilter, ARGLDiscrepancyByDocumentEnqResult>
	{
		#region CacheAttached

		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Financial Period")]
		protected virtual void ARGLDiscrepancyByCustomerEnqFilter_PeriodFrom_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXDefault]
		protected virtual void ARGLDiscrepancyByCustomerEnqFilter_CustomerID_CacheAttached(PXCache sender) { }

		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Original Amount")]
		protected virtual void ARGLDiscrepancyByDocumentEnqResult_OrigDocAmt_CacheAttached(PXCache sender) { }

		#endregion

		protected virtual IEnumerable rows()
		{
			ARGLDiscrepancyByCustomerEnqFilter header = Filter.Current;

			if (header == null ||
				header.BranchID == null ||
				header.PeriodFrom == null ||
				header.CustomerID == null)
			{
				return new ARGLDiscrepancyByDocumentEnqResult[0];
			}

			#region AR balances

			ARDocumentEnq graphAR = PXGraph.CreateInstance<ARDocumentEnq>();
			ARDocumentEnq.ARDocumentFilter filterAR = PXCache<ARDocumentEnq.ARDocumentFilter>.CreateCopy(graphAR.Filter.Current);

			filterAR.BranchID = header.BranchID;
			filterAR.CustomerID = header.CustomerID;
			filterAR.Period = header.PeriodFrom;
			filterAR.ARAcctID = header.AccountID;
			filterAR.SubCD = header.SubCD;
			filterAR.IncludeChildAccounts = false;
			filterAR = graphAR.Filter.Update(filterAR);

			Dictionary<ARDocKey, ARGLDiscrepancyByDocumentEnqResult> dict = new Dictionary<ARDocKey, ARGLDiscrepancyByDocumentEnqResult>();
			HashSet<int?> accountIDs = new HashSet<int?>();
			HashSet<int?> subAccountIDs = new HashSet<int?>();
			graphAR.Documents.Select();

			foreach (KeyValuePair<ARDocKey, ARDocumentResult> pair in graphAR.HandledDocuments)
			{
				ARDocKey key = pair.Key;
				ARDocumentResult handledDocument = pair.Value;
				ARGLDiscrepancyByDocumentEnqResult result;

				if (dict.TryGetValue(key, out result))
				{
					result.XXTurnover += (handledDocument.ARTurnover ?? 0m);
				}
				else
				{
					result = new ARGLDiscrepancyByDocumentEnqResult();
					result.GLTurnover = 0m;
					result.XXTurnover = (handledDocument.ARTurnover ?? 0m);
					PXCache<ARDocumentResult>.RestoreCopy(result, handledDocument);
					dict.Add(key, result);
				}

				accountIDs.Add(result.ARAccountID);
				subAccountIDs.Add(result.ARSubID);
			}

			#endregion

			#region GL balances

			AccountByPeriodEnq graphGL = PXGraph.CreateInstance<AccountByPeriodEnq>();
			AccountByPeriodFilter filterGL = PXCache<AccountByPeriodFilter>.CreateCopy(graphGL.Filter.Current);

			graphGL.Filter.Cache.SetDefaultExt<AccountByPeriodFilter.ledgerID>(filterGL);
			filterGL.BranchID = header.BranchID;
			filterGL.StartPeriodID = header.PeriodFrom;
			filterGL.EndPeriodID = header.PeriodFrom;
			filterGL.SubID = header.SubCD;
			filterGL = graphGL.Filter.Update(filterGL);

			foreach (int? accountID in accountIDs)
			{
				filterGL.AccountID = accountID;
				filterGL = graphGL.Filter.Update(filterGL);

				foreach (GLTranR gltran in graphGL.GLTranEnq.Select()
					.RowCast<GLTranR>()
					.Where(row =>
						row.Module == BatchModule.AR &&
						row.ReferenceID == header.CustomerID &&
						subAccountIDs.Contains(row.SubID)))
				{
					ARDocKey key = new ARDocKey(gltran.TranType, gltran.RefNbr);
					ARGLDiscrepancyByDocumentEnqResult result;

					if (dict.TryGetValue(key, out result))
					{
						decimal glTurnover = CalcGLTurnover(gltran);
						result.GLTurnover += glTurnover;
					}
				}
			}

			#endregion

			return dict.Values.Where(result => 
				header.ShowOnlyWithDiscrepancy != true ||
				result.Discrepancy != 0m);
		}
	}
}