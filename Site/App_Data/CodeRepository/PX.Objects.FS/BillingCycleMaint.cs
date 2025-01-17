using PX.Data;
using PX.Objects.AR;
using System;
using System.Linq;
using System.Collections.Generic;

namespace PX.Objects.FS
{
    public class BillingCycleMaint : PXGraph<BillingCycleMaint, FSBillingCycle>
    {
        protected class BillingCycleIDAndDate
        {
            public int? BillingCycleID;
            public DateTime? DocDate;
        }

        public PXSelect<FSBillingCycle> BillingCycleRecords;

        [PXHidden]
        public PXSetup<FSSetup> Setup;

        #region Private Methods
        /// <summary>
        /// Show/Hide fields and make them Required/Not Required depending on the Billing Cycle Type selected.
        /// </summary>
        /// <param name="cache">BillingCycleRecords cache.</param>
        /// <param name="fsBillingCycleRow">FSBillingCycle row.</param>
        private void BillingCycleTypeFieldsSetup(PXCache cache, FSBillingCycle fsBillingCycleRow)
        {
            switch (fsBillingCycleRow.BillingCycleType)
            {
                case ID.Billing_Cycle_Type.APPOINTMENT:
                case ID.Billing_Cycle_Type.SERVICE_ORDER:
                    PXUIFieldAttribute.SetVisible<FSBillingCycle.timeCycleType>(cache, fsBillingCycleRow, false);
                    PXUIFieldAttribute.SetVisible<FSBillingCycle.timeCycleWeekDay>(cache, fsBillingCycleRow, false);
                    PXUIFieldAttribute.SetVisible<FSBillingCycle.timeCycleDayOfMonth>(cache, fsBillingCycleRow, false);
                    PXUIFieldAttribute.SetEnabled<FSBillingCycle.groupBillByLocations>(cache, fsBillingCycleRow, false);
                    PXDefaultAttribute.SetPersistingCheck<FSBillingCycle.timeCycleDayOfMonth>(cache, fsBillingCycleRow, PXPersistingCheck.Nothing);
                    break;
                case ID.Billing_Cycle_Type.PURCHASE_ORDER:
                case ID.Billing_Cycle_Type.WORK_ORDER:
                    PXUIFieldAttribute.SetVisible<FSBillingCycle.timeCycleType>(cache, fsBillingCycleRow, false);
                    PXUIFieldAttribute.SetVisible<FSBillingCycle.timeCycleWeekDay>(cache, fsBillingCycleRow, false);
                    PXUIFieldAttribute.SetVisible<FSBillingCycle.timeCycleDayOfMonth>(cache, fsBillingCycleRow, false);
                    PXUIFieldAttribute.SetEnabled<FSBillingCycle.groupBillByLocations>(cache, fsBillingCycleRow, true);
                    PXDefaultAttribute.SetPersistingCheck<FSBillingCycle.timeCycleDayOfMonth>(cache, fsBillingCycleRow, PXPersistingCheck.Nothing);
                    break;
                case ID.Billing_Cycle_Type.TIME_FRAME:
                    PXUIFieldAttribute.SetVisible<FSBillingCycle.timeCycleType>(cache, fsBillingCycleRow, true);
                    PXUIFieldAttribute.SetEnabled<FSBillingCycle.groupBillByLocations>(cache, fsBillingCycleRow, true);

                    switch (fsBillingCycleRow.TimeCycleType)
                    {
                        case ID.Time_Cycle_Type.WEEKDAY:
                            PXUIFieldAttribute.SetVisible<FSBillingCycle.timeCycleDayOfMonth>(cache, fsBillingCycleRow, false);
                            PXUIFieldAttribute.SetVisible<FSBillingCycle.timeCycleWeekDay>(cache, fsBillingCycleRow, true);
                            PXDefaultAttribute.SetPersistingCheck<FSBillingCycle.timeCycleWeekDay>(cache, fsBillingCycleRow, PXPersistingCheck.NullOrBlank);
                            PXDefaultAttribute.SetPersistingCheck<FSBillingCycle.timeCycleDayOfMonth>(cache, fsBillingCycleRow, PXPersistingCheck.Nothing);
                            break;
                        case ID.Time_Cycle_Type.DAY_OF_MONTH:
                            PXUIFieldAttribute.SetVisible<FSBillingCycle.timeCycleWeekDay>(cache, fsBillingCycleRow, false);
                            PXUIFieldAttribute.SetVisible<FSBillingCycle.timeCycleDayOfMonth>(cache, fsBillingCycleRow, true);
                            PXDefaultAttribute.SetPersistingCheck<FSBillingCycle.timeCycleDayOfMonth>(cache, fsBillingCycleRow, PXPersistingCheck.NullOrBlank);
                            PXDefaultAttribute.SetPersistingCheck<FSBillingCycle.timeCycleWeekDay>(cache, fsBillingCycleRow, PXPersistingCheck.Nothing);
                            break;
                        default:
                            break;
                    }

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Resets the values of the Time Cycle options depending on the Billing and Time Cycle Types.
        /// </summary>
        /// <param name="fsBillingCycleRow">FSBillingCycle row.</param>
        private void ResetTimeCycleOptions(FSBillingCycle fsBillingCycleRow)
        {
            if (fsBillingCycleRow.BillingCycleType != ID.Billing_Cycle_Type.TIME_FRAME)
            {
                fsBillingCycleRow.TimeCycleWeekDay = null;
                fsBillingCycleRow.TimeCycleDayOfMonth = null;
            }
            else
            {
                switch (fsBillingCycleRow.TimeCycleType)
                {
                    case ID.Time_Cycle_Type.DAY_OF_MONTH:
                        fsBillingCycleRow.TimeCycleWeekDay = null;
                        break;
                    case ID.Time_Cycle_Type.WEEKDAY:
                        fsBillingCycleRow.TimeCycleDayOfMonth = null;
                        break;
                    default:
                        break;
                }
            }
        }

        private void VerifyPrepaidContractRelated(PXCache cache, FSBillingCycle fsBillingCycleRow)
        {
            if (fsBillingCycleRow.BillingBy == (string)cache.GetValueOriginal<FSBillingCycle.billingBy>(fsBillingCycleRow))
            {
                return;
            }

            if (Setup.Current != null)
            {
                int count = 0;

                if (Setup.Current.CustomerMultipleBillingOptions == false)
                {
                    count = PXSelectJoin<Customer, 
                            InnerJoin<FSBillingCycle, 
                                On<FSBillingCycle.billingCycleID, Equal<FSxCustomer.billingCycleID>>,
                            InnerJoin<FSServiceOrder,
                                On<FSServiceOrder.customerID, Equal<Customer.bAccountID>>>>,
                            Where<
                                FSServiceOrder.billContractPeriodID, IsNotNull,
                            And<
                                FSBillingCycle.billingCycleID, Equal<Required<FSBillingCycle.billingCycleID>>>>>
                            .Select(this, fsBillingCycleRow.BillingCycleID).Count;
                }
                else
                {
                    count = PXSelectJoin<FSCustomerBillingSetup,
                            InnerJoin<FSSrvOrdType,
                                On<FSSrvOrdType.srvOrdType, Equal<FSCustomerBillingSetup.srvOrdType>>,
                            InnerJoin<FSServiceOrder,
                                On<FSServiceOrder.srvOrdType, Equal<FSSrvOrdType.srvOrdType>,
                                And<FSServiceOrder.cBID, Equal<FSCustomerBillingSetup.cBID>>>>>,
                            Where<
                                FSServiceOrder.billContractPeriodID, IsNotNull,
                            And<
                                FSCustomerBillingSetup.billingCycleID, Equal<Required<FSCustomerBillingSetup.billingCycleID>>>>>
                            .Select(this, fsBillingCycleRow.BillingCycleID).Count;
                }

                if (count > 0)
                {
                    cache.RaiseExceptionHandling<FSBillingCycle.billingCycleCD>(fsBillingCycleRow, 
                        fsBillingCycleRow.BillingCycleCD, 
                        new PXSetPropertyException(TX.Error.NO_UPDATE_BILLING_CYCLE_SERVICE_CONTRACT_RELATED, PXErrorLevel.Error));
                    throw new PXException(TX.Error.NO_UPDATE_BILLING_CYCLE_SERVICE_CONTRACT_RELATED);
                }
            }
        }
        #endregion

        #region Events

        protected void FSBillingCycle_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSBillingCycle fsBillingCycleRow = (FSBillingCycle)e.Row;
            BillingCycleTypeFieldsSetup(cache, fsBillingCycleRow);
        }

        protected void FSBillingCycle_BillingCycleType_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSBillingCycle fsBillingCycleRow = (FSBillingCycle)e.Row;

            if (fsBillingCycleRow.BillingCycleType == ID.Billing_Cycle_Type.APPOINTMENT
                    || fsBillingCycleRow.BillingCycleType == ID.Billing_Cycle_Type.SERVICE_ORDER)
            {
                fsBillingCycleRow.GroupBillByLocations = false;
            }

            ResetTimeCycleOptions(fsBillingCycleRow);
        }

        protected void FSBillingCycle_TimeCycleType_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSBillingCycle fsBillingCycleRow = (FSBillingCycle)e.Row;
            ResetTimeCycleOptions(fsBillingCycleRow);
        }

        protected void FSBillingCycle_RowPersisting(PXCache cache, PXRowPersistingEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSBillingCycle fsBillingCycleRow = (FSBillingCycle)e.Row;

            if (fsBillingCycleRow.BillingBy == ID.Billing_By.SERVICE_ORDER && fsBillingCycleRow.BillingCycleType == ID.Billing_Cycle_Type.APPOINTMENT)
            {
                throw new PXException(TX.Error.CANT_DEFINE_BILLING_CYCLE_BILLED_BY_SERVICE_ORDER_AND_GROUPED_BY_APPOINTMENT);
            }

            if (e.Operation == PXDBOperation.Delete)
            {
                int billingCycleCount = (int)PXSelectGroupBy<FSCustomer,
                                            Where<
                                                FSxCustomer.billingCycleID, Equal<Required<FSxCustomer.billingCycleID>>,
                                                And<FSCustomerBillingSetup.active, Equal<True>>>,
                                            Aggregate<Count>>
                                            .Select(this, fsBillingCycleRow.BillingCycleID).RowCount;

                if (billingCycleCount > 0)
                {
                    throw new PXException(TX.Error.BILLING_CYCLE_ERROR_DELETING_CUSTOMER_USING_IT, fsBillingCycleRow);
                }
                else
                {
                    billingCycleCount = (int)PXSelectGroupBy<FSCustomerBillingSetup,
                                        Where<
                                            FSCustomerBillingSetup.billingCycleID, Equal<Required<FSCustomerBillingSetup.billingCycleID>>>,
                                        Aggregate<Count>>
                                        .Select(this, fsBillingCycleRow.BillingCycleID).RowCount;

                    if (billingCycleCount > 0)
                    {
                        throw new PXException(TX.Error.BILLING_CYCLE_ERROR_DELETING_CUSTOMER_USING_IT, fsBillingCycleRow);
                    }
                }
            }

            if (e.Operation == PXDBOperation.Update)
            {
                VerifyPrepaidContractRelated(cache, fsBillingCycleRow);
            }
        }

        protected void FSBillingCycle_RowPersisted(PXCache cache, PXRowPersistedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSBillingCycle fsBillingCycleRow = (FSBillingCycle)e.Row;

            if (e.Operation == PXDBOperation.Update
                    && e.TranStatus == PXTranStatus.Open
                        && fsBillingCycleRow.UpdateCutOffDate == true)
            {
                SharedFunctions.PreUpdateBillingInfoDocs(this, null, fsBillingCycleRow.BillingCycleID);
            }

            if (e.TranStatus == PXTranStatus.Completed)
            {
                if (fsBillingCycleRow.UpdateCutOffDate == true)
                {
                    fsBillingCycleRow.UpdateCutOffDate = false;
                    SharedFunctions.UpdateBillingInfoInDocsLO(this, null, fsBillingCycleRow.BillingCycleID);
                }
            }
        }

        protected void FSBillingCycle_RowUpdated(PXCache cache, PXRowUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            var newBillingCycleRow = (FSBillingCycle)e.Row;
            var oldBillingCycleRow = (FSBillingCycle)e.OldRow;

            if (newBillingCycleRow.BillingCycleType != oldBillingCycleRow.BillingCycleType
                || newBillingCycleRow.TimeCycleType != oldBillingCycleRow.TimeCycleType
                || newBillingCycleRow.TimeCycleDayOfMonth != oldBillingCycleRow.TimeCycleDayOfMonth
                || newBillingCycleRow.TimeCycleWeekDay != oldBillingCycleRow.TimeCycleWeekDay)
            {
                newBillingCycleRow.UpdateCutOffDate = true;
            }
        }
        #endregion

        #region Public Methods
        public virtual void UpdateBillingInfoInDocs(int? currentCustomerID, int? currentBillingCycleID)
        {
            var tempGraph = new PXGraph();
            var cutOffDateCache = new Dictionary<BillingCycleIDAndDate, DateTime?>();

            var resultSet = PXSelectJoin<FSServiceOrder,
                                        CrossJoinSingleTable<FSSetup,
                                        LeftJoin<FSCustomerBillingSetup,
                                            On<
                                                FSCustomerBillingSetup.customerID, Equal<FSServiceOrder.billCustomerID>,
                                                And<
                                                    Where2<
                                                        Where<FSSetup.customerMultipleBillingOptions, Equal<False>,
                                                            And<FSCustomerBillingSetup.srvOrdType, IsNull,
                                                            And<FSCustomerBillingSetup.active, Equal<True>>>>,
                                                        Or<
                                                        Where<FSSetup.customerMultipleBillingOptions, Equal<True>,
                                                            And<FSCustomerBillingSetup.srvOrdType, Equal<FSServiceOrder.srvOrdType>,
                                                            And<FSCustomerBillingSetup.active, Equal<True>>>>>>>>>>,
                                        Where2<
                                            Where<FSServiceOrder.postedBy, IsNull>,
                                            And<
                                                Where2<
                                                    Where<Required<FSServiceOrder.billCustomerID>, IsNotNull,
                                                        And<FSServiceOrder.billCustomerID, Equal<Required<FSServiceOrder.billCustomerID>>>>,
                                                    Or<
                                                        Where2<
                                                            Where<Required<FSServiceOrder.billCustomerID>, IsNull>,
                                                        And<
                                                            Where2<
                                                                Where2<
                                                                    Where<FSServiceOrder.cBID, IsNull,
                                                                        And<FSCustomerBillingSetup.cBID, IsNotNull>>,
                                                                    Or<
                                                                        Where<FSServiceOrder.cBID, IsNotNull,
                                                                            And<FSCustomerBillingSetup.cBID, IsNull>>>>,
                                                                Or<
                                                                    Where2<
                                                                        Where<
                                                                            FSServiceOrder.cBID, NotEqual<FSCustomerBillingSetup.cBID>,
                                                                            Or<
                                                                                Where<
                                                                                    FSCustomerBillingSetup.cBID, IsNotNull,
                                                                                    And<
                                                                                        FSServiceOrder.cutOffDate, IsNull>>>>,
                                                                        Or<
                                                                            Where<
                                                                                FSCustomerBillingSetup.billingCycleID, Equal<Required<FSCustomerBillingSetup.billingCycleID>>>>>>>>>>>>>>
                                        .Select(tempGraph, currentCustomerID, currentCustomerID, currentCustomerID, currentBillingCycleID);

            foreach (PXResult<FSServiceOrder, FSSetup, FSCustomerBillingSetup> result in resultSet)
            {
                FSServiceOrder fsServiceOrderRow = (FSServiceOrder)result;
                FSCustomerBillingSetup fsCustomerBillingSetupRow = (FSCustomerBillingSetup)result;

                UpdateBillingInfoInDoc(tempGraph, fsServiceOrderRow, fsCustomerBillingSetupRow.CBID, fsCustomerBillingSetupRow.BillingCycleID, currentBillingCycleID != null, currentCustomerID != null, cutOffDateCache);
            }
        }

        /// <summary>
        /// Keep synchronized the Customer's Billing Cycle settings when Multiple Billing Cycle option is turn on from Setup screen.
        /// </summary>
        public virtual void KeepCustomerMultBillCyleSettings(PXGraph setupGraph)
        {
            PXResultset<FSServiceOrder> customersAffectedRows = PXSelectJoinGroupBy<FSServiceOrder,
                                                                    InnerJoin<Customer, 
                                                                        On<Customer.bAccountID, Equal<FSServiceOrder.customerID>>,
                                                                    InnerJoin<FSSrvOrdType,
                                                                        On<FSSrvOrdType.srvOrdType, Equal<FSServiceOrder.srvOrdType>>,
                                                                    LeftJoin<FSCustomerBillingSetup, 
                                                                        On<FSCustomerBillingSetup.customerID, Equal<Customer.bAccountID>,
                                                                            And<FSCustomerBillingSetup.srvOrdType, Equal<FSServiceOrder.srvOrdType>,
                                                                                And<FSCustomerBillingSetup.billingCycleID, Equal<FSxCustomer.billingCycleID>>>>>>>,
                                                                    Where<FSCustomerBillingSetup.billingCycleID, IsNull,
                                                                        And<FSxCustomer.billingCycleID, IsNotNull,
                                                                        And<FSSrvOrdType.active, Equal<True>,
                                                                        And<FSSrvOrdType.behavior, NotEqual<FSSrvOrdType.behavior.Quote>>>>>,
                                                                    Aggregate<GroupBy<FSServiceOrder.customerID,
                                                                                GroupBy<FSServiceOrder.srvOrdType>>>,
                                                                    OrderBy<Asc<FSServiceOrder.customerID>>>.Select(this);

            var graphCustomer = PXGraph.CreateInstance<CustomerMaint>();
            int? lastCustomerID = null;

            foreach (FSServiceOrder currentCustomer in customersAffectedRows)
            {
                if (currentCustomer.CustomerID != lastCustomerID)
                {
                    graphCustomer.BAccount.Current = graphCustomer.BAccount.Search<Customer.bAccountID>(currentCustomer.CustomerID);
                    lastCustomerID = currentCustomer.CustomerID;
                }

                FSxCustomer fsxCustomerRow = graphCustomer.BAccount.Cache.GetExtension<FSxCustomer>(graphCustomer.BAccount.Current);

                if (fsxCustomerRow != null)
                {
                    SM_CustomerMaint sm_CustomerMaintGraph = graphCustomer.GetExtension<SM_CustomerMaint>();

                    FSCustomerBillingSetup fsCustomerBillingSetupRow = new FSCustomerBillingSetup();
                    fsCustomerBillingSetupRow.SrvOrdType = currentCustomer.SrvOrdType;
                    fsCustomerBillingSetupRow.BillingCycleID = fsxCustomerRow.BillingCycleID;
                    fsCustomerBillingSetupRow.SendInvoicesTo = fsxCustomerRow.SendInvoicesTo;
                    fsCustomerBillingSetupRow.BillShipmentSource = fsxCustomerRow.BillShipmentSource;

                    sm_CustomerMaintGraph.CustomerBillingCycles.Insert(fsCustomerBillingSetupRow);
                    graphCustomer.Save.Press();
                }
            }
        }
        #endregion

        #region Protected Methods
        protected virtual void UpdateBillingInfoInDoc(PXGraph tempGraph, FSServiceOrder fsServiceOrderRow, int? newCBID, int? newBillingCycleID, bool updateCutOffDate, bool forceUpdate, Dictionary<BillingCycleIDAndDate, DateTime?> cutOffDateCache)
        {
            if (fsServiceOrderRow.CBID == newCBID && updateCutOffDate == false && fsServiceOrderRow.CutOffDate != null && forceUpdate == false)
            {
                return;
            }

            if (fsServiceOrderRow.PostedBy == null)
            {
                using (PXTransactionScope ts = new PXTransactionScope())
                {
                    DateTime? newCutOffDate = null;
                    BillingCycleIDAndDate billingCycleIDAndDate = null;

                    if (newBillingCycleID != null)
                    {
                        billingCycleIDAndDate = new BillingCycleIDAndDate();
                        billingCycleIDAndDate.BillingCycleID = newBillingCycleID;
                        billingCycleIDAndDate.DocDate = fsServiceOrderRow.OrderDate;

                        if (cutOffDateCache.TryGetValue(billingCycleIDAndDate, out newCutOffDate) == false)
                        {
                            newCutOffDate = ServiceOrderCore.GetCutOffDate(tempGraph, fsServiceOrderRow.CBID, fsServiceOrderRow.OrderDate);
                            cutOffDateCache.Add(billingCycleIDAndDate, newCutOffDate);
                        }
                    }

                    if (fsServiceOrderRow.CBID != newCBID || fsServiceOrderRow.CutOffDate != newCutOffDate)
                    {
                        PXUpdate<
                            Set<FSServiceOrder.cBID, Required<FSServiceOrder.cBID>,
                            Set<FSServiceOrder.cutOffDate, Required<FSServiceOrder.cutOffDate>>>,
                        FSServiceOrder,
                        Where<
                            FSServiceOrder.sOID, Equal<Required<FSServiceOrder.sOID>>>>
                        .Update(tempGraph, newCBID, newCutOffDate, fsServiceOrderRow.SOID);
                    }

                    var appointmentSet = PXSelect<FSAppointment,
                                            Where<
                                                FSAppointment.sOID, Equal<Required<FSAppointment.sOID>>>>
                                                .Select(tempGraph, fsServiceOrderRow.SOID);
                    foreach (FSAppointment fsAppointmentRow in appointmentSet)
                    {
                        newCutOffDate = null;

                        if (newBillingCycleID != null)
                        {
                            billingCycleIDAndDate.DocDate = fsAppointmentRow.ExecutionDate;

                            if (cutOffDateCache.TryGetValue(billingCycleIDAndDate, out newCutOffDate) == false)
                            {
                                newCutOffDate = ServiceOrderCore.GetCutOffDate(tempGraph, fsServiceOrderRow.CBID, fsAppointmentRow.ExecutionDate);
                                cutOffDateCache.Add(billingCycleIDAndDate, newCutOffDate);
                            }
                        }

                        if (fsAppointmentRow.CutOffDate != newCutOffDate)
                        {
                            PXUpdate<
                                Set<FSAppointment.cutOffDate, Required<FSAppointment.cutOffDate>>,
                            FSAppointment,
                            Where<
                                FSAppointment.appointmentID, Equal<Required<FSAppointment.appointmentID>>>>
                            .Update(tempGraph, newCutOffDate, fsAppointmentRow.AppointmentID);
                        }
                    }

                    ts.Complete();
                }
            }
        }
        #endregion
    }
}
