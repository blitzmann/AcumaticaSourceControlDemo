using System;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CS;
using PX.Objects.PO;

namespace PX.Objects.FS
{
    public class SM_APReleaseProcess : PXGraphExtension<APReleaseProcess>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        [PXHidden]
        public PXSelect<FSServiceOrder> serviceOrderView;

        [PXOverride]
        public void VerifyStockItemLineHasReceipt(APRegister arRegisterRow, Action<APRegister> del)
        {
            if (arRegisterRow.CreatedByScreenID != ID.ScreenID.INVOICE_BY_APPOINTMENT
                    && arRegisterRow.CreatedByScreenID != ID.ScreenID.INVOICE_BY_SERVICE_ORDER)
            {
                if (del != null)
                {
                    del(arRegisterRow);
                }
            }
        }

        #region Event Subscribers
        protected virtual void POOrder_RowPersisted(PXCache cache, PXRowPersistedEventArgs e)
        {
            if (e.TranStatus == PXTranStatus.Open && e.Operation == PXDBOperation.Update)
            {
                POOrder poOrderRow = (POOrder)e.Row;
                string poOrderOldStatus = (string)cache.GetValueOriginal<POOrder.status>(poOrderRow);

                bool updateLines = false;
                List<POLine> poLineUpdatedList = new List<POLine>();

                foreach (object row in Base.poOrderLineUPD.Cache.Updated)
                {
                    if ((bool?)Base.poOrderLineUPD.Cache.GetValue<POLineUOpen.completed>(row) != false)
                    {
                        updateLines = true;
                    }

                    poLineUpdatedList.Add(SharedFunctions.ConvertToPOLine((POLineUOpen)row));
                }

                if (poOrderOldStatus != poOrderRow.Status || updateLines == true)
                {
                    SharedFunctions.UpdateFSSODetReferences(cache.Graph, serviceOrderView.Cache, poOrderRow, poLineUpdatedList);
                }
            }
        }
        #endregion
    }
}
