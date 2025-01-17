using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.CR;
using PX.SM;

namespace PX.Objects.FS
{
    public class ServiceOrderProcess : PXGraph<ServiceOrderProcess>
    {
        #region Delegate
        public ServiceOrderProcess()
        {
            ServiceOrderProcess graphServiceOrderProcess;

            ServiceOrderRecords.SetProcessDelegate(
                delegate(List<FSServiceOrder> fsServiceOrderRowList)
                {
                    graphServiceOrderProcess = PXGraph.CreateInstance<ServiceOrderProcess>();

                    ServiceOrderEntry graphServiceOrderEntry = PXGraph.CreateInstance<ServiceOrderEntry>();

                    int index = 0;

                    foreach (FSServiceOrder fsServiceOrderRow in fsServiceOrderRowList)
                    {
                        try
                        {
                            graphServiceOrderEntry.ServiceOrderRecords.Current = graphServiceOrderEntry.ServiceOrderRecords
                                                                                .Search<FSServiceOrder.refNbr>(fsServiceOrderRow.RefNbr, fsServiceOrderRow.SrvOrdType);

                            switch (Filter.Current.SOAction)
                            {
                                case ID.ServiceOrder_Action_Filter.COMPLETE:
                                    graphServiceOrderEntry.CompleteOrder();
                                    break;
                                case ID.ServiceOrder_Action_Filter.CANCEL:
                                    graphServiceOrderEntry.CancelOrder();
                                    break;
                                case ID.ServiceOrder_Action_Filter.REOPEN:
                                    graphServiceOrderEntry.ReopenOrder();
                                    break;
                                case ID.ServiceOrder_Action_Filter.CLOSE:
                                    graphServiceOrderEntry.CloseOrder();
                                    break;
                                case ID.ServiceOrder_Action_Filter.UNCLOSE:
                                    graphServiceOrderEntry.UncloseOrderWithOptions(false);
                                    break;
                                case ID.ServiceOrder_Action_Filter.ALLOWINVOICE:
                                    graphServiceOrderEntry.AllowInvoice();
                                    break;
                            }

                            PXProcessing<FSServiceOrder>.SetInfo(index, TX.Messages.RECORD_PROCESSED_SUCCESSFULLY);
                        }
                        catch (Exception e)
                        {
                            PXProcessing<FSServiceOrder>.SetError(index, e);
                        }

                        index++;
                    }
                });
        }
        #endregion

        #region Filter+Select
        public PXFilter<ServiceOrderFilter> Filter;
        public PXCancel<ServiceOrderFilter> Cancel;

        [PXFilterable]
        [PXViewDetailsButton(typeof(ServiceOrderFilter))]
        public PXFilteredProcessing<FSServiceOrder, ServiceOrderFilter,
                    Where2<
                        Where<CurrentValue<ServiceOrderFilter.soAction>, NotEqual<ServiceOrderFilter.soAction.Undefined>>,
                    And2<
                        Where<CurrentValue<ServiceOrderFilter.srvOrdType>, IsNull,
                        Or<FSServiceOrder.srvOrdType, Equal<CurrentValue<ServiceOrderFilter.srvOrdType>>>>,
                    And2<
                        Where<CurrentValue<ServiceOrderFilter.branchID>, IsNull,
                        Or<FSServiceOrder.branchID, Equal<CurrentValue<ServiceOrderFilter.branchID>>>>,
                    And2<
                        Where<CurrentValue<ServiceOrderFilter.branchLocationID>, IsNull,
                        Or<FSServiceOrder.branchLocationID, Equal<CurrentValue<ServiceOrderFilter.branchLocationID>>>>,
                    And2<
                        Where<CurrentValue<ServiceOrderFilter.status>, IsNull,
                        Or<FSServiceOrder.status, Equal<CurrentValue<ServiceOrderFilter.status>>>>,
                    And2<
                        Where<CurrentValue<ServiceOrderFilter.customerID>, IsNull,
                        Or<FSServiceOrder.customerID, Equal<CurrentValue<ServiceOrderFilter.customerID>>>>,
                    And2<
                        Where<CurrentValue<ServiceOrderFilter.serviceContractID>, IsNull,
                        Or<FSServiceOrder.serviceContractID, Equal<CurrentValue<ServiceOrderFilter.serviceContractID>>>>,
                    And2<
                        Where<CurrentValue<ServiceOrderFilter.fromDate>, IsNull,
                        Or<FSServiceOrder.orderDate, GreaterEqual<CurrentValue<ServiceOrderFilter.fromDate>>>>,
                    And2<
                        Where<CurrentValue<ServiceOrderFilter.toDate>, IsNull,
                        Or<FSServiceOrder.orderDate, LessEqual<CurrentValue<ServiceOrderFilter.toDate>>>>,
                    And<
                        Where2<
                            //// Complete Case
                            Where<
                                CurrentValue<ServiceOrderFilter.soAction>, Equal<ServiceOrderFilter.soAction.Complete>,
                            And<
                                FSServiceOrder.status, Equal<FSServiceOrder.status.Open>>>,
                        Or2<
                            //// Cancel Case
                            Where<
                                CurrentValue<ServiceOrderFilter.soAction>, Equal<ServiceOrderFilter.soAction.Cancel>,
                            And<
                                FSServiceOrder.status, Equal<FSServiceOrder.status.Open>>>,
                        Or2<
                            //// Reopen Case
                            Where<
                                CurrentValue<ServiceOrderFilter.soAction>, Equal<ServiceOrderFilter.soAction.Reopen>,
                            And<
                                Where<
                                    FSServiceOrder.status, Equal<FSServiceOrder.status.Canceled>,
                                Or<
                                    FSServiceOrder.status, Equal<FSServiceOrder.status.Completed>>>>>,
                        Or2<
                            //// Close Case
                            Where<
                                CurrentValue<ServiceOrderFilter.soAction>, Equal<ServiceOrderFilter.soAction.Close>,
                            And<
                                FSServiceOrder.status, Equal<FSServiceOrder.status.Completed>>>,
                        Or2<
                            //// Unclose Case
                            Where<
                                CurrentValue<ServiceOrderFilter.soAction>, Equal<ServiceOrderFilter.soAction.Unclose>,
                            And<
                                FSServiceOrder.status, Equal<FSServiceOrder.status.Closed>>>,
                        Or<
                            //// Allow Invoice Case
                            Where<
                                CurrentValue<ServiceOrderFilter.soAction>, Equal<ServiceOrderFilter.soAction.AllowInvoice>,
                            And<
                                FSServiceOrder.allowInvoice, Equal<False>>>>>>>>>>>>>>>>>>>> ServiceOrderRecords;
        #endregion
    }
}