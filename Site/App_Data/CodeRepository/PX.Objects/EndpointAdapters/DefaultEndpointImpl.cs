using PX.Api;
using PX.Api.ContractBased;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.GL;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.SO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects
{
	public abstract class DefaultEndpointImpl
	{
		[FieldsProcessed(new[] { "DetailID", "Value" })]
		protected void BusinessAccountPaymentInstructionDetail_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			IEnumerable<EntityValueField> valueFields = targetEntity.Fields.OfType<EntityValueField>();
			string targetID = valueFields.Single(f => f.Name.EndsWith("ID")).Value;
			string targetValue = valueFields.Single(f => f.Name.EndsWith("Value")).Value;

			switch (graph)
			{
				case VendorMaint vendorGraph:
					{
						CR.LocationExtAddress location = vendorGraph.DefLocation.Select();
						foreach (VendorPaymentMethodDetail paymentMethodDetail in vendorGraph.PaymentDetails.Select(location.BAccountID, location.LocationID, location.VPaymentMethodID))
							if (paymentMethodDetail.DetailID == targetID)
							{
								vendorGraph.PaymentDetails.Cache.SetValueExt<VendorPaymentMethodDetail.detailValue>(paymentMethodDetail, targetValue);
                                vendorGraph.PaymentDetails.Cache.Update(paymentMethodDetail);
                                return;
							}
						throw new PXException(Common.Messages.EntityWithIDDoesNotExist, Data.EntityHelper.GetFriendlyEntityName<VendorPaymentMethodDetail>(), targetID);
					}
				case CustomerMaint customerGraph:
					{
						CR.LocationExtAddress location = customerGraph.DefLocation.Select();
						foreach (CustomerPaymentMethodDetail paymentMethodDetail in customerGraph.DefPaymentMethodInstanceDetails.Select(location.BAccountID, location.LocationID, location.VPaymentMethodID))
							if (paymentMethodDetail.DetailID == targetID)
							{
								customerGraph.DefPaymentMethodInstanceDetails.Cache.SetValueExt<CustomerPaymentMethodDetail.value>(paymentMethodDetail, targetValue);
                                customerGraph.DefPaymentMethodInstanceDetails.Cache.Update(paymentMethodDetail);
                                return;
							}
						throw new PXException(Common.Messages.EntityWithIDDoesNotExist, Data.EntityHelper.GetFriendlyEntityName<CustomerPaymentMethodDetail>(), targetID);
					}
				default: throw new InvalidOperationException("Not applicable for " + graph.GetType());
			}
		}

        [FieldsProcessed(new[] { "BranchID" })]
		protected void AccountSummaryInquiry_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			AccountHistoryEnq accountHistoryEnq = (AccountHistoryEnq)graph;
			accountHistoryEnq.Filter.Current.OrganizationID = null;

			Dictionary<string, string> values = entity.Fields.OfType<EntityValueField>().ToDictionary(f => f.Name, f => f.Value);
			PXCache cache = accountHistoryEnq.Filter.Cache;

			SetValueIfPassed<GLHistoryEnqFilter.branchID>(cache, accountHistoryEnq.Filter.Current, "Branch", values);
			SetValueIfPassed<GLHistoryEnqFilter.ledgerID>(cache, accountHistoryEnq.Filter.Current, "Ledger", values);
			SetValueIfPassed<GLHistoryEnqFilter.accountClassID>(cache, accountHistoryEnq.Filter.Current, "AccountClass", values);
			SetValueIfPassed<GLHistoryEnqFilter.subCD>(cache, accountHistoryEnq.Filter.Current, "Subaccount", values, true);
			SetValueIfPassed<GLHistoryEnqFilter.finPeriodID>(cache, accountHistoryEnq.Filter.Current, "Period", values, true);

			accountHistoryEnq.Filter.Cache.Update(accountHistoryEnq.Filter.Current);
		}


		[FieldsProcessed(new[] { "BranchID" })]
		protected void AccountByPeriodInquiry_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			AccountHistoryByYearEnq accountHistoryByYearEnq = (AccountHistoryByYearEnq)graph;
			accountHistoryByYearEnq.Filter.Current.OrganizationID = null;

			Dictionary<string, string> values = entity.Fields.OfType<EntityValueField>().ToDictionary(f => f.Name, f => f.Value);
			PXCache cache = accountHistoryByYearEnq.Filter.Cache;

			SetValueIfPassed<AccountByYearFilter.branchID>(cache, accountHistoryByYearEnq.Filter.Current, "BranchID", values);
			SetValueIfPassed<AccountByYearFilter.ledgerID>(cache, accountHistoryByYearEnq.Filter.Current, "Ledger", values);
			SetValueIfPassed<AccountByYearFilter.accountID>(cache, accountHistoryByYearEnq.Filter.Current, "Account", values);
			SetValueIfPassed<AccountByYearFilter.subCD>(cache, accountHistoryByYearEnq.Filter.Current, "Subaccount", values, true);
			SetValueIfPassed<AccountByYearFilter.finYear>(cache, accountHistoryByYearEnq.Filter.Current, "FinancialYear", values);

			accountHistoryByYearEnq.Filter.Cache.Update(accountHistoryByYearEnq.Filter.Current);
		}

		[FieldsProcessed(new[] { "BranchID" })]
		protected void AccountBySubaccountInquiry_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			AccountHistoryBySubEnq AccountHistoryBySubEnq = (AccountHistoryBySubEnq)graph;
			AccountHistoryBySubEnq.Filter.Current.OrganizationID = null;

			Dictionary<string, string> values = entity.Fields.OfType<EntityValueField>().ToDictionary(f => f.Name, f => f.Value);
			PXCache cache = AccountHistoryBySubEnq.Filter.Cache;

			SetValueIfPassed<GLHistoryEnqFilter.branchID>(cache, AccountHistoryBySubEnq.Filter.Current, "Branch", values);
			SetValueIfPassed<GLHistoryEnqFilter.ledgerID>(cache, AccountHistoryBySubEnq.Filter.Current, "Ledger", values);
			SetValueIfPassed<GLHistoryEnqFilter.accountID>(cache, AccountHistoryBySubEnq.Filter.Current, "Account", values);
			SetValueIfPassed<GLHistoryEnqFilter.subCD>(cache, AccountHistoryBySubEnq.Filter.Current, "Subaccount", values, true);
			SetValueIfPassed<GLHistoryEnqFilter.finPeriodID>(cache, AccountHistoryBySubEnq.Filter.Current, "Period", values, true);

			AccountHistoryBySubEnq.Filter.Cache.Update(AccountHistoryBySubEnq.Filter.Current);
		}

		[FieldsProcessed(new[] { "BranchID" })]
		protected void AccountDetailsInquiry_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			AccountByPeriodEnq accountDetailsEnq = (AccountByPeriodEnq)graph;
			accountDetailsEnq.Filter.Current.OrganizationID = null;
			accountDetailsEnq.Filter.Current.SubID = null;

			Dictionary<string, string> values = entity.Fields.OfType<EntityValueField>().ToDictionary(f => f.Name, f => f.Value);
			PXCache cache = accountDetailsEnq.Filter.Cache;

			SetValueIfPassed<AccountByPeriodFilter.branchID>(cache, accountDetailsEnq.Filter.Current, "Branch", values);
			SetValueIfPassed<AccountByPeriodFilter.ledgerID>(cache, accountDetailsEnq.Filter.Current, "Ledger", values);
			SetValueIfPassed<AccountByPeriodFilter.accountID>(cache, accountDetailsEnq.Filter.Current, "Account", values);
			SetValueIfPassed<AccountByPeriodFilter.subCDWildcard>(cache, accountDetailsEnq.Filter.Current, "Subaccount", values, true);
			SetValueIfPassed<AccountByPeriodFilter.startPeriodID>(cache, accountDetailsEnq.Filter.Current, "FromPeriod", values, true);
			SetValueIfPassed<AccountByPeriodFilter.endPeriodID>(cache, accountDetailsEnq.Filter.Current, "ToPeriod", values, true);

			accountDetailsEnq.Filter.Cache.Update(accountDetailsEnq.Filter.Current);
		}

		private static void SetValueIfPassed<T>(PXCache cache, object current, string fieldName, Dictionary<string, string> valuesCollection, bool replaceDash = false)
			where T : IBqlField
		{
			if (valuesCollection.TryGetValue(fieldName, out string valueToSet))
			{
				if (replaceDash) valueToSet = valueToSet?.Replace("-", string.Empty);
				cache.SetValueExt<T>(current, valueToSet);
			}
			else cache.SetDefaultExt<T>(current);
		}

		[FieldsProcessed(new[] {
			"OrderType",
			"OrderNbr",
			"OrderLineNbr",
			"InventoryID"
		})]
		protected void ShipmentDetail_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var shipmentEntry = (SOShipmentEntry)graph;

			var filterCache = shipmentEntry.addsofilter.Cache;
			var filter = (AddSOFilter)filterCache.Current;

			var orderType = targetEntity.Fields.Single(f => f.Name == "OrderType") as EntityValueField;
			var orderNbr = targetEntity.Fields.Single(f => f.Name == "OrderNbr") as EntityValueField;
			var orderLineNbr = targetEntity.Fields.SingleOrDefault(f => f.Name == "OrderLineNbr") as EntityValueField;
			int? sOLineNbr = null;
			if (orderLineNbr != null && !string.IsNullOrEmpty(orderLineNbr.Value))
			{
				sOLineNbr = int.Parse(orderLineNbr.Value);
			}

			var inventoryId = targetEntity.Fields.SingleOrDefault(f => f.Name == "InventoryID") as EntityValueField;
            var lotSerialNbr = targetEntity.Fields.SingleOrDefault(f => f.Name == "LotSerialNbr") as EntityValueField;
            var shippedQty = targetEntity.Fields.SingleOrDefault(f => f.Name == "ShippedQty") as EntityValueField;

            //setting Add Sales Order filter parameters
            string oldOrderNbr = filter.OrderNbr;
            filter.OrderType = orderType.Value;
            filter.OrderNbr = orderNbr.Value;
            filterCache.RaiseFieldUpdated<AddSOFilter.orderNbr>(filter, oldOrderNbr);
            filterCache.Update(filter);

            //forming selection criteria
            Func<PXResult<SOShipmentPlan>, bool> lineCriteria;
            SOShipmentPlan item = new SOShipmentPlan();
			if (sOLineNbr != null && sOLineNbr >= 0)
			{
				lineCriteria = r => r.GetItem<SOLineSplit>().LineNbr == sOLineNbr;
				item = FindSalesOrderLine(shipmentEntry, lineCriteria);
			}
			else if (inventoryId != null && !string.IsNullOrWhiteSpace(inventoryId.Value))
			{
				var inventoryItem = PXSelect<InventoryItem, Where<InventoryItem.inventoryCD, Equal<Required<InventoryItem.inventoryCD>>>>.Select(graph, inventoryId.Value).FirstOrDefault().GetItem<InventoryItem>();
				lineCriteria = r => r.GetItem<SOLineSplit>().InventoryID == inventoryItem.InventoryID;

				if (lotSerialNbr != null && !string.IsNullOrWhiteSpace(lotSerialNbr.Value))
				{
					lineCriteria = PX.Common.Func.Conjoin(lineCriteria, t => t.GetItem<SOLineSplit>().LotSerialNbr == lotSerialNbr.Value);
				}
				if (shippedQty != null && shippedQty.Value != null && decimal.Parse(shippedQty.Value) > 0)
				{
					lineCriteria = PX.Common.Func.Conjoin(lineCriteria, t => t.GetItem<SOLineSplit>().OpenQty >= decimal.Parse(shippedQty.Value));
				}

				item = FindSalesOrderLine(shipmentEntry, lineCriteria);
			}

            if (item.InventoryID != null)
            {
                SelectLine(shipmentEntry, item);
            }
            else
            {
				//It should be possible to modify this in next endpoint
				if (sOLineNbr != null && sOLineNbr < 0)
				{
					filter.AddAllLines = true;
				}
				else
				{
					foreach (SOShipmentPlan row in shipmentEntry.soshipmentplan.Select())
					{
						SelectLine(shipmentEntry, row);
					}
				}
			}
           
            shipmentEntry.Actions["AddSO"].Press();

            var shipLineCurrent = shipmentEntry.Caches[typeof(SOShipLine)].Current as SOShipLine;
            if (shipLineCurrent == null)
                throw new InvalidOperationException(SO.Messages.CantAddShipmentDetail);

            var allocations = (targetEntity.Fields.SingleOrDefault(f => string.Equals(f.Name, "Allocations")) as EntityListField)?.Value ?? new EntityImpl[0];
            if (allocations.Any(a => a.Fields != null && a.Fields.Length > 0))
            {
                // clear already allocated lines, to replace them with new
                shipLineCurrent.ShippedQty = 0;
                shipLineCurrent.BaseShippedQty = 0;
                shipmentEntry.Caches[typeof(SOShipLine)].Update(shipLineCurrent);

                var current = shipmentEntry.splits.Cache.Current as SOShipLineSplit;
                if (current != null)
                {
                    var inserted = shipmentEntry.splits.Cache.Inserted;
                    foreach (SOShipLineSplit split in inserted)
                    {
                        if (split.LineNbr == shipLineCurrent.LineNbr)
                            shipmentEntry.splits.Cache.SetStatus(split, PXEntryStatus.InsertedDeleted);
                    }
                }

                var current2 = shipmentEntry.Caches[typeof(SOLineSplit2)].Current as SOLineSplit2;
                if (current2 != null)
                {
                    current2.ShippedQty = 0;
                    shipmentEntry.Caches[typeof(SOLineSplit2)].Update(current2);
                }
            }
        }

        private static SOShipmentPlan FindSalesOrderLine(SOShipmentEntry shipmentEntry, Func<PXResult<SOShipmentPlan>, bool> lineCriteria)
        {
            SOShipmentPlan item;
            try
            {
                item = shipmentEntry.soshipmentplan.Select().First(lineCriteria);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(SO.Messages.CannotSelectSpecificSOLine, ex);
            }

            return item;
        }

        private void SelectLine(SOShipmentEntry shipmentEntry, SOShipmentPlan item)
        {
            item.Selected = true;
            shipmentEntry.soshipmentplan.Cache.Update(item);
            AssertNoErrors(shipmentEntry.soshipmentplan.Cache, item);
        }

        private void AssertNoErrors(PXCache cache, object current)
		{
			var errors = PXUIFieldAttribute.GetErrors(cache, current);
			if (errors.Count == 0)
				return;

			throw new InvalidOperationException(string.Join("\n", errors.Select(p => p.Key + ": " + p.Value)));
		}

		[FieldsProcessed(new[] {
			"ShipmentNbr",
			"OrderNbr",
			"OrderType"
		})]
		protected void SalesInvoiceDetail_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var maint = (SOInvoiceEntry)graph;

			var shipmentNbr = targetEntity.Fields.SingleOrDefault(f => f.Name == "ShipmentNbr") as EntityValueField;
			var orderNbr = targetEntity.Fields.SingleOrDefault(f => f.Name == "OrderNbr") as EntityValueField;
			var orderType = targetEntity.Fields.SingleOrDefault(f => f.Name == "OrderType") as EntityValueField;
			var calculateAutomaticDiscounts = targetEntity.Fields.SingleOrDefault(f => f.Name == "CalculateDiscountsOnImport") as EntityValueField;

			var shipment = shipmentNbr != null ? shipmentNbr.Value : null;
			var number = orderNbr != null ? orderNbr.Value : null;
			var type = orderType != null ? orderType.Value : null;
			bool? calculateDiscounts = ConvertToNullableBool(calculateAutomaticDiscounts);

			if (shipment != null && number != null && type != null)
			{
				var shipments = maint.shipmentlist.Select().AsEnumerable().Select(s => s.GetItem<SOOrderShipment>())
				.Where(s => (shipment == null || s.ShipmentNbr.OrdinalEquals(shipment))
							&& (number == null || s.OrderNbr.OrdinalEquals(number))
							&& (type == null || s.OrderType.OrdinalEquals(type)));

				if (!shipments.Any())
				{
					throw new PXException(SO.Messages.ShipmentsNotFound);
				}

				foreach (var item in shipments)
				{
                    foreach (SOOrderShipment orderShipment in maint.shipmentlist.Select())
                    {
                        orderShipment.Selected = false;
                    }

					item.Selected = true;
					maint.shipmentlist.Update(item);
					maint.Actions["AddShipment"].Press();
				}
			}
			else if (shipment == null)
			{
				var detailsCache = maint.Transactions.Cache;

				ARTran row = (ARTran)detailsCache.CreateInstance();
				row.SOShipmentNbr = shipment;
				row.SOOrderType = type;
				row.SOOrderNbr = number;
				row.CalculateDiscountsOnImport = calculateDiscounts;

				FillInvoiceRowFromEntiry(maint, targetEntity, row);
				maint.InsertInvoiceDirectLine(row);
			}
		}

		[FieldsProcessed(new[] {
			"Value",
			"Active",
			"SegmentID"
		})]
		protected void SubItemStockItem_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var subitemNameField = targetEntity.Fields.Single(f => f.Name == "Value") as EntityValueField;
			var activeField = targetEntity.Fields.Single(f => f.Name == "Active") as EntityValueField;
			var segmentIDField = targetEntity.Fields.Single(f => f.Name == "SegmentID") as EntityValueField;

			var view = graph.Views["SubItem_" + segmentIDField.Value];
			var cache = view.Cache;

			foreach (INSubItemSegmentValueList.SValue row in view.SelectMulti(segmentIDField.Value))
			{
				if (row.Value == subitemNameField.Value)
				{
					if (activeField.Value == "true")
					{
						row.Active = true;
						cache.Update(row);
					}
					else
						cache.Delete(row);
				}
			}
		}

		[FieldsProcessed(new[] {
			"Value",
			"Active",
			"SegmentID"
		})]
		protected void SubItemStockItem_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var subitemNameField = targetEntity.Fields.Single(f => f.Name == "Value") as EntityValueField;
			var activeField = targetEntity.Fields.Single(f => f.Name == "Active") as EntityValueField;
			var segmentIDField = targetEntity.Fields.Single(f => f.Name == "SegmentID") as EntityValueField;

			var view = graph.Views["SubItem_" + segmentIDField.Value];
			var cache = view.Cache;

			foreach (INSubItemSegmentValueList.SValue row in view.SelectMulti(segmentIDField.Value))
			{
				if (row.Value == subitemNameField.Value)
				{
					if (activeField.Value == "true")
					{
						row.Active = true;
						cache.Update(row);
					}
					else
						cache.Delete(row);
				}
			}
		}

		[FieldsProcessed(new[] {
			"WarehouseID"
		})]
		protected void StockItemWarehouseDetail_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var warehouseIDField = targetEntity.Fields.Single(f => f.Name == "WarehouseID") as EntityValueField;

			var view = graph.Views["itemsiterecords"];
			var cache = view.Cache;

			var site = (INSite)PXSelect<INSite, Where<INSite.siteCD, Equal<Required<INSite.siteCD>>>>.SelectSingleBound(graph, null, new object[] { warehouseIDField.Value });

			if (site == null)
			{
				throw new PXException("Site '{0}' is missing.", warehouseIDField.Value);
			}

			var rows = view.SelectMulti().Cast<PXResult<INItemSite, INSite, INSiteStatusSummary>>().ToArray();

			foreach (INItemSite row in rows)
			{
				if (row.SiteID == site.SiteID)
					return;
			}

			var itemsite = (INItemSite)cache.CreateInstance();
			itemsite.SiteID = site.SiteID;
			cache.Insert(itemsite);
		}

		[FieldsProcessed(new[] {
			"InventoryID",
			"Location",
			"LotSerialNbr",
			"Subitem"
		})]
		protected void PhysicalInventoryCountDetail_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var InventoryIDField = targetEntity.Fields.Single(f => f.Name == "InventoryID") as EntityValueField;
			var LocationField = targetEntity.Fields.Single(f => f.Name == "Location") as EntityValueField;
			var LotSerialNumberField = targetEntity.Fields.FirstOrDefault(f => f.Name == "LotSerialNbr") as EntityValueField;
			var SubItemField = targetEntity.Fields.FirstOrDefault(f => f.Name == "Subitem") as EntityValueField;

			var maint = (INPICountEntry)graph;

			var view = maint.AddByBarCode;
			var cache = view.Cache;


			cache.Remove(view.Current);
			cache.Insert(new INBarCodeItem());
			var bci = (INBarCodeItem)cache.Current;

			cache.SetValueExt(bci, "InventoryID", InventoryIDField.Value);
			cache.SetValueExt(bci, "LocationID", LocationField.Value);
			if (LotSerialNumberField != null)
				cache.SetValueExt(bci, "LotSerialNbr", LotSerialNumberField.Value);
			if (SubItemField != null)
				cache.SetValueExt(bci, "SubItemID", SubItemField.Value);

			cache.Update(bci);

			maint.Actions["AddLine2"].Press();
		}

		private static EntityValueField GetEntityField(EntityImpl targetEntity, string fieldName)
		{
			return targetEntity.Fields.SingleOrDefault(f => f.Name == fieldName) as EntityValueField;
		}

		private static decimal? ConvertToNullableDecimal(EntityValueField field)
		{
			return field != null ? (decimal?)Convert.ToDecimal(field.Value) : null;
		}

		private static int? ConvertToNullableInt(EntityValueField field)
		{
			return field != null ? (int?)Convert.ToInt32(field.Value) : null;
		}

		private static bool? ConvertToNullableBool(EntityValueField field)
		{
			return field != null ? (bool?)Convert.ToBoolean(field.Value) : null;
		}

		private static void FillInvoiceRowFromEntiry(SOInvoiceEntry graph, EntityImpl targetEntity, ARTran row)
		{
			row.TranDesc = GetEntityField(targetEntity, "TransactionDescr")?.Value;
			row.UnitPrice = ConvertToNullableDecimal(GetEntityField(targetEntity, "UnitPrice"));
			row.LineNbr = ConvertToNullableInt(GetEntityField(targetEntity, "LineNbr"));
			row.Qty = ConvertToNullableDecimal(GetEntityField(targetEntity, "Qty"));
			row.CuryTranAmt = ConvertToNullableDecimal(GetEntityField(targetEntity, "Amount"));
			row.UOM = GetEntityField(targetEntity, "UOM")?.Value;
			row.DiscAmt = ConvertToNullableDecimal(GetEntityField(targetEntity, "DiscountAmount"));
			row.DiscPct = ConvertToNullableDecimal(GetEntityField(targetEntity, "DiscountPercent"));
			row.LotSerialNbr = GetEntityField(targetEntity, "LotSerialNbr")?.Value;
			row.SOOrderLineNbr = ConvertToNullableInt(GetEntityField(targetEntity, "OrderLineNbr"));
			row.TaxCategoryID = GetEntityField(targetEntity, "TaxCategory")?.Value;
			row.OrigInvoiceType = GetEntityField(targetEntity, "OrigInvType")?.Value;
			row.OrigInvoiceNbr = GetEntityField(targetEntity, "OrigInvNbr")?.Value;
			row.OrigInvoiceLineNbr = ConvertToNullableInt(GetEntityField(targetEntity, "OrigInvLineNbr"));

			string inventoryID = GetEntityField(targetEntity, "InventoryID")?.Value;
			string branchID = GetEntityField(targetEntity, "BranchID")?.Value;
			EntityValueField fieldExpirationDate = GetEntityField(targetEntity, "ExpirationDate");
			DateTime expirationDate = fieldExpirationDate != null ? Convert.ToDateTime(fieldExpirationDate.Value) : default(DateTime);
			string location = GetEntityField(targetEntity, "Location")?.Value;
			string warehouseID = GetEntityField(targetEntity, "WarehouseID")?.Value;

			if (inventoryID != null)
			{
				row.InventoryID = PXSelect<InventoryItem, Where<InventoryItem.inventoryCD, Equal<Required<InventoryItem.inventoryCD>>>>.Select(graph, inventoryID).FirstOrDefault()?.GetItem<InventoryItem>().InventoryID;
			}
			if (branchID != null)
			{
				row.BranchID = PXSelect<GL.Branch, Where<GL.Branch.branchCD, Equal<Required<GL.Branch.branchCD>>>>.Select(graph, branchID).FirstOrDefault()?.GetItem<GL.Branch>().BranchID;
			}
			if (expirationDate != default(DateTime))
			{
				row.ExpireDate = expirationDate;
			}
			if (warehouseID != null)
			{
				row.SiteID = PXSelect<INSite, Where<INSite.siteCD, Equal<Required<INSite.siteCD>>>>.Select(graph, warehouseID).FirstOrDefault()?.GetItem<INSite>().SiteID;
				if (location != null)
				{
					row.LocationID = PXSelect<INLocation, Where<INLocation.siteID, Equal<Required<INLocation.siteID>>, And<INLocation.locationCD, Equal<Required<INLocation.locationCD>>>>>.Select(graph, row.SiteID, location).FirstOrDefault()?.GetItem<INLocation>().LocationID;
				}
			}
		}

		[FieldsProcessed(new[] {
			"AttributeID",
			"Value"
		})]
		protected void AttributeValue_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			AttributeBase_Insert(graph, entity, targetEntity, "AttributeID");
		}

		[FieldsProcessed(new[] {
			"Attribute",
			"Value"
		})]
		protected void AttributeDetail_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			// TODO: merge AttributeDetail and AttributeValue entities in new endpoint version (2019r..)
			AttributeBase_Insert(graph, entity, targetEntity, "Attribute");
		}

		private void AttributeBase_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity, string attributeIdFieldName)
		{
			var attributeIdField = targetEntity.Fields.Single(f => f.Name == attributeIdFieldName) as EntityValueField;
			var valueField = targetEntity.Fields.Single(f => f.Name == "Value") as EntityValueField;

			var view = graph.Views[CS.Messages.CSAnswers];
			var cache = view.Cache;

			var rows = view.SelectMulti().OrderBy(row =>
			{
				var orderState = cache.GetStateExt(row, "Order") as PXFieldState;
				return orderState.Value;
			}).ToArray();

			foreach (var row in rows)
			{
				var attributeId = (cache.GetStateExt(row, "AttributeID") as PXFieldState).Value.ToString();
				if (attributeIdField.Value.OrdinalEquals(attributeId))
				{
					var state = cache.GetStateExt(row, "Value") as PXStringState;
					if (state != null && state.ValueLabelDic != null)
					{
						foreach (var rec in state.ValueLabelDic)
						{
							if (rec.Value == valueField.Value)
							{
								valueField.Value = rec.Key;
								break;
							}
						}
					}
					cache.SetValueExt(row, "Value", valueField.Value);
					cache.Update(row);
					break;
				}
			}
		}

		[FieldsProcessed(new[] {
			"POLineNbr",
			"POOrderType",
			"POOrderNbr",
			"TransferOrderType",
			"TransferOrderNbr",
			"TransferShipmentNbr"
		})]
		protected void PurchaseReceiptDetail_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var receiptEntry = (POReceiptEntry)graph;

			if (receiptEntry.Document.Current != null && receiptEntry.Document.Current.ReceiptType == POReceiptType.TransferReceipt)
			{
				var sOOrderType = targetEntity.Fields.SingleOrDefault(f => f.Name == "TransferOrderType") as EntityValueField;
				var sOOrderNbr = targetEntity.Fields.SingleOrDefault(f => f.Name == "TransferOrderNbr") as EntityValueField;
				var sOShipmentNbr = targetEntity.Fields.SingleOrDefault(f => f.Name == "TransferShipmentNbr") as EntityValueField;

				if (sOOrderType != null && sOOrderNbr != null && sOShipmentNbr != null)
				{
					receiptEntry.filter.Cache.Remove(receiptEntry.filter.Current);
					receiptEntry.filter.Cache.Insert(new POReceiptEntry.POOrderFilter());

					var orders = receiptEntry.openTransfers.Select().Select(r => r.GetItem<SOOrderShipment>());
					var order = orders.FirstOrDefault(o => o.OrderType == sOOrderType.Value && o.OrderNbr == sOOrderNbr.Value && o.ShipmentNbr == sOShipmentNbr.Value);
					if (order == null)
					{
						throw new PXException(PO.Messages.TransferOrderNotFound);
					}

					order.Selected = true;
					receiptEntry.openTransfers.Update(order);
					receiptEntry.Actions["AddTransfer2"].Press();

					return;
				}
			}

			var lineNbr = targetEntity.Fields.SingleOrDefault(f => f.Name == "POLineNbr") as EntityValueField;
			var orderType = targetEntity.Fields.SingleOrDefault(f => f.Name == "POOrderType") as EntityValueField;
			var orderNbr = targetEntity.Fields.SingleOrDefault(f => f.Name == "POOrderNbr") as EntityValueField;

			bool insertViaAddPO = lineNbr != null && orderNbr != null && orderType != null;

			if (!insertViaAddPO && (lineNbr != null || orderType != null || orderNbr != null))
			{
				throw new PXException(PO.Messages.POTypeNbrLineNbrMustBeFilled);
			}

			var detailsCache = receiptEntry.transactions.Cache;

			if (insertViaAddPO)
			{
				receiptEntry.filter.Cache.Remove(receiptEntry.filter.Current);
				receiptEntry.filter.Cache.Insert(new POReceiptEntry.POOrderFilter());
				var filter = receiptEntry.filter.Current;

				var state = receiptEntry.filter.Cache.GetStateExt(filter, "OrderType") as PXStringState;
				if (state != null && state.AllowedLabels.Contains(orderType.Value))
				{
					orderType.Value = state.ValueLabelDic.Single(p => p.Value == orderType.Value).Key;
				}

				receiptEntry.filter.Cache.SetValueExt(filter, "OrderType", orderType.Value);
				receiptEntry.filter.Cache.SetValueExt(filter, "OrderNbr", orderNbr.Value);
				filter = receiptEntry.filter.Update(filter);

				Dictionary<string, string> filterErrors = PXUIFieldAttribute.GetErrors(receiptEntry.filter.Cache, filter);

				if (filterErrors.Count() > 0)
				{
					throw new PXException(string.Join(";", filterErrors.Select(x => x.Key + "=" + x.Value)));
				}

				var orders = receiptEntry.poLinesSelection.Select().Select(r => r.GetItem<POReceiptEntry.POLineS>());
				var order = orders.FirstOrDefault(o => o.LineNbr == int.Parse(lineNbr.Value));
				if (order == null)
				{
					throw new PXException(PO.Messages.PurchaseOrderLineNotFound);
				}

				order.Selected = true;
				receiptEntry.poLinesSelection.Update(order);
				receiptEntry.Actions["AddPOOrderLine2"].Press();
			}
			else
			{
				detailsCache.Current = detailsCache.Insert();
				var receiptLineCurrent = detailsCache.Current as POReceiptLine;

				if (detailsCache.Current == null)
					throw new InvalidOperationException("Cannot insert Purchase Receipt detail.");

				var allocations = (targetEntity.Fields.SingleOrDefault(f => string.Equals(f.Name, "Allocations")) as EntityListField)?.Value ?? new EntityImpl[0];
				if (allocations.Any(a => a.Fields != null && a.Fields.Length > 0))
				{
					var InventoryIDField = targetEntity.Fields.SingleOrDefault(f => f.Name == "InventoryID") as EntityValueField;
					var SiteField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Warehouse") as EntityValueField;
					var LocationField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Location") as EntityValueField;
					var LotSerialNumberField = targetEntity.Fields.FirstOrDefault(f => f.Name == "LotSerialNbr") as EntityValueField;
					var SubItemField = targetEntity.Fields.FirstOrDefault(f => f.Name == "Subitem") as EntityValueField;
					var QtyField = targetEntity.Fields.FirstOrDefault(f => f.Name == "ReceiptQty") as EntityValueField;

					receiptEntry.transactions.Cache.SetValueExt(receiptLineCurrent, "InventoryID", InventoryIDField.Value);
					if (SiteField != null)
						receiptEntry.transactions.Cache.SetValueExt(receiptLineCurrent, "SiteID", SiteField.Value);
					if (LocationField != null)
						receiptEntry.transactions.Cache.SetValueExt(receiptLineCurrent, "LocationID", LocationField.Value);
					if (LotSerialNumberField != null)
						receiptEntry.transactions.Cache.SetValueExt(receiptLineCurrent, "LotSerialNbr", LotSerialNumberField.Value);
					if (SubItemField != null)
						receiptEntry.transactions.Cache.SetValueExt(receiptLineCurrent, "SubItemID", SubItemField.Value);
					if (QtyField != null)
					{
						receiptLineCurrent.ReceiptQty = decimal.Parse(QtyField.Value);
						receiptLineCurrent = receiptEntry.transactions.Update(receiptLineCurrent);
					}

					//All the created splits will be deleted. New splits will be inserted later.
					var inserted = receiptEntry.splits.Cache.Inserted;
					foreach (POReceiptLineSplit split in inserted)
					{
						if (split.LineNbr == receiptLineCurrent.LineNbr)
							receiptEntry.splits.Delete(split);
					}
				}
			}
		}

		/// <summary>
		/// Adds all lines of a given order to the shipment.
		/// </summary>
		protected void Action_AddOrder(PXGraph graph, ActionImpl action)
		{
			SOShipmentEntry shipmentEntry = (SOShipmentEntry)graph;
			shipmentEntry.addsofilter.Current.OrderType = ((EntityValueField)action.Fields.Single(f => f.Name == "OrderType")).Value;
			shipmentEntry.addsofilter.Current.OrderNbr = ((EntityValueField)action.Fields.Single(f => f.Name == "OrderNbr")).Value;
			shipmentEntry.addsofilter.Update(shipmentEntry.addsofilter.Current);

			foreach (SOShipmentPlan line in shipmentEntry.soshipmentplan.Select())
			{
				line.Selected = true;
				shipmentEntry.soshipmentplan.Update(line);
			}

			shipmentEntry.addSO.Press();
		}

		/// <summary>
		/// Handles creation of document details in the Bills and Adjustments (AP301000) screen.
		/// Specifically, if PO Type and Number are specified, will add an appropriate reference to the order
		/// using the <see cref="APInvoiceEntry.addPOOrder">Add PO action</see>.
		/// </summary>
		[FieldsProcessed(new[] {
			"POOrderType",
			"POOrderNbr"
		})]
		protected virtual void BillDetail_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var invoiceEntry = (AP.APInvoiceEntry)graph;

			var orderType = targetEntity.Fields.SingleOrDefault(f => f.Name == "POOrderType") as EntityValueField;
			var orderNumber = targetEntity.Fields.SingleOrDefault(f => f.Name == "POOrderNbr") as EntityValueField;
			bool insertViaAddPO = orderType != null && orderNumber != null;

			if (!insertViaAddPO && (orderNumber != null || orderType != null))
			{
				throw new PXException("Both POOrderType and POOrderNumber must be provided to add a Purchase Order to details.");
			}

			var detailsCache = invoiceEntry.Transactions.Cache;

			if (insertViaAddPO)
			{
				var state = invoiceEntry.Transactions.Cache.GetStateExt<APTran.pOOrderType>(new APTran { }) as PXStringState;

				if (state != null && state.AllowedLabels.Contains(orderType.Value))
				{
					orderType.Value = state.ValueLabelDic.Single(p => p.Value == orderType.Value).Key;
				}

                var orderGraphExtension = invoiceEntry.GetExtension<PO.GraphExtensions.APInvoiceSmartPanel.AddPOOrderExtension>();

                var orders = orderGraphExtension.poorderslist.Select().AsEnumerable().Select(r => r.GetItem<POOrderRS>());
				var order = orders.FirstOrDefault(o => o.OrderType == orderType.Value && o.OrderNbr == orderNumber.Value);

				if (order == null)
				{
					throw new PXException($"Purchase order {orderType.Value} - {orderNumber.Value} not found.");
				}

				order.Selected = true;
                orderGraphExtension.poorderslist.Update(order);

                orderGraphExtension.addPOOrder2.Press();
			}
			else
			{
				var row = detailsCache.CreateInstance();
				row = detailsCache.Insert(row);
				detailsCache.Current = row;
			}
		}

		[FieldsProcessed(new[] {
			"Name",
			"Description",
			"Value"
		})]
		protected void CustomerPaymentMethodDetail_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var maint = (CustomerPaymentMethodMaint)graph;

			var name = (EntityValueField)targetEntity.Fields.SingleOrDefault(f => f.Name.OrdinalEquals("Name"));
			var description = (EntityValueField)targetEntity.Fields.SingleOrDefault(f => f.Name.OrdinalEquals("Description"));
			var value = (EntityValueField)targetEntity.Fields.Single(f => f.Name.OrdinalEquals("Value"));

			var cache = maint.Details.Cache;
			foreach (CustomerPaymentMethodDetail detail in maint.Details.Select())
			{
				var selectorRow = PXSelectorAttribute.Select(cache, detail, "DetailID") as PaymentMethodDetail;
				if ((name != null && (selectorRow.Descr == name.Value || detail.DetailID == name.Value))
					|| (description != null && (selectorRow.Descr == description.Value || selectorRow.DetailID == description.Value)))
				{

					cache.SetValueExt(detail, "Value", value.Value);
					maint.Details.Update(detail);
					break;
				}
			}
		}

		protected void ItemWarehouse_SetProductManagerFields(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var productManagerValueField = (EntityValueField)targetEntity.Fields.SingleOrDefault(f => f.Name.OrdinalEquals("ProductManager"));
			var productWorkgroupValueField = (EntityValueField)targetEntity.Fields.SingleOrDefault(f => f.Name.OrdinalEquals("ProductWorkgroup"));

			if (productManagerValueField == null && productWorkgroupValueField == null) return;

			var maint = (INItemSiteMaint)graph;

			var inventoryCD = entity.InternalKeys[nameof(maint.itemsiterecord)][nameof(INItemSite.InventoryID)];
			var inventory = (InventoryItem)new PXSelectReadonly<InventoryItem,
				Where<InventoryItem.inventoryCD, Equal<Required<InventoryItem.inventoryCD>>>>(graph).Select(inventoryCD);

			var siteCD = entity.InternalKeys[nameof(maint.itemsiterecord)][nameof(INItemSite.SiteID)];
			var site = (INSite)new PXSelectReadonly<INSite,
				Where<INSite.siteCD, Equal<Required<INSite.siteCD>>>>(graph).Select(siteCD);

			INItemSite row = maint.itemsitesettings.Select(inventory.InventoryID, site.SiteID);

			bool productManagerValueExists = row.ProductManagerID != null;
			if (productManagerValueField != null)
				productManagerValueExists = !string.IsNullOrEmpty(productManagerValueField.Value);

			bool productWorkgroupValueExists = row.ProductWorkgroupID != null;
			if (productWorkgroupValueField != null)
				productWorkgroupValueExists = !string.IsNullOrEmpty(productWorkgroupValueField.Value);

			bool productManagerOverride = productManagerValueExists || productWorkgroupValueExists;

			maint.itemsitesettings.Cache.SetValueExt<INItemSite.productManagerOverride>(row, productManagerOverride);

			if (productManagerValueField != null)
				maint.itemsitesettings.Cache.SetValueExt<INItemSite.productManagerID>(row, productManagerValueField.Value);

			if (productWorkgroupValueField != null)
				maint.itemsitesettings.Cache.SetValueExt<INItemSite.productWorkgroupID>(row, productWorkgroupValueField.Value);

			maint.itemsitesettings.Update(row);
		}

		[FieldsProcessed(new[] {
			"ProductManager",
			"ProductWorkgroup"
			})]
		protected void ItemWarehouse_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
			=> ItemWarehouse_SetProductManagerFields(graph, entity, targetEntity);

		[FieldsProcessed(new[] {
			"ProductManager",
			"ProductWorkgroup"
			})]
		protected void ItemWarehouse_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
			=> ItemWarehouse_SetProductManagerFields(graph, entity, targetEntity);

		[FieldsProcessed(new[] {
			"ParentCategoryID",
			"Description"
		})]
		protected void ItemSalesCategory_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var maint = (INCategoryMaint)graph;

			maint.Folders.Current = maint.Folders.SelectSingle();
			maint.Actions["AddCategory"].Press();

			var parentCategoryId = entity.Fields.SingleOrDefault(f => f.Name.Equals("ParentCategoryID", StringComparison.OrdinalIgnoreCase)) as EntityValueField;
			var description = entity.Fields.SingleOrDefault(f => f.Name.Equals("Description", StringComparison.OrdinalIgnoreCase)) as EntityValueField;

			var item = maint.Folders.Cache.ActiveRow as INCategory;

			var cache = maint.Folders.Cache;

			if (parentCategoryId != null && !string.IsNullOrEmpty(parentCategoryId.Value))
			{
				cache.SetValueExt(item, "ParentID", int.Parse(parentCategoryId.Value));
			}

			if (description != null && !string.IsNullOrEmpty(description.Value))
			{
				cache.SetValueExt(item, "Description", description.Value);
			}

			maint.Folders.Cache.Current = item;
		}

		[FieldsProcessed(new[] {
			"TypeID",
			"Description",
			"WarehouseID"
		})]
		protected void PhysicalInventoryReview_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var maint = (INPIReview)graph;

			var typeId = targetEntity.Fields.SingleOrDefault(f => f.Name == "TypeID") as EntityValueField;
			var description = targetEntity.Fields.SingleOrDefault(f => f.Name == "Description") as EntityValueField;
			var warehouseid = targetEntity.Fields.SingleOrDefault(f => f.Name == "WarehouseID") as EntityValueField;


			var cache = maint.GeneratorSettings.Cache;
			cache.Clear();
			cache.Insert(new PIGeneratorSettings());

			var updateDic = new Dictionary<string, object>();
			if (typeId != null)
			{
				updateDic.Add("PIClassID", typeId.Value);
			}
			maint.ExecuteUpdate(maint.GeneratorSettings.View.Name, new Dictionary<string, object>(), updateDic);

			updateDic = new Dictionary<string, object>();
			if (description != null)
			{
				updateDic.Add("Descr", description.Value);
			}
			if (warehouseid != null)
			{
				updateDic.Add("SiteID", warehouseid.Value);
			}
			maint.ExecuteUpdate(maint.GeneratorSettings.View.Name, new Dictionary<string, object>(), updateDic);

			maint.GeneratorSettings.View.SetAnswer(null, WebDialogResult.OK);
			maint.Insert.Press();
		}


		protected void ItemSalesCategory_Delete(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var maint = (INCategoryMaint)graph;
			maint.Actions["DeleteCategory"].Press();
			maint.Actions["Save"].Press();
		}

		[FieldsProcessed(new[] {
			"Description",
			"ParentCategoryID"
		})]
		protected void ItemSalesCategory_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var maint = (INCategoryMaint)graph;

			var item = maint.Folders.Cache.Current as INCategory;

			var description = entity.Fields.SingleOrDefault(f => f.Name.Equals("Description", StringComparison.OrdinalIgnoreCase)) as EntityValueField;
			if (description != null && !string.IsNullOrEmpty(description.Value))
			{
				maint.Folders.Cache.SetValueExt<INCategory.description>(item, description.Value);
			}

			var parent = entity.Fields.SingleOrDefault(f => f.Name.Equals("ParentCategoryID", StringComparison.OrdinalIgnoreCase)) as EntityValueField;
			if (parent != null && !string.IsNullOrEmpty(parent.Value))
			{
				item.ParentID = int.Parse(parent.Value);
			}

			maint.Folders.Update(item);
		}

		protected INUnit GetINUnit(InventoryItemMaint maint, EntityValueField fromUnit, EntityValueField toUnit)
		{
			var conversions = maint.itemunits.Select().AsEnumerable();
			return conversions.Select(c => c[typeof(INUnit)] as INUnit)
				.FirstOrDefault(c => c != null
									 && (toUnit == null || string.IsNullOrEmpty(toUnit.Value) || string.Equals(c.ToUnit, toUnit.Value))
									 && string.Equals(c.FromUnit, fromUnit.Value));
		}

		[FieldsProcessed(new[] {
			"ToUOM",
			"FromUOM"
		})]
		protected void InventoryItemUOMConversion_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var maint = (InventoryItemMaint)graph;

			var fromUnit = targetEntity.Fields.SingleOrDefault(f => f.Name == "FromUOM") as EntityValueField;
			var toUnit = targetEntity.Fields.SingleOrDefault(f => f.Name == "ToUOM") as EntityValueField;

			var conversion = GetINUnit(maint, fromUnit, toUnit);
			if (conversion == null)
			{
				conversion = maint.itemunits.Insert(new INUnit()
				{
					ToUnit = toUnit != null && !string.IsNullOrEmpty(toUnit.Value) ? toUnit.Value : null,
					FromUnit = fromUnit.Value
				});
			}

			maint.itemunits.Current = conversion;
		}


		protected void InventoryItemUOMConversion_Delete(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var maint = (InventoryItemMaint)graph;

			var fromUnit = targetEntity.Fields.SingleOrDefault(f => f.Name == "FromUOM") as EntityValueField;
			var toUnit = targetEntity.Fields.SingleOrDefault(f => f.Name == "ToUOM") as EntityValueField;

			var conversion = GetINUnit(maint, fromUnit, toUnit);
			if (conversion != null)
			{
				maint.itemunits.Delete(conversion);
				maint.Save.Press();
			}
		}

		[FieldsProcessed(new[] {
			"FreightAmount"
		})]
		protected void Shipment_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var freightPrice = targetEntity.Fields.SingleOrDefault(f => f.Name == "FreightAmount") as EntityValueField;
			if (freightPrice != null)
			{
				var shipmentEntry = (SOShipmentEntry)graph;
				var shipment = shipmentEntry.Document.Current;
				if (shipment.FreightAmountSource == CS.FreightAmountSourceAttribute.OrderBased)
				{
					throw new InvalidOperationException("Cannot assign freight price because the Invoice Freight Price Based On set to Order.");
				}

				shipmentEntry.Document.SetValueExt<SOShipment.overrideFreightAmount>(shipment, true);
				shipmentEntry.Document.SetValueExt<SOShipment.curyFreightAmt>(shipment, freightPrice.Value);
			}
		}

		[FieldsProcessed(new[] {
			"PrinterName",
			"Description",
			"IsActive"
		})]
		protected void Printer_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var printerName = targetEntity.Fields.SingleOrDefault(f => f.Name == "PrinterName") as EntityValueField;
			var description = targetEntity.Fields.SingleOrDefault(f => f.Name == "Description") as EntityValueField;
			var isActive = targetEntity.Fields.SingleOrDefault(f => f.Name == "IsActive") as EntityValueField;

			if (printerName != null && printerName.Value != null)
			{
				PX.SM.SMPrinterMaint newPrinterGraph = (PX.SM.SMPrinterMaint)PXGraph.CreateInstance(typeof(PX.SM.SMPrinterMaint));
				PX.SM.SMPrinter printer = new PX.SM.SMPrinter();
				printer.PrinterName = printerName.Value;
				if (description != null)
					printer.Description = description.Value;
				if (isActive != null)
					printer.IsActive = isActive.Value == "true";

				newPrinterGraph.Printers.Insert(printer);
				newPrinterGraph.Save.Press();
			}
		}

		[FieldsProcessed(new[] {
			"PrinterName",
			"Description",
			"IsActive"
		})]
		protected void Printer_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			PX.SM.SMPrinterMaint newPrinterGraph = (PX.SM.SMPrinterMaint)PXGraph.CreateInstance(typeof(PX.SM.SMPrinterMaint));
			var isActive = targetEntity.Fields.SingleOrDefault(f => f.Name == "IsActive") as EntityValueField;
			string printerName = entity.InternalKeys["Printers"]["PrinterName"];

			foreach (PX.SM.SMPrinter existingPrinter in newPrinterGraph.Printers.Select())
			{
				if (existingPrinter.PrinterName == printerName && isActive != null && isActive.Value != null)
				{
					existingPrinter.IsActive = isActive.Value == "true";
					newPrinterGraph.Printers.Update(existingPrinter);
				}
			}
			if (newPrinterGraph.Printers.Cache.IsDirty)
			{
				newPrinterGraph.Save.Press();
			}
		}

		[FieldsProcessed(new[] {
			"JobID",
			"Printer",
			"ReportID",
			"Status"
		})]
		protected void PrintJob_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			PX.SM.SMPrintJobMaint newPrintJobGraph = (PX.SM.SMPrintJobMaint)PXGraph.CreateInstance(typeof(PX.SM.SMPrintJobMaint));
			var status = targetEntity.Fields.SingleOrDefault(f => f.Name == "Status") as EntityValueField;
			int jobID;
			int.TryParse(entity.InternalKeys["Job"]["JobID"], out jobID);

			if (jobID != 0 && status != null && status.Value != null)
			{
				foreach (PX.SM.SMPrintJob existingPrintJob in PXSelect<PX.SM.SMPrintJob, Where<PX.SM.SMPrintJob.jobID, Equal<Required<PX.SM.SMPrintJob.jobID>>>>.Select(newPrintJobGraph, jobID))
				{
					existingPrintJob.Status = status.Value; //status is expected in char form - D, P, F or U
					newPrintJobGraph.Job.Update(existingPrintJob);
				}
				if (newPrintJobGraph.Job.Cache.IsDirty)
				{
					newPrintJobGraph.Save.Press();
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void Action_SalesOrderAddInvoice(PXGraph graph, ActionImpl action)
		{
			var orderEntry = (SOOrderEntry)graph;

			foreach (InvoiceSplits line in orderEntry.invoicesplits.Select())
			{
				line.Selected = true;
				orderEntry.invoicesplits.Update(line);
			}

			orderEntry.addInvoiceOK.Press();
		}

		/// <summary>
		/// 
		/// </summary>
		protected void Action_SalesInvoiceAddOrder(PXGraph graph, ActionImpl action)
		{
			var invoiceEntry = (SOInvoiceEntry)graph;
			string orderNbr = ((EntityValueField)action.Fields.Single(f => f.Name == "OrderNbr")).Value;
			string orderType = ((EntityValueField)action.Fields.Single(f => f.Name == "OrderType")).Value;
			string shipmentNbr = ((EntityValueField)action.Fields.Single(f => f.Name == "ShipmentNbr")).Value;

			foreach (SOOrderShipment line in invoiceEntry.shipmentlist.Select().Select<SOOrderShipment>().Where(_ =>
																				_.OrderType == orderType &&
																				_.OrderNbr == orderNbr &&
																				_.ShipmentNbr == shipmentNbr))
			{
				line.Selected = true;
				invoiceEntry.shipmentlist.Update(line);
			}

			invoiceEntry.addShipment.Press();
		}

		/// <summary>
		/// 
		/// </summary>
		protected void Action_SalesOrderAddStockItem(PXGraph graph, ActionImpl action)
		{
			var orderEntry = (SOOrderEntry)graph;

			foreach (SOSiteStatusSelected line in orderEntry.sitestatus.Select())
			{
				line.Selected = true;
				orderEntry.sitestatus.Update(line);
			}

			orderEntry.addInvSelBySite.Press();
			orderEntry.sitestatusfilter.Cache.Clear();
		}

		/// <summary>
		/// 
		/// </summary>
		protected void Action_ShipmentAddOrder(PXGraph graph, ActionImpl action)
		{
			var shipmentEntry = (SOShipmentEntry)graph;

			foreach (SOShipmentPlan line in shipmentEntry.soshipmentplan.Select())
			{
				line.Selected = true;
				shipmentEntry.soshipmentplan.Update(line);
			}

			shipmentEntry.addSO.Press();
		}

		/// <summary>
		/// 
		/// </summary>
		protected void Action_PaymentLoadDocuments(PXGraph graph, ActionImpl action)
		{
			var paymentEntry = (ARPaymentEntry)graph;

			paymentEntry.LoadInvoicesProc(false, paymentEntry.loadOpts.Current);
		}

		/// <summary>
		/// 
		/// </summary>
		protected void Action_PaymentLoadOrders(PXGraph graph, ActionImpl action)
		{
			var paymentEntry = (ARPaymentEntry)graph;

			paymentEntry.LoadOrdersProc(false, paymentEntry.loadOpts.Current);
		}

	}
}
