﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.CS.Contracts.Interfaces;
using PX.Data;
using PX.Objects.AR.Standalone;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.TX;
using PX.Objects.TX.GraphExtensions;
using PX.TaxProvider;

namespace PX.Objects.AR
{
	public class ARCashSaleEntryExternalTax : ExternalTax<ARCashSaleEntry, ARCashSale>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.avalaraTax>();
		}

		protected virtual void _(Events.RowUpdated<ARCashSale> e)
		{
			if (e.Row.Released != true && IsDocumentExtTaxValid(e.Row) && !e.Cache.ObjectsEqual<ARCashSale.avalaraCustomerUsageType, ARCashSale.curyDiscountedTaxableTotal, ARCashSale.adjDate, ARCashSale.taxZoneID>(e.Row, e.OldRow))
			{
				e.Row.IsTaxValid = false;
			}
		}

		protected virtual void _(Events.RowInserted<ARTran> e)
		{
			if (IsDocumentExtTaxValid(Base.Document.Current) && e.Row.Released != true)
			{
				InvalidateExternalTax(Base.Document.Current);
			}
		}

		protected virtual void _(Events.RowUpdated<ARTran> e)
		{
			//if any of the fields that was saved in avalara has changed mark doc as TaxInvalid.
			if (IsDocumentExtTaxValid(Base.Document.Current) &&
				!e.Cache.ObjectsEqual<ARTran.accountID, ARTran.inventoryID, ARTran.tranDesc, ARTran.tranAmt, ARTran.tranDate, ARTran.taxCategoryID>(e.Row, e.OldRow))
			{
				InvalidateExternalTax(Base.Document.Current);
			}
		}

		protected virtual void _(Events.RowDeleted<ARTran> e)
		{
			if (IsDocumentExtTaxValid(Base.Document.Current) && e.Row.Released != true)
			{
				InvalidateExternalTax(Base.Document.Current);
			}
		}

		#region ARShippingAddress Events

		protected virtual void _(Events.RowUpdated<ARShippingAddress> e)
		{
			if (e.Row != null && Base.Document.Current != null
				&& e.Cache.ObjectsEqual<ARShippingAddress.postalCode, ARShippingAddress.countryID, ARShippingAddress.state>(e.Row, e.OldRow) == false)
			{
				InvalidateExternalTax(Base.Document.Current);
			}
		}

		protected virtual void _(Events.RowInserted<ARShippingAddress> e)
		{
			if (e.Row != null && Base.Document.Current != null)
			{
				InvalidateExternalTax(Base.Document.Current);
			}
		}

		protected virtual void _(Events.RowDeleted<ARShippingAddress> e)
		{
			if (e.Row != null && Base.Document.Current != null)
			{
				InvalidateExternalTax(Base.Document.Current);
			}
		}

		protected virtual void _(Events.FieldUpdating<ARShippingAddress, ARShippingAddress.overrideAddress> e)
		{
			if (e.Row != null && Base.Document.Current != null)
			{
				InvalidateExternalTax(Base.Document.Current);
			}
		}

		#endregion

		private void InvalidateExternalTax(ARCashSale doc)
		{
			if (IsExternalTax(doc.TaxZoneID))
			{
				doc.IsTaxValid = false;
				Base.Document.Cache.MarkUpdated(doc);
			}
		}

		public override ARCashSale CalculateExternalTax(ARCashSale invoice)
		{
			var toAddress = GetToAddress(invoice);
			bool isNonTaxable = IsNonTaxable(toAddress);

			if (isNonTaxable)
			{
				ApplyTax(invoice, GetTaxResult.Empty);
				invoice.IsTaxValid = true;
				invoice.NonTaxable = true;
				invoice = Base.Document.Update(invoice);

				SkipTaxCalcAndSave();

				return invoice;
            }
            else if (invoice.NonTaxable == true)
			{
				Base.Document.SetValueExt<ARRegister.nonTaxable>(invoice, false);
			}

			var service = TaxProviderFactory(Base, invoice.TaxZoneID);

			GetTaxRequest getRequest = BuildGetTaxRequest(invoice);

			if (getRequest.CartItems.Count == 0)
			{
				Base.Document.SetValueExt<ARCashSale.isTaxValid>(invoice, true);
				if (invoice.IsTaxSaved == true)
					Base.Document.SetValueExt<ARCashSale.isTaxSaved>(invoice, false);
				SkipTaxCalcAndSave();
			}

			GetTaxResult result = service.GetTax(getRequest);
			if (result.IsSuccess)
			{
				try
				{
					ApplyTax(invoice, result);
					SkipTaxCalcAndSave();
				}
				catch (PXOuterException ex)
				{
					try
					{
						CancelTax(invoice, VoidReasonCode.Unspecified);
					}
					catch (Exception)
					{
						throw new PXException(new PXException(ex, TX.Messages.FailedToApplyTaxes), TX.Messages.FailedToCancelTaxes);
					}

					string msg = TX.Messages.FailedToApplyTaxes;
					foreach (string err in ex.InnerMessages)
					{
						msg += Environment.NewLine + err;
					}

					throw new PXException(ex, msg);
				}
				catch (Exception ex)
				{
					try
					{
						CancelTax(invoice, VoidReasonCode.Unspecified);
					}
					catch (Exception)
					{
						throw new PXException(new PXException(ex, TX.Messages.FailedToApplyTaxes), TX.Messages.FailedToCancelTaxes);
					}

					string msg = TX.Messages.FailedToApplyTaxes;
					msg += Environment.NewLine + ex.Message;

					throw new PXException(ex, msg);
				}

				PostTaxRequest request = new PostTaxRequest();
				request.CompanyCode = getRequest.CompanyCode;
				request.DocCode = getRequest.DocCode;
				request.DocDate = getRequest.DocDate;
				request.DocType = getRequest.DocType;
				request.TotalAmount = result.TotalAmount;
				request.TotalTaxAmount = result.TotalTaxAmount;
				PostTaxResult postResult = service.PostTax(request);
				if (postResult.IsSuccess)
				{
					invoice.IsTaxValid = true;
					invoice = Base.Document.Update(invoice);
					SkipTaxCalcAndSave();
				}
			}
			else
			{
				LogMessages(result);

				throw new PXException(TX.Messages.FailedToGetTaxes);
			}


			return invoice;
		}

		[PXOverride]
		public virtual void Persist()
		{
			if (Base.Document.Current != null && IsExternalTax(Base.Document.Current.TaxZoneID) && Base.Document.Current.IsTaxValid != true && !skipExternalTaxCalcOnSave)
			{
				if (PXLongOperation.GetCurrentItem() == null)
				{
					PXLongOperation.StartOperation(Base, delegate ()
					{
						ARCashSaleEntry rg = PXGraph.CreateInstance<ARCashSaleEntry>();
						rg.Document.Current = PXSelect<ARCashSale, Where<ARCashSale.docType, Equal<Required<ARCashSale.docType>>, And<ARCashSale.refNbr, Equal<Required<ARCashSale.refNbr>>>>>.Select(rg, Base.Document.Current.DocType, Base.Document.Current.RefNbr);
						rg.CalculateExternalTax(rg.Document.Current);
					});
				}
				else
				{
					Base.CalculateExternalTax(Base.Document.Current);
				}
			}
		}

		protected virtual GetTaxRequest BuildGetTaxRequest(ARCashSale invoice)
		{
			if (invoice == null) throw new PXArgumentException(nameof(invoice), ErrorMessages.ArgumentNullException);

			Customer cust = (Customer)Base.customer.View.SelectSingleBound(new object[] { invoice });
			Location loc = (Location)Base.location.View.SelectSingleBound(new object[] { invoice });

			GetTaxRequest request = new GetTaxRequest();
			request.CompanyCode = CompanyCodeFromBranch(invoice.TaxZoneID, invoice.BranchID);
			request.CurrencyCode = invoice.CuryID;
			request.CustomerCode = cust.AcctCD;
			IAddressBase fromAddress = GetFromAddress(invoice);
			IAddressBase toAddress = GetToAddress(invoice);

			if (fromAddress == null)
				throw new PXException(Messages.FailedGetFrom);

			if (toAddress == null)
				throw new PXException(Messages.FailedGetTo);

			request.OriginAddress = AddressConverter.ConvertTaxAddress(fromAddress);
			request.DestinationAddress = AddressConverter.ConvertTaxAddress(toAddress);
			request.DocCode = $"AR.{invoice.DocType}.{invoice.RefNbr}";
			request.DocDate = invoice.DocDate.GetValueOrDefault();
			request.LocationCode = GetExternalTaxProviderLocationCode(invoice);
			if (!string.IsNullOrEmpty(invoice.AvalaraCustomerUsageType))
			{
				request.CustomerUsageType = invoice.AvalaraCustomerUsageType;
			}
			if (!string.IsNullOrEmpty(loc.CAvalaraExemptionNumber))
			{
				request.ExemptionNo = loc.CAvalaraExemptionNumber;
			}

			int mult = invoice.DocType == ARDocType.CashReturn ? -1 : 1;

			PXSelectBase<ARTran> select = new PXSelectJoin<ARTran,
				LeftJoin<InventoryItem, On<InventoryItem.inventoryID, Equal<ARTran.inventoryID>>,
					LeftJoin<Account, On<Account.accountID, Equal<ARTran.accountID>>>>,
				Where<ARTran.tranType, Equal<Current<ARCashSale.docType>>,
					And<ARTran.refNbr, Equal<Current<ARCashSale.refNbr>>,
					And<Where<ARTran.lineType, NotEqual<SOLineType.discount>, Or<ARTran.lineType, IsNull>>>>>,
				OrderBy<Asc<ARTran.tranType, Asc<ARTran.refNbr, Asc<ARTran.lineNbr>>>>>(Base);

			request.Discount = GetDocDiscount().GetValueOrDefault();
			DateTime? taxDate = invoice.OrigDocDate;

			foreach (PXResult<ARTran, InventoryItem, Account> res in select.View.SelectMultiBound(new object[] { invoice }))
			{
				ARTran tran = (ARTran)res;
				InventoryItem item = (InventoryItem)res;
				Account salesAccount = (Account)res;

				var line = new TaxCartItem();
				line.Index = tran.LineNbr ?? 0;
				line.Amount = mult * tran.CuryTranAmt.GetValueOrDefault();
				line.Description = tran.TranDesc;
				line.DestinationAddress = request.DestinationAddress;
				line.OriginAddress = request.OriginAddress;
				line.ItemCode = item.InventoryCD;
				line.Quantity = Math.Abs(tran.Qty.GetValueOrDefault());
				line.Discounted = request.Discount > 0;
				line.RevAcct = salesAccount.AccountCD;
				line.TaxCode = tran.TaxCategoryID;

				if (tran.OrigInvoiceDate != null)
					taxDate = tran.OrigInvoiceDate;

				request.CartItems.Add(line);
			}



			switch (invoice.DocType)
			{
				case ARDocType.Invoice:
				case ARDocType.DebitMemo:
				case ARDocType.FinCharge:
				case ARDocType.CashSale:
					request.DocType = TaxDocumentType.SalesInvoice;
					break;
				case ARDocType.CreditMemo:
				case ARDocType.CashReturn:
					if (invoice.OrigDocDate != null)
					{
						request.TaxOverride.Reason = Messages.ReturnReason;
						request.TaxOverride.TaxDate = taxDate.Value;
						request.TaxOverride.TaxOverrideType = TaxOverrideType.TaxDate;
					}
					request.DocType = TaxDocumentType.ReturnInvoice;
					break;

				default:
					throw new PXException(Messages.DocTypeNotSupported);
			}

			return request;
		}

		protected virtual void ApplyTax(ARCashSale invoice, GetTaxResult result)
		{
			TaxZone taxZone = null;
			AP.Vendor vendor = null;
			if (result.TaxSummary.Length > 0)
			{
				taxZone = (TaxZone)Base.taxzone.View.SelectSingleBound(new object[] { invoice });
				vendor = PXSelect<AP.Vendor, Where<AP.Vendor.bAccountID, Equal<Required<AP.Vendor.bAccountID>>>>.Select(Base, taxZone.TaxVendorID);
				if (vendor == null)
					throw new PXException(TX.Messages.ExternalTaxVendorNotFound);

				if (vendor.SalesTaxAcctID == null)
					throw new PXException(TX.Messages.TaxPayableAccountNotSpecified, vendor.AcctCD);

				if (vendor.SalesTaxSubID == null)
					throw new PXException(TX.Messages.TaxPayableSubNotSpecified, vendor.AcctCD);
			}
			//Clear all existing Tax transactions:
			foreach (PXResult<ARTaxTran, Tax> res in Base.Taxes.View.SelectMultiBound(new object[] { invoice }))
			{
				ARTaxTran taxTran = (ARTaxTran)res;
				Base.Taxes.Delete(taxTran);
			}

			Base.Views.Caches.Add(typeof(Tax));

			for (int i = 0; i < result.TaxSummary.Length; i++)
			{
				string taxID = result.TaxSummary[i].TaxName;
				if (string.IsNullOrEmpty(taxID))
					taxID = result.TaxSummary[i].JurisCode;

				if (string.IsNullOrEmpty(taxID))
				{
					PXTrace.WriteInformation(Messages.EmptyValuesFromExternalTaxProvider);
					continue;
				}

				//Insert Tax if not exists - just for the selectors sake
				Tax tx = PXSelect<Tax, Where<Tax.taxID, Equal<Required<Tax.taxID>>>>.Select(Base, taxID);
				if (tx == null)
				{
					tx = new Tax();
					tx.TaxID = taxID;
					tx.Descr = PXMessages.LocalizeFormatNoPrefixNLA(TX.Messages.ExternalTaxProviderTaxId, taxID);
					tx.TaxType = CSTaxType.Sales;
					tx.TaxCalcType = CSTaxCalcType.Doc;
					tx.TaxCalcLevel = result.TaxSummary[i].TaxCalculationLevel.ToCSTaxCalcLevel();
					tx.TaxApplyTermsDisc = CSTaxTermsDiscount.ToTaxableAmount;
					tx.SalesTaxAcctID = vendor.SalesTaxAcctID;
					tx.SalesTaxSubID = vendor.SalesTaxSubID;
					tx.ExpenseAccountID = vendor.TaxExpenseAcctID;
					tx.ExpenseSubID = vendor.TaxExpenseSubID;
					tx.TaxVendorID = taxZone.TaxVendorID;
					tx.IsExternal = true;

					Base.Caches[typeof(Tax)].Insert(tx);
				}

				ARTaxTran tax = new ARTaxTran();
				tax.Module = BatchModule.AR;
				tax.TranType = invoice.DocType;
				tax.RefNbr = invoice.RefNbr;
				tax.TaxID = taxID;
				tax.CuryTaxAmt = Math.Abs(result.TaxSummary[i].TaxAmount);
				tax.CuryTaxableAmt = Math.Abs(result.TaxSummary[i].TaxableAmount);
				tax.TaxRate = Convert.ToDecimal(result.TaxSummary[i].Rate) * 100;
				tax.TaxType = "S";
				tax.TaxBucketID = 0;
				tax.AccountID = vendor.SalesTaxAcctID;
				tax.SubID = vendor.SalesTaxSubID;
				tax.JurisType = result.TaxSummary[i].JurisType;
				tax.JurisName = result.TaxSummary[i].JurisName;

				Base.Taxes.Insert(tax);
			}

			bool requireControlTotal = Base.arsetup.Current.RequireControlTotal == true;

			if (invoice.Hold != true)
				Base.arsetup.Current.RequireControlTotal = false;


			try
			{
				invoice.CuryTaxTotal = Math.Abs(result.TotalTaxAmount);
				Base.Document.Cache.SetValueExt<ARCashSale.isTaxSaved>(invoice, true);
			}
			finally
			{
				Base.arsetup.Current.RequireControlTotal = requireControlTotal;
			}
		}

		protected virtual void CancelTax(ARCashSale invoice, VoidReasonCode code)
		{
			var request = new VoidTaxRequest();
			request.CompanyCode = CompanyCodeFromBranch(invoice.TaxZoneID, invoice.BranchID);
			request.Code = code;
			request.DocCode = $"AR.{invoice.DocType}.{invoice.RefNbr}";
			request.DocType = TaxDocumentType.SalesInvoice;

			var service = TaxProviderFactory(Base, invoice.TaxZoneID);
			var result = service.VoidTax(request);

			bool raiseError = false;
			if (!result.IsSuccess)
			{
				LogMessages(result);

				if (!result.IsSuccess && result.Messages.Any(t => t.Contains("DocumentNotFoundError")))
				{
					//just ignore this error. There is no document to delete in avalara.
				}
				else
				{
					raiseError = true;
				}
			}

			if (raiseError)
			{
				throw new PXException(TX.Messages.FailedToDeleteFromExternalTaxProvider);
			}
			else
			{
				invoice.IsTaxSaved = false;
				invoice.IsTaxValid = false;
				if (Base.Document.Cache.GetStatus(invoice) == PXEntryStatus.Notchanged)
					Base.Document.Cache.SetStatus(invoice, PXEntryStatus.Updated);
			}
		}

		protected virtual IAddressBase GetFromAddress(ARCashSale invoice)
		{
			PXSelectBase<Branch> select = new PXSelectJoin
				<Branch, InnerJoin<BAccountR, On<BAccountR.bAccountID, Equal<Branch.bAccountID>>,
					InnerJoin<Address, On<Address.addressID, Equal<BAccountR.defAddressID>>>>,
					Where<Branch.branchID, Equal<Required<Branch.branchID>>>>(Base);

			foreach (PXResult<Branch, BAccountR, Address> res in select.Select(invoice.BranchID))
				return (Address)res;

			return null;
		}

		protected virtual IAddressBase GetToAddress(ARCashSale invoice)
		{
			return (ARShippingAddress)Base.Shipping_Address.View.SelectSingleBound(new object[] { invoice });
		}

        public virtual bool IsDocumentExtTaxValid(ARCashSale doc)
        {
            return doc != null && IsExternalTax(doc.TaxZoneID) && doc.InstallmentNbr == null;
        }
	}
}
