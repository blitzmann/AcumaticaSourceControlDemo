using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.SM;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PX.Objects.FS
{
    public class SM_CustomerMaint : PXGraphExtension<CustomerMaint>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        #region Extended initialization
        public override void Initialize()
        {
            Base.inquiry.AddMenuAction(viewServiceOrderHistory);
            Base.inquiry.AddMenuAction(viewAppointmentHistory);
            Base.inquiry.AddMenuAction(viewEquipmentSummary);
            Base.inquiry.AddMenuAction(viewContractScheduleSummary);

            Base.action.AddMenuAction(openMultipleStaffMemberBoard);
            Base.action.AddMenuAction(openSingleStaffMemberBoard);
        }
        #endregion

        private bool doCopyBillingSettings;

        #region Selects

        public PXSelectJoin<FSCustomerBillingSetup,
               CrossJoin<FSSetup>,
               Where2<
                    Where<
                        FSSetup.customerMultipleBillingOptions, Equal<True>,
                        And<FSCustomerBillingSetup.customerID, Equal<Current<Customer.bAccountID>>,
                        And<FSCustomerBillingSetup.srvOrdType, IsNotNull,
                        And<FSCustomerBillingSetup.active, Equal<True>>>>>,
                    Or<
                        Where<
                            FSSetup.customerMultipleBillingOptions, Equal<False>,
                            And<FSCustomerBillingSetup.customerID, Equal<Current<Customer.bAccountID>>,
                            And<FSCustomerBillingSetup.srvOrdType, IsNull,
                            And<FSCustomerBillingSetup.active, Equal<True>>>>>>>>
               CustomerBillingCycles;

        [PXHidden]
        public PXSetup<FSSetup> Setup;

        #endregion

        #region Actions

        #region ViewServiceOrderHistory
        public PXAction<Customer> viewServiceOrderHistory;
        [PXUIField(DisplayName = "Service Order History", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(ImageKey = PX.Web.UI.Sprite.Main.Inquiry)]
        public virtual IEnumerable ViewServiceOrderHistory(PXAdapter adapter)
        {
            Customer customerRow = Base.CurrentCustomer.Current;

            if (customerRow != null && customerRow.BAccountID > 0L)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();

                Branch branchRow = PXSelect<Branch,
                                        Where<
                                            Branch.branchID, Equal<Required<Branch.branchID>>>>
                                        .Select(Base, Base.Accessinfo.BranchID);

                parameters["BranchID"] = branchRow.BranchCD;
                parameters["CustomerID"] = customerRow.AcctCD;
                throw new PXRedirectToGIWithParametersRequiredException(new Guid(TX.GenericInquiries_GUID.SERVICE_ORDER_HISTORY), parameters);
            }

            return adapter.Get();
        }
        #endregion
        #region ViewAppointmentHistory
        public PXAction<Customer> viewAppointmentHistory;
        [PXUIField(DisplayName = "Appointment History", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(ImageKey = PX.Web.UI.Sprite.Main.Inquiry)]
        public virtual IEnumerable ViewAppointmentHistory(PXAdapter adapter)
        {
            Customer customerRow = Base.CurrentCustomer.Current;

            if (customerRow != null && customerRow.BAccountID > 0L)
            {
                AppointmentInq graph = PXGraph.CreateInstance<AppointmentInq>();

                graph.Filter.Current.BranchID = Base.Accessinfo.BranchID;
                graph.Filter.Current.CustomerID = customerRow.BAccountID;

                throw new PXRedirectRequiredException(graph, null) { Mode = PXBaseRedirectException.WindowMode.Same };
            }

            return adapter.Get();
        }
        #endregion
        #region ViewEquipmentSummary
        public PXAction<Customer> viewEquipmentSummary;
        [PXUIField(DisplayName = "Equipment Summary", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(ImageKey = PX.Web.UI.Sprite.Main.Inquiry)]
        public virtual IEnumerable ViewEquipmentSummary(PXAdapter adapter)
        {
            Customer customerRow = Base.CurrentCustomer.Current;

            if (customerRow != null && customerRow.BAccountID > 0L)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();

                parameters["CustomerID"] = customerRow.AcctCD;
                throw new PXRedirectToGIWithParametersRequiredException(new Guid(TX.GenericInquiries_GUID.EQUIPMENT_SUMMARY), parameters);
            }

            return adapter.Get();
        }
        #endregion
        #region ViewContractScheduleSummary
        public PXAction<Customer> viewContractScheduleSummary;
        [PXUIField(DisplayName = "Contract Schedule Summary", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(ImageKey = PX.Web.UI.Sprite.Main.Inquiry)]
        public virtual IEnumerable ViewContractScheduleSummary(PXAdapter adapter)
        {
            Customer customerRow = Base.CurrentCustomer.Current;

            if (customerRow != null && customerRow.BAccountID > 0L)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();

                parameters["CustomerID"] = customerRow.AcctCD;
                throw new PXRedirectToGIWithParametersRequiredException(new Guid(TX.GenericInquiries_GUID.CONTRACT_SCHEDULE_SUMMARY), parameters);
            }

            return adapter.Get();
        }
        #endregion

        #region OpenMultipleStaffMemberBoard
        public PXAction<Customer> openMultipleStaffMemberBoard;
        [PXUIField(DisplayName = TX.ActionCalendarBoardAccess.MULTI_EMP_CALENDAR, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(ImageKey = PX.Web.UI.Sprite.Main.Dashboard)]
        public virtual IEnumerable OpenMultipleStaffMemberBoard(PXAdapter adapter)
        {
            Customer customerRow = Base.CurrentCustomer.Current;

            if (customerRow != null && customerRow.BAccountID > 0L)
            {
                KeyValuePair<string, string>[] parameters = new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>(typeof(FSServiceOrder.customerID).Name, customerRow.BAccountID.Value.ToString())
                };

                throw new PXRedirectToBoardRequiredException(Paths.ScreenPaths.MULTI_EMPLOYEE_DISPATCH, parameters);
            }

            return adapter.Get();
        }
        #endregion
        #region OpenSingleStaffMemberBoard
        public PXAction<Customer> openSingleStaffMemberBoard;
        [PXUIField(DisplayName = TX.ActionCalendarBoardAccess.SINGLE_EMP_CALENDAR, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton(ImageKey = PX.Web.UI.Sprite.Main.Dashboard)]
        public virtual IEnumerable OpenSingleStaffMemberBoard(PXAdapter adapter)
        {
            Customer customerRow = Base.CurrentCustomer.Current;

            if (customerRow != null && customerRow.BAccountID > 0L)
            {
                KeyValuePair<string, string>[] parameters = new KeyValuePair<string, string>[]
                {
                    new KeyValuePair<string, string>(typeof(FSServiceOrder.customerID).Name, customerRow.BAccountID.Value.ToString())
                };

                throw new PXRedirectToBoardRequiredException(Paths.ScreenPaths.SINGLE_EMPLOYEE_DISPATCH, parameters);
            }

            return adapter.Get();
        }
        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets the Customer Billing Cycle from its Customer Class.
        /// </summary>
        private void SetBillingCycleFromCustomerClass(PXCache cache, Customer customerRow)
        {
            if (customerRow.CustomerClassID == null)
            {
                return;
            }

            FSSetup fsSetupRow = ServiceManagementSetup.GetServiceManagementSetup(Base);

            if (fsSetupRow != null
                    && fsSetupRow.CustomerMultipleBillingOptions == true)
            {
                foreach (FSCustomerBillingSetup fsCustomerBillingSetupRow in this.CustomerBillingCycles.Select())
                {
                    this.CustomerBillingCycles.Delete(fsCustomerBillingSetupRow);
                }

                foreach (FSCustomerClassBillingSetup fsCustomerClassBillingSetupRow in 
                            PXSelect<FSCustomerClassBillingSetup, 
                            Where<
                                FSCustomerClassBillingSetup.customerClassID, Equal<Required<FSCustomerClassBillingSetup.customerClassID>>>>
                            .Select(Base, customerRow.CustomerClassID))
                {
                    FSCustomerBillingSetup fsCustomerBillingSetupRow = new FSCustomerBillingSetup();
                    fsCustomerBillingSetupRow.SrvOrdType = fsCustomerClassBillingSetupRow.SrvOrdType;
                    fsCustomerBillingSetupRow.BillingCycleID = fsCustomerClassBillingSetupRow.BillingCycleID;
                    fsCustomerBillingSetupRow.SendInvoicesTo = fsCustomerClassBillingSetupRow.SendInvoicesTo;
                    fsCustomerBillingSetupRow.BillShipmentSource = fsCustomerClassBillingSetupRow.BillShipmentSource;
                    fsCustomerBillingSetupRow.FrequencyType = fsCustomerClassBillingSetupRow.FrequencyType;

                    this.CustomerBillingCycles.Insert(fsCustomerBillingSetupRow);
                }

                return;  
            }

            SetSingleBillingSettings(cache, customerRow);
        }

        private void SetSingleBillingSettings(PXCache cache, Customer customerRow)
        {
            if (customerRow.CustomerClassID == null)
            {
                return;
            }

            FSxCustomer fsxCustomerRow = cache.GetExtension<FSxCustomer>(customerRow);
            FSxCustomerClass fsxCustomerClassRow = Base.CustomerClass.Cache.GetExtension<FSxCustomerClass>(Base.CustomerClass.Current);

            if (fsxCustomerClassRow == null)
            {
                return;
            }

            if (fsxCustomerClassRow.DfltBillingCycleID != null)
            {
                fsxCustomerRow.BillingCycleID = fsxCustomerClassRow.DfltBillingCycleID;
            }

            if (fsxCustomerClassRow.SendInvoicesTo != null)
            {
                fsxCustomerRow.SendInvoicesTo = fsxCustomerClassRow.SendInvoicesTo;
            }

            if (fsxCustomerClassRow.BillShipmentSource != null)
            {
                fsxCustomerRow.BillShipmentSource = fsxCustomerClassRow.BillShipmentSource;
            }
        }

        /// <summary>
        /// Resets the values of the Frequency Fields depending on the Frequency Type value.
        /// </summary>
        /// <param name="fsCustomerBillingSetupRow"><c>fsCustomerBillingRow</c> row.</param>
        private void ResetTimeCycleOptions(FSCustomerBillingSetup fsCustomerBillingSetupRow)
        {
            switch (fsCustomerBillingSetupRow.FrequencyType)
            {
                case ID.Time_Cycle_Type.DAY_OF_MONTH:
                    fsCustomerBillingSetupRow.MonthlyFrequency = 31;
                    break;
                case ID.Time_Cycle_Type.WEEKDAY:
                    fsCustomerBillingSetupRow.WeeklyFrequency = 5;
                    break;
                default:
                    fsCustomerBillingSetupRow.WeeklyFrequency  = null;
                    fsCustomerBillingSetupRow.MonthlyFrequency = null;
                    break;
            }
        }

        /// <summary>
        /// Configures the Multiple Services Billing options for the given Customer.
        /// </summary>
        /// <param name="cache">Cache of the view.</param>
        /// <param name="customerRow">Customer row.</param>
        private void DisplayCustomerBillingOptions(PXCache cache, Customer customerRow, FSxCustomer fsxCustomerRow)
        {
            FSSetup fsSetupRow = PXSelect<FSSetup>.Select(Base);
            
            bool enableMultipleServicesBilling = fsSetupRow != null ? fsSetupRow.CustomerMultipleBillingOptions == true : false;

            PXUIFieldAttribute.SetVisible<FSxCustomer.billingCycleID>(cache, customerRow, !enableMultipleServicesBilling);
            PXUIFieldAttribute.SetVisible<FSxCustomer.sendInvoicesTo>(cache, customerRow, !enableMultipleServicesBilling);
            PXUIFieldAttribute.SetVisible<FSxCustomer.billShipmentSource>(cache, customerRow, !enableMultipleServicesBilling);

            CustomerBillingCycles.AllowSelect = enableMultipleServicesBilling;

            if (fsxCustomerRow != null)
            {
                FSBillingCycle fsBillingCycleRow = PXSelect<FSBillingCycle,
                                                    Where<
                                                        FSBillingCycle.billingCycleID, Equal<Required<FSBillingCycle.billingCycleID>>>>
                                                    .Select(Base, fsxCustomerRow.BillingCycleID);

                bool forbidUpdateBillingOptions = SharedFunctions.IsNotAllowedBillingOptionsModification(fsBillingCycleRow);

                PXUIFieldAttribute.SetEnabled<FSxCustomer.sendInvoicesTo>(
                                                    cache,
                                                    customerRow,
                                                    forbidUpdateBillingOptions == false);

                PXUIFieldAttribute.SetEnabled<FSxCustomer.billShipmentSource>(
                                                    cache,
                                                    customerRow,
                                                    forbidUpdateBillingOptions == false);

                PXUIFieldAttribute.SetEnabled<FSxCustomer.billingCycleID>(
                                                    cache,
                                                    customerRow);
            }
        }

        /// <summary>
        /// Checks if the current row is valid for edition.
        /// The line is valid if there is not another line with the same Service Order Type.
        /// </summary>
        /// <param name="fsCustomerBillingSetupRow_Current"><c>currentFSCustomerBillingRow</c> row.</param>
        /// <returns>Returns true if the line is valid.</returns>
        private bool IsThisLineValid(FSCustomerBillingSetup fsCustomerBillingSetupRow_Current)
        {
            int count = 0;

            foreach (FSCustomerBillingSetup fsCustomerBillingRow in CustomerBillingCycles.Select())
            {
                if (fsCustomerBillingSetupRow_Current.SrvOrdType != null 
                        && fsCustomerBillingSetupRow_Current.SrvOrdType.Equals(fsCustomerBillingRow.SrvOrdType)
                        && fsCustomerBillingSetupRow_Current.Active == true)
                {
                    count++;
                }
            }

            return count <= 1;
        }

        /// <summary>
        /// Resets the value from Send to Invoices dropdown if the billing cycle can not be sent to specific locations.
        /// </summary>
        private void ResetSendInvoicesToFromBillingCycle(Customer customerRow, FSCustomerBillingSetup fsCustomerBillingSetupRow)
        {
            List<object> args = new List<object>();
            FSBillingCycle fsBillingCycleRow = null;
            BqlCommand billingCycleCommand = new Select<FSBillingCycle,
                                             Where<
                                                 FSBillingCycle.billingCycleID, Equal<Required<FSBillingCycle.billingCycleID>>>>();

            PXView billingCycleView = new PXView(Base, true, billingCycleCommand);

            if (customerRow != null)
            {
                FSxCustomer fsxCustomerRow = PXCache<Customer>.GetExtension<FSxCustomer>(customerRow);
                args.Add(fsxCustomerRow.BillingCycleID);

                fsBillingCycleRow = (FSBillingCycle)billingCycleView.SelectSingle(args.ToArray());

                if (fsBillingCycleRow != null)
                {
                    if (SharedFunctions.IsNotAllowedBillingOptionsModification(fsBillingCycleRow))
                    {
                        fsxCustomerRow.SendInvoicesTo = ID.Send_Invoices_To.BILLING_CUSTOMER_BILL_TO;
                        fsxCustomerRow.BillShipmentSource = ID.Ship_To.SERVICE_ORDER_ADDRESS;
                    }
                }
            }
            else if (fsCustomerBillingSetupRow != null)
            {
                args.Add(fsCustomerBillingSetupRow.BillingCycleID);
                fsBillingCycleRow = (FSBillingCycle)billingCycleView.SelectSingle(args.ToArray());

                if (fsBillingCycleRow != null)
                {
                    if (SharedFunctions.IsNotAllowedBillingOptionsModification(fsBillingCycleRow))
                    {
                        fsCustomerBillingSetupRow.SendInvoicesTo = ID.Send_Invoices_To.BILLING_CUSTOMER_BILL_TO;
                        fsCustomerBillingSetupRow.BillShipmentSource = ID.Ship_To.SERVICE_ORDER_ADDRESS;
                    }
                }
            }
        }

        private void InsertUpdateCustomerBillingSetup(PXCache cache, Customer customerRow, FSxCustomer fsxCustomerRow)
        {
            FSSetup fsSetupRow = PXSelect<FSSetup>.Select(Base);

            if (fsSetupRow != null
                     && fsSetupRow.CustomerMultipleBillingOptions == false)
            {
                FSCustomerBillingSetup fsCustomerBillingSetupRow = CustomerBillingCycles.Select();

                if (fsxCustomerRow.BillingCycleID == null)
                {
                    CustomerBillingCycles.Delete(fsCustomerBillingSetupRow);
                    return;
                }

                if (fsCustomerBillingSetupRow == null)
                {
                    fsCustomerBillingSetupRow = CustomerBillingCycles.Insert(new FSCustomerBillingSetup());
                    fsCustomerBillingSetupRow.SrvOrdType = null;
                }

                fsCustomerBillingSetupRow.BillingCycleID = fsxCustomerRow.BillingCycleID;
                fsCustomerBillingSetupRow.SendInvoicesTo = fsxCustomerRow.SendInvoicesTo;
                fsCustomerBillingSetupRow.BillShipmentSource = fsxCustomerRow.BillShipmentSource;
                fsCustomerBillingSetupRow.FrequencyType = ID.Frequency_Type.NONE;

                CustomerBillingCycles.Update(fsCustomerBillingSetupRow);
            }
        }

        private bool IsCBIDRelatedToPostedDocuments(int? cBID)
        {
            int? rowCount = PXSelectJoinGroupBy<FSServiceOrder,
                            InnerJoin<FSPostDoc,
                                On<FSPostDoc.sOID, Equal<FSServiceOrder.sOID>>>,
                            Where<
                                FSServiceOrder.cBID, Equal<Required<FSServiceOrder.cBID>>>,
                            Aggregate<Count>>
                            .Select(Base, cBID).RowCount;

            return rowCount.HasValue && rowCount > 0;
        }

        private void SetBillingCustomerSetting(PXCache cache, Customer customerRow)
        {
            FSxCustomer fsxCustomerRow = cache.GetExtension<FSxCustomer>(customerRow);
            FSxCustomerClass fsxCustomerClassRow = Base.CustomerClass.Cache.GetExtension<FSxCustomerClass>(Base.CustomerClass.Current);

            fsxCustomerRow.DefaultBillingCustomerSource = fsxCustomerClassRow.DefaultBillingCustomerSource;
            fsxCustomerRow.BillCustomerID = fsxCustomerClassRow.BillCustomerID;
            fsxCustomerRow.BillLocationID = fsxCustomerClassRow.BillLocationID;
        }

        private void EnableDisableCustomerBilling(PXCache cache, Customer customerRow, FSxCustomer fsxCustomerRow)
        {
            bool isSpecificCustomer = fsxCustomerRow.DefaultBillingCustomerSource == ID.Default_Billing_Customer_Source.SPECIFIC_CUSTOMER;

            PXUIFieldAttribute.SetVisible<FSxCustomer.billCustomerID>(cache, customerRow, isSpecificCustomer);
            PXUIFieldAttribute.SetVisible<FSxCustomer.billLocationID>(cache, customerRow, isSpecificCustomer);
            PXDefaultAttribute.SetPersistingCheck<FSxCustomer.billCustomerID>(cache, customerRow, isSpecificCustomer == true ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
            PXDefaultAttribute.SetPersistingCheck<FSxCustomer.billLocationID>(cache, customerRow, isSpecificCustomer == true ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);

            if (isSpecificCustomer == false)
            {
                fsxCustomerRow.BillCustomerID = null;
                fsxCustomerRow.BillLocationID = null;
            }
        }

        private void VerifyPrepaidContractRelated(PXCache cache, Customer customerRow, FSxCustomer fsxCustomerRow)
        {
            if (fsxCustomerRow.BillingCycleID == (int?)cache.GetValueOriginal<FSxCustomer.billingCycleID>(fsxCustomerRow))
            {
                return;
            }

            if (Setup.Current != null
                    && Setup.Current.CustomerMultipleBillingOptions == false)
            {
                int count = PXSelect<FSServiceOrder,
                            Where<
                                FSServiceOrder.billContractPeriodID, IsNotNull,
                            And<
                                FSServiceOrder.customerID, Equal<Required<FSServiceOrder.customerID>>>>>
                            .Select(Base, customerRow.BAccountID).Count;

                if (count > 0)
                {
                    cache.RaiseExceptionHandling<FSxCustomer.billingCycleID>(customerRow, 
                        fsxCustomerRow.BillingCycleID, 
                        new PXSetPropertyException(TX.Error.NO_UPDATE_BILLING_CYCLE_SERVICE_CONTRACT_RELATED, PXErrorLevel.Error));
                    throw new PXException(TX.Error.NO_UPDATE_BILLING_CYCLE_SERVICE_CONTRACT_RELATED);
                }
            }
        }

        private void VerifyPrepaidContractRelated(PXCache cache, FSCustomerBillingSetup fsCustomerBillingSetupRow)
        {
            if (fsCustomerBillingSetupRow.BillingCycleID == (int?)cache.GetValueOriginal<FSCustomerBillingSetup.billingCycleID>(fsCustomerBillingSetupRow))
            {
                return;
            }

            if (Setup.Current != null
                    && Setup.Current.CustomerMultipleBillingOptions == true)
            {
                int count = PXSelectJoin<FSServiceOrder,
                            InnerJoin<FSSrvOrdType,
                                On<FSSrvOrdType.srvOrdType, Equal<FSServiceOrder.srvOrdType>>>,
                            Where<
                                FSServiceOrder.billContractPeriodID, IsNotNull,
                            And<
                                FSServiceOrder.customerID, Equal<Required<FSServiceOrder.customerID>>,
                            And<
                                FSServiceOrder.srvOrdType, Equal<Required<FSServiceOrder.srvOrdType>>>>>>
                            .Select(Base, fsCustomerBillingSetupRow.CustomerID, fsCustomerBillingSetupRow.SrvOrdType).Count;

                if (count > 0)
                {
                    cache.RaiseExceptionHandling<FSCustomerBillingSetup.srvOrdType>(fsCustomerBillingSetupRow, 
                        fsCustomerBillingSetupRow.SrvOrdType, 
                        new PXSetPropertyException(TX.Error.NO_UPDATE_BILLING_CYCLE_SERVICE_CONTRACT_RELATED, PXErrorLevel.Error));
                    throw new PXException(TX.Error.NO_UPDATE_BILLING_CYCLE_SERVICE_CONTRACT_RELATED);
                }
            }
        }


        protected virtual bool CheckDuplicatedEntry(PXCache cache, FSCustomerBillingSetup fsCustomerBillingSetupRow)
        {
            foreach (FSCustomerBillingSetup row in CustomerBillingCycles.Select())
            {
                if (row.SrvOrdType == fsCustomerBillingSetupRow.SrvOrdType
                        && row.CBID != fsCustomerBillingSetupRow.CBID)
                {
                    cache.RaiseExceptionHandling<FSCustomerBillingSetup.srvOrdType>(
                                    fsCustomerBillingSetupRow, fsCustomerBillingSetupRow.SrvOrdType,
                                    new PXSetPropertyException(ErrorMessages.DuplicateEntryAdded));

                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Event Handlers

        #region Customer Events

        protected virtual void Customer_CustomerClassID_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
        {
            Customer customerRow = (Customer)e.Row;
            CustomerClass customerClassRow = 
                (CustomerClass)PXSelectorAttribute.Select<Customer.customerClassID>(cache, customerRow, e.NewValue);

            this.doCopyBillingSettings = false;
            if (customerClassRow != null)
            {
                this.doCopyBillingSettings = true;
                if (cache.GetStatus(customerRow) != PXEntryStatus.Inserted)
                {
                    if (Base.CurrentCustomer.Ask(TX.WebDialogTitles.UPDATE_BILLING_SETTINGS, TX.Warning.CUSTOMER_CLASS_BILLING_SETTINGS, MessageButtons.YesNo) == WebDialogResult.No)
                    {
                        this.doCopyBillingSettings = false;
                    }
                }
            }
        }

        protected void Customer_CustomerClassID_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            Customer customerRow = (Customer)e.Row;

            if (this.doCopyBillingSettings == true)
            {
                FSxCustomer fsxCustomerRow = cache.GetExtension<FSxCustomer>(customerRow);
                SetBillingCycleFromCustomerClass(cache, customerRow);
                InsertUpdateCustomerBillingSetup(cache, customerRow, fsxCustomerRow);
            }

            SetBillingCustomerSetting(cache, customerRow);
        }

        protected void Customer_RowInserted(PXCache cache, PXRowInsertedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            Customer customerRow = (Customer)e.Row;

            if (this.doCopyBillingSettings == false)
            {
                SetBillingCycleFromCustomerClass(cache, customerRow);
            }
        }

        protected void Customer_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            Customer customerRow = (Customer)e.Row;
            FSxCustomer fsxCustomerRow = cache.GetExtension<FSxCustomer>(customerRow);
            PXUIFieldAttribute.SetEnabled<FSxCustomer.sendInvoicesTo>(cache, customerRow, fsxCustomerRow.BillingCycleID != null);
            PXUIFieldAttribute.SetEnabled<FSxCustomer.billShipmentSource>(cache, customerRow, fsxCustomerRow.BillingCycleID != null);

            DisplayCustomerBillingOptions(cache, customerRow, fsxCustomerRow);

            viewServiceOrderHistory.SetEnabled(customerRow.BAccountID > 0);
            viewAppointmentHistory.SetEnabled(customerRow.BAccountID > 0);
            viewEquipmentSummary.SetEnabled(customerRow.BAccountID > 0);
            viewContractScheduleSummary.SetEnabled(customerRow.BAccountID > 0);

            openMultipleStaffMemberBoard.SetEnabled(customerRow.BAccountID > 0);
            openSingleStaffMemberBoard.SetEnabled(customerRow.BAccountID > 0);

            EnableDisableCustomerBilling(cache, customerRow, fsxCustomerRow);
        }
        
        protected void Customer_RowPersisted(PXCache cache, PXRowPersistedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            Customer customerRow = (Customer)e.Row;
            FSxCustomer fsxCustomerRow = cache.GetExtension<FSxCustomer>(customerRow);

            if (e.Operation == PXDBOperation.Update
                    && e.TranStatus == PXTranStatus.Open
                        && fsxCustomerRow.BillingOptionsChanged == true)
            {
                SharedFunctions.PreUpdateBillingInfoDocs(Base, customerRow.BAccountID, null);
            }

            if (e.TranStatus == PXTranStatus.Completed && fsxCustomerRow.BillingOptionsChanged == true)
            {
                fsxCustomerRow.BillingOptionsChanged = false;
                SharedFunctions.UpdateBillingInfoInDocsLO(Base, customerRow.BAccountID, null);
            }
        }

        protected void Customer_RowPersisting(PXCache cache, PXRowPersistingEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            Customer customerRow = (Customer)e.Row;
            FSxCustomer fsxCustomerRow = cache.GetExtension<FSxCustomer>(customerRow);

            if (e.Operation == PXDBOperation.Update)
            {
                VerifyPrepaidContractRelated(cache, customerRow, fsxCustomerRow);
            }
            else if (e.Operation == PXDBOperation.Insert)
            {
                if (this.doCopyBillingSettings == false)
                {
                    InsertUpdateCustomerBillingSetup(cache, customerRow, fsxCustomerRow);
                }
            }
        }

        protected void Customer_BillingCycleID_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            Customer customerRow = (Customer)e.Row;
            FSxCustomer fsxCustomerRow = cache.GetExtension<FSxCustomer>(customerRow);

            if (fsxCustomerRow.BillingCycleID == null)
            {
                fsxCustomerRow.SendInvoicesTo = ID.Send_Invoices_To.DEFAULT_BILLING_CUSTOMER_LOCATION;
            }

            ResetSendInvoicesToFromBillingCycle(customerRow, null);
            InsertUpdateCustomerBillingSetup(cache, customerRow, fsxCustomerRow);
        }

        protected void Customer_SendInvoicesTo_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            Customer customerRow = (Customer)e.Row;
            FSxCustomer fsxCustomerRow = cache.GetExtension<FSxCustomer>(customerRow);
            InsertUpdateCustomerBillingSetup(cache, customerRow, fsxCustomerRow);
        }

        protected void Customer_BillShipmentSource_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            Customer customerRow = (Customer)e.Row;
            FSxCustomer fsxCustomerRow = cache.GetExtension<FSxCustomer>(customerRow);
            InsertUpdateCustomerBillingSetup(cache, customerRow, fsxCustomerRow);
        }
        #endregion

        #region FSCustomerSetupBillingEvents
        protected void FSCustomerBillingSetup_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSCustomerBillingSetup fsCustomerBillingSetupRow = (FSCustomerBillingSetup)e.Row;

            bool lineValid = IsThisLineValid(fsCustomerBillingSetupRow);

            PXUIFieldAttribute.SetEnabled<FSCustomerBillingSetup.billingCycleID>(cache, fsCustomerBillingSetupRow, lineValid);
            PXUIFieldAttribute.SetEnabled<FSCustomerBillingSetup.sendInvoicesTo>(cache, fsCustomerBillingSetupRow, lineValid);
            PXUIFieldAttribute.SetEnabled<FSCustomerBillingSetup.billShipmentSource>(cache, fsCustomerBillingSetupRow, lineValid);
            PXUIFieldAttribute.SetEnabled<FSCustomerBillingSetup.frequencyType>(cache, fsCustomerBillingSetupRow, lineValid);

            bool enableFieldsBySrvOrdType = string.IsNullOrEmpty(fsCustomerBillingSetupRow.SrvOrdType) == false;
            bool enableSrvOrdType = !enableFieldsBySrvOrdType || cache.GetStatus(fsCustomerBillingSetupRow) == PXEntryStatus.Inserted;

            PXUIFieldAttribute.SetEnabled<FSCustomerBillingSetup.srvOrdType>(cache, fsCustomerBillingSetupRow, enableSrvOrdType);
            PXUIFieldAttribute.SetEnabled<FSCustomerBillingSetup.billingCycleID>(cache, fsCustomerBillingSetupRow, enableFieldsBySrvOrdType);

            //Disables the TimeCycleType field if the type of the BillingCycleID selected is Time Cycle.
            if (fsCustomerBillingSetupRow.BillingCycleID != null)
            {
                FSBillingCycle fsBillingCycleRow =
                                                    PXSelect<FSBillingCycle,
                                                    Where<
                                                        FSBillingCycle.billingCycleID, Equal<Required<FSBillingCycle.billingCycleID>>>>
                                                    .Select(Base, fsCustomerBillingSetupRow.BillingCycleID);

                PXUIFieldAttribute.SetEnabled<FSCustomerBillingSetup.frequencyType>(cache, fsCustomerBillingSetupRow, fsBillingCycleRow.BillingCycleType != ID.Billing_Cycle_Type.TIME_FRAME);

                bool forbidUpdateBillingOptions = SharedFunctions.IsNotAllowedBillingOptionsModification(fsBillingCycleRow);

                PXUIFieldAttribute.SetEnabled<FSCustomerBillingSetup.sendInvoicesTo>(cache, fsCustomerBillingSetupRow, forbidUpdateBillingOptions == false);
                PXUIFieldAttribute.SetEnabled<FSCustomerBillingSetup.billShipmentSource>(cache, fsCustomerBillingSetupRow, forbidUpdateBillingOptions == false);
            }
            else 
            {
                PXUIFieldAttribute.SetEnabled<FSCustomerBillingSetup.frequencyType>(cache, fsCustomerBillingSetupRow, false);
            }
        }

        protected void FSCustomerBillingSetup_FrequencyType_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSCustomerBillingSetup fsCustomerBillingSetupRow = (FSCustomerBillingSetup)e.Row;
            ResetTimeCycleOptions(fsCustomerBillingSetupRow);
        }

        protected void FSCustomerBillingSetup_BillingCycleID_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSCustomerBillingSetup fsCustomerBillingSetupRow = (FSCustomerBillingSetup)e.Row;
            ResetSendInvoicesToFromBillingCycle(null, fsCustomerBillingSetupRow);
        }

        protected virtual void FSCustomerBillingSetup_RowDeleting(PXCache cache, PXRowDeletingEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSCustomerBillingSetup fscustomerBillingSetupRow = (FSCustomerBillingSetup)e.Row;

            bool setDocumentToInactive = IsCBIDRelatedToPostedDocuments(fscustomerBillingSetupRow.CBID);
            if (setDocumentToInactive)
            {
                fscustomerBillingSetupRow.Active = false;
                cache.SetStatus(fscustomerBillingSetupRow, PXEntryStatus.Updated);
            }
        }

        protected virtual void FSCustomerBillingSetup_RowDeleted(PXCache cache, PXRowDeletedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSxCustomer fsxCustomerRow = PXCache<Customer>.GetExtension<FSxCustomer>(Base.BAccount.Current);
            fsxCustomerRow.BillingOptionsChanged = true;
        }

        protected virtual void FSCustomerBillingSetup_RowInserted(PXCache cache, PXRowInsertedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSxCustomer fsxCustomerRow = PXCache<Customer>.GetExtension<FSxCustomer>(Base.BAccount.Current);
            fsxCustomerRow.BillingOptionsChanged = true;
        }

        protected virtual void FSCustomerBillingSetup_RowInserting(PXCache cache, PXRowInsertingEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSCustomerBillingSetup fsCustomerBillingSetupRow = (FSCustomerBillingSetup)e.Row;

            CheckDuplicatedEntry(cache, fsCustomerBillingSetupRow);
        }


        protected virtual void FSCustomerBillingSetup_RowUpdating(PXCache cache, PXRowUpdatingEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSCustomerBillingSetup fsCustomerBillingSetupRow = (FSCustomerBillingSetup)e.NewRow;

            e.Cancel = CheckDuplicatedEntry(cache, fsCustomerBillingSetupRow);
        }

        protected virtual void FSCustomerBillingSetup_RowPersisting(PXCache cache, PXRowPersistingEventArgs e)
        {
            if (e.Row == null || Base.BAccount.Current == null)
            {
                return;
            }

            FSCustomerBillingSetup fsCustomerBillingSetupRow = (FSCustomerBillingSetup)e.Row;

            bool duplicatedEntry = CheckDuplicatedEntry(cache, fsCustomerBillingSetupRow);

            if (duplicatedEntry == true)
            {
                e.Cancel = duplicatedEntry;
                return;
            }

            FSCustomerBillingSetup fsCustomerBillingSetup = (FSCustomerBillingSetup)e.Row;
            FSxCustomer fsxCustomerRow = PXCache<Customer>.GetExtension<FSxCustomer>(Base.BAccount.Current);

            if (cache.GetStatus(fsCustomerBillingSetup) == PXEntryStatus.Updated)
            {
                bool insertNewRow = IsCBIDRelatedToPostedDocuments(fsCustomerBillingSetup.CBID);

                if (insertNewRow)
                {
                    fsxCustomerRow.BillingOptionsChanged = true;

                    cache.SetValue<FSCustomerBillingSetup.active>(fsCustomerBillingSetup, false);

                    FSCustomerBillingSetup newCustomerBilingSetupRow = new FSCustomerBillingSetup();
                    var tempFSCustomerBillingSetupCache = new PXCache<FSCustomerBillingSetup>(Base);
                    newCustomerBilingSetupRow.CustomerID = fsCustomerBillingSetup.CustomerID;
                    newCustomerBilingSetupRow.SrvOrdType = fsCustomerBillingSetup.SrvOrdType;
                    newCustomerBilingSetupRow.Active = true;
                    newCustomerBilingSetupRow = (FSCustomerBillingSetup)tempFSCustomerBillingSetupCache.Insert(newCustomerBilingSetupRow);

                    foreach (var field in cache.Fields)
                    {
                        if (!cache.Keys.Contains(field)
                            && field.ToLower() != typeof(FSCustomerBillingSetup.active).Name.ToLower())
                        {
                            tempFSCustomerBillingSetupCache.SetValue(
                                    newCustomerBilingSetupRow,
                                    field.ToString(),
                                    cache.GetValue(
                                        fsCustomerBillingSetup, field.ToString()));
                        }
                    }

                    tempFSCustomerBillingSetupCache.Persist(PXDBOperation.Insert);

                    foreach (var field in cache.Fields)
                    {
                        if (!cache.Keys.Contains(field)
                            && field.ToLower() != typeof(FSCustomerBillingSetup.active).Name.ToLower())
                        {
                            cache.SetValue(
                                fsCustomerBillingSetup,
                                field.ToString(),
                                cache.GetValueOriginal(
                                        fsCustomerBillingSetup,
                                        field.ToString()));
                        }
                    }

                    cache.SetValue<FSCustomerBillingSetup.active>(fsCustomerBillingSetup, false);
                }
            }

            if (e.Operation == PXDBOperation.Update)
            {
                VerifyPrepaidContractRelated(cache, fsCustomerBillingSetup);
            }
        }

        protected virtual void FSCustomerBillingSetup_RowUpdated(PXCache cache, PXRowUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            var newCustomerBillingSetup = (FSCustomerBillingSetup)e.Row;
            var oldCustomerBillingSetup = (FSCustomerBillingSetup)e.OldRow;

            if (newCustomerBillingSetup.SrvOrdType != oldCustomerBillingSetup.SrvOrdType
                || newCustomerBillingSetup.BillingCycleID != oldCustomerBillingSetup.BillingCycleID
                || newCustomerBillingSetup.FrequencyType != oldCustomerBillingSetup.FrequencyType
                || newCustomerBillingSetup.Active != oldCustomerBillingSetup.Active)
            {
                FSxCustomer fsxCustomerRow = PXCache<Customer>.GetExtension<FSxCustomer>(Base.BAccount.Current);
                fsxCustomerRow.BillingOptionsChanged = true;
            }
        }
        #endregion

        #endregion
    }
}
