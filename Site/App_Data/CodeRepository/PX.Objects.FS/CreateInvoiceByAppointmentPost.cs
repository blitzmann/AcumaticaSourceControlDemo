using PX.Objects.FS.ParallelProcessing;
using PX.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using PX.Objects.IN;

namespace PX.Objects.FS
{
    public class CreateInvoiceByAppointmentPost : CreateInvoiceBase<CreateInvoiceByAppointmentPost, AppointmentToPost>
    {
        #region Selects
        [PXFilterable]
        public new PXFilteredProcessing<AppointmentToPost, CreateInvoiceFilter,
               Where2<
                   Where<
                       Current<FSSetup.filterInvoicingManually>, Equal<False>,
                       Or<
                           Current<CreateInvoiceFilter.loadData>, Equal<True>>>,
               And<
                   Where2<
                       Where2<
                           Where<
                                Current<CreateInvoiceFilter.postTo>, Equal<ListField_PostTo.AR_AP>,
                                And<
                                    AppointmentToPost.postTo, Equal<ListField_PostTo.AR>>>,
                        Or<
                           Where2<
                               Where<
                                    Current<CreateInvoiceFilter.postTo>, Equal<ListField_PostTo.SO>,
                                    And<
                                        AppointmentToPost.postTo, Equal<ListField_PostTo.SO>>>,
                            Or<
                                Where<
                                    Current<CreateInvoiceFilter.postTo>, Equal<ListField_PostTo.SI>,
                                    And<
                                        AppointmentToPost.postTo, Equal<ListField_PostTo.SI>>>>>>>,
                   And<
                       Where2<
                            Where<
                                AppointmentToPost.billingBy, Equal<ListField_Billing_By.Appointment>,
                                And<AppointmentToPost.pendingAPARSOPost, Equal<True>,
                                And<
                                    Where<AppointmentToPost.postedBy, Equal<ListField_Billing_By.Appointment>,
                                        Or<AppointmentToPost.postedBy, IsNull>>>>>,
                        And<
                            Where2<
                                Where<
                                    Current<CreateInvoiceFilter.billingCycleID>, IsNull,
                                Or<
                                    AppointmentToPost.billingCycleID, Equal<Current<CreateInvoiceFilter.billingCycleID>>>>,
                            And<
                                Where2<
                                    Where<
                                        Current<CreateInvoiceFilter.customerID>, IsNull,
                                    Or<
                                        AppointmentToPost.billCustomerID, Equal<Current<CreateInvoiceFilter.customerID>>>>,
                                And<
                                    Where2<
                                        Where2<
                                            Where<
                                                AppointmentToPost.billingCycleType, NotEqual<ListField_Billing_Cycle_Type.WorkOrder>,
                                                And<AppointmentToPost.billingCycleType, NotEqual<ListField_Billing_Cycle_Type.PurchaseOrder>>>,
                                                Or<
                                                    Where2<
                                                        Where<
                                                            AppointmentToPost.billingCycleType, Equal<ListField_Billing_Cycle_Type.WorkOrder>>,
                                                    Or<
                                                        Where<
                                                            AppointmentToPost.billingCycleType, Equal<ListField_Billing_Cycle_Type.PurchaseOrder>>>>>>,
                                    And<
                                        Where2<
                                            Where<
                                                Current<CreateInvoiceFilter.ignoreBillingCycles>, Equal<False>,
                                            And<
                                                AppointmentToPost.cutOffDate, LessEqual<Current<CreateInvoiceFilter.upToDate>>>>,
                                        Or<
                                            Where<
                                                Current<CreateInvoiceFilter.ignoreBillingCycles>, Equal<True>,
                                            And<
                                                AppointmentToPost.filterDate, LessEqual<Current<CreateInvoiceFilter.upToDate>>>>>>>>>>>>>>>>>>> PostLines;
        #endregion

        #region ViewPostBatch
        public PXAction<CreateInvoiceFilter> viewPostBatch;
        [PXUIField(DisplayName = "")]
        public virtual IEnumerable ViewPostBatch(PXAdapter adapter)
        {
            if (PostLines.Current != null)
            {
                AppointmentToPost postLineRow = PostLines.Current;
                PostBatchMaint graphPostBatchMaint = PXGraph.CreateInstance<PostBatchMaint>();

                if (postLineRow.BatchID != null)
                {
                    graphPostBatchMaint.BatchRecords.Current = graphPostBatchMaint.BatchRecords.Search<FSPostBatch.batchID>(postLineRow.BatchID);
                    throw new PXRedirectRequiredException(graphPostBatchMaint, null) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
                }
            }

            return adapter.Get();
        }
        #endregion

        #region CacheAttached
        #region AppointmentToPost_AppointmentID
        [PXDBIdentity]
        [PXUIField(DisplayName = "Appointment Nbr.")]
        [PXSelector(typeof(
            Search<FSAppointment.appointmentID,
            Where<
                FSAppointment.srvOrdType, Equal<Current<AppointmentToPost.srvOrdType>>>>),
            SubstituteKey = typeof(FSAppointment.refNbr))]
        protected virtual void AppointmentToPost_AppointmentID_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region AppointmentToPost_SOID
        [PXDBInt]
        [PXUIField(DisplayName = "Service Order Nbr.")]
        [PXSelector(typeof(
            Search<AppointmentToPost.sOID,
            Where<
                AppointmentToPost.srvOrdType, Equal<Current<AppointmentToPost.srvOrdType>>>>),
            SubstituteKey = typeof(AppointmentToPost.soRefNbr))]
        protected virtual void AppointmentToPost_SOID_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region CreateInvoiceFilter_IgnoreBillingCycles
        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Ignore the Time Frame")]
        protected virtual void CreateInvoiceFilter_IgnoreBillingCycles_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #endregion

        #region Event Handlers
        protected override void CreateInvoiceFilter_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            base.CreateInvoiceFilter_RowSelected(cache, e);

            CreateInvoiceFilter createInvoiceFilterRow = (CreateInvoiceFilter)e.Row;

            string errorMessage = PXUIFieldAttribute.GetErrorOnly<CreateInvoiceFilter.invoiceFinPeriodID>(cache, createInvoiceFilterRow);
            bool enableProcessButtons = string.IsNullOrEmpty(errorMessage) == true;

            PostLines.SetProcessAllEnabled(enableProcessButtons);
            PostLines.SetProcessEnabled(enableProcessButtons);
        }
        #endregion

        public CreateInvoiceByAppointmentPost() : base()
        {
            billingBy = ID.Billing_By.APPOINTMENT;
            CreateInvoiceByAppointmentPost graphCreateInvoiceByAppointmentPost = null;

            PostLines.SetProcessDelegate(
                delegate(List<AppointmentToPost> appointmentToPostRows)
                {
                    graphCreateInvoiceByAppointmentPost = PXGraph.CreateInstance<CreateInvoiceByAppointmentPost>();

                    var jobExecutor = new JobExecutor<InvoicingProcessStepGroupShared>(true);

                    CreateInvoices(graphCreateInvoiceByAppointmentPost, appointmentToPostRows, Filter.Current, this.UID, jobExecutor, PXQuickProcess.ActionFlow.NoFlow);
                });
        }

        public override List<DocLineExt> GetInvoiceLines(Guid currentProcessID, int billingCycleID, string groupKey, bool getOnlyTotal, out decimal? invoiceTotal, string postTo)
        {
            PXGraph tempGraph = new PXGraph();

            if (getOnlyTotal == true)
            {
                /* Always keep both BQLs with the same Joins and Where conditions */
                FSAppointmentDet fsAppointmentDetRow =
                        PXSelectJoinGroupBy<FSAppointmentDet,
                            InnerJoin<FSAppointment,
                                On<FSAppointment.appointmentID, Equal<FSAppointmentDet.appointmentID>>,
                            InnerJoin<FSServiceOrder,
                                On<FSServiceOrder.sOID, Equal<FSAppointment.sOID>>,
                            InnerJoin<FSSrvOrdType,
                                On<FSSrvOrdType.srvOrdType, Equal<FSServiceOrder.srvOrdType>>,
                            InnerJoin<FSPostDoc,
                                On<
                                    FSPostDoc.appointmentID, Equal<FSAppointment.appointmentID>,
                                    And<FSPostDoc.entityType, Equal<ListField_PostDoc_EntityType.Appointment>>>,
                            LeftJoin<FSPostInfo,
                                On<
                                    FSPostInfo.postID, Equal<FSAppointmentDet.postID>>>>>>>,
                        Where<
                            FSPostDoc.processID, Equal<Required<FSPostDoc.processID>>,
                            And<FSPostDoc.billingCycleID, Equal<Required<FSPostDoc.billingCycleID>>,
                            And<FSPostDoc.groupKey, Equal<Required<FSPostDoc.groupKey>>,
                            And<FSAppointmentDet.lineType, NotEqual<FSAppointmentDet.lineType.Comment_Service>,
                            And<FSAppointmentDet.lineType, NotEqual<FSAppointmentDet.lineType.Comment_Part>,
                            And<FSAppointmentDet.lineType, NotEqual<FSAppointmentDet.lineType.Instruction_Part>,
                            And<FSAppointmentDet.lineType, NotEqual<FSAppointmentDet.lineType.Instruction_Service>,
                            And<FSAppointmentDet.status, NotEqual<FSAppointmentDet.status.Canceled>,
                            And<FSAppointmentDet.lineType, NotEqual<ListField_LineType_Pickup_Delivery.Pickup_Delivery>,
                            And<FSAppointmentDet.isPrepaid, Equal<False>,
                            And<FSAppointmentDet.isBillable, Equal<True>,
                            And<
                                Where2<
                                    Where<
                                        FSAppointmentDet.postID, IsNull>,
                                    Or<
                                        Where<
                                            FSPostInfo.aRPosted, Equal<False>,
                                            And<FSPostInfo.aPPosted, Equal<False>,
                                            And<FSPostInfo.sOPosted, Equal<False>,
                                            And<FSPostInfo.sOInvPosted, Equal<False>,
                                            And<
                                                Where2<
                                                    Where<
                                                        Required<FSPostBatch.postTo>, NotEqual<FSPostBatch.postTo.SO>>,
                                                    Or<
                                                        Where<
                                                            Required<FSPostBatch.postTo>, Equal<FSPostBatch.postTo.SO>,
                                                            And<FSPostInfo.iNPosted, Equal<False>>>>>>>>>>>>>>>>>>>>>>>>,
                        Aggregate<
                            Sum<FSAppointmentDet.tranAmt>>>
                        .Select(tempGraph, currentProcessID, billingCycleID, groupKey, postTo, postTo);

                invoiceTotal = fsAppointmentDetRow.TranAmt;

                FSAppointmentInventoryItem fsAppointmentInventoryItem =
                        PXSelectJoinGroupBy<FSAppointmentInventoryItem,
                            InnerJoin<FSAppointment,
                                On<FSAppointment.appointmentID, Equal<FSAppointmentInventoryItem.appointmentID>>,
                            InnerJoin<FSServiceOrder,
                                On<FSServiceOrder.sOID, Equal<FSAppointment.sOID>>,
                            InnerJoin<FSSrvOrdType,
                                On<FSSrvOrdType.srvOrdType, Equal<FSServiceOrder.srvOrdType>>,
                            InnerJoin<FSPostDoc,
                                On<
                                    FSPostDoc.appointmentID, Equal<FSAppointment.appointmentID>,
                                    And<FSPostDoc.entityType, Equal<ListField_PostDoc_EntityType.Appointment>>>,
                            LeftJoin<FSPostInfo,
                                On<
                                    FSPostInfo.postID, Equal<FSAppointmentInventoryItem.postID>>>>>>>,
                        Where<
                            FSAppointmentInventoryItem.lineType, Equal<ListField_LineType_Pickup_Delivery.Pickup_Delivery>,
                            And<FSPostDoc.processID, Equal<Required<FSPostDoc.processID>>,
                            And<FSPostDoc.billingCycleID, Equal<Required<FSPostDoc.billingCycleID>>,
                            And<FSPostDoc.groupKey, Equal<Required<FSPostDoc.groupKey>>,
                            And<
                                Where2<
                                    Where<
                                        FSAppointmentInventoryItem.postID, IsNull>,
                                    Or<
                                        Where<
                                            FSPostInfo.aRPosted, Equal<False>,
                                            And<FSPostInfo.aPPosted, Equal<False>,
                                            And<FSPostInfo.sOPosted, Equal<False>,
                                            And<FSPostInfo.sOInvPosted, Equal<False>,
                                            And<
                                                Where2<
                                                    Where<
                                                        Required<FSPostBatch.postTo>, NotEqual<FSPostBatch.postTo.SO>>,
                                                    Or<
                                                        Where<
                                                            Required<FSPostBatch.postTo>, Equal<FSPostBatch.postTo.SO>,
                                                            And<FSPostInfo.iNPosted, Equal<False>>>>>>>>>>>>>>>>>,
                        Aggregate<
                            Sum<FSAppointmentInventoryItem.tranAmt>>>
                        .Select(tempGraph, currentProcessID, billingCycleID, groupKey, postTo, postTo);

                invoiceTotal += fsAppointmentInventoryItem.TranAmt ?? 0;

                return null;
            }
            else
            {
                invoiceTotal = null;

                /* Always keep both BQLs with the same Joins and Where conditions */
                var resultSet1 = PXSelectJoin<FSAppointmentDet,
                            InnerJoin<FSAppointment,
                                On<FSAppointment.appointmentID, Equal<FSAppointmentDet.appointmentID>>,
                            InnerJoin<FSServiceOrder,
                                On<FSServiceOrder.sOID, Equal<FSAppointment.sOID>>,
                            InnerJoin<FSSrvOrdType,
                                On<FSSrvOrdType.srvOrdType, Equal<FSServiceOrder.srvOrdType>>,
                            InnerJoin<FSPostDoc,
                                On<
                                    FSPostDoc.appointmentID, Equal<FSAppointment.appointmentID>,
                                    And<FSPostDoc.entityType, Equal<ListField_PostDoc_EntityType.Appointment>>>,
                            LeftJoin<FSPostInfo,
                                On<
                                    FSPostInfo.postID, Equal<FSAppointmentDet.postID>>,
                            LeftJoin<FSSODet,
                                On<FSSODet.srvOrdType, Equal<FSServiceOrder.srvOrdType>,
                                    And<FSSODet.refNbr, Equal<FSServiceOrder.refNbr>,
                                    And<FSSODet.sODetID, Equal<FSAppointmentDet.sODetID>>>>,
                            LeftJoin<FSSODetSplit,
                                On<FSSODetSplit.srvOrdType, Equal<FSSODet.srvOrdType>,
                                    And<FSSODetSplit.refNbr, Equal<FSSODet.refNbr>,
                                    And<FSSODetSplit.lineNbr, Equal<FSSODet.lineNbr>>>>,
                            LeftJoin<INItemPlan, 
                                On<INItemPlan.planID, Equal<FSSODetSplit.planID>>>>>>>>>>,
                        Where<
                            FSPostDoc.processID, Equal<Required<FSPostDoc.processID>>,
                            And<FSPostDoc.billingCycleID, Equal<Required<FSPostDoc.billingCycleID>>,
                            And<FSPostDoc.groupKey, Equal<Required<FSPostDoc.groupKey>>,
                            And<FSAppointmentDet.lineType, NotEqual<FSAppointmentDet.lineType.Comment_Service>,
                            And<FSAppointmentDet.lineType, NotEqual<FSAppointmentDet.lineType.Comment_Part>,
                            And<FSAppointmentDet.lineType, NotEqual<FSAppointmentDet.lineType.Instruction_Part>,
                            And<FSAppointmentDet.lineType, NotEqual<FSAppointmentDet.lineType.Instruction_Service>,
                            And<FSAppointmentDet.status, NotEqual<FSAppointmentDet.status.Canceled>,
                            And<FSAppointmentDet.lineType, NotEqual<ListField_LineType_Pickup_Delivery.Pickup_Delivery>,
                            And<FSAppointmentDet.isPrepaid, Equal<False>,
                            And<
                                Where2<
                                    Where<
                                        FSAppointmentDet.postID, IsNull>,
                                    Or<
                                        Where<
                                            FSPostInfo.aRPosted, Equal<False>,
                                            And<FSPostInfo.aPPosted, Equal<False>,
                                            And<FSPostInfo.sOPosted, Equal<False>,
                                            And<FSPostInfo.sOInvPosted, Equal<False>,
                                            And<
                                                Where2<
                                                    Where<
                                                        Required<FSPostBatch.postTo>, NotEqual<FSPostBatch.postTo.SO>>,
                                                    Or<
                                                        Where<
                                                            Required<FSPostBatch.postTo>, Equal<FSPostBatch.postTo.SO>,
                                                            And<FSPostInfo.iNPosted, Equal<False>>>>>>>>>>>>>>>>>>>>>>>,
                        OrderBy<
                            Asc<FSAppointment.executionDate,
                            Asc<FSAppointmentDet.appointmentID,
                            Asc<FSAppointmentDet.appDetID>>>>>
                        .Select(tempGraph, currentProcessID, billingCycleID, groupKey, postTo, postTo);

                var docLines = new List<DocLineExt>();

                foreach (PXResult<FSAppointmentDet, FSAppointment, FSServiceOrder, FSSrvOrdType, FSPostDoc, FSPostInfo, FSSODet, FSSODetSplit, INItemPlan> row in resultSet1)
                {
                    docLines.Add(new DocLineExt(row));
                }

                var resultSet2 = PXSelectJoin<FSAppointmentInventoryItem,
                            InnerJoin<FSAppointment,
                                On<FSAppointment.appointmentID, Equal<FSAppointmentInventoryItem.appointmentID>>,
                            InnerJoin<FSServiceOrder,
                                On<FSServiceOrder.sOID, Equal<FSAppointment.sOID>>,
                            InnerJoin<FSSrvOrdType,
                                On<FSSrvOrdType.srvOrdType, Equal<FSServiceOrder.srvOrdType>>,
                            InnerJoin<FSPostDoc,
                                On<
                                    FSPostDoc.appointmentID, Equal<FSAppointment.appointmentID>,
                                    And<FSPostDoc.entityType, Equal<ListField_PostDoc_EntityType.Appointment>>>,
                            LeftJoin<FSPostInfo,
                                On<
                                    FSPostInfo.postID, Equal<FSAppointmentInventoryItem.postID>>>>>>>,
                        Where<
                            FSAppointmentInventoryItem.lineType, Equal<ListField_LineType_Pickup_Delivery.Pickup_Delivery>,
                            And<FSPostDoc.processID, Equal<Required<FSPostDoc.processID>>,
                            And<FSPostDoc.billingCycleID, Equal<Required<FSPostDoc.billingCycleID>>,
                            And<FSPostDoc.groupKey, Equal<Required<FSPostDoc.groupKey>>,
                            And<
                                Where2<
                                    Where<
                                        FSAppointmentInventoryItem.postID, IsNull>,
                                    Or<
                                        Where<
                                            FSPostInfo.aRPosted, Equal<False>,
                                            And<FSPostInfo.aPPosted, Equal<False>,
                                            And<FSPostInfo.sOPosted, Equal<False>,
                                            And<FSPostInfo.sOInvPosted, Equal<False>,
                                            And<
                                                Where2<
                                                    Where<
                                                        Required<FSPostBatch.postTo>, NotEqual<FSPostBatch.postTo.SO>>,
                                                    Or<
                                                        Where<
                                                            Required<FSPostBatch.postTo>, Equal<FSPostBatch.postTo.SO>,
                                                            And<FSPostInfo.iNPosted, Equal<False>>>>>>>>>>>>>>>>>,
                        OrderBy<
                            Asc<FSAppointment.executionDate,
                            Asc<FSAppointmentInventoryItem.appointmentID,
                            Asc<FSAppointmentInventoryItem.appDetID>>>>>
                        .Select(tempGraph, currentProcessID, billingCycleID, groupKey, postTo, postTo);

                DocLineExt docLineExtRow;

                foreach (PXResult<FSAppointmentInventoryItem, FSAppointment, FSServiceOrder, FSSrvOrdType, FSPostDoc, FSPostInfo> row in resultSet2)
                {
                    docLineExtRow = new DocLineExt(row);

                    docLineExtRow.docLine.AcctID = ServiceOrderCore.Get_TranAcctID_DefaultValue(
                                                        this,
                                                        docLineExtRow.fsSrvOrdType.SalesAcctSource,
                                                        docLineExtRow.docLine.InventoryID,
                                                        docLineExtRow.fsServiceOrder);

                    docLines.Add(docLineExtRow);
                }

                return docLines;
            }
        }

        public override void UpdateSourcePostDoc(PXCache<FSPostDet> cacheFSPostDet, FSPostBatch fsPostBatchRow, FSPostDoc fsPostDocRow)
        {
            PXUpdate<
                Set<FSAppointment.finPeriodID, Required<FSAppointment.finPeriodID>,
                Set<FSAppointment.pendingAPARSOPost, False,
                Set<FSAppointment.postingStatusAPARSO, ListField_Status_Posting.Posted>>>,
            FSAppointment,
            Where<
                FSAppointment.appointmentID, Equal<Required<FSAppointment.appointmentID>>>>
            .Update(cacheFSPostDet.Graph, fsPostBatchRow.FinPeriodID, fsPostDocRow.AppointmentID);

            PXUpdate<
                Set<FSServiceOrder.finPeriodID, Required<FSServiceOrder.finPeriodID>,
                Set<FSServiceOrder.postedBy, Required<FSServiceOrder.postedBy>,
                Set<FSServiceOrder.pendingAPARSOPost, False>>>,
            FSServiceOrder,
            Where<
                FSServiceOrder.sOID, Equal<Required<FSServiceOrder.sOID>>,
                And<FSServiceOrder.pendingAPARSOPost, Equal<True>>>>
            .Update(cacheFSPostDet.Graph, fsPostBatchRow.FinPeriodID, ID.Billing_By.APPOINTMENT, fsPostDocRow.SOID);
        }
    }
}
