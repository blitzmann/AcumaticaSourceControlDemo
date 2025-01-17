using PX.Data;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using System;

namespace PX.Objects.FS
{
    public class SetupMaint : PXGraph<SetupMaint>
    {
        public PXSave<FSSetup> Save;
        public PXCancel<FSSetup> Cancel;
        public PXSelect<FSSetup> SetupRecord;
        public CRNotificationSetupList<FSNotification> Notifications;
        public PXSelect<NotificationSetupRecipient,
            Where<NotificationSetupRecipient.setupID, Equal<Current<FSNotification.setupID>>>> Recipients;

        public PXSelect<FSSrvOrdType, Where<FSSrvOrdType.requireRoom, Equal<True>>> SrvOrdTypeRequireRoomRecords;

        public SetupMaint()
            : base()
        {
            FieldUpdating.AddHandler(
                                    typeof(FSSetup),
                                    typeof(FSSetup.dfltCalendarStartTime).Name + PXDBDateAndTimeAttribute.TIME_FIELD_POSTFIX,
                                    FSSetup_DfltCalendarStartTime_Time_FieldUpdating);

            FieldUpdating.AddHandler(
                                    typeof(FSSetup),
                                    typeof(FSSetup.dfltCalendarEndTime).Name + PXDBDateAndTimeAttribute.TIME_FIELD_POSTFIX,
                                    FSSetup_DfltCalendarEndTime_Time_FieldUpdating);
        }

        #region CacheAttached
        #region NotificationSetupRecipient_ContactType
        [PXDBString(10)]
        [PXDefault]
        [ApptContactType.ClassList]
        [PXUIField(DisplayName = "Contact Type")]
        [PXCheckUnique(typeof(NotificationSetupRecipient.contactID),
            Where = typeof(Where<NotificationSetupRecipient.setupID, Equal<Current<NotificationSetupRecipient.setupID>>>))]
        public virtual void NotificationSetupRecipient_ContactType_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #region NotificationSetupRecipient_ContactID
        [PXDBInt]
        [PXUIField(DisplayName = "Contact ID")]
        [PXNotificationContactSelector(typeof(NotificationSetupRecipient.contactType),
            typeof(
            Search2<Contact.contactID,
            LeftJoin<EPEmployee,
                On<EPEmployee.parentBAccountID, Equal<Contact.bAccountID>,
                And<EPEmployee.defContactID, Equal<Contact.contactID>>>>,
            Where<
                Current<NotificationSetupRecipient.contactType>, Equal<NotificationContactType.employee>,
                And<EPEmployee.acctCD, IsNotNull>>>))]
        public virtual void NotificationSetupRecipient_ContactID_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #endregion

        #region Public methods

        /// <summary>
        /// Updates <c>FSSrvOrdType.createTimeActivitiesFromAppointment</c> when the Time Card integration is enabled.
        /// </summary>
        /// <param name="graph">PXGraph instance.</param>
        /// <param name="enableEmpTimeCardIntegration">Flag that says whether the TimeCard integration is enabled or not.</param>
        public static void Update_SrvOrdType_TimeActivitiesFromAppointment(PXGraph graph, bool? enableEmpTimeCardIntegration)
        {
            if (enableEmpTimeCardIntegration == true)
            {
                PXUpdate<
                    Set<FSSrvOrdType.createTimeActivitiesFromAppointment, True>,
                FSSrvOrdType>
                .Update(graph);
            }
        }

        public static void EnableDisable_Document(PXCache cache, FSSetup fsSetupRow)
        {
            bool isDistributionModuleInstalled = PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

            
            PXDefaultAttribute.SetPersistingCheck<FSSetup.contractPostOrderType>(cache, fsSetupRow, PXPersistingCheck.Nothing);
            PXDefaultAttribute.SetPersistingCheck<FSSetup.dfltContractTermIDARSO>(cache, fsSetupRow, PXPersistingCheck.Nothing);
        }
        #endregion

        #region Event Handlers

        protected virtual void FSSetup_AppAutoConfirmGap_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSetup fsSetupRow = (FSSetup)e.Row;

            if (fsSetupRow.AppAutoConfirmGap < 1)
            {
                cache.RaiseExceptionHandling<FSSetup.appAutoConfirmGap>(
                                                                        fsSetupRow,
                                                                        fsSetupRow.AppAutoConfirmGap,
                                                                        new PXSetPropertyException(PXMessages.LocalizeFormatNoPrefix(TX.Error.MINIMUN_VALUE, " 00 h 01 m"), PXErrorLevel.Error));
            }
        }

        protected virtual void FSSetup_DfltCalendarStartTime_Time_FieldUpdating(PXCache cache, PXFieldUpdatingEventArgs e)
        {
            if (e.Row == null || e.NewValue == null)
            {
                return;
            }

            FSSetup fsSetupRow = (FSSetup)e.Row;

            fsSetupRow.DfltCalendarStartTime = SharedFunctions.TryParseHandlingDateTime(cache, e.NewValue);

            DateTime? bussinessDatePlusCurrentHours = new DateTime(
                                                                Accessinfo.BusinessDate.Value.Year,
                                                                Accessinfo.BusinessDate.Value.Month,
                                                                Accessinfo.BusinessDate.Value.Day,
                                                                0,
                                                                0,
                                                                0);

            fsSetupRow.DfltCalendarStartTime = SharedFunctions.GetCustomDateTime(bussinessDatePlusCurrentHours, fsSetupRow.DfltCalendarStartTime);
        }

        protected virtual void FSSetup_DfltCalendarEndTime_Time_FieldUpdating(PXCache cache, PXFieldUpdatingEventArgs e)
        {
            if (e.Row == null || e.NewValue == null)
            {
                return;
            }

            FSSetup fsSetupRow = (FSSetup)e.Row;

            fsSetupRow.DfltCalendarEndTime = SharedFunctions.TryParseHandlingDateTime(cache, e.NewValue);

            DateTime? bussinessDatePlusCurrentHours = new DateTime(
                                                                Accessinfo.BusinessDate.Value.Year,
                                                                Accessinfo.BusinessDate.Value.Month,
                                                                Accessinfo.BusinessDate.Value.Day,
                                                                0,
                                                                0,
                                                                0);

            fsSetupRow.DfltCalendarEndTime = SharedFunctions.GetCustomDateTime(bussinessDatePlusCurrentHours, fsSetupRow.DfltCalendarEndTime);
        }

        protected virtual void FSSetup_CustomerMultipleBillingOptions_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSetup fsSetupRow = (FSSetup)e.Row;
            fsSetupRow.BillingOptionsChanged = true;

            cache.RaiseExceptionHandling<FSSetup.customerMultipleBillingOptions>(
                                    fsSetupRow,
                                    fsSetupRow.CustomerMultipleBillingOptions,
                                    new PXSetPropertyException(TX.Warning.CUSTOMER_MULTIPLE_BILLING_OPTIONS_CHANGING, PXErrorLevel.Warning));
        }

        protected virtual void FSSetup_ManageRooms_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSetup fsSetupRow = (FSSetup)e.Row;

            if (fsSetupRow.ManageRooms == false)
            {
                var fsServiceOrderTypeSet = SrvOrdTypeRequireRoomRecords.Select();

                if (fsServiceOrderTypeSet != null && fsServiceOrderTypeSet.Count > 0)
                {
                    WebDialogResult result = SrvOrdTypeRequireRoomRecords.Ask(TX.WebDialogTitles.CONFIRM_MANAGE_ROOMS, TX.Messages.CANNOT_HIDE_ROOMS_IN_SM, MessageButtons.YesNo);
                    if (result == WebDialogResult.Yes)
                    {
                        SvrOrdTypeMaint graphSvrOrdTypeMaint = PXGraph.CreateInstance<SvrOrdTypeMaint>();
                        foreach (FSSrvOrdType fsSrvOrdTypeRow in fsServiceOrderTypeSet)
                        {
                            fsSrvOrdTypeRow.RequireRoom = false;
                            graphSvrOrdTypeMaint.SvrOrdTypeRecords.Update(fsSrvOrdTypeRow);
                            graphSvrOrdTypeMaint.Save.Press();
                            graphSvrOrdTypeMaint.Clear();
                        }
                    }
                    else
                    {
                        fsSetupRow.ManageRooms = true;
                    }
                }
            }

            PXUIFieldAttribute.SetEnabled<FSSetup.manageAttendees>(cache, fsSetupRow, (bool)fsSetupRow.ManageRooms);
            if (fsSetupRow.ManageRooms == false)
            {
                fsSetupRow.ManageAttendees = false;
            }
        }

        public virtual void FSSetup_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSetup fsSetupRow = (FSSetup)e.Row;

            EnableDisable_Document(cache, fsSetupRow);
        }

        protected virtual void FSSetup_RowInserted(PXCache cache, PXRowInsertedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSetup fsSetupRow = (FSSetup)e.Row;
            fsSetupRow.BillingOptionsChanged = true;
        }

        protected virtual void FSSetup_RowPersisting(PXCache cache, PXRowPersistingEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSetup fsSetupRow = (FSSetup)e.Row;
            Update_SrvOrdType_TimeActivitiesFromAppointment(this, fsSetupRow.EnableEmpTimeCardIntegration);
        }

        protected virtual void FSSetup_RowPersisted(PXCache cache, PXRowPersistedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSetup fsSetupRow = (FSSetup)e.Row;

            if (e.Operation == PXDBOperation.Update
                    && e.TranStatus == PXTranStatus.Open
                        && fsSetupRow.BillingOptionsChanged == true)
            {
                SharedFunctions.PreUpdateBillingInfoDocs(this, null, null);
            }

            if (e.TranStatus == PXTranStatus.Completed && fsSetupRow.BillingOptionsChanged == true)
            {
                fsSetupRow.BillingOptionsChanged = false;
                SharedFunctions.UpdateBillingInfoInDocsLO(this, null, null);
            }
        }

        [System.Obsolete("Remove for 2019R2")]
        public virtual void FSSetup_ContractPostTo_FieldDefaulting(PXCache cache, PXFieldDefaultingEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            e.NewValue = ID.Contract_PostTo.ACCOUNTS_RECEIVABLE_MODULE;
        }
        #endregion
    }
}