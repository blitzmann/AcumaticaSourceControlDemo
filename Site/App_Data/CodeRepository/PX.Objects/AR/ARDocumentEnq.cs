using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

using PX.Data;
using PX.Objects.AR.Repositories;
using PX.Objects.GL;
using PX.Objects.GL.Attributes;
using PX.Objects.CS;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.Common;

using PX.Objects.GL.FinPeriods;
using PX.Objects.Common.Tools;
using PX.Objects.GL.FinPeriods.TableDefinition;

namespace PX.Objects.AR
{
	[TableAndChartDashboardType]
	public class ARDocumentEnq : PXGraph<ARDocumentEnq>
	{
		#region Internal Types
		[Serializable]
		public partial class ARDocumentFilter : IBqlTable
		{
			#region OrganizationID
			public abstract class organizationID : PX.Data.BQL.BqlInt.Field<organizationID> { }

			[Organization(false, Required = false)]
			public int? OrganizationID { get; set; }
			#endregion
			#region BranchID
			public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

			[BranchOfOrganization(typeof(ARDocumentFilter.organizationID), false)]
			public int? BranchID { get; set; }
			#endregion
			#region CustomerID
			public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
			protected Int32? _CustomerID;
			[PXDefault()]
			[Customer(DescriptionField = typeof(Customer.acctName))]
			public virtual Int32? CustomerID
			{
				get
				{
					return this._CustomerID;
				}
				set
				{
					this._CustomerID = value;
				}
			}
			#endregion
			#region ARAcctID
			public abstract class aRAcctID : PX.Data.BQL.BqlInt.Field<aRAcctID> { }
			protected Int32? _ARAcctID;
			[GL.Account(null, typeof(Search5<Account.accountID,
					InnerJoin<ARHistory, On<Account.accountID, Equal<ARHistory.accountID>>>,
					Where<Match<Current<AccessInfo.userName>>>,
					Aggregate<GroupBy<Account.accountID>>>),
			   DisplayName = "AR Account", DescriptionField = typeof(GL.Account.description))]
			public virtual Int32? ARAcctID
			{
				get
				{
					return this._ARAcctID;
				}
				set
				{
					this._ARAcctID = value;
				}
			}
			#endregion
			#region ARSubID
			public abstract class aRSubID : PX.Data.BQL.BqlInt.Field<aRSubID> { }
			protected Int32? _ARSubID;
			[GL.SubAccount(DisplayName = "AR Sub.", DescriptionField = typeof(GL.Sub.description))]
			public virtual Int32? ARSubID
			{
				get
				{
					return this._ARSubID;
				}
				set
				{
					this._ARSubID = value;
				}
			}
			#endregion
			#region SubCD
			public abstract class subCD : PX.Data.BQL.BqlString.Field<subCD> { }
			protected String _SubCD;
			[PXDBString(30, IsUnicode = true)]
			[PXUIField(DisplayName = "AR Subaccount", Visibility = PXUIVisibility.Invisible, FieldClass = SubAccountAttribute.DimensionName)]
			[PXDimension("SUBACCOUNT", ValidComboRequired = false)]
			public virtual String SubCD
			{
				get
				{
					return this._SubCD;
				}
				set
				{
					this._SubCD = value;
				}
			}
			#endregion
			#region UseMasterCalendar
			public abstract class useMasterCalendar : PX.Data.BQL.BqlBool.Field<useMasterCalendar> { }

			[PXDBBool]
			[PXUIField(DisplayName = Common.Messages.UseMasterCalendar)]
			[PXUIVisible(typeof(FeatureInstalled<FeaturesSet.multipleCalendarsSupport>))]
			public bool? UseMasterCalendar { get; set; }
			#endregion

			#region Period
			public abstract class period : PX.Data.BQL.BqlString.Field<period> { }

			[AnyPeriodFilterable(null, null,
				branchSourceType: typeof(ARDocumentFilter.branchID),
				organizationSourceType: typeof(ARDocumentFilter.organizationID),
				useMasterCalendarSourceType: typeof(ARDocumentFilter.useMasterCalendar),
				redefaultOrRevalidateOnOrganizationSourceUpdated: false)]
			[PXUIField(DisplayName = "Period", Visibility = PXUIVisibility.Visible)]
			public virtual string Period
				{
				get;
				set;
			}
			#endregion
			#region MasterFinPeriodID
			public abstract class masterFinPeriodID : PX.Data.BQL.BqlString.Field<masterFinPeriodID> { }
			[Obsolete("This is an absolete field. It will be removed in 2019R2")]
			[PeriodID]
			public virtual string MasterFinPeriodID { get; set; }
			#endregion
			#region DocType
			public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
			protected String _DocType;
			[PXDBString(3, IsFixed = true)]
			[PXDefault()]
			[ARDocType.List()]
			[PXUIField(DisplayName = "Type")]
			public virtual String DocType
			{
				get
				{
					return this._DocType;
				}
				set
				{
					this._DocType = value;
				}
			}
			#endregion
			#region ShowAllDocs
			public abstract class showAllDocs : PX.Data.BQL.BqlBool.Field<showAllDocs> { }
			protected bool? _ShowAllDocs;
			[PXDBBool()]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Show All Documents")]
			public virtual bool? ShowAllDocs
			{
				get
				{
					return this._ShowAllDocs;
				}
				set
				{
					this._ShowAllDocs = value;
				}
			}
			#endregion
			#region IncludeUnreleased
			public abstract class includeUnreleased : PX.Data.BQL.BqlBool.Field<includeUnreleased> { }
			protected bool? _IncludeUnreleased;
			[PXDBBool()]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Include Unreleased Documents")]
			public virtual bool? IncludeUnreleased
			{
				get
				{
					return this._IncludeUnreleased;
				}
				set
				{
					this._IncludeUnreleased = value;
				}
			}
			#endregion
			#region CuryID
			public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
			protected String _CuryID;
			[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL")]
			[PXSelector(typeof(CM.Currency.curyID), CacheGlobal = true)]
			[PXUIField(DisplayName = "Currency")]
			public virtual String CuryID
			{
				get
				{
					return this._CuryID;
				}
				set
				{
					this._CuryID = value;
				}
			}
			#endregion
			#region SubCD Wildcard
			public abstract class subCDWildcard : PX.Data.BQL.BqlString.Field<subCDWildcard> { };
			[PXDBString(30, IsUnicode = true)]
			public virtual String SubCDWildcard
			{
				get
				{
					return SubCDUtils.CreateSubCDWildcard(this._SubCD, SubAccountAttribute.DimensionName);
				}
			}



			#endregion
			#region CuryBalanceSummary
			public abstract class curyBalanceSummary : PX.Data.BQL.BqlDecimal.Field<curyBalanceSummary> { }
			protected Decimal? _CuryBalanceSummary;
			[PXCury(typeof(ARDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance by Documents (Currency)", Enabled = false)]
			public virtual Decimal? CuryBalanceSummary
			{
				get
				{
					return this._CuryBalanceSummary;
				}
				set
				{
					this._CuryBalanceSummary = value;
				}
			}
			#endregion
			#region BalanceSummary
			public abstract class balanceSummary : PX.Data.BQL.BqlDecimal.Field<balanceSummary> { }
			protected Decimal? _BalanceSummary;
			[PXDBBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance by Documents", Enabled = false)]
			public virtual Decimal? BalanceSummary
			{
				get
				{
					return this._BalanceSummary;
				}
				set
				{
					this._BalanceSummary = value;
				}
			}
			#endregion
			#region CuryCustomerBalance
			public abstract class curyCustomerBalance : PX.Data.BQL.BqlDecimal.Field<curyCustomerBalance> { }
			protected Decimal? _CuryCustomerBalance;
			[PXCury(typeof(ARDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Current Balance (Currency)", Enabled = false)]
			public virtual Decimal? CuryCustomerBalance
			{
				get
				{
					return this._CuryCustomerBalance;
				}
				set
				{
					this._CuryCustomerBalance = value;
				}
			}
			#endregion
			#region CustomerBalance
			public abstract class customerBalance : PX.Data.BQL.BqlDecimal.Field<customerBalance> { }
			protected Decimal? _CustomerBalance;
			[PXDBBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Current Balance", Enabled = false)]
			public virtual Decimal? CustomerBalance
			{
				get
				{
					return this._CustomerBalance;
				}
				set
				{
					this._CustomerBalance = value;
				}
			}
			#endregion
			#region CuryVCustomerRetainedBalance
			public abstract class curyCustomerRetainedBalance : PX.Data.BQL.BqlDecimal.Field<curyCustomerRetainedBalance> { }
			[PXCury(typeof(ARDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Retained Balance (Currency)", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual Decimal? CuryCustomerRetainedBalance
			{
				get;
				set;
			}
			#endregion
			#region CustomerRetainedBalance
			public abstract class customerRetainedBalance : PX.Data.BQL.BqlDecimal.Field<customerRetainedBalance> { }

			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Retained Balance", Enabled = false, FieldClass = nameof(FeaturesSet.Retainage))]
			public virtual Decimal? CustomerRetainedBalance
			{
				get;
				set;
			}
			#endregion
			#region CuryCustomerDepositsBalance
			public abstract class curyCustomerDepositsBalance : PX.Data.BQL.BqlDecimal.Field<curyCustomerDepositsBalance> { }

			protected Decimal? _CuryCustomerDepositsBalance;
			[PXCury(typeof(ARDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Prepayments Balance (Currency)", Enabled = false)]
			public virtual Decimal? CuryCustomerDepositsBalance
			{
				get
				{
					return this._CuryCustomerDepositsBalance;
				}
				set
				{
					this._CuryCustomerDepositsBalance = value;
				}
			}
			#endregion
			#region CustomerDepositsBalance

			public abstract class customerDepositsBalance : PX.Data.BQL.BqlDecimal.Field<customerDepositsBalance> { }
			protected Decimal? _CustomerDepositsBalance;
			[PXDBBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Prepayment Balance", Enabled = false)]
			public virtual Decimal? CustomerDepositsBalance
			{
				get
				{
					return this._CustomerDepositsBalance;
				}
				set
				{
					this._CustomerDepositsBalance = value;
				}
			}
			#endregion
			#region CuryDifference
			public abstract class curyDifference : PX.Data.BQL.BqlDecimal.Field<curyDifference> { }

			[PXCury(typeof(ARDocumentFilter.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance Discrepancy (Currency)", Enabled = false)]
			public virtual Decimal? CuryDifference
			{
				[PXDependsOnFields(typeof(ARDocumentFilter.curyCustomerBalance),typeof(ARDocumentFilter.curyBalanceSummary),typeof(ARDocumentFilter.curyCustomerDepositsBalance))]
				get
				{
					return (this._CuryCustomerBalance - this._CuryBalanceSummary + this._CuryCustomerDepositsBalance);
				}
			}
			#endregion
			#region Difference
			public abstract class difference : PX.Data.BQL.BqlDecimal.Field<difference> { }
			[PXDBBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance Discrepancy", Enabled = false)]
			public virtual Decimal? Difference
			{
				[PXDependsOnFields(typeof(ARDocumentFilter.customerBalance), typeof(ARDocumentFilter.balanceSummary), typeof(ARDocumentFilter.customerDepositsBalance))]
				get
				{
					return (this._CustomerBalance - this._BalanceSummary + this._CustomerDepositsBalance);
				}
			}
			#endregion
			#region IncludeChildAccounts
			public abstract class includeChildAccounts : PX.Data.BQL.BqlBool.Field<includeChildAccounts> { }

			[PXDBBool]
			[PXDefault(typeof(Search<CS.FeaturesSet.parentChildAccount>))]
			[PXUIField(DisplayName = "Include Child Accounts")]
			public virtual bool? IncludeChildAccounts { get; set; }
			#endregion

			public virtual void ClearSummary()
			{
				CustomerBalance = Decimal.Zero;
				BalanceSummary = Decimal.Zero;
				CustomerDepositsBalance = Decimal.Zero;
				CuryCustomerBalance = Decimal.Zero;
				CuryBalanceSummary = Decimal.Zero;
				CuryCustomerDepositsBalance = Decimal.Zero; 
				CuryCustomerRetainedBalance = Decimal.Zero;
				CustomerRetainedBalance = Decimal.Zero;
			}

		}

		[Serializable()]
		[PXPrimaryGraph(typeof(ARDocumentEnq), Filter = typeof(ARDocumentFilter))]
		[PXCacheName(Messages.ARHistoryForReport)]
		public partial class ARHistoryForReport : ARHistory { }

		[Serializable()]
		public partial class ARDocumentResult : ARRegister
		{
            public new class docType : PX.Data.BQL.BqlString.Field<docType> { }
			public new class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }

			#region DisplayDocType
			public abstract class displayDocType : PX.Data.BQL.BqlString.Field<displayDocType> { }
			protected String _DisplayDocType;
			[PXString(3, IsFixed = true)]
			[ARDisplayDocType.List()]
			[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true)]
			public virtual String DisplayDocType
			{
				[PXDependsOnFields(typeof(docType))]
				get
				{
					return (String.IsNullOrEmpty(this._DisplayDocType) ? this._DocType : this._DisplayDocType);
				}
				set
				{
					this._DisplayDocType = value;
				}
			}
			#endregion
			#region CuryOrigDocAmt
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(ARRegister.curyInfoID), typeof(ARRegister.origDocAmt))]
			[PXUIField(DisplayName = "Currency Origin. Amount")]
			public override Decimal? CuryOrigDocAmt
			{
				get
				{
					return this._CuryOrigDocAmt;
				}
				set
				{
					this._CuryOrigDocAmt = value;
				}
			}
			#endregion
			#region OrigDocAmt
			[PXDBBaseCury()]
			[PXUIField(DisplayName = "Origin. Amount")]
			[PXDefault(TypeCode.Decimal, "0.0")]
			public override Decimal? OrigDocAmt
			{
				get
				{
					return this._OrigDocAmt;
				}
				set
				{
					this._OrigDocAmt = value;
				}
			}
			#endregion
			#region CuryDocBal
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXDBCurrency(typeof(ARRegister.curyInfoID), typeof(ARRegister.curyDocBal))]
			[PXUIField(DisplayName = "Currency Balance")]
			public override Decimal? CuryDocBal
			{
				get
				{
					return this._CuryDocBal;
				}
				set
				{
					this._CuryDocBal = value;
				}
			} 
			#endregion
			#region DocBal
			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Balance")]
			public override Decimal? DocBal
			{
				get
				{
					return this._DocBal;
				}
				set
				{
					this._DocBal = value;
				}
			}
			#endregion
			#region ARTurnover
			public abstract class aRTurnover : PX.Data.BQL.BqlDecimal.Field<aRTurnover> { }

			/// <summary>
			/// Expected GL turnover for the document.
			/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
			/// </summary>
			[PXBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "AR Turnover")]
			public virtual decimal? ARTurnover
			{
				get;
				set;
			}
			#endregion

			#region RGOLAmt
			[PXDBBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "RGOL Amount")]
			public override Decimal? RGOLAmt
			{
				get
				{
					return this._RGOLAmt;
				}
				set
				{
					this._RGOLAmt = value;
				}
			}
			#endregion
			#region ExtRefNbr
			public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
			protected String _ExtRefNbr;
			[PXString(30, IsUnicode = true)]
			[PXUIField(DisplayName = "Customer Invoice Nbr./Payment Nbr.")]
			public virtual String ExtRefNbr
			{
				get
				{
					return this._ExtRefNbr;
				}
				set
				{
					this._ExtRefNbr = value;
				}
			}
			#endregion
			#region PaymentMethodID
			public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
			protected String _PaymentMethodID;
			[PXString(10, IsUnicode = true, InputMask = ">aaaaaaaaaa")]
			[PXUIField(DisplayName = "Payment Method")]
			public virtual String PaymentMethodID
			{
				get
				{
					return this._PaymentMethodID;
				}
				set
				{
					this._PaymentMethodID = value;
				}
			}
			#endregion
			#region CustomerID
			public new abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

			[Customer(Enabled = false, Visible = false, DescriptionField = typeof(Customer.acctName))]
			public override Int32? CustomerID
			{
				get { return _CustomerID; }
				set { _CustomerID = value; }
			}
			#endregion
			#region CuryBegBalance
			public abstract class curyBegBalance : PX.Data.BQL.BqlDecimal.Field<curyBegBalance> { }
			protected Decimal? _CuryBegBalance;
			[PXCury(typeof(ARRegister.curyID))]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Currency Period Beg. Balance")]
			public virtual Decimal? CuryBegBalance
			{
				get
				{
					return this._CuryBegBalance;
				}
				set
				{
					this._CuryBegBalance = value;
				}
			}
			#endregion
			#region BegBalance
			public abstract class begBalance : PX.Data.BQL.BqlDecimal.Field<begBalance> { }
			protected Decimal? _BegBalance;
			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Period Beg. Balance")]
			public virtual Decimal? BegBalance
			{
				get
				{
					return this._BegBalance;
				}
				set
				{
					this._BegBalance = value;
				}
			}
			#endregion
			
			#region CuryDiscActTaken
			public abstract class curyDiscActTaken : PX.Data.BQL.BqlDecimal.Field<curyDiscActTaken> { }
			protected Decimal? _CuryDiscActTaken;
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXCury(typeof(ARRegister.curyID))]
			[PXUIField(DisplayName = "Currency Cash Discount Taken")]
			public virtual Decimal? CuryDiscActTaken
			{
				get
				{
					return this._CuryDiscActTaken;
				}
				set
				{
					this._CuryDiscActTaken = value;
				}
			}
			#endregion
			#region DiscActTaken
			public abstract class discActTaken : PX.Data.BQL.BqlDecimal.Field<discActTaken> { }
			protected Decimal? _DiscActTaken;
			[PXBaseCury()]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Cash Discount Taken")]
			public virtual Decimal? DiscActTaken
			{
				get
				{
					return this._DiscActTaken;
				}
				set
				{
					this._DiscActTaken = value;
				}
			}
			#endregion
			#region DueDate
			public new abstract class dueDate : PX.Data.BQL.BqlDateTime.Field<dueDate> { }
			[PXDate()]
			[PXUIField(DisplayName = "Due Date", Visibility = PXUIVisibility.SelectorVisible)]
			public override DateTime? DueDate
			{
				get
				{
					return this._DueDate;
				}
				set
				{
					this._DueDate = value;
				}
			}
			#endregion
			#region Payable
			public override Boolean? Payable
			{
				[PXDependsOnFields(typeof(isComplementary))]
				get
				{

					if(!(this.IsComplementary==true) )
						return base.Payable;
					else
						return (base.Payable == false); //Invert type
				}
				set
				{
				}
			}
			#endregion
			#region Paying
			public override Boolean? Paying
			{
				[PXDependsOnFields(typeof(isComplementary))]
				get
				{
					if (!(this.IsComplementary==true))
						return base.Paying;
					else
						return (base.Paying == false); //Invert Sign
				}
				set
				{
				}
			}
			#endregion
			#region SignBalance
			public override Decimal? SignBalance
			{
				[PXDependsOnFields(typeof(docType),typeof(isComplementary))]
				get
				{
					Decimal? result = null;
					if (this._DocType == ARDocType.CashSale || this._DocType == ARDocType.CashReturn)
					{
						if (this._DocType == ARDocType.CashSale)
							result = Decimal.MinusOne;
						else if (this._DocType == ARDocType.CashReturn)
							result = Decimal.One;
					}
					else
					{
						result = base.SignBalance;
					}
					if (this.IsComplementary==true && result.HasValue)
						result *= Decimal.MinusOne;
					return result;
				}
				set
				{
				}
			}
			#endregion
			#region IsComplementary
			public abstract class isComplementary : PX.Data.BQL.BqlBool.Field<isComplementary> { }

			protected Boolean? _IsComplimentary;
			public Boolean? IsComplementary
			{
				get 
				{
					return _IsComplimentary;
				}
				set
				{
					_IsComplimentary = value;
				}
			}
			#endregion
		}

		private sealed class ARDisplayDocType : ARDocType
		{
			public const string CashReturnInvoice = "RCI";
			public const string CashSaleInvoice = "CSI";
			public new class ListAttribute : PXStringListAttribute
			{
				public ListAttribute()
					: base(
					new string[] { Invoice, DebitMemo, CreditMemo, Payment, VoidPayment, Prepayment, Refund, VoidRefund, FinCharge, SmallBalanceWO, SmallCreditWO, CashSale, CashReturn, CashSaleInvoice, CashReturnInvoice },
					new string[] { Messages.Invoice, Messages.DebitMemo, Messages.CreditMemo, Messages.Payment, Messages.VoidPayment, Messages.Prepayment, Messages.Refund, Messages.VoidRefund, Messages.FinCharge, Messages.SmallBalanceWO, Messages.SmallCreditWO, Messages.CashSale, Messages.CashReturn,Messages.CashSaleInvoice,Messages.CashReturnInvoice }) { }
			}
		}
		private sealed class decimalZero : PX.Data.BQL.BqlDecimal.Constant<decimalZero>
		{
			public decimalZero()
				: base(Decimal.Zero)
			{
			}
		}

		#endregion

		#region Ctor
		public ARDocumentEnq()
		{
			ARSetup setup = this.ARSetup.Current;
			Company company = this.Company.Current;

			Documents.Cache.AllowDelete = false;
			Documents.Cache.AllowInsert = false;
			Documents.Cache.AllowUpdate = false;

			PXUIFieldAttribute.SetVisibility<ARRegister.finPeriodID>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<ARRegister.customerID>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<ARRegister.customerLocationID>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<ARRegister.curyDiscBal>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<ARRegister.curyOrigDocAmt>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisibility<ARRegister.curyDiscTaken>(Documents.Cache, null, PXUIVisibility.Visible);
			PXUIFieldAttribute.SetVisible<ARRegister.customerID>(Documents.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.parentChildAccount>());

			this.actionsfolder.MenuAutoOpen = true;
			this.actionsfolder.AddMenuAction(this.createInvoice);
			this.actionsfolder.AddMenuAction(this.createPayment);
			this.actionsfolder.AddMenuAction(this.payDocument);

			this.reportsfolder.MenuAutoOpen = true;
			this.reportsfolder.AddMenuAction(this.aRBalanceByCustomerReport);
			this.reportsfolder.AddMenuAction(this.customerHistoryReport);
			this.reportsfolder.AddMenuAction(this.aRAgedPastDueReport);
			this.reportsfolder.AddMenuAction(this.aRAgedOutstandingReport);
			this.reportsfolder.AddMenuAction(this.aRRegisterReport);

			CustomerRepository = new CustomerRepository(this);
		}
		public override bool IsDirty
		{
			get
			{
				return false;
			}
		}
		#endregion

		#region Actions

		public PXCancel<ARDocumentFilter> Cancel;
		[Obsolete("Will be removed in Acumatica 2019R1")]
		public PXAction<ARDocumentFilter> viewDocument;
		public PXAction<ARDocumentFilter> viewOriginalDocument;
		public PXAction<ARDocumentFilter> previousPeriod;
		public PXAction<ARDocumentFilter> nextPeriod;

		public PXAction<ARDocumentFilter> actionsfolder;

		public PXAction<ARDocumentFilter> createInvoice;
		public PXAction<ARDocumentFilter> createPayment;
		public PXAction<ARDocumentFilter> payDocument;

		public PXAction<ARDocumentFilter> reportsfolder;
		public PXAction<ARDocumentFilter> aRBalanceByCustomerReport;
		public PXAction<ARDocumentFilter> customerHistoryReport;
		public PXAction<ARDocumentFilter> aRAgedPastDueReport;
		public PXAction<ARDocumentFilter> aRAgedOutstandingReport;
		public PXAction<ARDocumentFilter> aRRegisterReport;

		[InjectDependency]
		public IFinPeriodRepository FinPeriodRepository { get; set; }

		#endregion

		protected CustomerRepository CustomerRepository;

		#region Action Delegates

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXEditDetailButton()]
		public virtual IEnumerable ViewDocument(PXAdapter adapter)
		{
			if (this.Documents.Current != null)
			{
				PXRedirectHelper.TryRedirect(Documents.Cache, Documents.Current, "Document", PXRedirectHelper.WindowMode.NewWindow);
			}
			return Filter.Select();
		}

		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable ViewOriginalDocument(PXAdapter adapter)
		{
			if (Documents.Current != null)
			{
				ARInvoiceEntry graph = PXGraph.CreateInstance<ARInvoiceEntry>();
				ARRegister origDoc = PXSelect<ARRegister,
					Where<ARRegister.refNbr, Equal<Required<ARRegister.origRefNbr>>,
						And<ARRegister.docType, Equal<Required<ARRegister.origDocType>>>>>
					.SelectSingleBound(graph, null, Documents.Current.OrigRefNbr, Documents.Current.OrigDocType);
				if (origDoc != null)
				{
					PXRedirectHelper.TryRedirect(graph.Document.Cache, origDoc, "Document", PXRedirectHelper.WindowMode.NewWindow);
				}
			}
			return adapter.Get();
		}

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXPreviousButton]
		public virtual IEnumerable PreviousPeriod(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current as ARDocumentFilter;

			int? calendarOrganizationID = FinPeriodRepository.GetCalendarOrganizationID(filter.OrganizationID, filter.BranchID, filter.UseMasterCalendar);

			FinPeriod prevPeriod = FinPeriodRepository.FindPrevPeriod(calendarOrganizationID, filter.Period, looped: true);

			filter.Period = prevPeriod?.FinPeriodID;

			return adapter.Get();
		}

		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXNextButton]
		public virtual IEnumerable NextPeriod(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current as ARDocumentFilter;

			int? calendarOrganizationID = FinPeriodRepository.GetCalendarOrganizationID(filter.OrganizationID, filter.BranchID, filter.UseMasterCalendar);

			FinPeriod nextPeriod = FinPeriodRepository.FindNextPeriod(calendarOrganizationID, filter.Period, looped: true);

			filter.Period = nextPeriod?.FinPeriodID;

			return adapter.Get();
		}

		[PXUIField(DisplayName = "Actions", MapEnableRights = PXCacheRights.Select)]
		[PXButton(SpecialType = PXSpecialButtonType.ActionsFolder)]
		protected virtual IEnumerable Actionsfolder(PXAdapter adapter)
		{
			return adapter.Get();
		}

		[PXUIField(DisplayName = "Reports", MapEnableRights = PXCacheRights.Select)]
		[PXButton(SpecialType = PXSpecialButtonType.ReportsFolder)]
		protected virtual IEnumerable Reportsfolder(PXAdapter adapter)
		{
			return adapter.Get();
		}


		[PXUIField(DisplayName = Messages.NewInvoice, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton()]
		public virtual IEnumerable CreateInvoice(PXAdapter adapter)
		{
			if (this.Filter.Current != null)
			{
				if (this.Filter.Current.CustomerID != null)
				{
					ARInvoiceEntry invoiceEntry = PXGraph.CreateInstance<ARInvoiceEntry>();
					invoiceEntry.Clear();

					ARInvoice newDoc = invoiceEntry.Document.Insert(new ARInvoice());
					newDoc.CustomerID = this.Filter.Current.CustomerID;

					object oldCustomerValue = null;
					object newCustomerValue = Filter.Current.CustomerID;

					invoiceEntry.Document.Cache.RaiseFieldVerifying<ARInvoice.customerID>(
						newDoc,
						ref newCustomerValue);

					invoiceEntry.Document.Cache.RaiseFieldUpdated<ARInvoice.customerID>(
						newDoc,
						oldCustomerValue);

					throw new PXRedirectRequiredException(invoiceEntry, "CreateInvoice");
				}
			}
			return Filter.Select();
		}

		[PXUIField(DisplayName = Messages.NewPayment, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton()]
		public virtual IEnumerable CreatePayment(PXAdapter adapter)
		{
			if (this.Filter.Current != null)
			{
				if (this.Filter.Current.CustomerID != null)
				{
					ARPaymentEntry graph = PXGraph.CreateInstance<ARPaymentEntry>();
					graph.Clear();
					ARPayment newDoc = graph.Document.Insert(new ARPayment());
					newDoc.CustomerID = this.Filter.Current.CustomerID;
					graph.Document.Cache.RaiseFieldUpdated<ARPayment.customerID>(newDoc, null);

					throw new PXRedirectRequiredException(graph, "CreatePayment");
				}
			}
			return Filter.Select();
		}


		[PXUIField(DisplayName = Messages.EnterPayment, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton()]
		public virtual IEnumerable PayDocument(PXAdapter adapter)
		{
			if (Documents.Current != null)
			{
				if (this.Documents.Current.Payable != true)
					throw new PXException(Messages.Only_Invoices_MayBe_Payed);

				if (this.Documents.Current.Status != ARDocStatus.Open)
					throw new PXException(AP.Messages.Only_Open_Documents_MayBe_Processed);

				ARInvoiceEntry graph = PXGraph.CreateInstance<ARInvoiceEntry>();
				ARInvoice inv = FindDoc<ARInvoice>(Documents.Current);
				if (inv != null)
				{
					graph.Document.Current = inv;
					graph.PayInvoice(adapter);
				}
			}
			return Filter.Select();
		}

		#endregion

		#region Report Actions Delegates

		[PXUIField(DisplayName = Messages.ARBalanceByCustomerReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ARBalanceByCustomerReport(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current;

			if (filter != null)
			{
				Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<ARDocumentFilter.customerID>>>>.Select(this);
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				if (!string.IsNullOrEmpty(filter.Period))
				{
					parameters["PeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.Period);
				}
				parameters["CustomerID"] = customer.AcctCD;
				parameters["UseMasterCalendar"] = filter.UseMasterCalendar==true?true.ToString():false.ToString();
				throw new PXReportRequiredException(parameters, "AR632500", PXBaseRedirectException.WindowMode.NewWindow , Messages.ARBalanceByCustomerReport);
			}
			return adapter.Get();
		}


		[PXUIField(DisplayName = Messages.CustomerHistoryReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable CustomerHistoryReport(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current;
			if (filter != null)
			{
				Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<ARDocumentFilter.customerID>>>>.Select(this);
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				if (!string.IsNullOrEmpty(filter.Period))
				{
					parameters["FromPeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.Period);
					parameters["ToPeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.Period);
				}
				parameters["CustomerID"] = customer.AcctCD;
				throw new PXReportRequiredException(parameters, "AR652000",PXBaseRedirectException.WindowMode.NewWindow, Messages.CustomerHistoryReport);
			}
			return adapter.Get();
		}


		[PXUIField(DisplayName = Messages.ARAgedPastDueReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ARAgedPastDueReport(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current;
			if (filter != null)
			{
				Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<ARDocumentFilter.customerID>>>>.Select(this);
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				parameters["CustomerID"] = customer.AcctCD;
				throw new PXReportRequiredException(parameters, "AR631000", PXBaseRedirectException.WindowMode.NewWindow, Messages.ARAgedPastDueReport);
			}
			return adapter.Get();
		}


		[PXUIField(DisplayName = Messages.ARAgedOutstandingReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ARAgedOutstandingReport(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current;
			if (filter != null)
			{
				Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<ARDocumentFilter.customerID>>>>.Select(this);
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				parameters["CustomerID"] = customer.AcctCD;
				throw new PXReportRequiredException(parameters, "AR631500", PXBaseRedirectException.WindowMode.NewWindow, Messages.ARAgedOutstandingReport);
			}
			return adapter.Get();
		}


		[PXUIField(DisplayName = Messages.ARRegisterReport, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ARRegisterReport(PXAdapter adapter)
		{
			ARDocumentFilter filter = Filter.Current;
			if (filter != null)
			{
				Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<ARDocumentFilter.customerID>>>>.Select(this);
				Dictionary<string, string> parameters = new Dictionary<string, string>();
				if (!string.IsNullOrEmpty(filter.Period))
				{
					parameters["StartPeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.Period);
					parameters["EndPeriodID"] = FinPeriodIDFormattingAttribute.FormatForDisplay(filter.Period);
				}
				parameters["CustomerID"] = customer.AcctCD;
				throw new PXReportRequiredException(parameters, "AR621500", PXBaseRedirectException.WindowMode.NewWindow, Messages.ARRegisterReport);
			}
			return adapter.Get();
		}
		#endregion
	
		#region Selects
		public PXFilter<ARDocumentFilter> Filter;
		[PXFilterable]
		public PXSelectOrderBy<ARDocumentResult, OrderBy<Desc<ARDocumentResult.docDate>>> Documents;
		public PXSetup<ARSetup> ARSetup;
		public PXSetup<Company> Company;

		#endregion

		#region Select Delegates
		protected virtual IEnumerable documents()
		{
			ARDocumentFilter header = Filter.Current;
			List<ARDocumentResult> result = new List<ARDocumentResult>();
			if (header == null)
			{
				return result;
			}

			int?[] relevantCustomerIDs = null;

			if (Filter.Current?.CustomerID != null && Filter.Current?.IncludeChildAccounts == true)
			{
				relevantCustomerIDs = CustomerFamilyHelper
					.GetCustomerFamily<Override.BAccount.consolidatingBAccountID>(this, Filter.Current.CustomerID)
					.Where(customerInfo => customerInfo.BusinessAccount.BAccountID != null)
					.Select(customerInfo => customerInfo.BusinessAccount.BAccountID)
					.ToArray();
			}
			else if (Filter.Current?.CustomerID != null)
			{
				relevantCustomerIDs = new[] { Filter.Current.CustomerID };
			}

			PXSelectBase<ARDocumentResult> sel = new PXSelectReadonly2<
				ARDocumentResult,
					InnerJoin<Sub, On<Sub.subID, Equal<ARDocumentResult.aRSubID>>,
					LeftJoin<ARInvoice, 
						On<ARInvoice.docType, Equal<ARDocumentResult.docType>,
						And<ARInvoice.refNbr, Equal<ARDocumentResult.refNbr>,
						And<ARInvoice.customerID, Equal<ARDocumentResult.customerID>>>>,
					LeftJoin<ARPayment, 
						On<ARPayment.docType, Equal<ARDocumentResult.docType>,
						And<ARPayment.refNbr, Equal<ARDocumentResult.refNbr>,
						And<ARPayment.customerID, Equal<ARDocumentResult.customerID>>>>>>>,
				Where<
					ARDocumentResult.customerID, In<Required<ARDocumentResult.customerID>>>>
				(this);

			int[] branchIDs = null;

			if (header.BranchID != null)
			{
				sel.WhereAnd<Where<ARRegister.branchID, Equal<Current<ARDocumentFilter.branchID>>>>();
			}
			else if (header.OrganizationID != null)
			{
				branchIDs = PXAccess.GetChildBranchIDs(header.OrganizationID, false);

				sel.WhereAnd<Where<ARRegister.branchID, In<Required<ARDocumentResult.branchID>>,
				  And<MatchWithBranch<ARRegister.branchID>>>>();
			}

			if (header.ARAcctID != null)
			{
				sel.WhereAnd<Where<ARRegister.aRAccountID, Equal<Current<ARDocumentFilter.aRAcctID>>>>();
			}

			if (header.ARSubID != null)
			{
				sel.WhereAnd<Where<ARRegister.aRSubID, Equal<Current<ARDocumentFilter.aRSubID>>>>();
			}

			if ((header.IncludeUnreleased ?? false) == false)
			{
				sel.WhereAnd<Where<ARRegister.released, Equal<True>>>();
			}
			else
			{
				sel.WhereAnd<Where<ARRegister.scheduled, Equal<False>, And<Where<ARRegister.voided, Equal<False>, Or<ARRegister.released, Equal<True>>>>>>();
			}

			if (!SubCDUtils.IsSubCDEmpty(header.SubCD))
			{
				sel.WhereAnd<Where<Sub.subCD, Like<Current<ARDocumentFilter.subCDWildcard>>>>();
			}

			if (header.DocType != null)
			{
				sel.WhereAnd<Where<ARRegister.docType, Equal<Current<ARDocumentFilter.docType>>>>();
			}

			if (header.CuryID != null)
			{
				sel.WhereAnd<Where<ARRegister.curyID, Equal<Current<ARDocumentFilter.curyID>>>>();
			}
            
			PXSelectBase<ARAdjust> selectAdjusted = new PXSelectReadonly2<ARAdjust,
				InnerJoin<ARDocumentResult, On<ARDocumentResult.docType, Equal<ARAdjust.adjdDocType>,
					And<ARDocumentResult.refNbr, Equal<ARAdjust.adjdRefNbr>>>,
				InnerJoin<Branch,
				    On<ARAdjust.adjgBranchID, Equal<Branch.branchID>>,
				InnerJoin<FinPeriod,
				    On<Branch.organizationID, Equal<FinPeriod.organizationID>,
				        And<FinPeriod.finPeriodID, Equal<Current<ARDocumentFilter.period>>>>>>>,
				Where<ARAdjust.adjgDocType, Equal<Required<ARDocumentResult.docType>>,
					And<ARAdjust.adjgRefNbr, Equal<Required<ARDocumentResult.refNbr>>>>>(this);
													
			PXSelectBase<ARAdjust> selectAdjusting = new PXSelectReadonly2<ARAdjust,
				InnerJoin<ARDocumentResult, On<ARDocumentResult.docType, Equal<ARAdjust.adjgDocType>,
					And<ARDocumentResult.refNbr, Equal<ARAdjust.adjgRefNbr>>>,
				InnerJoin<Branch,
				    On<ARAdjust.adjdBranchID, Equal<Branch.branchID>>,
				InnerJoin<FinPeriod,
				    On<Branch.organizationID, Equal<FinPeriod.organizationID>,
				        And<FinPeriod.finPeriodID, Equal<Current<ARDocumentFilter.period>>>>>>>,
				Where<ARAdjust.adjdDocType, Equal<Required<ARDocumentResult.docType>>,
					And<ARAdjust.adjdRefNbr, Equal<Required<ARDocumentResult.refNbr>>>>>(this);

			PXSelectBase<ARInvoice> selectRetainageInvoices = new PXSelect<ARInvoice,
				Where<ARInvoice.origDocType, Equal<Required<ARInvoice.docType>>,
					And<ARInvoice.origRefNbr, Equal<Required<ARInvoice.refNbr>>,
					And<ARInvoice.released, Equal<True>,
					And<ARInvoice.isRetainageDocument, Equal<True>>>>>>(this);

			bool byPeriod = (header.Period != null);

		    var pars = new List<object>(){  };

			if (!byPeriod)
			{
				if (header.ShowAllDocs == false)
				{
					sel.WhereAnd<Where<ARRegister.openDoc, Equal<True>>>();
				}
			}
			else
			{
			    if (header.UseMasterCalendar == true)
			    {
			        sel.WhereAnd<Where<ARRegister.tranPeriodID, LessEqual<Current<ARDocumentFilter.period>>>>();
			        sel.WhereAnd<Where<ARRegister.closedTranPeriodID, GreaterEqual<Current<ARDocumentFilter.period>>,
			            Or<ARRegister.closedTranPeriodID, IsNull>>>();

			        selectAdjusted.WhereAnd<Where<ARAdjust.adjgTranPeriodID, LessEqual<Current<ARDocumentFilter.period>>>>();
			        selectAdjusting.WhereAnd<Where<ARAdjust.adjgTranPeriodID, LessEqual<Current<ARDocumentFilter.period>>>>();

			        selectRetainageInvoices.WhereAnd<Where<ARInvoice.tranPeriodID, LessEqual<Current<ARDocumentFilter.period>>>>();
                }
			    else
			    {
                    sel.WhereAnd<Where<ARRegister.finPeriodID, LessEqual<Current<ARDocumentFilter.period>>>>();
			        sel.WhereAnd<Where<ARRegister.closedFinPeriodID, GreaterEqual<Current<ARDocumentFilter.period>>,
			            Or<ARRegister.closedFinPeriodID, IsNull>>>();

			        selectAdjusted.WhereAnd<Where<ARAdjust.adjgTranPeriodID, LessEqual<FinPeriod.masterFinPeriodID>>>();

                    selectAdjusting.WhereAnd<Where<ARAdjust.adjgTranPeriodID, LessEqual<FinPeriod.masterFinPeriodID>>>();

			        selectRetainageInvoices.WhereAnd<Where<ARInvoice.finPeriodID, LessEqual<Current<ARDocumentFilter.period>>>>();
                }
            }

			bool anyDoc = false;
			handledDocuments = new Dictionary<ARDocKey, ARDocumentResult>();
			
			foreach (PXResult<ARDocumentResult, Sub, ARInvoice, ARPayment> reg in sel.Select(relevantCustomerIDs, branchIDs))
			{
				ARDocumentResult res = (ARDocumentResult)Documents.Cache.CreateCopy((ARDocumentResult)reg);
				res.IsComplementary = false;
				ARInvoice invoice = reg;
				ARPayment payment = reg;

				// Invalid/unknown data - skip record - user notification.
				//
				if (!res.Paying.HasValue) continue;
				
				if (res.DocType == ARDocType.CashSale || res.DocType == ARDocType.CashReturn) 
				{
					// Artificial split of CashSale record on invoice and payment parts.
					//
					ARDocumentResult invPart = (ARDocumentResult)Documents.Cache.CreateCopy(res);
					invPart.DisplayDocType = res.DocType == ARDocType.CashSale
						? ARDisplayDocType.CashSaleInvoice
						: ARDisplayDocType.CashReturnInvoice;
					invPart.IsComplementary = true;

					ARDocumentResult copy1 = HandleDocument(invPart, payment, invoice, header, selectAdjusted, selectAdjusting, selectRetainageInvoices, byPeriod);
					if (copy1 != null)
					{
						result.Add(copy1);
					}
				}

				ARDocumentResult copy = HandleDocument(res, payment, invoice, header, selectAdjusted, selectAdjusting, selectRetainageInvoices, byPeriod);
				if (copy != null)
				{
					result.Add(copy);
				}

				anyDoc = true;
			}

			viewDocument.SetEnabled(anyDoc);

			return result;
		}

		private Dictionary<ARDocKey, ARDocumentResult> handledDocuments;

		/// <summary>
		/// The property to get all selected documents including their applications
		/// with calculated expected GL turnover.
		/// </summary>
		public virtual Dictionary<ARDocKey, ARDocumentResult> HandledDocuments
		{
			get { return handledDocuments ?? new Dictionary<ARDocKey, ARDocumentResult>(); }
		}

		/// <summary>
		/// The method to calculate expected GL turnover values for all selected
		/// documents and their applications.
		/// Note, that <see cref="handledDocuments"></see> dictionary includes
		/// all documents showed on the UI, moreover all adjusting documents
		/// whom applications may produce GL transactions. We need all such documents
		/// to reconcile correctly turnover values between GL and AR modules because all
		/// GL transactions producing by applications are always marked as adjusting 
		/// document transactions (except CreditWO documents).
		/// </summary>
		private void AdjustARTurnover(
			ARDocumentResult res,
			decimal? aRTurnover)
		{
			ARDocumentResult handledDocument;
			ARDocKey key = new ARDocKey(res.DocType, res.RefNbr);

			if (handledDocuments.TryGetValue(key, out handledDocument))
			{
				handledDocument.ARTurnover += aRTurnover;

				if (!Equals(handledDocument, res))
				{
					res.ARTurnover = handledDocument.ARTurnover;
					handledDocuments[key] = res;
				}
			}
			else
			{
				res.ARTurnover = aRTurnover;
				handledDocuments.Add(key, res);
			}
		}

		protected virtual ARDocumentResult HandleDocument(
			ARDocumentResult aDoc, 
			ARPayment payment, 
			ARInvoice invoice, 
			ARDocumentFilter header, 
			PXSelectBase<ARAdjust> selectAdjusted, 
			PXSelectBase<ARAdjust> selectAdjusting, 
			PXSelectBase<ARInvoice> selectRetainageInvoices,
			bool byPeriod) 
		{
			ARDocumentResult res = aDoc;

			if (res.Paying == true)
			{
				res.ExtRefNbr = (res.DocType == ARDocType.CreditMemo && !string.IsNullOrEmpty(invoice.RefNbr)) 
					? invoice.InvoiceNbr 
					: payment.ExtRefNbr;
				res.PaymentMethodID = payment.PaymentMethodID;
				res.DueDate = null;
			}
			else
			{
				res.ExtRefNbr = invoice.InvoiceNbr;
				res.PaymentMethodID = null;
				res.DueDate = invoice.DueDate;
			}
			
			bool isCashSale = (res.DocType == ARDocType.CashSale || res.DocType == ARDocType.CashReturn);

			if (byPeriod)
			{
			    string documentPeriod = header.UseMasterCalendar == true ? res.TranPeriodID : res.FinPeriodID;

				bool createdInPeriod = (documentPeriod == header.Period);
				
				// Skip Cash sales created outside the period - they can't balance change  in it.
				// 
				if (!createdInPeriod && isCashSale) return null;  

				#region ARTurnover calculation

				decimal? aRTurnover = documentPeriod == header.Period &&
					res.SelfVoidingDoc != true &&
					res.Released == true
						? res.OrigDocAmt * res.SignBalance
						: 0m;

				AdjustARTurnover(res, aRTurnover);

				#endregion

				res.DocBal = res.OrigDocAmt;
				res.CuryDocBal = res.CuryOrigDocAmt;

				res.BegBalance = (documentPeriod == header.Period) ? 0m : res.OrigDocAmt;
				res.CuryBegBalance = (documentPeriod == header.Period) ? 0m : res.CuryOrigDocAmt;
				res.DiscActTaken = 0m;
				res.CuryDiscActTaken = 0m;
				res.RGOLAmt = 0m;
				res.RetainageUnreleasedAmt = res.RetainageTotal;
				res.CuryRetainageUnreleasedAmt = res.CuryRetainageTotal;

				// Filter out master record for multiple installments.
				// 
				if (invoice != null && invoice.InstallmentCntr != null) return null;

                // Scan payments, which were applyed to invoice (for invoices).
			    //

                foreach (PXResult<ARAdjust, ARDocumentResult, Branch, FinPeriod> it in selectAdjusting.Select(res.DocType, res.RefNbr))
				{
					ARAdjust adjustment = it;
					ARDocumentResult adjustingDocument = it;
				    FinPeriod documentRelatedFilterFinPeriod = it;

                    // Reversals should not be counted in Small-Credit Balance. it is always zero.
                    // 
                    if (adjustment.IsSelfAdjustment() && res.IsComplementary != true ||
						adjustment.Released != true && header.IncludeUnreleased != true ||
						adjustment.AdjdDocType == ARDocType.SmallCreditWO && adjustment.AdjgDocType == ARDocType.VoidPayment) continue;

					decimal? applicationAmount = adjustment.AdjAmt + adjustment.AdjDiscAmt + adjustment.AdjWOAmt + adjustment.SignedRGOLAmt;
					decimal? curyApplicationAmount = adjustment.CuryAdjdAmt + adjustment.CuryAdjdDiscAmt + adjustment.CuryAdjdWOAmt;

					res.DocBal -= applicationAmount;
					res.CuryDocBal -= curyApplicationAmount;
					res.DiscActTaken += adjustment.AdjDiscAmt;
					res.RGOLAmt += adjustment.RGOLAmt;
					res.CuryDiscActTaken += adjustment.CuryAdjdDiscAmt;

					if (adjustment.AdjgTranPeriodID != GetFilterMasterPeriodForAdjust(header, documentRelatedFilterFinPeriod))
					{
						res.BegBalance -= adjustment.AdjAmt + adjustment.AdjDiscAmt + adjustment.AdjWOAmt;
						res.CuryBegBalance -= adjustment.CuryAdjdAmt + adjustment.CuryAdjdDiscAmt + adjustment.CuryAdjdWOAmt;
					}

					#region ARTurnover calculation

					aRTurnover = adjustment.AdjgTranPeriodID == GetFilterMasterPeriodForAdjust(header, documentRelatedFilterFinPeriod) &&
						adjustment.Released == true
							? applicationAmount * adjustment.AdjdTBSign
							: 0m;

					// GL transactions for the CreditWO applications
					// are marked as Invoice transactions this why we 
					// should adjust turnover not for Payment document 
					// as usual.
					//
					AdjustARTurnover(res.DocType == ARDocType.SmallCreditWO ? res : adjustingDocument, aRTurnover);

					#endregion
				}
				
				// Scan invoices, to which were  payment was applied (for checks).
				// 
				if (res.IsComplementary != true)
				{
					foreach (PXResult <ARAdjust, ARDocumentResult, Branch, FinPeriod> it in selectAdjusted.Select(res.DocType, res.RefNbr))
					{
						ARAdjust adjustment = it;
						ARDocumentResult adjustedDocument = it;
					    FinPeriod documentRelatedFilterFinPeriod = it;

                        if (adjustment.Released != true && header.IncludeUnreleased != true) continue;

						decimal? applicationAmount = adjustment.AdjAmt;
						decimal? curyApplicationAmount = adjustment.CuryAdjgAmt;

						res.DocBal -= applicationAmount * adjustment.AdjgBalSign;
						res.CuryDocBal -= curyApplicationAmount * adjustment.AdjgBalSign;

						if (adjustment.AdjgTranPeriodID != GetFilterMasterPeriodForAdjust(header, documentRelatedFilterFinPeriod))
						{
							res.BegBalance -= adjustment.AdjAmt * adjustment.AdjgBalSign + adjustment.SignedRGOLAmt;
							res.CuryBegBalance -= adjustment.CuryAdjgAmt * adjustment.AdjgBalSign;
						}

						#region ARTurnover calculation

						aRTurnover = adjustment.AdjgTranPeriodID == GetFilterMasterPeriodForAdjust(header, documentRelatedFilterFinPeriod) &&
							adjustment.Released == true
								? applicationAmount * adjustment.AdjgTBSign
								: 0m;

						// GL transactions for the CreditWO applications
						// are marked as Invoice transactions this why we 
						// should adjust turnover not for Payment document 
						// as usual.
						//
						AdjustARTurnover(adjustedDocument.DocType == ARDocType.SmallCreditWO ? adjustedDocument : res, aRTurnover);

						#endregion
					}
				}
				
				foreach (ARInvoice retainageInovice in selectRetainageInvoices.Select(res.DocType, res.RefNbr))
				{
					res.RetainageUnreleasedAmt -= retainageInovice.OrigDocAmt * retainageInovice.SignAmount;
					res.CuryRetainageUnreleasedAmt -= retainageInovice.CuryOrigDocAmt * retainageInovice.SignAmount;
				}

				if ((res.Voided == true || res.DocType == ARDocType.VoidPayment) &&
					string.CompareOrdinal(header.Period, header.UseMasterCalendar == true ? res.ClosedTranPeriodID : res.ClosedFinPeriodID) >= 0)
				{
					res.DocBal = 0m;
					res.CuryDocBal = 0m;
				}

				if (res.OrigDocAmt == 0m
					|| (res.BegBalance == 0m
						&& res.DocBal == 0m
						&& res.RetainageUnreleasedAmt == 0m
						&& !createdInPeriod))
					return null;
			}
			else
			{
				bool isDiscNotTaken = isCashSale && res.IsComplementary != true;
				res.CuryDiscActTaken = isDiscNotTaken ? 0m : res.CuryDiscTaken ?? 0m;
				res.DiscActTaken = isDiscNotTaken ? 0m : res.DiscTaken ?? 0m;
				res.ARTurnover = 0m;
			}

			res.RGOLAmt = -res.RGOLAmt;
			SetValuesSign(res);
			
			return res;
		}

	    protected virtual string GetFilterMasterPeriodForAdjust(ARDocumentFilter header, FinPeriod documentRelatedFilterFinPeriod)
	    {
	        if (header.UseMasterCalendar == true)
	        {
	            return header.Period;
	        }
	        else
	        {
	            return documentRelatedFilterFinPeriod.MasterFinPeriodID;
	        }
        }

        protected virtual IEnumerable filter()
		{
			PXCache cache = this.Caches[typeof(ARDocumentFilter)];
			if (cache != null)
			{
				ARDocumentFilter filter = cache.Current as ARDocumentFilter;

				if (filter != null)
				{
					filter.ClearSummary();
					foreach (ARDocumentResult it in Documents.Select())
					{
						Aggregate(filter, it);
					}

					if (filter.CustomerID != null)
					{
						ARCustomerBalanceEnq balanceBO = PXGraph.CreateInstance<ARCustomerBalanceEnq>();
						ARCustomerBalanceEnq.ARHistoryFilter histFilter = balanceBO.Filter.Current;
                        ARCustomerBalanceEnq.Copy(histFilter, filter);

					    balanceBO.Filter.Update(histFilter);

						ARCustomerBalanceEnq.ARHistorySummary summary = balanceBO.Summary.Select();
						SetSummary(filter, summary);
					}
				}

				yield return cache.Current;
				cache.IsDirty = false;
			}
		}
		#endregion

		#region CacheAttached
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Original Document")]
		protected virtual void ARDocumentResult_OrigRefNbr_CacheAttached(PXCache sender) { }
		
		#endregion

		#region Events Handlers
		public virtual void ARDocumentFilter_ARAcctID_ExceptionHandling(PXCache cache, PXExceptionHandlingEventArgs e)
		{
			ARDocumentFilter header = e.Row as ARDocumentFilter;
			if (header != null)
			{
				e.Cancel = true;
				header.ARAcctID = null;
			}
		}

		public virtual void ARDocumentFilter_ARSubID_ExceptionHandling(PXCache cache, PXExceptionHandlingEventArgs e)
		{
			ARDocumentFilter header = e.Row as ARDocumentFilter;
			if (header != null)
			{
				e.Cancel = true;
				header.ARSubID = null;
			}
		}

		public virtual void ARDocumentFilter_CuryID_ExceptionHandling(PXCache cache, PXExceptionHandlingEventArgs e)
		{
			ARDocumentFilter header = e.Row as ARDocumentFilter;
			if (header != null)
			{
				e.Cancel = true;
				header.CuryID = null;
			}
		}

		public virtual void ARDocumentFilter_SubCD_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		public virtual void ARDocumentFilter_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			ARDocumentFilter row = (ARDocumentFilter)e.Row;
			if (row == null) return;
			PXCache docCache = this.Documents.Cache;

			bool byPeriod = (row.Period != null);

			bool isMCFeatureInstalled = PXAccess.FeatureInstalled<FeaturesSet.multicurrency>();
			bool isForeignCurrencySelected = String.IsNullOrEmpty(row.CuryID) == false && (row.CuryID != this.Company.Current.BaseCuryID);
			bool isBaseCurrencySelected = String.IsNullOrEmpty(row.CuryID) == false && (row.CuryID == this.Company.Current.BaseCuryID);

			PXUIFieldAttribute.SetVisible<ARDocumentFilter.showAllDocs>(cache, row, !byPeriod);
			PXUIFieldAttribute.SetVisible<ARDocumentFilter.includeChildAccounts>(cache, row, PXAccess.FeatureInstalled<CS.FeaturesSet.parentChildAccount>());

			PXUIFieldAttribute.SetVisible<ARDocumentFilter.curyID>(cache, row, isMCFeatureInstalled);
			PXUIFieldAttribute.SetVisible<ARDocumentFilter.curyBalanceSummary>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentFilter.curyDifference>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentFilter.curyCustomerBalance>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentFilter.curyCustomerRetainedBalance>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentFilter.curyCustomerDepositsBalance>(cache, row, isMCFeatureInstalled && isForeignCurrencySelected);

			PXUIFieldAttribute.SetRequired<ARDocumentFilter.branchID>(cache, false);

			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyID>(docCache, null, isMCFeatureInstalled);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.rGOLAmt>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyBegBalance>(docCache, null, byPeriod && isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.begBalance>(docCache, null, byPeriod);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyOrigDocAmt>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyDocBal>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyDiscActTaken>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyRetainageTotal>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyOrigDocAmtWithRetainageTotal>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);
			PXUIFieldAttribute.SetVisible<ARDocumentResult.curyRetainageUnreleasedAmt>(docCache, null, isMCFeatureInstalled && !isBaseCurrencySelected);

			Customer customer = null;

			if (row.CustomerID != null)
			{
				customer = CustomerRepository.FindByID(row.CustomerID);
			}

			createInvoice.SetEnabled(customer != null &&
				(customer.Status == BAccount.status.Active
				|| customer.Status == BAccount.status.OneTime));

			bool isPaymentAllowed = customer != null && customer.Status != BAccount.status.Inactive;

			createPayment.SetEnabled(isPaymentAllowed);
			payDocument.SetEnabled(isPaymentAllowed);

			aRBalanceByCustomerReport.SetEnabled(row.CustomerID != null);
			customerHistoryReport.SetEnabled(row.CustomerID != null);
			aRAgedPastDueReport.SetEnabled(row.CustomerID != null);
			aRAgedOutstandingReport.SetEnabled(row.CustomerID != null);
			aRRegisterReport.SetEnabled(row.CustomerID != null);
		
		}
		#endregion

		#region Utility Functions - internal
		protected virtual void SetSummary(ARDocumentFilter aDest, ARCustomerBalanceEnq.ARHistorySummary aSrc)
		{
			aDest.CustomerBalance = aSrc.BalanceSummary;
			aDest.CustomerDepositsBalance = aSrc.DepositsSummary;
			aDest.CuryCustomerBalance = aSrc.CuryBalanceSummary;
			aDest.CuryCustomerDepositsBalance = aSrc.CuryDepositsSummary;
		}

		protected virtual void Aggregate(ARDocumentFilter aDest, ARDocumentResult aSrc)
		{
			if (string.IsNullOrEmpty(aDest.Period) && aSrc.Released == false)
			{
				aDest.BalanceSummary += aSrc.OrigDocAmt ?? Decimal.Zero;
				aDest.CuryBalanceSummary += aSrc.CuryOrigDocAmt ?? Decimal.Zero;
			}
			else
			{
				aDest.BalanceSummary += aSrc.DocBal ?? Decimal.Zero;
				aDest.CuryBalanceSummary += aSrc.CuryDocBal ?? Decimal.Zero;
				aDest.CustomerRetainedBalance += aSrc.RetainageUnreleasedAmt ?? Decimal.Zero;
				aDest.CuryCustomerRetainedBalance += aSrc.CuryRetainageUnreleasedAmt ?? Decimal.Zero;
			}
		}

		protected TDoc FindDoc<TDoc>(ARDocumentResult aRes)
			where TDoc : ARRegister, new()
		{
			return FindDoc<TDoc>(this, aRes.DocType, aRes.RefNbr);
		}

		protected virtual void SetValuesSign(ARDocumentResult aRes)
		{
			if (aRes.SignBalance.HasValue)
			{
				decimal sign = aRes.SignBalance.Value;
				aRes.OrigDocAmt *= sign  ;
				aRes.DocBal *= sign;
				aRes.BegBalance *= sign;
				aRes.DiscActTaken *= sign;
				aRes.DiscTaken *= sign;
				aRes.OrigDiscAmt *= sign;
				aRes.DiscBal *= sign;
				aRes.RetainageUnreleasedAmt *= sign;
				aRes.CuryOrigDocAmt *= sign;
				aRes.CuryDocBal *= sign;
				aRes.CuryBegBalance *= sign;
				aRes.CuryDiscActTaken *= sign;
				aRes.CuryDiscTaken *= sign;
				aRes.CuryOrigDiscAmt *= sign;
				aRes.CuryDiscBal *= sign;
				aRes.CuryRetainageUnreleasedAmt *= sign;
			}
		}

		#endregion

		#region Utility Functions - public
		public static TDoc FindDoc<TDoc>(PXGraph aGraph, string aDocType, string apRefNbr)
			where TDoc : ARRegister, new()
		{
			return PXSelect<TDoc,
				Where<ARRegister.docType, Equal<Required<ARRegister.docType>>,
					And<ARRegister.refNbr, Equal<Required<ARRegister.refNbr>>>>>
				.Select(aGraph, aDocType, apRefNbr);
		}

		[Obsolete("Obsoilete. Will be removed in Acumatica ERP 2019R1")]
		public static bool? IsInvoiceType(string aDocType)
		{
			if (aDocType == ARDocType.Invoice || aDocType == ARDocType.DebitMemo || aDocType == ARDocType.FinCharge) return true;
			if (aDocType == ARDocType.Payment || aDocType == ARDocType.CreditMemo || aDocType == ARDocType.Refund || aDocType == ARDocType.VoidPayment) return false;
			return null;
		}
		#endregion
	}
}

