﻿using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.FS
{
    public class SM_ARInvoiceEntry : PXGraphExtension<ARInvoiceEntry>, IInvoiceGraph, IInvoiceContractGraph
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public static bool IsFSIntegrationEnabled(ARInvoice arInvoiceRow, FSxARInvoice fsxARInvoiceRow)
        {
            if (arInvoiceRow.CreatedByScreenID.Substring(0, 2) == "FS")
            {
                return true;
            }

            if (fsxARInvoiceRow.HasFSEquipmentInfo == true)
            {
                return true;
            }

            return false;
        }

        public static void SetUnpersistedFSInfo(PXCache cache, PXRowSelectingEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            ARInvoice arInvoiceRow = (ARInvoice)e.Row;

            if (arInvoiceRow.CreatedByScreenID == null || arInvoiceRow.CreatedByScreenID.Substring(0, 2) == "FS")
            {
                // If the document was created by FS then the FS fields will be visible
                // regardless of whether there is equipment information
                return;
            }

            FSxARInvoice fsxARInvoiceRow = cache.GetExtension<FSxARInvoice>(arInvoiceRow);

            if (PXAccess.FeatureInstalled<FeaturesSet.equipmentManagementModule>() == false)
            {
                fsxARInvoiceRow.HasFSEquipmentInfo = false;
                return;
            }

            if (fsxARInvoiceRow.HasFSEquipmentInfo == null)
            {
                using (new PXConnectionScope())
                {
                    PXResultset<InventoryItem> inventoryItemSet = PXSelectJoin<InventoryItem,
                                InnerJoin<ARTran,
                                            On<ARTran.inventoryID, Equal<InventoryItem.inventoryID>,
                                            And<ARTran.tranType, Equal<ARDocType.invoice>>>,
                                   InnerJoin<SOLineSplit,
                                        On<SOLineSplit.orderType, Equal<ARTran.sOOrderType>,
                                        And<SOLineSplit.orderNbr, Equal<ARTran.sOOrderNbr>,
                                        And<SOLineSplit.lineNbr, Equal<ARTran.sOOrderLineNbr>>>>,
                                   InnerJoin<SOLine,
                                        On<SOLine.orderType, Equal<SOLineSplit.orderType>,
                                        And<SOLine.orderNbr, Equal<SOLineSplit.orderNbr>,
                                        And<SOLine.lineNbr, Equal<SOLineSplit.lineNbr>>>>,
                                   LeftJoin<SOShipLineSplit,
                                        On<SOShipLineSplit.origOrderType, Equal<SOLineSplit.orderType>,
                                        And<SOShipLineSplit.origOrderNbr, Equal<SOLineSplit.orderNbr>,
                                        And<SOShipLineSplit.origLineNbr, Equal<SOLineSplit.lineNbr>,
                                        And<SOShipLineSplit.origSplitLineNbr, Equal<SOLineSplit.splitLineNbr>>>>> >>>>,
                                   Where<
                                        ARTran.tranType, Equal<Required<ARInvoice.docType>>,
                                        And<ARTran.refNbr, Equal<Required<ARInvoice.refNbr>>,
                                        And<FSxSOLine.equipmentAction, NotEqual<ListField_EquipmentAction.None>>>>>
                                    .Select(cache.Graph, arInvoiceRow.DocType, arInvoiceRow.RefNbr);

                    fsxARInvoiceRow.HasFSEquipmentInfo = inventoryItemSet.Count > 0 ? true : false;
                }
            }
        }

        #region Event Handlers
        #region ARInvoice Event Handlers
        protected virtual void ARInvoice_RowSelecting(PXCache cache, PXRowSelectingEventArgs e)
        {
            SetUnpersistedFSInfo(cache, e);
        }

        protected virtual void ARInvoice_RowPersisting(PXCache cache, PXRowPersistingEventArgs e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            ARInvoice arInvoiceRow = (ARInvoice)e.Row;

            if (e.Operation == PXDBOperation.Update
                    && (Base.Accessinfo.ScreenID != SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.INVOICE_BY_APPOINTMENT)
                        && Base.Accessinfo.ScreenID != SharedFunctions.SetScreenIDToDotFormat(ID.ScreenID.INVOICE_BY_SERVICE_ORDER)))
            {
                FSCreatedDoc fsCreatedDocRow = PXSelectJoin<FSCreatedDoc,
                                                InnerJoin<FSPostBatch, On<FSCreatedDoc.batchID, Equal<FSPostBatch.batchID>>>,
                                                Where<
                                                    FSPostBatch.status, Equal<FSPostBatch.status.temporary>,
                                                    And<FSPostBatch.postTo, Equal<FSPostBatch.postTo.AR>,
                                                    And<FSCreatedDoc.createdRefNbr, Equal<Required<FSCreatedDoc.createdRefNbr>>,
                                                    And<FSCreatedDoc.createdDocType, Equal<Required<FSCreatedDoc.createdDocType>>>>>>>
                                             .Select(Base, arInvoiceRow.RefNbr, arInvoiceRow.DocType);

                if (fsCreatedDocRow != null)
                {
                    throw new PXException(TX.Error.CANNOT_UPDATE_DOCUMENT_BECAUSE_BATCH_STATUS_IS_TEMPORARY);
                }
            }
        }
        protected virtual void ARInvoice_RowPersisted(PXCache cache, PXRowPersistedEventArgs e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            ARInvoice arInvoiceRow = (ARInvoice)e.Row;
            if (e.Operation == PXDBOperation.Delete
                    && e.TranStatus == PXTranStatus.Open)
            {
                InvoicingFunctions.CleanPostingInfoLinkedToDoc(arInvoiceRow);
                InvoicingFunctions.CleanContractPostingInfoLinkedToDoc(arInvoiceRow);
            }
        }
        #endregion
        #region ARTran Event Handlers
        protected virtual void ARTran_Qty_FieldUpdating(PXCache cache, PXFieldUpdatingEventArgs e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            ARTran arTranRow = (ARTran)e.Row;

            if (IsLineCreatedFromAppSO(Base, Base.Document.Current, arTranRow, typeof(ARTran.qty).Name) == true)
            {
                throw new PXSetPropertyException(TX.Error.NO_UPDATE_ALLOWED_DOCLINE_LINKED_TO_APP_SO);
            }
        }

        protected virtual void ARTran_UOM_FieldUpdating(PXCache cache, PXFieldUpdatingEventArgs e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            ARTran arTranRow = (ARTran)e.Row;

            if (IsLineCreatedFromAppSO(Base, Base.Document.Current, arTranRow, typeof(ARTran.uOM).Name) == true)
            {
                throw new PXSetPropertyException(TX.Error.NO_UPDATE_ALLOWED_DOCLINE_LINKED_TO_APP_SO);
            }
        }

        protected virtual void ARTran_InventoryID_FieldUpdating(PXCache cache, PXFieldUpdatingEventArgs e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            ARTran arTranRow = (ARTran)e.Row;

            if (IsLineCreatedFromAppSO(Base, Base.Document.Current, arTranRow, typeof(ARTran.inventoryID).Name) == true)
            {
                throw new PXSetPropertyException(TX.Error.NO_UPDATE_ALLOWED_DOCLINE_LINKED_TO_APP_SO);
            }
        }

        protected virtual void ARTran_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
        {
            ARInvoice arInvoiceRow = Base.Document.Current;
            FSxARInvoice fsxARInvoiceRow = Base.Document.Cache.GetExtension<FSxARInvoice>(arInvoiceRow);

            bool fsIntegrationEnabled = IsFSIntegrationEnabled(arInvoiceRow, fsxARInvoiceRow);
            DACHelper.SetExtensionVisibleInvisible<FSxARTran>(cache, e, fsIntegrationEnabled, false);

            if (e.Row == null)
            {
                return;
            }
        }

        protected virtual void ARTran_RowDeleting(PXCache cache, PXRowDeletingEventArgs e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            ARTran arTranRow = (ARTran)e.Row;

            if (e.ExternalCall == true)
            {
                if (IsLineCreatedFromAppSO(Base, Base.Document.Current, arTranRow, null) == true)
                {
                    throw new PXException(TX.Error.NO_DELETION_ALLOWED_DOCLINE_LINKED_TO_APP_SO);
                }
            }
        }

        protected virtual void ARTran_RowPersisting(PXCache cache, PXRowPersistingEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            ARTran arTranRow = (ARTran)e.Row;

            if(e.Operation == PXDBOperation.Insert)
            {
                FillAppointmentSOFields(cache, arTranRow);
            }

            FillEquipmentFields(cache, (ARTran)e.Row);
        }

        protected virtual void ARTran_RowPersisted(PXCache cache, PXRowPersistedEventArgs e)
        {
            if (e.Row == null)
            {
                return;
            }

            ARTran arTranRow = (ARTran)e.Row;

            //The CheckCacheState needs to be run when the transaction is aborted to verify the status of the fields
            //for the current row in order to catch exceptions thrown by other modules to allow them be shown in the
            //Generate Invoice screen.
            if (e.TranStatus == PXTranStatus.Aborted && IsInvoiceProcessRunning == true)
            {
                SharedFunctions.CheckCacheState(cache, arTranRow);
            }
        }
        #endregion
        #endregion

        #region Invoicing Methods
        public virtual bool IsInvoiceProcessRunning { get; set; }

        public virtual List<ErrorInfo> GetErrorInfo()
        {
            List<ErrorInfo> errorList = new List<ErrorInfo>();
            ErrorInfo errorInfo;

            string headerErrorMessage = SharedFunctions.CheckCacheState(Base.Document.Cache, Base.Document.Current, true);
            bool headerHasError = string.IsNullOrEmpty(headerErrorMessage) == false;

            foreach (ARTran arTranRow in Base.Transactions.Select())
            {
                string errorMessage = SharedFunctions.CheckCacheState(Base.Transactions.Cache, arTranRow, true, true, true);

                if (string.IsNullOrEmpty(errorMessage) == false || headerHasError)
                {
                    FSxARTran fsxARTRanRow = Base.Transactions.Cache.GetExtension<FSxARTran>(arTranRow);

                    errorInfo = new ErrorInfo()
                    {
                        HeaderError = headerHasError,
                        SOID = fsxARTRanRow.SOID,
                        AppointmentID = fsxARTRanRow.AppointmentID,
                        ErrorMessage = (headerHasError ? headerErrorMessage : errorMessage) + Environment.NewLine
                    };

                    errorList.Add(errorInfo);
                    if (headerHasError)
                    {
                        break;
                    }
                }
            }

            return errorList;
        }

        public virtual void CreateInvoice(PXGraph graphProcess, List<DocLineExt> docLines, List<DocLineExt> docLinesGrouped, short invtMult, DateTime? invoiceDate, string invoiceFinPeriodID, OnDocumentHeaderInsertedDelegate onDocumentHeaderInserted, OnTransactionInsertedDelegate onTransactionInserted, PXQuickProcess.ActionFlow quickProcessFlow)
        {
            if (docLinesGrouped.Count == 0)
            {
                return;
            }

            bool? initialHold = false;

            FSServiceOrder fsServiceOrderRow = docLinesGrouped[0].fsServiceOrder;
            FSSrvOrdType fsSrvOrdTypeRow = docLinesGrouped[0].fsSrvOrdType;
            FSPostDoc fsPostDocRow = docLinesGrouped[0].fsPostDoc;
            FSAppointment fsAppointmentRow = docLinesGrouped[0].fsAppointment;

            Base.FieldDefaulting.AddHandler<ARInvoice.branchID>((sender, e) =>
            {
                e.NewValue = fsServiceOrderRow.BranchID;
                e.Cancel = true;
            });

            ARInvoice arInvoiceRow = new ARInvoice();

            if (invtMult >= 0)
            {
                arInvoiceRow.DocType = ARInvoiceType.Invoice;
            }
            else
            {
                arInvoiceRow.DocType = ARInvoiceType.CreditMemo;
            }

            arInvoiceRow.DocDate = invoiceDate;
            arInvoiceRow.FinPeriodID = invoiceFinPeriodID;
            arInvoiceRow.InvoiceNbr = fsServiceOrderRow.CustPORefNbr;
            arInvoiceRow = Base.Document.Insert(arInvoiceRow);
            initialHold = arInvoiceRow.Hold;
            arInvoiceRow.NoteID = null;
            PXNoteAttribute.GetNoteIDNow(Base.Document.Cache, arInvoiceRow);

            Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.hold>(arInvoiceRow, true);
            Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.customerID>(arInvoiceRow, fsServiceOrderRow.BillCustomerID);
            Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.customerLocationID>(arInvoiceRow, fsServiceOrderRow.BillLocationID);
            Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.curyID>(arInvoiceRow, fsServiceOrderRow.CuryID);

            Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.taxZoneID>(arInvoiceRow, fsAppointmentRow != null ? fsAppointmentRow.TaxZoneID : fsServiceOrderRow.TaxZoneID);

            string termsID = InvoicingFunctions.GetTermsIDFromCustomerOrVendor(graphProcess, fsServiceOrderRow.BillCustomerID, null);
            if (termsID != null)
            {
                Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.termsID>(arInvoiceRow, termsID);
            }
            else
            {
                Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.termsID>(arInvoiceRow, fsSrvOrdTypeRow.DfltTermIDARSO);
            }

            if (fsServiceOrderRow.ProjectID != null)
            {
                Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.projectID>(arInvoiceRow, fsServiceOrderRow.ProjectID);
            }

            Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.docDesc>(arInvoiceRow, fsServiceOrderRow.DocDesc);
            arInvoiceRow.FinPeriodID = invoiceFinPeriodID;
            arInvoiceRow = Base.Document.Update(arInvoiceRow);

            InvoicingFunctions.SetContactAndAddress(Base, fsServiceOrderRow);

            if (onDocumentHeaderInserted != null)
            {
                onDocumentHeaderInserted(Base, arInvoiceRow);
            }

            IDocLine docLine = null;
            ARTran arTranRow = null;
            FSxARTran fsxARTranRow = null;
            int? acctID;

            foreach (DocLineExt docLineExt in docLinesGrouped)
            {
                docLine = docLineExt.docLine;
                fsPostDocRow = docLineExt.fsPostDoc;
                fsServiceOrderRow = docLineExt.fsServiceOrder;
                fsSrvOrdTypeRow = docLineExt.fsSrvOrdType;
                fsAppointmentRow = docLineExt.fsAppointment;

                arTranRow = new ARTran();
                arTranRow = Base.Transactions.Insert(arTranRow);

                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.branchID>(arTranRow, docLine.BranchID);
                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.inventoryID>(arTranRow, docLine.InventoryID);
                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.uOM>(arTranRow, docLine.UOM);

                if (docLine.ProjectID != null && docLine.ProjectTaskID != null)
                {
                    PMTask pmTaskRow = PXSelect<PMTask, Where<PMTask.taskID, Equal<Required<PMTask.taskID>>>>.Select(graphProcess, docLine.ProjectTaskID);
                    Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.taskID>(arTranRow, pmTaskRow.TaskCD);
                }

                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.qty>(arTranRow, docLine.GetQty(FieldType.BillableField));
                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.tranDesc>(arTranRow, docLine.TranDesc);

                fsPostDocRow.DocLineRef = arTranRow = Base.Transactions.Update(arTranRow);

                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.salesPersonID>(arTranRow, fsAppointmentRow == null ? fsServiceOrderRow.SalesPersonID : fsAppointmentRow.SalesPersonID);

                if (docLine.AcctID != null)
                {
                    acctID = docLine.AcctID;
                }
                else
                {
                    acctID = (int?)ServiceOrderCore.Get_TranAcctID_DefaultValue(
                        graphProcess,
                        fsSrvOrdTypeRow.SalesAcctSource,
                        docLine.InventoryID,
                        fsServiceOrderRow);
                }

                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.accountID>(arTranRow, acctID);

                if (docLine.SubID != null)
                {
                    try
                    {
                        Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.subID>(arTranRow, docLine.SubID);
                    }
                    catch (PXException)
                    {
                        arTranRow.SubID = null;
                    }
                }
                else
                {
                    InvoicingFunctions.SetCombinedSubID(
                                        graphProcess,
                                        Base.Transactions.Cache,
                                        arTranRow,
                                        null,
                                        null,
                                        fsSrvOrdTypeRow,
                                        arTranRow.BranchID,
                                        arTranRow.InventoryID,
                                        arInvoiceRow.CustomerLocationID,
                                        fsServiceOrderRow.BranchLocationID,
                                        fsServiceOrderRow.SalesPersonID,
                                        docLine.IsService);
                }


                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.manualPrice>(arTranRow, docLine.ManualPrice);
                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.curyUnitPrice>(arTranRow, docLine.CuryUnitPrice * invtMult);
                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.taxCategoryID>(arTranRow, docLine.TaxCategoryID);
                //Base.Transactions.Cache.SetValueExt<ARTran.curyExtPrice>(arTranRow, docLine.GetTranAmt(FieldType.BillableField) * invtMult);
                Base.Transactions.Cache.SetValueExtIfDifferent<ARTran.commissionable>(arTranRow, fsAppointmentRow?.Commissionable ?? fsServiceOrderRow.Commissionable ?? false);

                Base.Transactions.Cache.SetValueExt<ARTran.costCodeID>(arTranRow, docLine.CostCodeID);

                if (docLine.IsBillable == false)
                {
                    Base.Transactions.Cache.SetValueExt<ARTran.manualDisc>(arTranRow, true);
                    Base.Transactions.Cache.SetValueExt<ARTran.curyDiscAmt>(arTranRow, docLine.GetQty(FieldType.BillableField) * docLine.CuryUnitPrice * invtMult);
                }

                fsxARTranRow = Base.Transactions.Cache.GetExtension<FSxARTran>(arTranRow);
                fsxARTranRow.Source = docLine.BillingBy;
                fsxARTranRow.SOID = fsServiceOrderRow.SOID;
                fsxARTranRow.ServiceOrderDate = fsServiceOrderRow.OrderDate;
                fsxARTranRow.BillCustomerID = fsServiceOrderRow.BillCustomerID;
                fsxARTranRow.CustomerLocationID = fsServiceOrderRow.BillLocationID;
                fsxARTranRow.SODetID = docLine.PostSODetID;
                fsxARTranRow.AppointmentID = docLine.PostAppointmentID;
                fsxARTranRow.AppointmentDate = fsAppointmentRow?.ExecutionDate;
                fsxARTranRow.AppDetID = docLine.PostAppDetID;

                fsxARTranRow.Mem_PreviousPostID = docLine.PostID;
                fsxARTranRow.Mem_TableSource = docLine.SourceTable;

                SharedFunctions.CopyNotesAndFiles(Base.Transactions.Cache, arTranRow, docLine, fsSrvOrdTypeRow);
                fsPostDocRow.DocLineRef = arTranRow = Base.Transactions.Update(arTranRow);

                if (onTransactionInserted != null)
                {
                    onTransactionInserted(Base, arTranRow);
                }
            }

            arInvoiceRow = Base.Document.Update(arInvoiceRow);

            if (Base.ARSetup.Current.RequireControlTotal == true)
            {
                Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.curyOrigDocAmt>(arInvoiceRow, arInvoiceRow.CuryDocBal);
            }

            if (initialHold != true)
            {
                Base.Document.Cache.SetValueExtIfDifferent<ARInvoice.hold>(arInvoiceRow, false);
            }

            arInvoiceRow = Base.Document.Update(arInvoiceRow);
        }

        public virtual FSCreatedDoc PressSave(int batchID, BeforeSaveDelegate beforeSave)
        {
            if (Base.Document.Current == null)
            {
                throw new SharedClasses.TransactionScopeException();
            }

            if (beforeSave != null)
            {
                beforeSave(Base);
            }

            ARInvoiceEntryExternalTax TaxGraphExt = Base.GetExtension<ARInvoiceEntryExternalTax>();

            if (TaxGraphExt != null)
            {
                TaxGraphExt.SkipTaxCalcAndSave();
            }
            else
            {
            Base.Save.Press();
            }

            string docType = Base.Document.Current.DocType;
            string refNbr = Base.Document.Current.RefNbr;

            // Reload ARInvoice to get the current value of IsTaxValid
            Base.Clear();
            Base.Document.Current = PXSelect<ARInvoice, Where<ARInvoice.docType, Equal<Required<ARInvoice.docType>>, And<ARInvoice.refNbr, Equal<Required<ARInvoice.refNbr>>>>>.Select(Base, docType, refNbr);
            ARInvoice arInvoiceRow = Base.Document.Current;

            var fsCreatedDocRow = new FSCreatedDoc()
            {
                BatchID = batchID,
                PostTo = ID.Batch_PostTo.AR,
                CreatedDocType = arInvoiceRow.DocType,
                CreatedRefNbr = arInvoiceRow.RefNbr
            };

            return fsCreatedDocRow;
        }

        public virtual void Clear()
        {
            Base.Clear(PXClearOption.ClearAll);
        }

        public virtual PXGraph GetGraph()
        {
            return Base;
        }

        public virtual void DeleteDocument(FSCreatedDoc fsCreatedDocRow)
        {
            Base.Document.Current = Base.Document.Search<ARInvoice.refNbr>(fsCreatedDocRow.CreatedRefNbr, fsCreatedDocRow.CreatedDocType);

            if (Base.Document.Current != null)
            {
                if (Base.Document.Current.RefNbr == fsCreatedDocRow.CreatedRefNbr
                        && Base.Document.Current.DocType == fsCreatedDocRow.CreatedDocType)
                {
                    Base.Delete.Press();
                }
            }
        }

        public virtual void CleanPostInfo(PXGraph cleanerGraph, FSPostDet fsPostDetRow)
        {
            PXUpdate<
                Set<FSPostInfo.aRLineNbr, Null,
                Set<FSPostInfo.arRefNbr, Null,
                Set<FSPostInfo.arDocType, Null,
                Set<FSPostInfo.aRPosted, False>>>>,
            FSPostInfo,
            Where<
                FSPostInfo.postID, Equal<Required<FSPostInfo.postID>>,
                And<FSPostInfo.aRPosted, Equal<True>>>>
            .Update(cleanerGraph, fsPostDetRow.PostID);
        }

        public virtual bool IsLineCreatedFromAppSO(PXGraph cleanerGraph, object document, object lineDoc, string fieldName)
        {
            if (document == null || lineDoc == null
                || Base.Accessinfo.ScreenID.Replace(".", "") == ID.ScreenID.INVOICE_BY_SERVICE_ORDER
                || Base.Accessinfo.ScreenID.Replace(".", "") == ID.ScreenID.INVOICE_BY_APPOINTMENT)
            {
                return false;
            }

            string refNbr = ((ARInvoice)document).RefNbr;
            string docType = ((ARInvoice)document).DocType;
            int? lineNbr = ((ARTran)lineDoc).LineNbr;

            return PXSelect<FSPostInfo,
                    Where<FSPostInfo.arRefNbr, Equal<Required<FSPostInfo.arRefNbr>>,
                          And<FSPostInfo.arDocType, Equal<Required<FSPostInfo.arDocType>>,
                          And<FSPostInfo.aRLineNbr, Equal<Required<FSPostInfo.aRLineNbr>>,
                          And<FSPostInfo.aRPosted, Equal<True>>>>>>
            .Select(cleanerGraph, refNbr, docType, lineNbr).Count() > 0;
        }

        public virtual void FillAppointmentSOFields(PXCache cache, ARTran arTranRow)
        {
            if (arTranRow.SOOrderType != null
                    && arTranRow.SOOrderNbr != null
                        && arTranRow.SOOrderLineNbr != null)
            {
                PXResult<SOLine, SOOrder, FSServiceOrder, FSAppointment> bqlResult = (PXResult<SOLine, SOOrder, FSServiceOrder, FSAppointment>)
                    PXSelectJoin<SOLine,
                        InnerJoin<SOOrder,
                            On<SOOrder.orderNbr, Equal<SOLine.orderNbr>,
                            And<SOOrder.orderType, Equal<SOLine.orderType>>>,
                        LeftJoin<FSServiceOrder,
                            On<Where2<
                                    Where<
                                        FSServiceOrder.refNbr, Equal<FSxSOOrder.soRefNbr>,
                                        And<FSServiceOrder.srvOrdType, Equal<FSxSOOrder.srvOrdType>>>,
                                    Or<FSServiceOrder.sOID, Equal<FSxSOLine.sOID>>>>,
                        LeftJoin<FSAppointment,
                            On<FSAppointment.appointmentID, Equal<FSxSOLine.appointmentID>>>>>,
                        Where<
                            SOLine.orderType, Equal<Required<SOLine.orderType>>,
                        And<
                            SOLine.orderNbr, Equal<Required<SOLine.orderNbr>>,
                        And<
                            SOLine.lineNbr, Equal<Required<SOLine.lineNbr>>>>>>
                        .Select(cache.Graph, arTranRow.SOOrderType, arTranRow.SOOrderNbr, arTranRow.SOOrderLineNbr);

                SOLine soLineRow = (SOLine)bqlResult;
                FSServiceOrder fsServiceOrderRow = (FSServiceOrder)bqlResult;
                FSAppointment fsAppointmentRow = (FSAppointment)bqlResult;

                if (soLineRow != null)
                {
                    FSxARTran fsxARTranRow = cache.GetExtension<FSxARTran>(arTranRow);

                    if (fsServiceOrderRow != null
                            && fsServiceOrderRow.SOID != null)
                    {
                        fsxARTranRow.SOID = fsServiceOrderRow.SOID;
                        fsxARTranRow.BillCustomerID = fsServiceOrderRow.BillCustomerID;
                        fsxARTranRow.CustomerLocationID = fsServiceOrderRow.BillLocationID;
                        fsxARTranRow.ServiceOrderDate = fsServiceOrderRow.OrderDate;
                    }

                    if (fsAppointmentRow != null
                            && fsAppointmentRow.AppointmentID != null)
                    {
                        fsxARTranRow.AppointmentID = fsAppointmentRow.AppointmentID;
                        fsxARTranRow.AppointmentDate = fsAppointmentRow.ScheduledDateTimeBegin;
                    }
                }
            }
        }

        #region Invoice By Contract Period Methods 
        public virtual FSContractPostDoc CreateInvoiceByContract(PXGraph graphProcess, DateTime? invoiceDate, string invoiceFinPeriodID, FSContractPostBatch fsContractPostBatchRow, FSServiceContract fsServiceContractRow, FSContractPeriod fsContractPeriodRow, List<ContractInvoiceLine> docLines)
        {
            if (docLines.Count == 0)
            {
                return null;
            }

            FSSetup fsSetupRow = ServiceManagementSetup.GetServiceManagementSetup(graphProcess);

            ARInvoice arInvoiceRow = new ARInvoice();

            arInvoiceRow.DocType = ARInvoiceType.Invoice;

            arInvoiceRow.DocDate = invoiceDate;
            arInvoiceRow.FinPeriodID = invoiceFinPeriodID;
            arInvoiceRow.Hold = true;
            arInvoiceRow = Base.Document.Insert(arInvoiceRow);

            Base.Document.Cache.SetValueExt<ARInvoice.customerID>(arInvoiceRow, fsServiceContractRow.BillCustomerID);
            Base.Document.Cache.SetValueExt<ARInvoice.customerLocationID>(arInvoiceRow, fsServiceContractRow.BillLocationID);
            Base.Document.Cache.SetValueExt<ARInvoice.branchID>(arInvoiceRow, fsServiceContractRow.BranchID);
            Base.Document.Cache.SetValueExt<ARInvoice.docDesc>(arInvoiceRow, "Contract Standardized Billing: " + fsServiceContractRow.RefNbr + " " + fsServiceContractRow.DocDesc);

            string termsID = InvoicingFunctions.GetTermsIDFromCustomerOrVendor(graphProcess, fsServiceContractRow.BillCustomerID, null);

            if (termsID != null)
            {
                Base.Document.Cache.SetValueExt<ARInvoice.termsID>(arInvoiceRow, termsID);
            }
            else
            {
                Base.Document.Cache.SetValueExt<ARInvoice.termsID>(arInvoiceRow, fsSetupRow.DfltContractTermIDARSO);
            }

            ARTran arTranRow = null;
            FSxARTran fsxARTranRow = null;
            int? acctID;

            foreach (ContractInvoiceLine docLine in docLines)
            {
                arTranRow = new ARTran();
                arTranRow = Base.Transactions.Insert(arTranRow);

                Base.Transactions.Cache.SetValueExt<ARTran.inventoryID>(arTranRow, docLine.InventoryID);
                Base.Transactions.Cache.SetValueExt<ARTran.uOM>(arTranRow, docLine.UOM);
                Base.Transactions.Cache.SetValueExt<ARTran.salesPersonID>(arTranRow, docLine.SalesPersonID);

                arTranRow = Base.Transactions.Update(arTranRow);
                
                if (docLine.AcctID != null)
                {
                    acctID = docLine.AcctID;
                }
                else
                {
                    acctID = (int?)ServiceOrderCore.Get_INItemAcctID_DefaultValue(
                        graphProcess,
                        fsSetupRow.ContractSalesAcctSource,
                        docLine.InventoryID,
                        fsServiceContractRow);
                }
                
                Base.Transactions.Cache.SetValueExt<ARTran.accountID>(arTranRow, acctID);

                if (docLine.SubID != null)
                {
                    try
                    {
                        Base.Transactions.Cache.SetValueExt<ARTran.subID>(arTranRow, docLine.SubID);
                    }
                    catch (PXException)
                    {
                        arTranRow.SubID = null;
                    }
                }
                else
                {
                    InvoicingFunctions.SetCombinedSubID(
                                        graphProcess,
                                        Base.Transactions.Cache,
                                        arTranRow,
                                        null,
                                        null,
                                        fsSetupRow,
                                        arTranRow.BranchID,
                                        arTranRow.InventoryID,
                                        arInvoiceRow.CustomerLocationID,
                                        fsServiceContractRow.BranchLocationID);
                }

                Base.Transactions.Cache.SetValueExt<ARTran.qty>(arTranRow, docLine.Qty);

                Base.Transactions.Cache.SetValueExt<ARTran.curyUnitPrice>(arTranRow, docLine.CuryUnitPrice);

                Base.Transactions.Cache.SetValueExt<ARTran.tranDesc>(arTranRow, docLine.TranDescPrefix + arTranRow.TranDesc);

                arTranRow = Base.Transactions.Update(arTranRow);

                Base.Transactions.Cache.SetValueExt<ARTran.commissionable>(arTranRow, docLine.Commissionable ?? false);

                fsxARTranRow = Base.Transactions.Cache.GetExtension<FSxARTran>(arTranRow);

                fsxARTranRow.Source = ID.Billing_By.CONTRACT;
                fsxARTranRow.ServiceContractID = fsServiceContractRow.ServiceContractID;
                fsxARTranRow.ContractPeriodID = fsContractPeriodRow.ContractPeriodID;
                fsxARTranRow.BillCustomerID = fsServiceContractRow.BillCustomerID;
                fsxARTranRow.CustomerLocationID = fsServiceContractRow.BillLocationID;

                arTranRow = Base.Transactions.Update(arTranRow);
            }

            if (Base.ARSetup.Current.RequireControlTotal == true)
            {
                Base.Document.Cache.SetValueExt<ARInvoice.curyOrigDocAmt>(arInvoiceRow, arInvoiceRow.CuryDocBal);
            }

            Base.Document.Cache.SetValueExt<ARInvoice.hold>(arInvoiceRow, false);

            Exception newException = null;

            try
            {
            Base.Save.Press();
            }
            catch (Exception e)
            {
                List<ErrorInfo> errorList = this.GetErrorInfo();

                var exceptionWithContextMessage = ExceptionHelper.GetExceptionWithContextMessage(
                                    PXMessages.Localize(TX.Messages.ERROR_CREATING_INVOICE_IN_POSTING_BATCH),
                                    e);

                newException = InvoicingFunctions.GetErrorInfoInLines(errorList, exceptionWithContextMessage);
            }

            if (newException != null)
            {
                throw newException;
            }

            arInvoiceRow = Base.Document.Current;

            FSContractPostDoc fsContractCreatedDocRow = new FSContractPostDoc()
            {
                ContractPeriodID = fsContractPeriodRow.ContractPeriodID,
                ContractPostBatchID = fsContractPostBatchRow.ContractPostBatchID,
                PostDocType = arInvoiceRow.DocType,
                PostedTO = ID.Batch_PostTo.AR,
                PostRefNbr = arInvoiceRow.RefNbr,
                ServiceContractID = fsServiceContractRow.ServiceContractID
            };

            return fsContractCreatedDocRow;
        }
        #endregion
        #endregion

        #region Equipment Customization
        public virtual void FillEquipmentFields(PXCache cache, ARTran arTranRow)
        {
            if (arTranRow.SOOrderType != null
                    && arTranRow.SOOrderNbr != null
                        && arTranRow.SOOrderLineNbr != null)
            {
                PXResult<SOLine, SOOrder, FSServiceOrder, FSAppointment> bqlResult = (PXResult<SOLine, SOOrder, FSServiceOrder, FSAppointment>)
                    PXSelectJoin<SOLine,
                    InnerJoin<SOOrder,
                    On<SOOrder.orderNbr, Equal<SOLine.orderNbr>,
                    And<SOOrder.orderType, Equal<SOLine.orderType>>>,
                    LeftJoin<FSServiceOrder,
                    On<
                        Where2<
                            Where<
                                FSServiceOrder.refNbr, Equal<FSxSOOrder.soRefNbr>,
                                And<FSServiceOrder.srvOrdType, Equal<FSxSOOrder.srvOrdType>>>,
                            Or<FSServiceOrder.sOID, Equal<FSxSOLine.sOID>>>>,
                    LeftJoin<FSAppointment,
                        On<FSAppointment.appointmentID, Equal<FSxSOLine.appointmentID>>>>>,
                    Where<
                        SOLine.orderType, Equal<Required<SOLine.orderType>>,
                    And<
                        SOLine.orderNbr, Equal<Required<SOLine.orderNbr>>,
                    And<
                        SOLine.lineNbr, Equal<Required<SOLine.lineNbr>>>>>>
                    .Select(cache.Graph, arTranRow.SOOrderType, arTranRow.SOOrderNbr, arTranRow.SOOrderLineNbr);

                SOLine soLineRow = (SOLine)bqlResult;
                SOOrder soOrderRow = (SOOrder)bqlResult;
                FSServiceOrder fsServiceOrderRow = (FSServiceOrder)bqlResult;
                FSAppointment fsAppointmentRow = (FSAppointment)bqlResult;

                if (soLineRow != null)
                {
                    FSxSOOrder fsxSOOrderRow = PXCache<SOOrder>.GetExtension<FSxSOOrder>(soOrderRow);
                    FSxSOLine fsxSOLineRow = PXCache<SOLine>.GetExtension<FSxSOLine>(soLineRow);
                    FSxARTran fsxARTranRow = cache.GetExtension<FSxARTran>(arTranRow);

                    fsxARTranRow.SMEquipmentID = fsxSOLineRow.SMEquipmentID;
                    fsxARTranRow.ComponentID = fsxSOLineRow.ComponentID;
                    fsxARTranRow.EquipmentLineRef = fsxSOLineRow.EquipmentLineRef;
                    fsxARTranRow.EquipmentAction = fsxSOLineRow.EquipmentAction;
                    fsxARTranRow.Comment = fsxSOLineRow.Comment;

                    SOLine soLineRow2 = PXSelect<SOLine,
                                        Where<
                                        SOLine.orderType, Equal<Required<SOLine.orderType>>,
                                        And<SOLine.orderNbr, Equal<Required<SOLine.orderNbr>>,
                                        And<SOLine.lineNbr, Equal<Required<SOLine.lineNbr>>>>>>
                                        .Select(cache.Graph, arTranRow.SOOrderType, arTranRow.SOOrderNbr, fsxSOLineRow.NewTargetEquipmentLineNbr);

                    if (soLineRow2 != null)
                    {
                        ARTran arTranRow2 = Base.Transactions.Select().Where(x => ((ARTran)x).SOOrderType == arTranRow.SOOrderType
                                            && ((ARTran)x).SOOrderNbr == arTranRow.SOOrderNbr
                                            && ((ARTran)x).SOOrderLineNbr == soLineRow2.LineNbr).RowCast<ARTran>().FirstOrDefault();

                        fsxARTranRow.NewTargetEquipmentLineNbr = arTranRow2?.LineNbr;
                    }

                    if (fsxSOOrderRow.SDEnabled == true
                            || fsServiceOrderRow.SOID != null)
                    {
                        fsxARTranRow.SOID = fsServiceOrderRow.SOID;
                        fsxARTranRow.BillCustomerID = fsServiceOrderRow.BillCustomerID;
                        fsxARTranRow.CustomerLocationID = fsServiceOrderRow.BillLocationID;
                    }

                    if (fsAppointmentRow?.AppointmentID != null)
                    {
                        fsxARTranRow.AppointmentID = fsAppointmentRow.AppointmentID;
                        fsxARTranRow.AppointmentDate = fsAppointmentRow.ScheduledDateTimeBegin;
                    }
                }
            }
        }
        #endregion
    }
}
