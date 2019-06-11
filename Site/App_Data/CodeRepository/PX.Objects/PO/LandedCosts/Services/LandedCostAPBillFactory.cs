using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CM;
using PX.Objects.Common.Extensions;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.TX;

namespace PX.Objects.PO.LandedCosts
{
	public class LandedCostAPBillFactory
	{
		private readonly PXGraph _pxGraph;

		public LandedCostAPBillFactory(PXGraph pxGraph)
		{
			_pxGraph = pxGraph;
		}

		public APInvoiceWrapper CreateLandedCostBill(POLandedCostDoc doc, IEnumerable<POLandedCostDetail> details, IEnumerable<POLandedCostTaxTran> taxes)
		{
			var apTransactions = CreateTransactions(doc, details);

			var apTaxes = taxes.Select(tax => new APTaxTran()
			{
				Module = BatchModule.AP,
				TaxID = tax.TaxID,
				JurisType = tax.JurisType,
				JurisName = tax.JurisName,
				TaxRate = tax.TaxRate,
				CuryID = doc.CuryID,
				CuryInfoID = doc.CuryInfoID,
				CuryTaxableAmt = tax.CuryTaxableAmt,
				TaxableAmt = tax.TaxableAmt,
				CuryTaxAmt = tax.CuryTaxAmt,
				TaxAmt = tax.TaxAmt,
				CuryExpenseAmt = tax.CuryExpenseAmt,
				ExpenseAmt = tax.ExpenseAmt,
				TaxZoneID = tax.TaxZoneID
			}).ToList();

			var newdoc = new APInvoice();

			newdoc.DocType = APDocType.Invoice;

			if (doc.PayToVendorID.HasValue && doc.PayToVendorID != doc.VendorID && PXAccess.FeatureInstalled<FeaturesSet.vendorRelations>())
			{
				newdoc.VendorID = doc.PayToVendorID;
				newdoc.SuppliedByVendorID = doc.VendorID;
				newdoc.SuppliedByVendorLocationID = doc.VendorLocationID;
			}
			else
			{
				newdoc.VendorID = doc.VendorID;
				newdoc.VendorLocationID = doc.VendorLocationID;
			}

			newdoc.DocDate = doc.BillDate;
			newdoc.TaxCalcMode = TaxCalculationMode.TaxSetting;
			newdoc.TermsID = doc.TermsID;
			newdoc.InvoiceNbr = doc.VendorRefNbr;
			newdoc.CuryID = doc.CuryID;
			newdoc.CuryInfoID = doc.CuryInfoID;
			newdoc.BranchID = doc.BranchID;
			newdoc.TaxCalcMode = TaxCalculationMode.TaxSetting;

			newdoc.TaxZoneID = doc.TaxZoneID;
			newdoc.CuryOrigDocAmt = doc.CuryDocTotal;
			newdoc.DueDate = doc.DueDate;
			newdoc.DiscDate = doc.DiscDate;
			newdoc.Hold = false;

			var result = new APInvoiceWrapper(newdoc, apTransactions, apTaxes);

			return result;
		}

		public virtual APTran[] CreateTransactions(POLandedCostDoc doc, IEnumerable<POLandedCostDetail> landedCostDetail)
		{
			var result = new List<APTran>();

			foreach (var detail in landedCostDetail)
			{
				var aLCCode = GetLandedCostCode(detail.LandedCostCodeID);

				APTran aDest = new APTran();

				aDest.AccountID = detail.LCAccrualAcct;
				aDest.SubID = detail.LCAccrualSub;
				
				aDest.UOM = null;
				aDest.Qty = Decimal.One;

				aDest.CuryUnitCost = Math.Abs(detail.CuryLineAmt ?? 0);
				aDest.CuryTranAmt = Math.Abs(detail.CuryLineAmt ?? 0);
				
				aDest.TranDesc = detail.Descr;
				aDest.InventoryID = null;
				aDest.TaxCategoryID = detail.TaxCategoryID;
				//aDest.TaxID = aSrc.TaxID;
				aDest.LCDocType = doc.DocType;
				aDest.LCRefNbr = doc.RefNbr;
				aDest.LCLineNbr = detail.LineNbr;
				// retainage is always zero for landed cost trans
				aDest.RetainagePct = 0m;
				aDest.CuryRetainageAmt = 0m;
				aDest.RetainageAmt = 0m;

				aDest.BranchID = detail.BranchID;

				aDest.ReceiptLineNbr = null;
				aDest.LandedCostCodeID = detail.LandedCostCodeID;

				result.Add(aDest);
			}

			return result.ToArray();
		}

		protected virtual LandedCostCode GetLandedCostCode(string landedCostCodeID) => LandedCostCode.PK.Find(_pxGraph, landedCostCodeID);
	}
}
