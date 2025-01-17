﻿using PX.Data;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.CT;
using PX.Objects.EP;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.PO;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using PX.Data.BQL;

namespace PX.Objects.FS
{
    #region Lookups Group - BusinessAccount (Customer, Prospect, Vendor and Combined)

    #region BusinessAccount - Base
    public class FSSelectorBusinessAccount_BaseAttribute : PXDimensionSelectorAttribute
    {
        public FSSelectorBusinessAccount_BaseAttribute(string dimensionName, Type whereType)
            : base(
                dimensionName,
                BqlCommand.Compose(
                            typeof(Search2<,,>),
                            typeof(BAccountSelectorBase.bAccountID),
                            typeof(LeftJoin<,,>),
                            typeof(Contact),
                            typeof(On<
                                    Contact.bAccountID, Equal<BAccountSelectorBase.bAccountID>,
                                    And<Contact.contactID, Equal<BAccountSelectorBase.defContactID>>>),
                            typeof(LeftJoin<,,>),
                            typeof(Address),
                            typeof(On<
                                    Address.bAccountID, Equal<BAccountSelectorBase.bAccountID>,
                                    And<Address.addressID, Equal<BAccountSelectorBase.defAddressID>>>),
                            typeof(LeftJoin<,>),
                            typeof(Customer),
                            typeof(On<
                                    Customer.bAccountID, Equal<BAccountSelectorBase.bAccountID>>),
                            typeof(Where<>),
                            whereType),
                typeof(BAccountSelectorBase.acctCD),
                new Type[]
                {
                    typeof(BAccountSelectorBase.acctCD), 
                    typeof(BAccountSelectorBase.acctName), 
                    typeof(BAccountSelectorBase.type), 
                    typeof(Customer.customerClassID),
                    typeof(BAccountSelectorBase.status),
                    typeof(Contact.phone1),
                    typeof(Address.addressLine1), 
                    typeof(Address.addressLine2), 
                    typeof(Address.postalCode),
                    typeof(Address.city),
                    typeof(Address.countryID)
                })
        {
            DescriptionField = typeof(BAccountSelectorBase.acctName);
        }
    }
    #endregion

    #region BusinessAccount - Customer
    public class FSSelectorBAccountTypeCustomerOrCombinedAttribute : FSSelectorBusinessAccount_BaseAttribute
    {
        public FSSelectorBAccountTypeCustomerOrCombinedAttribute()
            : base(
                CustomerAttribute.DimensionName,
                typeof(
                    Where<BAccountSelectorBase.type, Equal<BAccountType.customerType>,
                    Or<BAccountSelectorBase.type, Equal<BAccountType.combinedType>>>))
        {
        }
    }
    #endregion

    #region BusinessAccount - Vendor
    public class FSSelectorBusinessAccount_VEAttribute : FSSelectorBusinessAccount_BaseAttribute
    {
        public FSSelectorBusinessAccount_VEAttribute()
            : base(VendorAttribute.DimensionName, typeof(Where<BAccountSelectorBase.type, Equal<BAccountType.vendorType>>))
        {
        }
    }
    #endregion

    #region BusinessAccount - Customer, Prospect and Combined
    public class FSSelectorBusinessAccount_CU_PR_VCAttribute : FSSelectorBusinessAccount_BaseAttribute
    {
        public FSSelectorBusinessAccount_CU_PR_VCAttribute()
            : base(
                CustomerAttribute.DimensionName, 
                typeof(
                    Where<BAccountSelectorBase.type, Equal<BAccountType.customerType>,
                    Or<BAccountSelectorBase.type, Equal<BAccountType.prospectType>,
                    Or<BAccountSelectorBase.type, Equal<BAccountType.combinedType>>>>))
        {
        }
    }
    #endregion

    #region Customer
    public class FSSelectorCustomerAttribute : PXDimensionSelectorAttribute
    {
        public FSSelectorCustomerAttribute()
            : base(
                CustomerAttribute.DimensionName,
                typeof(Search2<FSCustomer.bAccountID,
                    LeftJoin<Contact,
                        On<
                            Contact.bAccountID, Equal<FSCustomer.bAccountID>,
                            And<Contact.contactID, Equal<FSCustomer.defContactID>>>,
                    LeftJoin<Address,
                        On<
                            Address.bAccountID, Equal<FSCustomer.bAccountID>,
                            And<Address.addressID, Equal<FSCustomer.defAddressID>>>>>,
                    Where<
                        FSCustomer.type, Equal<BAccountType.customerType>,
                        Or<FSCustomer.type, Equal<BAccountType.combinedType>>>>),
                typeof(FSCustomer.acctCD),
                new Type[]
                {
                    typeof(FSCustomer.acctCD),
                    typeof(FSCustomer.status),
                    typeof(FSCustomer.acctName),
                    typeof(FSCustomer.classID),                
                    typeof(Contact.phone1),
                    typeof(Address.city),
                    typeof(Address.countryID)
                })
        {
            DescriptionField = typeof(FSCustomer.acctName);
            DirtyRead = true;
        }
    }
    #endregion

    #region Contract Schedule Customer
    public class FSSelectorContractScheduleCustomerAttribute : PXDimensionSelectorAttribute
    {
        public FSSelectorContractScheduleCustomerAttribute(Type whereType)
            : base(
                CustomerAttribute.DimensionName,
                BqlCommand.Compose(
                            typeof(Search5<,,,>),
                            typeof(BAccountSelectorBase.bAccountID),
                            typeof(InnerJoin<,,>),
                            typeof(FSServiceContract),
                            typeof(On<
                                    FSServiceContract.customerID, Equal<BAccountSelectorBase.bAccountID>>),
                            typeof(LeftJoin<,,>),
                            typeof(Contact),
                            typeof(On<
                                    Contact.bAccountID, Equal<BAccountSelectorBase.bAccountID>,
                                    And<Contact.contactID, Equal<BAccountSelectorBase.defContactID>>>),
                            typeof(LeftJoin<,,>),
                            typeof(Address),
                            typeof(On<
                                    Address.bAccountID, Equal<BAccountSelectorBase.bAccountID>,
                                    And<Address.addressID, Equal<BAccountSelectorBase.defAddressID>>>),
                            typeof(LeftJoin<,>),
                            typeof(Customer),
                            typeof(On<
                                    Customer.bAccountID, Equal<BAccountSelectorBase.bAccountID>>),
                            typeof(Where<>),
                            whereType,
                            typeof(Aggregate<>),
                            typeof(GroupBy<BAccountSelectorBase.bAccountID>)),
                typeof(BAccountSelectorBase.acctCD),
                new Type[]
                {
                    typeof(BAccountSelectorBase.acctCD),
                    typeof(BAccountSelectorBase.acctName),
                    typeof(BAccountSelectorBase.type),
                    typeof(Customer.customerClassID),
                    typeof(BAccountSelectorBase.status),
                    typeof(Contact.phone1),
                    typeof(Address.addressLine1),
                    typeof(Address.addressLine2),
                    typeof(Address.postalCode),
                    typeof(Address.city),
                    typeof(Address.countryID)
                })
        {
            DescriptionField = typeof(BAccountSelectorBase.acctName);
        }
    }
    #endregion

    #endregion

    #region Contact
    public class FSSelectorContactAttribute : PXSelectorAttribute
    {
        public FSSelectorContactAttribute()
            : base(
                typeof(Search2<Contact.contactID,
                    InnerJoin<BAccount, 
                        On<
                            BAccount.bAccountID, Equal<Contact.bAccountID>>>,
                Where<
                    Contact.contactType, NotEqual<ContactTypesAttribute.bAccountProperty>,
                    And<
                        Where2<
                            Where<
                                BAccount.type, Equal<BAccountType.customerType>,
                                Or<BAccount.type, Equal<BAccountType.prospectType>,
                                Or<BAccount.type, Equal<BAccountType.combinedType>>>>,
                            And<
                                Where<
                                    BAccount.bAccountID, Equal<Current<FSServiceOrder.customerID>>,
                                    Or<Current<FSServiceOrder.customerID>, IsNull>>>>>>>))
        {
            SubstituteKey = typeof(Contact.displayName);
            Filterable = true;
            DirtyRead = true;
        }
    }

    public class FSSelectorAttendeeContact : PXSelectorAttribute
    {
        public FSSelectorAttendeeContact()
            : base(
                typeof(Search<Contact.contactID,
                Where<
                    Contact.contactType, NotEqual<ContactTypesAttribute.bAccountProperty>,
                    And<Contact.bAccountID, Equal<Current<FSSelectorHelper.mem_int>>>>>))
        {
            SubstituteKey = typeof(Contact.displayName);
            DescriptionField = typeof(Contact.displayName);
            Filterable = true;
            DirtyRead = true;
        }
    }

    #endregion

    #region Contract
    public class FSSelectorContractAttribute : PXSelectorAttribute
    {
        public FSSelectorContractAttribute()
            : base(
                typeof(Search<Contract.contractID,
                    Where<
                        Contract.baseType, Equal<CTPRType.contract>,
                        And<
                            Where<
                                Current<FSServiceOrder.customerID>, IsNull,
                                Or<
                                    Contract.customerID, Equal<Current<FSServiceOrder.customerID>>,
                                    And<
                                        Where<
                                              Current<FSServiceOrder.locationID>, IsNull,
                                              Or<Contract.locationID, Equal<Current<FSServiceOrder.locationID>>>>>>>>>,
                OrderBy<
                    Desc<Contract.contractCD>>>))
        {
            SubstituteKey = typeof(Contract.contractCD);
            Filterable = true;
        }
    }

    public class FSSelectorContractRefNbrAttributeAttribute : PXSelectorAttribute
    {
        public FSSelectorContractRefNbrAttributeAttribute(Type serviceContract_RecordType)
            : this(serviceContract_RecordType, typeof(Where<True, Equal<True>>))
        {
        }

        public FSSelectorContractRefNbrAttributeAttribute(Type serviceContract_RecordType, Type Where)
            : base(
                    BqlCommand.Compose(
                                        typeof(Search2<,,,>),
                                            typeof(FSServiceContract.refNbr),
                                        typeof(LeftJoin<,,>),
                                            typeof(Customer),
                                        typeof(On<
                                                Customer.bAccountID, Equal<FSServiceContract.customerID>>),
                                        typeof(LeftJoin<,>),
                                        typeof(Address),
                                        typeof(On<
                                                Address.bAccountID, Equal<Customer.bAccountID>,
                                                And<Address.addressID, Equal<Customer.defAddressID>>>),
                                        typeof(Where<,,>),
                                            typeof(FSServiceContract.recordType),
                                        typeof(Equal<>),
                                            serviceContract_RecordType,
                                        typeof(And<>),
                                            Where,
                                        typeof(OrderBy<Desc<FSServiceContract.refNbr>>)),
                new Type[]
                {
                    typeof(FSServiceContract.refNbr),
                    typeof(FSServiceContract.customerContractNbr),
                    typeof(FSServiceContract.customerID),
                    typeof(Customer.acctName), 
                    typeof(FSServiceContract.status), 
                    typeof(FSServiceContract.customerLocationID),
                    typeof(Address.addressLine1),
                    typeof(Address.city),
                    typeof(Address.state)
                })
        {
        }
    }

    public class FSSelectorCustomerContractNbrAttributeAttribute : PXSelectorAttribute
    {
        public FSSelectorCustomerContractNbrAttributeAttribute(Type serviceContract_RecordType, Type CurrentCustomer)
            : this(serviceContract_RecordType, CurrentCustomer, typeof(Where<True, Equal<True>>))
        {
        }

        public FSSelectorCustomerContractNbrAttributeAttribute(Type serviceContract_RecordType, Type CurrentCustomer, Type Where)
            : base(
                    BqlCommand.Compose(
                                        typeof(Search2<,,,>),
                                            typeof(FSServiceContract.customerContractNbr),
                                        typeof(LeftJoin<,,>),
                                            typeof(Customer),
                                        typeof(On<
                                                Customer.bAccountID, Equal<FSServiceContract.customerID>>),
                                        typeof(LeftJoin<,>),
                                        typeof(Address),
                                        typeof(On<
                                                Address.bAccountID, Equal<Customer.bAccountID>,
                                                And<Address.addressID, Equal<Customer.defAddressID>>>),
                                        typeof(Where<,,>),
                                            typeof(FSServiceContract.customerID),
                                            typeof(Equal<>),
                                            typeof(Current<>),
                                            CurrentCustomer,
                                        typeof(And<,,>),
                                            typeof(FSServiceContract.recordType),
                                        typeof(Equal<>),
                                            serviceContract_RecordType,
                                        typeof(And<>),
                                            Where,
                                        typeof(OrderBy<Desc<FSServiceContract.customerContractNbr>>)),
                new Type[]
                {
                    typeof(FSServiceContract.refNbr),
                    typeof(FSServiceContract.customerID),
                    typeof(Customer.acctName),
                    typeof(FSServiceContract.status),
                    typeof(FSServiceContract.customerLocationID),
                    typeof(Address.addressLine1),
                    typeof(Address.city),
                    typeof(Address.state)
                })
        {
        }
    }


    public class FSSelectorPrepaidServiceContract : PXSelectorAttribute
    {
        public FSSelectorPrepaidServiceContract(Type currentCustomerID, Type currentBillingCustomerID)
            : base(
                  BqlCommand.Compose(
                      typeof(Search<,,>),
                        typeof(FSServiceContract.serviceContractID),
                        typeof(Where2<,>),
                            typeof(Where<,,>),
                                typeof(FSServiceContract.customerID),
                                typeof(Equal<>),
                                    typeof(Current<>),
                                        currentCustomerID,
                                typeof(And<>),
                                    typeof(Where<,>),
                                        typeof(FSServiceContract.billCustomerID),
                                        typeof(Equal<>),
                                            typeof(Current<>),
                                                currentBillingCustomerID,
                            typeof(And<
                                Where<FSServiceContract.status, Equal<FSServiceContract.status.Active>,
                                And<FSServiceContract.billingType, Equal<FSServiceContract.billingType.StandardizedBillings>>>>),
                            typeof(OrderBy<Asc<FSServiceContract.refNbr>>)),
                  new Type[]
                {
                    typeof(FSServiceContract.customerID),
                    typeof(FSServiceContract.customerLocationID),
                    typeof(FSServiceContract.refNbr),
                    typeof(FSServiceContract.customerContractNbr),
                    typeof(FSServiceContract.status),
                    typeof(FSServiceContract.vendorID),
                    typeof(FSServiceContract.sourcePrice),
                    typeof(FSServiceContract.billCustomerID),
                    typeof(FSServiceContract.billLocationID),
                    typeof(FSServiceContract.docDesc),
                })
        {
            SubstituteKey = typeof(FSServiceContract.refNbr);
            DescriptionField = typeof(FSServiceContract.docDesc);
        }
    }
    #endregion

    #region Contract - BillingPeriod

    public class FSSelectorContractBillingPeriodAttribute : PXCustomSelectorAttribute
    {
        public FSSelectorContractBillingPeriodAttribute()
            : base(typeof(FSContractPeriod.contractPeriodID), typeof(FSContractPeriod.billingPeriod), typeof(FSContractPeriod.status), typeof(FSContractPeriod.invoiced))
        {
            this.SubstituteKey = typeof(FSContractPeriod.billingPeriod);
            this.ValidateValue = false;
        }

        protected virtual IEnumerable GetRecords()
        {
            FSContractPeriod currentRow = null;
            PXCache cache = this._Graph.Caches[typeof(FSContractPeriod)];

            foreach (object item in PXView.Currents)
            {
                if (item != null && (item.GetType() == typeof(FSContractPeriod)))
                {
                    currentRow = (FSContractPeriod)item;
                    break;
                }
            }

            if (currentRow == null)
            {
                currentRow = (FSContractPeriod)cache.Current;
            }

            if (currentRow != null)
            {
                if (currentRow.ContractPeriodID > 0)
                {
                    var rows = PXSelect<FSContractPeriod,
                                Where<FSContractPeriod.serviceContractID, Equal<Current<FSServiceContract.serviceContractID>>>,
                                OrderBy<Desc<FSContractPeriod.endPeriodDate>>>
                               .Select(_Graph);

                    string actionType = ID.ContractPeriod_Actions.SEARCH_BILLING_PERIOD;
                    FSContractPeriodFilter fsContractPeriodFilterRow = (FSContractPeriodFilter)_Graph.Caches[typeof(FSContractPeriodFilter)].Current;

                    if (fsContractPeriodFilterRow != null)
                    {
                        actionType = fsContractPeriodFilterRow.Actions;
                    }

                    if (rows.Count > 0)
                    {
                        foreach (FSContractPeriod row in rows)
                        {
                            FSContractPeriod fsContractPeriodRow = (FSContractPeriod)row;
                            if (fsContractPeriodRow.StartPeriodDate.HasValue
                                    && fsContractPeriodRow.EndPeriodDate.HasValue)
                            {
                                fsContractPeriodRow.BillingPeriod = fsContractPeriodRow.StartPeriodDate.Value.ToString("MM/dd/yyyy") + " - " + fsContractPeriodRow.EndPeriodDate.Value.ToString("MM/dd/yyyy");
                            }

                            if (actionType == ID.ContractPeriod_Actions.MODIFY_UPCOMING_BILLING_PERIOD
                                    && fsContractPeriodRow.Status == ID.Status_ContractPeriod.INACTIVE)
                            {
                                yield return fsContractPeriodRow;
                            }
                            else if (actionType == ID.ContractPeriod_Actions.SEARCH_BILLING_PERIOD
                                        && (fsContractPeriodRow.Status != ID.Status_ContractPeriod.INACTIVE
                                                || fsContractPeriodRow.Invoiced == true))
                            {
                                yield return fsContractPeriodRow;
                            }
                        }
                    }
                }
                else
                {
                    if (currentRow.StartPeriodDate.HasValue
                                    && currentRow.EndPeriodDate.HasValue)
                    {
                        currentRow.BillingPeriod = currentRow.StartPeriodDate.Value.ToString("MM/dd/yyyy") + " - " + currentRow.EndPeriodDate.Value.ToString("MM/dd/yyyy");
                    }
                    yield return currentRow;
                }
            }
        }
    }
    #endregion

    #region ContractPeriod - Equipment for Maintenance
    public class FSSelectorContractPeriodEquipmentAttribute : PXSelectorAttribute
    {
        public FSSelectorContractPeriodEquipmentAttribute()
            : base(
                typeof(
                        Search2<FSEquipment.SMequipmentID,
                        CrossJoinSingleTable<FSSetup>,
                        Where<
                            FSEquipment.requireMaintenance, Equal<True>,
                            And<FSSetup.enableAllTargetEquipment, Equal<True>,
                            Or<
                                Where2<
                                    /*Begin of Owner is Third Party*/
                                    Where<
                                        FSEquipment.ownerType, Equal<ListField_OwnerType_Equipment.Customer>,
                                        And<
                                            Where2<
                                                /*Begin of Owner is Customer*/
                                                Where<
                                                    FSEquipment.ownerID, Equal<Current<FSServiceContract.customerID>>>,
                                                /*End of Owner is Customer*/
                                                Or<
                                                    /*Begin of Location is Customer*/
                                                    Where<
                                                        FSEquipment.locationType, Equal<ListField_LocationType.Customer>,
                                                        And<
                                                            FSEquipment.customerID, Equal<Current<FSServiceContract.customerID>>,
                                                        And<
                                                            Where2<
                                                                Where<
                                                                    FSEquipment.customerLocationID, Equal<Current<FSServiceContract.customerLocationID>>>,
                                                                Or<
                                                                    Where<
                                                                        FSEquipment.customerLocationID, IsNull>>>>>>>>>>,
                                    /*End of Location is Customer*/
                                    /*End of Owner is Third Party and Location is Customer*/
                                    Or<
                                        /*Begin of Owner is Own and Location is Customer*/
                                        Where<
                                            FSEquipment.ownerType, Equal<ListField_OwnerType_Equipment.OwnCompany>,
                                            And<
                                                FSEquipment.locationType, Equal<ListField_LocationType.Customer>,
                                            And<
                                                FSEquipment.customerID, Equal<Current<FSServiceContract.customerID>>,
                                            And<
                                                Where2<
                                                    Where<
                                                        FSEquipment.customerLocationID, Equal<Current<FSServiceContract.customerLocationID>>>,
                                                    Or<
                                                        Where<
                                                            FSEquipment.customerLocationID, IsNull>>>>>>>>>>>>>),
                    /*End for Owner Company and Location is Customer*/
                    SelectorBase_Equipment.selectorColumns)
        {
            SubstituteKey = typeof(FSEquipment.refNbr);
            DescriptionField = typeof(FSEquipment.descr);
        }
    }
    #endregion

    #region Lookups Group - AccountLocation

    public class FSSelectorLocationAttribute : PXDimensionSelectorAttribute
    {
        public FSSelectorLocationAttribute()
            : base(
                LocationIDBaseAttribute.DimensionName,
                typeof(Search<Location.locationID>),
                typeof(Location.locationCD))
        {
            DescriptionField = typeof(Location.descr);
            DirtyRead = true;
        }

        public FSSelectorLocationAttribute(Type currentBAccountID)
            : base(
                LocationIDBaseAttribute.DimensionName,
                BqlCommand.Compose(
                                   typeof(Search<,>),
                                        typeof(Location.locationID),
                                   typeof(Where<,>),
                                        typeof(Location.bAccountID),
                                   typeof(Equal<>),
                                   typeof(Current<>),
                                    currentBAccountID),
                typeof(Location.locationCD))
        {
            DescriptionField = typeof(Location.descr);
            DirtyRead = true;
        }
    }

    #endregion

    #region Lookups Group - Service

    #region ServiceAttribute
    [PXDBInt]
    [PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.Visible)]
    public class ServiceAttribute : FSInventoryAttribute
    {
        private static Type[] defaultHeaders = new Type[]
        {
            typeof(InventoryItem.inventoryCD), 
            typeof(InventoryItem.itemClassID),
            typeof(FSxServiceClass.mem_RouteService),
            typeof(InventoryItem.itemStatus),
            typeof(InventoryItem.descr), 
            typeof(InventoryItem.itemType),
            typeof(InventoryItem.baseUnit),
            typeof(InventoryItem.salesUnit),
            typeof(InventoryItem.purchaseUnit),
            typeof(InventoryItem.basePrice),
            typeof(FSxService.actionType)
        };

        public ServiceAttribute(Type[] headers = null)
            : this(typeof(Where<True, Equal<True>>), headers)
        {
        }
        
        public ServiceAttribute(Type whereType, Type[] headers = null)
            : base(
                    BqlCommand.Compose(
                            typeof(Search2<,,>),
                            typeof(InventoryItem.inventoryID),
                            typeof(InnerJoin<,>),
                            typeof(INItemClass),
                            typeof(On<INItemClass.itemClassID, Equal<InventoryItem.itemClassID>>),
                            typeof(Where2<,>),
                            typeof(Match<Current<AccessInfo.userName>>),
                            typeof(And2<,>),
                            typeof(Where<
                                        InventoryItem.itemType, Equal<INItemTypes.serviceItem>,
                                        And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.inactive>,
                                        And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.markedForDeletion>,
                                        And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.noSales>>>>>),
                            typeof(And<>),
                            whereType),
                    typeof(InventoryItem.inventoryCD),
                    typeof(InventoryItem.descr),
                    headers ?? defaultHeaders)
        {
        }
    }
    #endregion

    #region InventoryIDByLineTypeAttribute
    [PXDBInt]
    [PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.Visible)]
    [PXRestrictor(typeof(Where<
                            InventoryItem.itemStatus, NotEqual<InventoryItemStatus.inactive>,
                            And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.markedForDeletion>,
                            And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.noSales>>>>),
                    PX.Objects.IN.Messages.InventoryItemIsInStatus, typeof(InventoryItem.itemStatus))]
    [PXRestrictor(typeof(Where<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.unknown>>), PM.Messages.ReservedForProject)]
    public class InventoryIDByLineTypeAttribute : FSInventoryAttribute
    {
        private static Type[] defaultHeaders = new Type[]
        {
            typeof(InventoryItem.inventoryCD),
            typeof(InventoryItem.itemClassID),
            typeof(FSxServiceClass.mem_RouteService),
            typeof(InventoryItem.itemStatus),
            typeof(InventoryItem.descr),
            typeof(InventoryItem.itemType),
            typeof(InventoryItem.baseUnit),
            typeof(InventoryItem.salesUnit),
            typeof(InventoryItem.purchaseUnit),
            typeof(InventoryItem.basePrice),
            typeof(FSxService.actionType)
        };

        public InventoryIDByLineTypeAttribute(Type lineType, Type[] headers = null)
            : this(typeof(Where<True, Equal<True>>), lineType, headers)
        {
        }

        public InventoryIDByLineTypeAttribute(Type whereType, Type lineType, Type[] headers = null)
            : base(
                    BqlCommand.Compose(
                            typeof(Search2<,,>),
                                typeof(InventoryItem.inventoryID),
                            typeof(LeftJoin<,>),
                                typeof(INItemClass),
                                    typeof(On<INItemClass.itemClassID, Equal<InventoryItem.itemClassID>>),
                            typeof(Where2<,>),
                                typeof(Match<Current<AccessInfo.userName>>),
                                typeof(And<>),
                                    typeof(Where2<,>),
                                        typeof(Where2<,>),
                                            typeof(Where<,,>),
                                                typeof(Current<>),
                                                    lineType,
                                                typeof(Equal<ListField_LineType_ALL.Inventory_Item>),
                                                typeof(And<InventoryItem.stkItem, Equal<True>>),
                                            typeof(Or2<,>),
                                                typeof(Where<,,>),
                                                    typeof(Current<>),
                                                        lineType,
                                                    typeof(Equal<ListField_LineType_ALL.Service>),
                                                    typeof(And<InventoryItem.stkItem, Equal<False>, And<InventoryItem.itemType, Equal<INItemTypes.serviceItem>>>),
                                                typeof(Or<>),
                                                    typeof(Where<,,>),
                                                        typeof(Current<>),
                                                            lineType,
                                                        typeof(Equal<ListField_LineType_ALL.NonStockItem>),
                                                        typeof(And<InventoryItem.stkItem, Equal<False>, And<InventoryItem.itemType, NotEqual<INItemTypes.serviceItem>>>),
                                        typeof(And<>),
                                            whereType),
                    typeof(InventoryItem.inventoryCD),
                    typeof(InventoryItem.descr),
                    headers ?? defaultHeaders)
        {
        }
    }
    #endregion

    #region EquipmentModelItemAttribute
    [PXDBInt]
    [PXUIField(DisplayName = "Model Equipment", Visibility = PXUIVisibility.Visible)]
    public class EquipmentModelItemAttribute : FSInventoryAttribute
    {
        private static Type[] defaultHeaders = new Type[]
        {
            typeof(InventoryItem.inventoryCD), 
            typeof(InventoryItem.descr),
            typeof(InventoryItem.itemClassID),
            typeof(InventoryItem.itemType),
            typeof(InventoryItem.baseUnit), 
            typeof(InventoryItem.salesUnit),
            typeof(InventoryItem.basePrice)
        };

        public EquipmentModelItemAttribute(Type[] headers = null)
            : this(typeof(Where<True, Equal<True>>), headers)
        {
        }

        public EquipmentModelItemAttribute(Type whereType, Type[] headers = null)
            : base(
                    BqlCommand.Compose(
                            typeof(Search<,>),
                            typeof(InventoryItem.inventoryID),
                            typeof(Where<FSxEquipmentModel.eQEnabled, Equal<True>>)),
                    typeof(InventoryItem.inventoryCD),
                    typeof(InventoryItem.descr),
                    headers ?? defaultHeaders)
        {
        }
    }
    #endregion

    #region PickupDeliveryItemAttribute
    [PXDBInt]
    [PXUIField(DisplayName = "Pickup/Delivery Item ID", Visibility = PXUIVisibility.Visible)]
    public class PickupDeliveryItemAttribute : FSInventoryAttribute
    {
        private static Type[] defaultHeaders = new Type[]
        {
            typeof(InventoryItem.inventoryCD), 
            typeof(InventoryItem.descr),
            typeof(InventoryItem.itemClassID),
            typeof(InventoryItem.itemType),
            typeof(InventoryItem.baseUnit), 
            typeof(InventoryItem.salesUnit),
            typeof(InventoryItem.basePrice)
        };

        public PickupDeliveryItemAttribute(Type[] headers = null)
            : this(typeof(Where<True, Equal<True>>), headers)
        {
        }

        public PickupDeliveryItemAttribute(Type whereType, Type[] headers = null)
            : base(
                    BqlCommand.Compose(
                            typeof(Search2<,,>),
                            typeof(InventoryItem.inventoryID),
                            typeof(InnerJoin<,>),
                            typeof(FSServiceInventoryItem),
                            typeof(On<FSServiceInventoryItem.inventoryID, Equal<InventoryItem.inventoryID>>),
                            typeof(Where<FSServiceInventoryItem.serviceID, Equal<Current<FSAppointmentInventoryItem.pickupDeliveryServiceID>>>)),
                    typeof(InventoryItem.inventoryCD),
                    typeof(InventoryItem.descr),
                    headers ?? defaultHeaders)
        {
        }
    }
    #endregion

    #region ContractSchedule_ServiceInventoryAttribute
    [PXDBInt]
    [PXUIField(DisplayName = "Service ID", Visibility = PXUIVisibility.Visible)]
    public class ContractSchedule_ServiceInventoryAttribute : FSInventoryAttribute
    {
        public ContractSchedule_ServiceInventoryAttribute()
            : base(
                typeof(Search2<InventoryItem.inventoryID,
                            LeftJoin<INItemClass,
                                On<INItemClass.itemClassID, Equal<InventoryItem.itemClassID>>,
                            InnerJoin<FSSrvOrdType,
                                On<FSSrvOrdType.srvOrdType, Equal<Current<FSSchedule.srvOrdType>>>>>,
                            Where2<
                                Where<
                                    Optional<FSScheduleDetService.lineType>, Equal<ListField_LineType_Service_ServiceContract.Service>,
                                    Or<Optional<FSScheduleDetService.lineType>, Equal<ListField_LineType_Service_ServiceContract.Service_Template>,
                                    Or<Optional<FSScheduleDetService.lineType>, Equal<ListField_LineType_Service_ServiceContract.Comment_Service>,
                                    Or<Optional<FSScheduleDetService.lineType>, Equal<ListField_LineType_Service_ServiceContract.Instruction_Service>,
                                    Or<Optional<FSScheduleDetService.lineType>, Equal<ListField_LineType_Service_ServiceContract.NonStockItem>>>>>>,
                                And<
                                    InventoryItem.stkItem, Equal<False>,                                    
                                    And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.inactive>,
                                    And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.markedForDeletion>,
                                    And<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.noSales>,
                                    And<
                                        Where2<
                                            Where<
                                                FSScheduleDetService.lineType.Service, NotEqual<Current<FSScheduleDetService.lineType>>,
                                                And<InventoryItem.itemType, NotEqual<INItemTypes.serviceItem>>>,
                                            Or<
                                                Where2<
                                                    Where<
                                                        FSxServiceClass.requireRoute, Equal<FSSrvOrdType.requireRoute>>,
                                                    And<
                                                        Where<
                                                            FSScheduleDetService.lineType.Service, Equal<Current<FSScheduleDetService.lineType>>,
                                                            And<InventoryItem.itemType, Equal<INItemTypes.serviceItem>>>>>>>>>>>>>>),
                typeof(InventoryItem.inventoryCD),
                typeof(InventoryItem.descr),
                new Type[]
                {
                    typeof(InventoryItem.inventoryCD), 
                    typeof(InventoryItem.itemClassID),
                    typeof(InventoryItem.itemStatus),
                    typeof(InventoryItem.descr), 
                    typeof(InventoryItem.itemType),
                    typeof(InventoryItem.baseUnit),
                    typeof(InventoryItem.salesUnit),
                    typeof(InventoryItem.purchaseUnit),
                    typeof(InventoryItem.basePrice),
                    typeof(FSxService.actionType)
                })
        {
        }
    }
    #endregion

    #endregion

    #region Lookups Group - Project and Task

    #region Project
    public class FSSelectorProjectAttribute : PXSelectorAttribute
    {
        public FSSelectorProjectAttribute()
            : base(
                typeof(Search<Contract.contractID,
                Where<
                    Contract.baseType, Equal<CTPRType.project>,
                    And<Contract.nonProject, Equal<False>>>>))
        {
            SubstituteKey = typeof(Contract.contractCD);
            DescriptionField = typeof(Contract.description);
        }
    }
    #endregion

    #region Task
    public class FSSelectorTaskAttribute : PXSelectorAttribute
    {
        public FSSelectorTaskAttribute()
            : base(
                typeof(Search<PMTask.taskID, 
                Where<
                    PMTask.projectID, Equal<Current<FSSODet.projectID>>>>))
        {
            SubstituteKey = typeof(PMTask.taskCD);
            DescriptionField = typeof(PMTask.description);
        }
    }

    public class FSSelectorActive_AR_SO_ProjectTaskAttribute : PXSelectorAttribute
    {
        public FSSelectorActive_AR_SO_ProjectTaskAttribute(Type whereType)
            : base(
                    BqlCommand.Compose(
                            typeof(Search2<,,>),
                            typeof(PMTask.taskID),
                            typeof(InnerJoin<,>),
                            typeof(FSSrvOrdType),
                            typeof(On<FSSrvOrdType.srvOrdType, Equal<Current<FSServiceOrder.srvOrdType>>>),
                            typeof(Where2<,>),
                            whereType,
                            typeof(And<PMTask.isActive, Equal<True>,
                                   And2<
                                       Where<FSSrvOrdType.enableINPosting, Equal<False>, Or<PMTask.visibleInIN, Equal<True>>>,
                                   And<
                                        Where2<
                                            Where<
                                                FSSrvOrdType.postTo, Equal<ListField_PostTo_SrvOrdType.None>>,
                                            Or<
                                                Where2<
                                                    Where<
                                                        FSSrvOrdType.postTo, Equal<ListField_PostTo_SrvOrdType.Accounts_Receivable_Module>,
                                                        And<
                                                            Where<
                                                                PMTask.visibleInAR, Equal<True>>>>,
                                                Or<
                                                    Where2<
                                                        Where<
                                                            FSSrvOrdType.postTo, Equal<ListField_PostTo_SrvOrdType.Sales_Order_Module>,
                                                                Or<FSSrvOrdType.postTo, Equal<ListField_PostTo_SrvOrdType.Sales_Order_Invoice>>>,
                                                        And<
                                                            Where<
                                                                PMTask.visibleInSO, Equal<True>>>>>>>>>>>)),
                typeof(PMTask.taskCD),
                typeof(PMTask.description))
                {
                    SubstituteKey = typeof(PMTask.taskCD);
                    DescriptionField = typeof(PMTask.description);
                    DirtyRead = true;
                }
    }
    #endregion

    #endregion

    #region Staff Member

    public class FSSelector_StaffMember_ServiceOrderProjectIDAttribute : PXDimensionSelectorAttribute
    {
        public FSSelector_StaffMember_ServiceOrderProjectIDAttribute()
            : base(
                "BIZACCT",
                typeof(
                    Search2<BAccountStaffMember.bAccountID,
                    LeftJoin<Vendor, 
                        On<Vendor.bAccountID, Equal<BAccountStaffMember.bAccountID>,
                        And<Vendor.status, NotEqual<Vendor.status.inactive>>>,
                    LeftJoin<EPEmployee, 
                        On<EPEmployee.bAccountID, Equal<BAccountStaffMember.bAccountID>,
                        And<EPEmployee.status, NotEqual<EPEmployee.status.inactive>>>,
                    LeftJoin<PMProject, 
                        On<PMProject.contractID, Equal<Current<FSServiceOrder.projectID>>>,
                    LeftJoin<EPEmployeeContract, 
                        On<EPEmployeeContract.contractID, Equal<PMProject.contractID>,
                        And<EPEmployeeContract.employeeID, Equal<BAccountStaffMember.bAccountID>>>, 
                    LeftJoin<EPEmployeePosition, 
                        On<EPEmployeePosition.employeeID, Equal<EPEmployee.bAccountID>,
                        And<EPEmployeePosition.isActive, Equal<True>>>>>>>>,
                    Where<
						PMProject.isActive, Equal<True>,
                        And<PMProject.baseType, Equal<CT.CTPRType.project>,
                        And<
                            Where2<
                                Where<
                                    FSxVendor.sDEnabled, Equal<True>>,
                                Or<
                                    Where<
                                        FSxEPEmployee.sDEnabled, Equal<True>,
                                        And<
                                            Where<
                                                PMProject.restrictToEmployeeList, Equal<False>,
                                                Or<EPEmployeeContract.employeeID, IsNotNull>>>>>>>>>,
                    OrderBy<Asc<BAccountStaffMember.acctCD>>>),
                typeof(BAccountStaffMember.acctCD),
                new Type[]
                {
                    typeof(BAccountStaffMember.acctCD),
                    typeof(BAccountStaffMember.acctName),
                    typeof(BAccountStaffMember.type),
                    typeof(BAccountStaffMember.status),
                    typeof(EPEmployeePosition.positionID)
                })
        {
            DescriptionField = typeof(BAccountStaffMember.acctName);
        }
    }

    public class FSSelector_StaffMember_AllAttribute : PXDimensionSelectorAttribute
    {
        // The SubstituteKey parameter as AcctName is used to correct an issue in Employee-Room screen (SD-5617)
        public FSSelector_StaffMember_AllAttribute(Type parmSubstituteKey = null)
            : base(
                "BIZACCT",
                typeof(
                    Search2<BAccountStaffMember.bAccountID,
                    LeftJoin<Vendor, 
                        On<Vendor.bAccountID, Equal<BAccountStaffMember.bAccountID>,
                        And<Vendor.status, NotEqual<Vendor.status.inactive>>>,
                    LeftJoin<EPEmployee, 
                        On<EPEmployee.bAccountID, Equal<BAccountStaffMember.bAccountID>,
                        And<EPEmployee.status, NotEqual<EPEmployee.status.inactive>>>,
                    LeftJoin<EPEmployeePosition, 
                        On<EPEmployeePosition.employeeID, Equal<EPEmployee.bAccountID>,
                        And<EPEmployeePosition.isActive, Equal<True>>>>>>,                    
                    Where2<
                        Where<
                            FSxVendor.sDEnabled, Equal<True>,
                            And<
                                Where<
                                    Vendor.status, Equal<BAccountStaffMember.status.active>,
                                    Or<Vendor.status, Equal<BAccountStaffMember.status.oneTime>>>>>,
                        Or<
                            Where<
                                FSxEPEmployee.sDEnabled, Equal<True>>>>,
                    OrderBy<Asc<BAccountStaffMember.acctCD>>>),
                parmSubstituteKey ?? typeof(BAccountStaffMember.acctCD),
                new Type[]
                {
                    typeof(BAccountStaffMember.acctCD),
                    typeof(BAccountStaffMember.acctName),
                    typeof(BAccountStaffMember.type),
                    typeof(BAccountStaffMember.status),
                    typeof(EPEmployeePosition.positionID)
                })
        {
            DescriptionField = typeof(BAccountStaffMember.acctName);
        }            
    }

    public class FSSelector_StaffMember_Employee_OnlyAttribute : PXDimensionSelectorAttribute
    {
        public FSSelector_StaffMember_Employee_OnlyAttribute()
            : base(
                "BIZACCT",
                typeof(
                    Search2<BAccountStaffMember.bAccountID,
                    LeftJoin<EPEmployee,
                        On<EPEmployee.bAccountID, Equal<BAccountStaffMember.bAccountID>>,
                    LeftJoin<EPEmployeePosition,
                        On<EPEmployeePosition.employeeID, Equal<EPEmployee.bAccountID>,
                        And<EPEmployeePosition.isActive, Equal<True>>>>>,
                    Where<
                        FSxEPEmployee.sDEnabled, Equal<True>>,
                    OrderBy<Asc<BAccountStaffMember.acctCD>>>),
                    typeof(BAccountStaffMember.acctCD),
                new Type[]
                {
                    typeof(BAccountStaffMember.acctCD),
                    typeof(BAccountStaffMember.acctName),
                    typeof(BAccountStaffMember.type),
                    typeof(BAccountStaffMember.status),
                    typeof(EPEmployeePosition.positionID)
                })
        {
            DescriptionField = typeof(BAccountStaffMember.acctName);
        }
    }

    // Route Drivers
    public class FSSelector_Driver_AllAttribute : PXDimensionSelectorAttribute
    {
        public FSSelector_Driver_AllAttribute()
            : base(
                "BIZACCT",
                typeof(
                    Search<EPEmployee.bAccountID,
                    Where<
                        FSxEPEmployee.sDEnabled, Equal<True>,
                        And<EPEmployee.status, NotEqual<EPEmployee.status.inactive>,
                        And<FSxEPEmployee.isDriver, Equal<True>>>>,
                    OrderBy<Asc<EPEmployee.acctCD>>>),
                typeof(EPEmployee.acctCD),
                new Type[]
                {
                    typeof(EPEmployee.acctCD), 
                    typeof(EPEmployee.acctName), 
                    typeof(EPEmployee.status),
                    typeof(EPEmployee.departmentID)
                })
        {
            DescriptionField = typeof(EPEmployee.acctName);
        }
    }

    public class FSSelector_Driver_RouteDocumentRouteIDAttribute : PXSelectorWithCustomOrderByAttribute
    {
        public FSSelector_Driver_RouteDocumentRouteIDAttribute()
            : base(
                typeof(Search<EPEmployeeFSRouteEmployee.bAccountID,
                       Where<
                            EPEmployeeFSRouteEmployee.routeID, Equal<Current<FSRouteDocument.routeID>>>,
                       OrderBy<
                            Asc<EPEmployeeFSRouteEmployee.priorityPreference>>>),
                new Type[]
                {
                    typeof(EPEmployeeFSRouteEmployee.acctCD), 
                    typeof(EPEmployeeFSRouteEmployee.acctName), 
                    typeof(EPEmployeeFSRouteEmployee.priorityPreference),
                    typeof(EPEmployeeFSRouteEmployee.status),
                    typeof(EPEmployeeFSRouteEmployee.departmentID)
                })
        {
            SubstituteKey = typeof(EPEmployeeFSRouteEmployee.acctCD);
            DescriptionField = typeof(EPEmployeeFSRouteEmployee.acctName);
        }
    }

    //All Employees
    public class FSSelector_Employee_AllAttribute : PXDimensionSelectorAttribute
    {
        public FSSelector_Employee_AllAttribute()
            : base(
                "BIZACCT",
                typeof(
                    Search<EPEmployee.bAccountID,
                    Where<
                        FSxEPEmployee.sDEnabled, Equal<True>,
                        And<EPEmployee.status, NotEqual<EPEmployee.status.inactive>>>,
                    OrderBy<Asc<EPEmployee.acctCD>>>),
                typeof(EPEmployee.acctCD),
                new Type[]
                {
                    typeof(EPEmployee.acctCD), 
                    typeof(EPEmployee.acctName), 
                    typeof(EPEmployee.status),
                    typeof(EPEmployee.departmentID)
                })
        {
            DescriptionField = typeof(EPEmployee.acctName);
        }
    }

    #endregion

    #region State
    public class FSSelectorStateAttribute : PXSelectorAttribute
    {
        public FSSelectorStateAttribute()
            : base(
                typeof(Search<State.stateID,
                Where<State.countryID, Equal<Current<FSLicenseType.countryID>>>>))
        {
            DescriptionField = typeof(State.name);
        }
    }
    #endregion

    #region Lookups Group - License

    #region LicenseBusiness
    public class FSSelectorLicenseBusinessAttribute : PXSelectorAttribute
    {
        public FSSelectorLicenseBusinessAttribute()
            : base(
                typeof(Search2<FSLicense.licenseID, 
                    InnerJoin<FSLicenseType,
                        On<
                            FSLicenseType.licenseTypeID, Equal<FSLicense.licenseTypeID>>>,
                Where<
                    FSLicenseType.ownerType, Equal<FSLicenseType.ownerType.Business>>>))
        {
            DescriptionField = typeof(FSLicense.descr);
        }
    }
    #endregion

    #region LicenseEmployee
    public class FSSelectorLicenseEmployeeAttribute : PXSelectorAttribute
    {
        public FSSelectorLicenseEmployeeAttribute()
            : base(
                typeof(Search2<FSLicense.licenseID, 
                    InnerJoin<FSLicenseType, 
                        On<
                            FSLicenseType.licenseTypeID, Equal<FSLicense.licenseTypeID>>>,
                Where<
                    FSLicenseType.ownerType, Equal<FSLicenseType.ownerType.Employee>>>))
        {
            DescriptionField = typeof(FSLicense.descr);
        }
    }
    #endregion

    #endregion

    #region Lookups Group - ServiceOrder and ServiceOrderDetail

    #region SORefNbr
    public class FSSelectorSORefNbrAttribute : PXSelectorAttribute
    {
        public FSSelectorSORefNbrAttribute()
            : base(
                typeof(Search2<FSServiceOrder.refNbr,
                       LeftJoin<BAccountSelectorBase,
                            On<BAccountSelectorBase.bAccountID, Equal<FSServiceOrder.customerID>>,
                       LeftJoin<Location, 
                            On<Location.locationID, Equal<FSServiceOrder.locationID>>>>,
                       Where<
                            FSServiceOrder.srvOrdType, Equal<Optional<FSServiceOrder.srvOrdType>>>,
                       OrderBy<
                            Desc<FSServiceOrder.refNbr>>>),
                new Type[]
                {
                    typeof(FSServiceOrder.refNbr), 
                    typeof(FSServiceOrder.srvOrdType),
                    typeof(BAccountSelectorBase.type),
                    typeof(BAccountSelectorBase.acctCD),
                    typeof(BAccountSelectorBase.acctName),
                    typeof(Location.locationCD),
                    typeof(FSServiceOrder.status),
                    typeof(FSServiceOrder.priority), 
                    typeof(FSServiceOrder.severity),
                    typeof(FSServiceOrder.orderDate), 
                    typeof(FSServiceOrder.sLAETA), 
                    typeof(FSServiceOrder.promisedDate),
                    typeof(FSServiceOrder.assignedEmpID),
                    typeof(FSServiceOrder.sourceType), 
                    typeof(FSServiceOrder.sourceRefNbr)
                })
        {
            Filterable = true;
        }
    }
    #endregion

    #region SORefNbr_Appointment
    public class FSSelectorSORefNbr_AppointmentAttribute : PXSelectorAttribute
    {
        public FSSelectorSORefNbr_AppointmentAttribute()
            : base(
                typeof(Search2<FSServiceOrder.refNbr,
                LeftJoin<BAccount, 
                    On<BAccount.bAccountID, Equal<FSServiceOrder.customerID>>>,
                Where<
                    FSServiceOrder.srvOrdType, Equal<Current<FSAppointment.srvOrdType>>, 
                    And<
                        Where2<
                            Where<Current<FSAppointment.appointmentID>, Greater<Zero>,
                                Or<FSServiceOrder.status, Equal<FSServiceOrder.status.Open>>>,
                        And<
                            Where<BAccount.bAccountID, IsNull, 
                            Or<
                                Where<BAccount.bAccountID, IsNotNull,
                                And<BAccount.type, NotEqual<BAccountType.prospectType>>>>>>>>>,
                OrderBy<
                    Desc<FSServiceOrder.refNbr>>>),
                new Type[]
                {
                    typeof(FSServiceOrder.refNbr), 
                    typeof(FSServiceOrder.srvOrdType),
                    typeof(FSServiceOrder.customerID), 
                    typeof(FSServiceOrder.status),
                    typeof(FSServiceOrder.priority), 
                    typeof(FSServiceOrder.severity),
                    typeof(FSServiceOrder.orderDate), 
                    typeof(FSServiceOrder.sLAETA), 
                    typeof(FSServiceOrder.promisedDate),
                    typeof(FSServiceOrder.assignedEmpID),
                    typeof(FSServiceOrder.sourceType), 
                    typeof(FSServiceOrder.sourceRefNbr)
                })
        {
            Filterable = true;
        }
    }
    #endregion

    #region SODetIDService
    public class FSSelectorSODetIDServiceAttribute : PXSelectorAttribute
    {
        public FSSelectorSODetIDServiceAttribute()
            : base(
                typeof(Search<FSSODet.sODetID,
                       Where<
                           FSSODet.sOID, Equal<Current<FSAppointment.sOID>>,
                           And<FSSODet.status, NotEqual<ListField_Status_AppointmentDet.Canceled>,
                           And<FSSODet.status, NotEqual<ListField_Status_AppointmentDet.Completed>,
                       And<
                           Where<
                               FSSODet.lineType, Equal<ListField_LineType_Service_ServiceTemplate.Service>,
                               Or<FSSODet.lineType, Equal<ListField_LineType_Service_ServiceTemplate.Comment_Service>,
                               Or<FSSODet.lineType, Equal<ListField_LineType_Service_ServiceTemplate.Instruction_Service>,
                               Or<FSSODet.lineType, Equal<ListField_LineType_Service_ServiceTemplate.NonStockItem>>>>>>>>>>),
                new Type[]
                {
                    typeof(FSSODet.lineRef), 
                    typeof(FSSODet.lineType),
                    typeof(FSSODet.status), 
                    typeof(FSSODet.inventoryID), 
                    typeof(FSSODet.lastModifiedDateTime)
                })
        {
            SubstituteKey = typeof(FSSODet.lineRef);
        }
    }
    #endregion

    #region SODetIDService
    public class FSSelectorServiceOrderSODetIDAttribute : PXSelectorAttribute
    {
        public FSSelectorServiceOrderSODetIDAttribute()
            : base(
                typeof(Search<FSSODet.lineRef,
                            Where<FSSODet.sOID, Equal<Current<FSServiceOrder.sOID>>,
                                And<FSSODet.lineType, Equal<ListField_LineType_ALL.Service>>>>),
                new Type[]
                {
                    typeof(FSSODet.lineRef),
                    typeof(FSSODet.lineType),
                    typeof(FSSODet.status),
                    typeof(FSSODet.inventoryID),
                    typeof(FSSODet.tranDesc)
                })
        {
            SubstituteKey = typeof(FSSODet.lineRef);
            DescriptionField = typeof(FSSODet.inventoryID);
            DirtyRead = true;
        }
    }

    public class FSSelectorAppointmentSODetIDAttribute : PXSelectorAttribute
    {
        public FSSelectorAppointmentSODetIDAttribute()
            : base(
                typeof(Search<FSAppointmentDet.lineRef,
                            Where<
                                FSAppointmentDet.appointmentID, Equal<Current<FSAppointment.appointmentID>>,
                                And<FSAppointmentDet.lineType, Equal<ListField_LineType_ALL.Service>,
                                And<FSAppointmentDet.lineRef, IsNotNull>>>>),
                new Type[]
                {
                    typeof(FSAppointmentDet.lineRef),
                    typeof(FSAppointmentDet.lineType),
                    typeof(FSAppointmentDet.status),
                    typeof(FSAppointmentDet.inventoryID),
                    typeof(FSAppointmentDet.tranDesc)
                })
        {
            SubstituteKey = typeof(FSAppointmentDet.lineRef);
            DescriptionField = typeof(FSAppointmentDet.inventoryID);
            DirtyRead = true;
        }
    }
    #endregion

    #region SODetID of services in the Appointment for Pickup/Delivery
    public class FSSelectorServiceSODetIDInAppointmentAttribute : PXSelectorAttribute
    {
        public FSSelectorServiceSODetIDInAppointmentAttribute()
            : base(
                typeof(Search2<FSSODet.sODetID,
                            InnerJoin<FSAppointmentDet, 
                                On<FSAppointmentDet.sODetID, Equal<FSSODet.sODetID>>,
                            InnerJoin<FSAppointment,
                                On<FSAppointment.appointmentID, Equal<FSAppointmentDet.appointmentID>>>>,
                        Where<
                            FSSODet.sOID, Equal<Current<FSAppointment.sOID>>,
                            And<FSAppointment.appointmentID, Equal<Current<FSAppointment.appointmentID>>,
                            And<FSSODet.status, NotEqual<FSSODet.status.Canceled>,
                            And<FSSODet.status, NotEqual<FSSODet.status.Completed>,
                            And<FSSODet.lineType, Equal<FSSODet.lineType.Service>,
                            And<FSAppointmentDet.lineType, Equal<FSAppointmentDet.lineType.Service>,
                            And<
                                Where<
                                    FSAppointmentDet.serviceType, Equal<FSAppointmentDet.serviceType.Delivered_Items>,
                                    Or<FSAppointmentDet.serviceType, Equal<FSAppointmentDet.serviceType.Picked_Up_Items>>>>>>>>>>>),
                new Type[]
                {
                    typeof(FSSODet.lineRef), 
                    typeof(FSSODet.lineType),
                    typeof(FSSODet.status), 
                    typeof(FSSODet.inventoryID), 
                    typeof(FSSODet.tranDesc)
                })
        {
            SubstituteKey = typeof(FSSODet.lineRef);
            DescriptionField = typeof(FSSODet.inventoryID);
        }
    }
    #endregion

    #region SODetIDPart
    public class FSSelectorSODetIDPartAttribute : PXSelectorAttribute
    {
        public FSSelectorSODetIDPartAttribute()
            : base(
                typeof(Search<FSSODet.sODetID,
                       Where<
                           FSSODet.sOID, Equal<Current<FSAppointment.sOID>>,
                           And<
                               Where2<
                                   Where<
                                       FSSODet.lineType, Equal<ListField_LineType_Part_ALL.Inventory_Item>>,
                                   Or<
                                       Where<
                                            FSSODet.lineType, Equal<ListField_LineType_Part_ALL.Comment_Part>,
                                            Or<FSSODet.lineType, Equal<ListField_LineType_Part_ALL.Instruction_Part>>>>>>>>),
                new Type[]
                {
                    typeof(FSSODet.lineRef), 
                    typeof(FSSODet.lineType), 
                    typeof(FSSODet.inventoryID),
                    typeof(FSSODet.lastModifiedDateTime)
                })
        {
            SubstituteKey = typeof(FSSODet.lineRef);
        }
    }
    #endregion

    #region Equipment Type
    public class FSSelectorEquipmentTypeAttribute : PXSelectorAttribute
    {
        public FSSelectorEquipmentTypeAttribute()
            : base(
                typeof(Search<FSEquipmentType.equipmentTypeID>))
        {
            SubstituteKey = typeof(FSEquipmentType.equipmentTypeCD);
            DescriptionField = typeof(FSEquipmentType.descr);
        }
    }
    #endregion

    #region CompanyLocation
    public class FSSelectorBranchLocationAttribute : PXSelectorAttribute
    {
        public FSSelectorBranchLocationAttribute()
            : base(
                typeof(Search<FSBranchLocation.branchLocationID,
                        Where<FSBranchLocation.branchID, Equal<Current<AccessInfo.branchID>>>>))
        {
            SubstituteKey = typeof(FSBranchLocation.branchLocationCD);
            DescriptionField = typeof(FSBranchLocation.descr);
        }
    }
    #endregion

    public class FSSelectorBranchLocationByFSScheduleAttribute : PXSelectorAttribute
    {
        public FSSelectorBranchLocationByFSScheduleAttribute()
            : base(
                typeof(Search<FSBranchLocation.branchLocationID,
                        Where<FSBranchLocation.branchID, Equal<Current<FSSchedule.branchID>>>>))
        {
            SubstituteKey = typeof(FSBranchLocation.branchLocationCD);
            DescriptionField = typeof(FSBranchLocation.descr);
        }
    }
    #endregion

    #region Lookups Group - Appointment

    internal class FSSelectorAppointmentPostingINAttribute : PXSelectorAttribute
    {
        public FSSelectorAppointmentPostingINAttribute()
            : base(
                typeof(Search5<
                            FSAppointment.appointmentID,
                        InnerJoin<
                            FSSrvOrdType, On<FSSrvOrdType.srvOrdType, Equal<FSAppointment.srvOrdType>>,
                        InnerJoin<
                            FSAppointmentInventoryItem, On<FSAppointmentInventoryItem.appointmentID, Equal<FSAppointment.appointmentID>>>>,
                        Where<
                            FSAppointmentInventoryItem.lineType, Equal<ListField_LineType_Pickup_Delivery.Pickup_Delivery>,
                            And<FSAppointment.status, Equal<ListField_Status_Appointment.Closed>,
                            And<FSAppointment.executionDate, LessEqual<Current<UpdateInventoryFilter.cutOffDate>>,
                            And<FSSrvOrdType.enableINPosting, Equal<True>,
                            And<
                                Where<
                                    FSAppointment.routeDocumentID, Equal<Current<UpdateInventoryFilter.routeDocumentID>>,
                                Or<
                                    Current<UpdateInventoryFilter.routeDocumentID>, IsNull>>>>>>>,
                        Aggregate<GroupBy<FSAppointment.appointmentID>>>))
        {
            SubstituteKey = typeof(FSAppointment.refNbr);
        }
    }

    #endregion

    #region Lookups Group - Workflow and WorkflowStage

    #region Workflow
    public class FSSelectorWorkflowAttribute : PXSelectorAttribute
    {
        public FSSelectorWorkflowAttribute()
            : base(
                typeof(Search<FSSrvOrdType.srvOrdTypeID,
                Where<
                    FSSrvOrdType.active, Equal<True>>>))
        {
            SubstituteKey = typeof(FSSrvOrdType.srvOrdType);
            DescriptionField = typeof(FSSrvOrdType.descr);
        }
    }
    #endregion

    #region WorkflowStage
    public class FSSelectorWorkflowStageAttribute : PXSelectorAttribute
    {
        public FSSelectorWorkflowStageAttribute(Type currentSrvOrdType)
            : base(
                  BqlCommand.Compose(
                      typeof(Search2<,,,>),
                      typeof(FSWFStage.wFStageID),
                      typeof(InnerJoin<FSSrvOrdType,
                                On<
                                    FSSrvOrdType.srvOrdTypeID, Equal<FSWFStage.wFID>>>),
                      typeof(Where<,>),
                      typeof(FSSrvOrdType.srvOrdType),
                      typeof(Equal<>),
                      typeof(Current<>),
                      currentSrvOrdType,
                      typeof(OrderBy<
                        Asc<FSWFStage.parentWFStageID,
                        Asc<FSWFStage.sortOrder>>>)))
        {
            SubstituteKey = typeof(FSWFStage.wFStageCD);
            DescriptionField = typeof(FSWFStage.descr);
        }
    }
    #endregion

    #region WorkflowStage In Reason
    public class FSSelectorWorkflowStageInReasonAttribute : PXSelectorAttribute
    {
        public FSSelectorWorkflowStageInReasonAttribute()
            : base(
                typeof(Search<FSWFStage.wFStageID,
                Where<
                    FSWFStage.wFID, Equal<Current<FSReasonFilter.wFID>>>,
                OrderBy<
                    Asc<FSWFStage.parentWFStageID,
                    Asc<FSWFStage.sortOrder>>>>))
        {
            SubstituteKey = typeof(FSWFStage.wFStageCD);
        }
    }
    #endregion

    #endregion

    #region Lookups Group - SrvOrdType

    public class FSSelectorSrvOrdTypeAttribute : PXSelectorAttribute
    {
        public FSSelectorSrvOrdTypeAttribute()
            : base(
                typeof(Search<FSSrvOrdType.srvOrdType>),
                new Type[]
                {
                    typeof(FSSrvOrdType.srvOrdType), 
                    typeof(FSSrvOrdType.descr), 
                    typeof(FSSrvOrdType.behavior) 
                })
        {
            DescriptionField = typeof(FSSrvOrdType.descr);
        }
    }

    public class FSSelectorContractSrvOrdTypeAttribute : PXSelectorAttribute
    {
        public FSSelectorContractSrvOrdTypeAttribute()
            : base(
                typeof(Search<FSSrvOrdType.srvOrdType,
                    Where<
                        FSSrvOrdType.active, Equal<True>,
                        And<FSSrvOrdType.behavior, NotEqual<FSSrvOrdType.behavior.Quote>,
                        And<FSSrvOrdType.behavior, NotEqual<FSSrvOrdType.behavior.RouteAppointment>,
                        And<FSSrvOrdType.behavior, NotEqual<FSSrvOrdType.behavior.InternalAppointment>>>>>>))
        {
            DescriptionField = typeof(FSSrvOrdType.descr);
        }
    }

    public class FSSelectorRouteContractSrvOrdTypeAttribute : PXSelectorAttribute
    {
        public FSSelectorRouteContractSrvOrdTypeAttribute()
            : base(
                typeof(Search<FSSrvOrdType.srvOrdType,
                    Where<FSSrvOrdType.active, Equal<True>,
                        And<FSSrvOrdType.behavior, Equal<FSSrvOrdType.behavior.RouteAppointment>>>>))
        {
            DescriptionField = typeof(FSSrvOrdType.descr);
        }
    }

    public class FSSelectorSrvOrdTypeNOTQuoteAttribute : PXSelectorAttribute
    {
        public FSSelectorSrvOrdTypeNOTQuoteAttribute()
            : base(
                typeof(Search<FSSrvOrdType.srvOrdType,
                    Where<FSSrvOrdType.behavior, NotEqual<FSSrvOrdType.behavior.Quote>>>),
                new Type[]
                {
                    typeof(FSSrvOrdType.srvOrdType), 
                    typeof(FSSrvOrdType.descr), 
                    typeof(FSSrvOrdType.behavior) 
                })
        {
            DescriptionField = typeof(FSSrvOrdType.descr);
        }
    }

    public class FSSelectorSrvOrdTypeNOTQuoteInternalAttribute : PXSelectorAttribute
    {
        public FSSelectorSrvOrdTypeNOTQuoteInternalAttribute()
            : base(
                typeof(Search<FSSrvOrdType.srvOrdType,
                    Where<FSSrvOrdType.active, Equal<True>,
                        And<FSSrvOrdType.behavior, NotEqual<FSSrvOrdType.behavior.Quote>,
                        And<FSSrvOrdType.behavior, NotEqual<FSSrvOrdType.behavior.InternalAppointment>>>>>),
                new Type[]
                {
                    typeof(FSSrvOrdType.srvOrdType),
                    typeof(FSSrvOrdType.descr),
                    typeof(FSSrvOrdType.behavior)
                })
        {
            DescriptionField = typeof(FSSrvOrdType.descr);
        }
    }

    public class FSSelectorSrvOrdTypeRoute : PXSelectorAttribute
    {
        public FSSelectorSrvOrdTypeRoute()
            : base(
                typeof(Search<FSSrvOrdType.srvOrdType,
                    Where<FSSrvOrdType.active, Equal<True>,
                        And<FSSrvOrdType.behavior, Equal<FSSrvOrdType.behavior.RouteAppointment>>>>),
                new Type[]
                {
                    typeof(FSSrvOrdType.srvOrdType), 
                    typeof(FSSrvOrdType.descr), 
                    typeof(FSSrvOrdType.behavior) 
                })
        {
            DescriptionField = typeof(FSSrvOrdType.descr);
        }
    }

    public class FSSelectorActiveSrvOrdType : PXSelectorAttribute
    {
        public FSSelectorActiveSrvOrdType()
            : base(
                typeof(
                    Search<FSSrvOrdType.srvOrdType,
                    Where<
                        FSSrvOrdType.active, Equal<True>>>))
        {
            DescriptionField = typeof(FSSrvOrdType.descr);
        }
    }
    #endregion

    #region Lookups Group - Equipment, Resources and Vehicles

    public static class SelectorBase_Equipment
    {
        public static Type[] selectorColumns = new Type[]
                                                {
                                                    typeof(FSEquipment.refNbr),
                                                    typeof(FSEquipment.descr),
                                                    typeof(FSEquipment.serialNumber),
                                                    typeof(FSEquipment.ownerType),
                                                    typeof(FSEquipment.ownerID),
                                                    typeof(FSEquipment.locationType),
                                                    typeof(FSEquipment.customerID),
                                                    typeof(FSEquipment.customerLocationID),
                                                    typeof(FSEquipment.branchID),
                                                    typeof(FSEquipment.branchLocationID),
                                                    typeof(FSEquipment.inventoryID),
                                                    typeof(FSEquipment.iNSerialNumber),
                                                    typeof(FSEquipment.colorID),
                                                    typeof(FSEquipment.status)
                                                };
    }

    #region All SMEquipment
    public class FSSelectorSMEquipmentRefNbrAttribute : PXSelectorAttribute
    {
        public FSSelectorSMEquipmentRefNbrAttribute()
            : base(
                typeof(
                        Search3<FSEquipment.refNbr,
                        OrderBy<
                            Asc<FSEquipment.refNbr>>>),
                SelectorBase_Equipment.selectorColumns)
        {
        }
    }
    #endregion

    #region Equipment for Maintenance
    public class FSSelectorMaintenanceEquipmentAttribute : PXSelectorAttribute
    {
        public FSSelectorMaintenanceEquipmentAttribute(Type srvOrdType, Type billCustomerID, Type customerID, Type customerLocationID, Type branchLocationID)
            : base(
                 BqlCommand.Compose(
                            typeof(Search2<,,>),
                            typeof(FSEquipment.SMequipmentID),
                            typeof(InnerJoin<,,>),
                            typeof(FSSrvOrdType),
                                    typeof(On<,>), typeof(FSSrvOrdType.srvOrdType), typeof(Equal<>), typeof(Current<>), srvOrdType,
                            typeof(CrossJoinSingleTable<>),
                            typeof(FSSetup),
                            typeof(Where<,,>),
                                typeof(FSEquipment.requireMaintenance), typeof(Equal<True>),
                            typeof(And<>),
                                typeof(Where2<,>),
                                    typeof(Where<FSSetup.enableAllTargetEquipment, Equal<True>>),
                                    typeof(Or<>),
                                        typeof(Where2<,>),
                                            typeof(Where<,,>),
                                                //LocationType: Customer
                                                typeof(FSEquipment.locationType), typeof(Equal<ListField_LocationType.Customer>),
                                                    typeof(And2<,>),
                                                        typeof(Where<,,>),
                                                            typeof(FSEquipment.customerID), typeof(Equal<>), typeof(Current<>), customerID,
                                                            typeof(And<>),
                                                                typeof(Where<,,>),
                                                                    typeof(FSEquipment.customerLocationID), typeof(Equal<>), typeof(Current<>), customerLocationID,
                                                                    typeof(Or<FSEquipment.customerLocationID, IsNull>),
                                                        typeof(And<>),
                                                            typeof(Where2<,>),
                                                                typeof(Where<,,>),
                                                                    //In case OwnerType: Customer -> Check if billCustomer is either the Equipment's customer or its OwnerID.
                                                                    typeof(FSEquipment.ownerType), typeof(Equal<FSEquipment.ownerType.Customer>),
                                                                    typeof(And<>),
                                                                        typeof(Where2<,>),
                                                                            typeof(Where<,>),
                                                                                typeof(FSEquipment.customerID), typeof(Equal<>), typeof(Current<>), billCustomerID,
                                                                        typeof(Or<>),
                                                                            typeof(Where<,>),
                                                                                typeof(FSEquipment.ownerID), typeof(Equal<>), typeof(Current<>), billCustomerID,
                                                                typeof(Or<
                                                                          Where<FSEquipment.ownerType, Equal<FSEquipment.ownerType.OwnCompany>>>),
                                            typeof(Or<>),
                                                //LocationType: Company
                                                typeof(Where<,,>),
                                                    typeof(FSEquipment.locationType), typeof(Equal<ListField_LocationType.Company>),
                                                    typeof(And<,,>),
                                                        typeof(FSEquipment.branchID), typeof(Equal<Current<AccessInfo.branchID>>),
                                                        typeof(And2<,>),
                                                            typeof(Where<,,>),
                                                                typeof(FSEquipment.branchLocationID), typeof(Equal<>), typeof(Current<>), branchLocationID,                                                        
                                                                typeof(Or<FSEquipment.branchLocationID, IsNull>),
                                                        typeof(And<>),
                                                            typeof(Where2<,>),
                                                                typeof(Where<,,>),
                                                                    typeof(FSEquipment.ownerType), typeof(Equal<FSEquipment.ownerType.Customer>),
                                                                    typeof(And<,>),
                                                                        typeof(FSEquipment.ownerID), typeof(Equal<>), typeof(Current<>), billCustomerID,
                                                                typeof(Or<>),
                                                                    typeof(Where<,,>),
                                                                        //All equipments with LocationType: Company and OwnerType: Company only appear in Internal Appointments.
                                                                        typeof(FSEquipment.ownerType), typeof(Equal<FSEquipment.ownerType.OwnCompany>),
                                                                        typeof(And<,>),
                                                                            typeof(FSSrvOrdType.behavior), typeof(Equal<FSSrvOrdType.behavior.InternalAppointment>)
                    ),
        SelectorBase_Equipment.selectorColumns)
        {
            SubstituteKey = typeof(FSEquipment.refNbr);
            DescriptionField = typeof(FSEquipment.descr);
        }
    }

    #endregion

    #region Resource Equipment

    public class FSSelectorServiceOrderResourceEquipmentAttribute : PXSelectorAttribute
    {
        public FSSelectorServiceOrderResourceEquipmentAttribute()
            : base(
                typeof(
                        Search<FSEquipment.SMequipmentID,
                        Where<
                            FSEquipment.resourceEquipment, Equal<True>,
                            And<
                                Where2<
                                    Where2<
                                        Where<
                                            FSEquipment.locationType, Equal<ListField_LocationType.Company>,
                                            And<
                                                FSEquipment.branchID, Equal<Current<FSServiceOrder.branchID>>,
                                            And<
                                                FSEquipment.branchLocationID, Equal<Current<FSServiceOrder.branchLocationID>>>>>,
                                        Or<
                                            Where<
                                                FSEquipment.locationType, Equal<ListField_LocationType.Company>,
                                                And<
                                                    FSEquipment.branchID, Equal<Current<FSServiceOrder.branchID>>,
                                                And<
                                                    FSEquipment.branchLocationID, IsNull>>>>>,
                                    Or2<
                                        Where<
                                            FSEquipment.locationType, Equal<ListField_LocationType.Customer>,
                                            And<
                                                FSEquipment.customerID, Equal<Current<FSServiceOrder.customerID>>,
                                            And<
                                                FSEquipment.customerLocationID, Equal<Current<FSServiceOrder.locationID>>>>>,
                                        Or<
                                            Where<
                                                FSEquipment.locationType, Equal<ListField_LocationType.Customer>,
                                                And<
                                                    FSEquipment.customerID, Equal<Current<FSServiceOrder.customerID>>,
                                                And<
                                                    FSEquipment.customerLocationID, IsNull>>>>>>>>>),
                    SelectorBase_Equipment.selectorColumns)
        {
            SubstituteKey = typeof(FSEquipment.refNbr);
            DescriptionField = typeof(FSEquipment.descr);
        }
    }

    #endregion

    #region Vehicles
    public class FSSelectorVehicleAttribute : PXSelectorAttribute
    {
        public FSSelectorVehicleAttribute()
            : base(
                typeof(Search<FSVehicle.SMequipmentID,
                           Where<
                                FSVehicle.isVehicle, Equal<True>>,
                           OrderBy<Asc<FSVehicle.refNbr>>>),
                new Type[]
                {
                    typeof(FSVehicle.refNbr), 
                    typeof(FSVehicle.status),
                    typeof(FSVehicle.vehicleTypeID),
                    typeof(FSVehicle.descr),
                    typeof(FSVehicle.registrationNbr),
                    typeof(FSVehicle.manufacturerModelID),
                    typeof(FSVehicle.manufacturerID),
                    typeof(FSVehicle.manufacturingYear),
                    typeof(FSVehicle.colorID)
                })
        {
            SubstituteKey = typeof(FSVehicle.refNbr);
            DescriptionField = typeof(FSVehicle.descr);
        }
    }
    #endregion

    #endregion

    #region Lookups Equipment Management
    #region ModelEquipment - FSxEquipmentModel
    public class FSSelectorComponentIDAttribute : PXSelectorAttribute
    {
        public FSSelectorComponentIDAttribute()
            : base(
                typeof(Search<FSModelTemplateComponent.componentID,
                       Where<
                            FSModelTemplateComponent.modelTemplateID, Equal<Current<InventoryItem.itemClassID>>,
                       And<
                            FSModelTemplateComponent.active, Equal<True>>>>),
                new Type[]
                {
                    typeof(FSModelTemplateComponent.componentCD),
                    typeof(FSModelTemplateComponent.descr),
                    typeof(FSModelTemplateComponent.optional),
                    typeof(FSModelTemplateComponent.classID)
                })
        {
            this.SubstituteKey = typeof(FSModelTemplateComponent.componentCD);
        }
    }
    #endregion
    #region FSEquipment
    public class FSSelectorComponentIDEquipmentAttribute : PXSelectorAttribute
    {
        public FSSelectorComponentIDEquipmentAttribute()
            : base(
                typeof(Search2<FSModelTemplateComponent.componentID,
                            InnerJoin<FSModelComponent,
                                On<FSModelComponent.componentID, Equal<FSModelTemplateComponent.componentID>>>,
                            Where<FSModelComponent.modelID, Equal<Current<FSEquipment.inventoryID>>>>),
                new Type[]
                {
                    typeof(FSModelTemplateComponent.componentCD),
                    typeof(FSModelTemplateComponent.optional),
                    typeof(FSModelComponent.active),
                    typeof(FSModelComponent.descr),
                    typeof(FSModelComponent.classID)
                })
        {
            SubstituteKey = typeof(FSModelTemplateComponent.componentCD);
        }
    }
    #endregion
    #region SalesOrder
    public class FSSelectorComponentIDSalesOrderAttribute : PXCustomSelectorAttribute
    {
        public FSSelectorComponentIDSalesOrderAttribute()
            : base(typeof(FSModelTemplateComponent.componentID),
                  typeof(FSModelTemplateComponent.componentCD),
                  typeof(FSModelComponent.optional),
                  typeof(FSModelComponent.classID))
        {
            this.SubstituteKey = typeof(FSModelTemplateComponent.componentCD);
        }

        protected virtual IEnumerable GetRecords()
        {
            PXCache cache = this._Graph.Caches[typeof(SOLine)];
            int? pivotInventoryID = null;
            int? pivotItemClassID = null;

            object current = null;

            foreach (object item in PXView.Currents)
            {
                if (item != null && (item.GetType() == typeof(SOLine)))
                {
                    current = item;
                    break;
                }
            }

            if (current == null)
            {
                current = cache.Current;
            }

            SOLine currentSOLine = (SOLine)current;
            FSxSOLine currentFSxSOLineRow = PXCache<SOLine>.GetExtension<FSxSOLine>(currentSOLine);
            InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(_Graph, currentSOLine.InventoryID);

            if (currentFSxSOLineRow.NewTargetEquipmentLineNbr != null)
            {
                foreach (object item in cache.Cached)
                {
                    SOLine currentSOLine_temp = item as SOLine;

                    if (currentSOLine_temp.LineNbr == currentFSxSOLineRow.NewTargetEquipmentLineNbr)
                    {
                        pivotInventoryID = currentSOLine_temp.InventoryID;
                        break;
                    }
                }
            }
            else if (currentFSxSOLineRow.SMEquipmentID != null)
            {
                FSEquipment fsEquipmentRow = SharedFunctions.GetEquipmentRow(_Graph, currentFSxSOLineRow.SMEquipmentID);
                pivotInventoryID = fsEquipmentRow.InventoryID;
            }

            if (pivotInventoryID != null)
            {
                pivotItemClassID = inventoryItemRow.ItemClassID;
                PXResultset<FSModelTemplateComponent> resultRows;
                if (currentFSxSOLineRow.EquipmentAction != ID.Equipment_Action.NONE)
                {
                    resultRows = PXSelectJoin<FSModelTemplateComponent,
                                InnerJoin<FSModelComponent,
                                On<FSModelTemplateComponent.componentID, Equal<FSModelComponent.componentID>>>,
                                Where<
                                    FSModelComponent.active, Equal<True>,
                                    And<FSModelComponent.modelID, Equal<Required<FSModelComponent.modelID>>,
                                    And<FSModelComponent.classID, Equal<Required<FSModelComponent.classID>>>>>>
                            .Select(_Graph, pivotInventoryID, pivotItemClassID);
                }
                else
                {
                    resultRows = PXSelectJoin<FSModelTemplateComponent,
                                InnerJoin<FSModelComponent,
                                On<FSModelTemplateComponent.componentID, Equal<FSModelComponent.componentID>>>,
                                Where<
                                    FSModelComponent.active, Equal<True>,
                                    And<FSModelComponent.modelID, Equal<Required<FSModelComponent.modelID>>>>>
                            .Select(_Graph, pivotInventoryID);
                }

                return resultRows;
            }

            return null;
        }
    }
    public class FSSelectorNewTargetEquipmentSalesOrderAttribute : PXCustomSelectorAttribute
    {
        [PXHidden]
        public partial class FSSOLine : IBqlTable
        {
            #region LineNbr
            public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
            [PXInt]
            [PXUIField(DisplayName = "Line Nbr.")]
            public int? LineNbr { get; set; }
            #endregion
            #region SortOrder
            public abstract class sortOrder : PX.Data.BQL.BqlInt.Field<sortOrder> { }
            [PXInt]
            [PXUIField(DisplayName = "Line Order.", Visibility = PXUIVisibility.SelectorVisible)]
            public int? SortOrder { get; set; }
            #endregion
            #region InventoryID
            public abstract class inventoryID : PX.Data.BQL.BqlString.Field<inventoryID> { }
            [PXString()]
            [PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
            public virtual String InventoryID { get; set; }
            #endregion
        }

        public FSSelectorNewTargetEquipmentSalesOrderAttribute()
            : base(typeof(FSSOLine.lineNbr), typeof(FSSOLine.sortOrder), typeof(FSSOLine.inventoryID))
        {
            this.SubstituteKey = typeof(FSSOLine.sortOrder);
        }

        protected virtual IEnumerable GetRecords()
        {
            var rows = PXSelectJoin<SOLine,
                        InnerJoin<InventoryItem,
                            On<InventoryItem.inventoryID, Equal<SOLine.inventoryID>>>,
                        Where<
                            FSxEquipmentModel.equipmentItemClass, Equal<ListField_EquipmentItemClass.ModelEquipment>,
                            And<FSxSOLine.equipmentAction, Equal<FSxSOLine.equipmentAction.SellingTargetEquipment>,
                            And<
                                Where<
                                    SOLine.orderNbr, Equal<Current<SOLine.orderNbr>>,
                                    And<SOLine.orderType, Equal<Current<SOLine.orderType>>>>>>>>
                        .Select(_Graph);

            if (rows.Count > 0)
            {
                foreach (PXResult<SOLine, InventoryItem> row in rows)
                {
                    SOLine sOLineRow = (SOLine)row;
                    InventoryItem inventoryItemRow = (InventoryItem)row;
                    yield return new FSSOLine { LineNbr = sOLineRow.LineNbr, SortOrder = sOLineRow.SortOrder, InventoryID = inventoryItemRow.InventoryCD };
                }
            }
        }
    }

    public class FSSelectorNewTargetEquipmentSOInvoiceAttribute : PXCustomSelectorAttribute
    {
        [PXHidden]
        public partial class FSARTran : IBqlTable
        {
            #region LineNbr
            public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
            [PXInt]
            [PXUIField(DisplayName = "Line Nbr.")]
            public int? LineNbr { get; set; }
            #endregion
            #region SortOrder
            public abstract class sortOrder : PX.Data.BQL.BqlInt.Field<sortOrder> { }
            [PXInt]
            [PXUIField(DisplayName = "Line Order.", Visibility = PXUIVisibility.SelectorVisible)]
            public int? SortOrder { get; set; }
            #endregion
            #region InventoryID
            public abstract class inventoryID : PX.Data.BQL.BqlString.Field<inventoryID> { }
            [PXString()]
            [PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
            public virtual String InventoryID { get; set; }
            #endregion
        }

        public FSSelectorNewTargetEquipmentSOInvoiceAttribute()
            : base(typeof(ARTran.lineNbr), typeof(ARTran.sortOrder), typeof(ARTran.inventoryID))
        {
            this.SubstituteKey = typeof(ARTran.sortOrder);
        }

        protected virtual IEnumerable GetRecords()
        {
            var rows = PXSelectJoin<ARTran,
                        InnerJoin<InventoryItem,
                            On<InventoryItem.inventoryID, Equal<ARTran.inventoryID>>>,
                        Where<
                            FSxEquipmentModel.equipmentItemClass, Equal<ListField_EquipmentItemClass.ModelEquipment>,
                            And<FSxARTran.equipmentAction, Equal<FSxARTran.equipmentAction.SellingTargetEquipment>,
                            And<
                                Where<
                                    ARTran.refNbr, Equal<Current<ARTran.refNbr>>,
                                    And<ARTran.tranType, Equal<Current<ARTran.tranType>>>>>>>>
                        .Select(_Graph);

            if (rows.Count > 0)
            {
                foreach (PXResult<ARTran, InventoryItem> row in rows)
                {
                    ARTran ARTranRow = (ARTran)row;
                    InventoryItem inventoryItemRow = (InventoryItem)row;
                    yield return new FSARTran { LineNbr = ARTranRow.LineNbr, SortOrder = ARTranRow.SortOrder, InventoryID = inventoryItemRow.InventoryCD };
                }
            }
        }
    }
    public class FSSelectorEquipmentLineRefSalesOrderAttribute : PXSelectorAttribute
    {
        public FSSelectorEquipmentLineRefSalesOrderAttribute()
            : base(
                typeof(Search2<FSEquipmentComponent.lineNbr,
                       InnerJoin<InventoryItem,
                            On<InventoryItem.inventoryID, Equal<Current<SOLine.inventoryID>>>>,
                       Where2<
                           Where<
                               FSEquipmentComponent.status, Equal<FSEquipmentComponent.status.Active>,
                                And<FSEquipmentComponent.SMequipmentID, Equal<Current<FSxSOLine.sMEquipmentID>>>>,
                           And<
                                Where2<
                                    Where<FSEquipmentComponent.itemClassID, Equal<InventoryItem.itemClassID>,
                                        Or<Current<FSxSOLine.equipmentAction>, Equal<FSxSOLine.equipmentAction.None>>>,
                                And<
                                    Where<
                                        Current<FSxSOLine.componentID>, IsNull,
                                        Or<FSEquipmentComponent.componentID, Equal<Current<FSxSOLine.componentID>>>>>>>>>),
                new Type[]
                {
                    typeof(FSEquipmentComponent.lineRef),
                    typeof(FSEquipmentComponent.componentID),
                    typeof(FSEquipmentComponent.longDescr),
                    typeof(FSEquipmentComponent.serialNumber),
                    typeof(FSEquipmentComponent.comment)
                })
        {
            this.SubstituteKey = typeof(FSEquipmentComponent.lineRef);
        }
    }

    public class FSSelectorEquipmentLineRefSOInvoiceAttribute : PXSelectorAttribute
    {
        public FSSelectorEquipmentLineRefSOInvoiceAttribute()
            : base(
                typeof(Search2<FSEquipmentComponent.lineNbr,
                       InnerJoin<InventoryItem,
                            On<InventoryItem.inventoryID, Equal<Current<ARTran.inventoryID>>>>,
                       Where2<
                           Where<
                               FSEquipmentComponent.status, Equal<FSEquipmentComponent.status.Active>,
                                And<FSEquipmentComponent.SMequipmentID, Equal<Current<FSxARTran.sMEquipmentID>>>>,
                           And<
                                Where2<
                                    Where<FSEquipmentComponent.itemClassID, Equal<InventoryItem.itemClassID>,
                                        Or<Current<FSxARTran.equipmentAction>, Equal<FSxARTran.equipmentAction.None>>>,
                                And<
                                    Where<
                                        Current<FSxARTran.componentID>, IsNull,
                                        Or<FSEquipmentComponent.componentID, Equal<Current<FSxARTran.componentID>>>>>>>>>),
                new Type[]
                {
                    typeof(FSEquipmentComponent.lineRef),
                    typeof(FSEquipmentComponent.componentID),
                    typeof(FSEquipmentComponent.longDescr),
                    typeof(FSEquipmentComponent.serialNumber),
                    typeof(FSEquipmentComponent.comment)
                })
        {
            this.SubstituteKey = typeof(FSEquipmentComponent.lineRef);
        }
    }
    #endregion
    #region ServiceOrder - Appointment
    public class FSSelectorComponentIDServiceOrderAttribute : PXCustomSelectorAttribute
    {
        private readonly Type CurrentTable;
        private readonly Type SourceTable;

        public FSSelectorComponentIDServiceOrderAttribute(Type currentTable, Type sourceTable)
            : base(typeof(FSModelTemplateComponent.componentID),
                  typeof(FSModelTemplateComponent.componentCD),
                  typeof(FSModelComponent.optional),
                  typeof(FSModelComponent.classID))
        {
            this.CurrentTable = currentTable;
            this.SourceTable = sourceTable;
            this.SubstituteKey = typeof(FSModelTemplateComponent.componentCD);
            this.CacheGlobal = false;
        }

        protected virtual IEnumerable GetRecords()
        {
            PXCache currentCache = this._Graph.Caches[CurrentTable];
            PXCache sourceCache = this._Graph.Caches[SourceTable];
            PXCache pivotCache;
            FSSODet currentRow = null;
            var rows = new PXResultset<FSModelTemplateComponent>();
            InventoryItem inventoryItemRow;

            int? pivotInventoryID = null;
            int? pivotItemClassID = null;

            foreach (object item in PXView.Currents)
            {
                if (item != null && (item.GetType() == CurrentTable))
                {
                    currentRow = item as FSSODet;
                    break;
                }
            }

            if (currentRow == null)
            {
                currentRow = currentCache.Current as FSSODet;
                if (currentRow == null)
                {
                    return null;
                }
            }

            pivotCache = currentCache.Equals(sourceCache) ? currentCache : sourceCache;
            inventoryItemRow = SharedFunctions.GetInventoryItemRow(_Graph, currentRow.InventoryID);

            if (currentRow.NewTargetEquipmentLineNbr != null)
            {
                foreach (object item in pivotCache.Cached)
                {
                    FSSODet currentFSSODet_temp = item as FSSODet;

                    if (currentFSSODet_temp.LineRef == currentRow.NewTargetEquipmentLineNbr)
                    {
                        pivotInventoryID = currentFSSODet_temp.InventoryID;
                        break;
                    }
                }
            }
            else if (currentRow.SMEquipmentID != null)
            {
                FSEquipment fsEquipmentRow = SharedFunctions.GetEquipmentRow(_Graph, currentRow.SMEquipmentID);
                pivotInventoryID = fsEquipmentRow.InventoryID;
            }

            if (pivotInventoryID != null)
            {
                pivotItemClassID = inventoryItemRow?.ItemClassID;
                PXResultset<FSModelTemplateComponent> resultRows;

                if (currentRow.EquipmentAction != ID.Equipment_Action.NONE)
                {
                    resultRows = PXSelectJoin<FSModelTemplateComponent,
                                InnerJoin<FSModelComponent,
                                On<FSModelTemplateComponent.componentID, Equal<FSModelComponent.componentID>>>,
                                Where2<
                                    Where<
                                        FSModelComponent.active, Equal<True>,
                                        And<FSModelComponent.modelID, Equal<Required<FSModelComponent.modelID>>>>,
                                    And<
                                        Where<
                                            FSModelComponent.classID, Equal<Required<FSModelComponent.classID>>,
                                            Or<Required<FSModelComponent.classID>, IsNull>>>>>
                            .Select(_Graph, pivotInventoryID, pivotItemClassID, pivotItemClassID);
                }
                else
                {
                    resultRows = PXSelectJoin<FSModelTemplateComponent,
                                InnerJoin<FSModelComponent,
                                On<FSModelTemplateComponent.componentID, Equal<FSModelComponent.componentID>>>,
                                Where<
                                    FSModelComponent.active, Equal<True>,
                                    And<FSModelComponent.modelID, Equal<Required<FSModelComponent.modelID>>>>>
                            .Select(_Graph, pivotInventoryID);
                }

                return resultRows;
            }

            return null;
        }
    }
    public class FSSelectorEquipmentLineRefServiceOrderAppointmentAttribute : PXSelectorAttribute
    {
        public FSSelectorEquipmentLineRefServiceOrderAppointmentAttribute(Type inventoryID, Type smEquipmentID, Type componentID , Type equipmentAction)
            : 
            base(

                BqlCommand.Compose(
                            typeof(Search2<,,>),
                                typeof(FSEquipmentComponent.lineNbr),
                            typeof(LeftJoin<,>),
                                typeof(InventoryItem),
                            typeof(On<,>),
                                    typeof(InventoryItem.inventoryID),
                                    typeof(Equal<>),
                                    typeof(Current<>),
                                    inventoryID,
                            typeof(Where2<,>),
                                typeof(Where<,,>),
                                    typeof(FSEquipmentComponent.SMequipmentID),
                                    typeof(Equal<>),
                                    typeof(Current<>),
                                        smEquipmentID,
                                    typeof(And<FSEquipmentComponent.status, Equal<FSEquipmentComponent.status.Active>>),
                            typeof(And<>),
                                typeof(Where2<,>),
                                    typeof(Where<,,>),
                                        typeof(FSEquipmentComponent.itemClassID),
                                        typeof(Equal<InventoryItem.itemClassID>),
                                    typeof(Or<,,>),
                                        typeof(Current<>),
                                            inventoryID,
                                        typeof(IsNull),
                                    typeof(Or<,>),
                                        typeof(Current<>),
                                            equipmentAction,
                                        typeof(Equal<ListField_EquipmentAction.None>),
                                typeof(And<>),
                                        typeof(Where<,,>),
                                            typeof(FSEquipmentComponent.componentID),
                                            typeof(Equal<>),
                                            typeof(Current<>),
                                            componentID,
                                        typeof(Or<,>),
                                            typeof(Current<>),
                                                componentID,
                                            typeof(IsNull)),
                new Type[]
                {
                    typeof(FSEquipmentComponent.lineRef),
                    typeof(FSEquipmentComponent.componentID),
                    typeof(FSEquipmentComponent.longDescr),
                    typeof(FSEquipmentComponent.serialNumber),
                    typeof(FSEquipmentComponent.comment)
                })
        {
            this.SubstituteKey = typeof(FSEquipmentComponent.lineRef);
        }
    }

    public class FSSelectorNewTargetEquipmentServiceOrderAttribute : PXSelectorAttribute
    {
        public FSSelectorNewTargetEquipmentServiceOrderAttribute()
            : base(
                typeof(Search2<FSSODet.lineRef,
                            InnerJoin<InventoryItem,
                                On<InventoryItem.inventoryID, Equal<FSSODet.inventoryID>>>,
                        Where<
                            FSxEquipmentModel.equipmentItemClass, Equal<ListField_EquipmentItemClass.ModelEquipment>,
                            And<FSSODet.equipmentAction, Equal<FSSODet.equipmentAction.SellingTargetEquipment>,
                            And<FSSODet.sOID, Equal<Current<FSServiceOrder.sOID>>>>>>),
                new Type[]
                {
                typeof(FSSODet.lineRef),
                typeof(FSSODet.inventoryID)
                })
        {
        }
    }

    public class FSSelectorComponentIDAppointmentAttribute : PXCustomSelectorAttribute
    {
        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);
            sender.Graph.CommandPreparing.AddHandler(sender.GetItemType(), _FieldName, SubstituteKeyCommandPreparing);
        }

        private readonly Type CurrentTable;
        private readonly Type SourceTable;

        public FSSelectorComponentIDAppointmentAttribute(Type currentTable, Type sourceTable)
             : base(typeof(FSModelTemplateComponent.componentID),
                 new Type[]
                 {
                    typeof(FSModelTemplateComponent.componentCD),
                    typeof(FSModelTemplateComponent.descr),
                    typeof(FSModelTemplateComponent.optional),
                    typeof(FSModelTemplateComponent.classID)
                 })
        {
            this.CurrentTable = currentTable;
            this.SourceTable = sourceTable;
            this.SubstituteKey = typeof(FSModelTemplateComponent.componentCD);
            this.CacheGlobal = false;
        }

        protected virtual IEnumerable GetRecords()
        {
            PXCache currentCache = this._Graph.Caches[CurrentTable];
            PXCache sourceCache = this._Graph.Caches[SourceTable];
            PXCache pivotCache;
            FSAppointmentDet currentRow = null;
            var rows = new PXResultset<FSModelTemplateComponent>();
            InventoryItem inventoryItemRow;

            int? pivotInventoryID = null;
            int? pivotItemClassID = null;

            foreach (object item in PXView.Currents)
            {
                if (item != null && (item.GetType() == CurrentTable))
                {
                    currentRow = item as FSAppointmentDet;
                    break;
                }
            }

            if (currentRow == null)
            {
                currentRow = currentCache.Current as FSAppointmentDet;
                if (currentRow == null)
                {
                    return null;
                }
            }

            pivotCache = currentCache.Equals(sourceCache) ? currentCache : sourceCache;
            inventoryItemRow = SharedFunctions.GetInventoryItemRow(_Graph, currentRow.InventoryID);

            if (currentRow.NewTargetEquipmentLineNbr != null)
            {
                foreach (object item in pivotCache.Cached)
                {
                    FSAppointmentDet currentAppointmentDet_temp = item as FSAppointmentDet;

                    if (currentAppointmentDet_temp.LineRef == currentRow.NewTargetEquipmentLineNbr)
                    {
                        pivotInventoryID = currentAppointmentDet_temp.InventoryID;
                        break;
                    }
                }
            }
            else if (currentRow.SMEquipmentID != null)
            {
                FSEquipment fsEquipmentRow = SharedFunctions.GetEquipmentRow(_Graph, currentRow.SMEquipmentID);
                pivotInventoryID = fsEquipmentRow.InventoryID;
            }

            if (pivotInventoryID != null)
            {
                pivotItemClassID = inventoryItemRow?.ItemClassID;
                PXResultset<FSModelTemplateComponent> resultRows;

                if (currentRow.EquipmentAction != ID.Equipment_Action.NONE)
                {
                    resultRows = PXSelectJoin<FSModelTemplateComponent,
                                LeftJoin<FSModelComponent,
                                On<FSModelTemplateComponent.componentID, Equal<FSModelComponent.componentID>>>,
                                Where2<
                                    Where<
                                        FSModelComponent.active, Equal<True>,
                                        And<FSModelComponent.modelID, Equal<Required<FSModelComponent.modelID>>>>,
                                    And<
                                        Where<
                                            FSModelComponent.classID, Equal<Required<FSModelComponent.classID>>,
                                            Or<Required<FSModelComponent.classID>, IsNull>>>>>
                            .Select(_Graph, pivotInventoryID, pivotItemClassID, pivotItemClassID);
                }
                else
                {
                    resultRows = PXSelectJoin<FSModelTemplateComponent,
                                InnerJoin<FSModelComponent,
                                On<FSModelTemplateComponent.componentID, Equal<FSModelComponent.componentID>>>,
                                Where<
                                    FSModelComponent.active, Equal<True>,
                                    And<FSModelComponent.modelID, Equal<Required<FSModelComponent.modelID>>>>>
                            .Select(_Graph, pivotInventoryID);
                }
                return resultRows;
            }

            return null;
        }
    }

    public class FSSelectorNewTargetEquipmentAppointmentAttribute : PXSelectorAttribute
    {
        public FSSelectorNewTargetEquipmentAppointmentAttribute()
            : base(
                typeof(Search2<FSAppointmentDetPart.lineRef,
                            InnerJoin<InventoryItem,
                                On<InventoryItem.inventoryID, Equal<FSAppointmentDetPart.inventoryID>>>,
                        Where<
                            FSxEquipmentModel.equipmentItemClass, Equal<ListField_EquipmentItemClass.ModelEquipment>,
                            And<FSAppointmentDetPart.equipmentAction, Equal<FSAppointmentDet.equipmentAction.SellingTargetEquipment>,
                            And<FSAppointmentDetPart.appointmentID, Equal<Current<FSAppointment.appointmentID>>,
                            And<FSAppointmentDetPart.lineRef, IsNotNull>>>>>),
                new Type[]
                {
                    typeof(FSAppointmentDetPart.lineRef),
                    typeof(FSAppointmentDetPart.inventoryID)
                })
        {
            DirtyRead = true;
        }
    }
    #endregion
    #region schedule

    public class FSSelectorEquipmentLineRefAttribute : PXSelectorAttribute
    {
        public FSSelectorEquipmentLineRefAttribute(Type smEquipmentID, Type componentID)
            :
            base(

                BqlCommand.Compose(
                            typeof(Search<,>),
                                typeof(FSEquipmentComponent.lineNbr),
                            typeof(Where2<,>),
                                typeof(Where<,,>),
                                    typeof(FSEquipmentComponent.SMequipmentID),
                                    typeof(Equal<>),
                                    typeof(Current<>),
                                        smEquipmentID,
                                    typeof(And<FSEquipmentComponent.status, Equal<FSEquipmentComponent.status.Active>>),
                            typeof(And<>),
                                    typeof(Where<,,>),
                                        typeof(Current<>),
                                            componentID,
                                        typeof(IsNull),
                                    typeof(Or<,>),
                                        typeof(FSEquipmentComponent.componentID),
                                        typeof(Equal<>),
                                        typeof(Current<>),
                                        componentID),
                new Type[]
                {
                    typeof(FSEquipmentComponent.lineRef),
                    typeof(FSEquipmentComponent.componentID),
                    typeof(FSEquipmentComponent.longDescr),
                    typeof(FSEquipmentComponent.serialNumber),
                    typeof(FSEquipmentComponent.comment)
                })
        {
            this.SubstituteKey = typeof(FSEquipmentComponent.lineRef);
        }
    }

    public class FSSelectorComponentIDByFSEquipmentComponentAttribute : PXSelectorAttribute
    {
        public FSSelectorComponentIDByFSEquipmentComponentAttribute(Type smEquipmentID)
            :
            base(
                BqlCommand.Compose(
                            typeof(Search2<,,>),
                                typeof(FSModelTemplateComponent.componentID),
                            typeof(LeftJoin<,>),
                                typeof(FSEquipmentComponent),
                            typeof(On<,>),
                                typeof(FSEquipmentComponent.componentID),
                                typeof(Equal<>),
                                typeof(FSModelTemplateComponent.componentID),
                            typeof(Where<,,>),
                                typeof(FSEquipmentComponent.SMequipmentID),
                                typeof(Equal<>),
                                typeof(Current<>),
                                smEquipmentID,
                            typeof(And<FSEquipmentComponent.status, Equal<FSEquipmentComponent.status.Active>>)),
                new Type[]
                {
                    typeof(FSModelTemplateComponent.componentCD),
                    typeof(FSModelTemplateComponent.descr),
                    typeof(FSModelTemplateComponent.optional),
                    typeof(FSModelTemplateComponent.classID)
                })
        {
            this.SubstituteKey = typeof(FSModelTemplateComponent.componentCD);
        }
    }

    #endregion
    #endregion

    #region Route
    public class FSSelectorRouteAttribute : PXSelectorAttribute
    {
        public FSSelectorRouteAttribute()
            : base(
                typeof(Search<FSRouteDocument.routeDocumentID>),
                new Type[]
                {
                    typeof(FSRouteDocument.refNbr),
                    typeof(FSRouteDocument.date),
                    typeof(FSRouteDocument.driverID),
                    typeof(FSRouteDocument.routeID),
                    typeof(FSRouteDocument.status),
                    typeof(FSRouteDocument.vehicleID)                                                                     
                })
        {
            SubstituteKey = typeof(FSRouteDocument.refNbr);
        }
    }

    public class FSSelectorRouteIDAttribute : PXSelectorAttribute
    {
        public FSSelectorRouteIDAttribute()
            : base(
                typeof(Search<FSRoute.routeID>),
                new Type[]
                {
                    typeof(FSRoute.routeCD), 
                    typeof(FSRoute.descr), 
                    typeof(FSRoute.routeShort), 
                    typeof(FSRoute.weekCode)
                })
        {
            SubstituteKey = typeof(FSRoute.routeCD);
            DescriptionField = typeof(FSRoute.descr);
        }
    }

    internal class FSSelectorRouteDocumentPostingINAttribute : PXSelectorAttribute
    {
        public FSSelectorRouteDocumentPostingINAttribute()
            : base(
                typeof(Search5<
                            FSRouteDocument.routeDocumentID,
                        InnerJoin<
                            FSAppointment, On<FSAppointment.routeDocumentID, Equal<FSRouteDocument.routeDocumentID>>,
                        InnerJoin<
                            FSAppointmentInventoryItem, On<FSAppointment.appointmentID, Equal<FSAppointmentInventoryItem.appointmentID>>,
                        InnerJoin<
                            FSSrvOrdType, On<FSSrvOrdType.srvOrdType, Equal<FSAppointment.srvOrdType>>>>>,
                        Where<
                            FSAppointmentInventoryItem.lineType, Equal<ListField_LineType_Pickup_Delivery.Pickup_Delivery>>,
                        Aggregate<GroupBy<FSRouteDocument.routeDocumentID>>>))
        {
            SubstituteKey = typeof(FSRouteDocument.refNbr);
        }
    }

    #endregion

    #region Vendors - SDEnabled
    public class FSSelectorVendorAttribute : PXDimensionSelectorAttribute
    {
        public FSSelectorVendorAttribute()
            : base(
                VendorAttribute.DimensionName,
                typeof(Search2<Vendor.bAccountID,
                    LeftJoin<Contact,
                        On<
                            Contact.bAccountID, Equal<Vendor.bAccountID>,
                            And<Contact.contactID, Equal<Vendor.defContactID>>>,
                    LeftJoin<Address,
                        On<
                            Address.bAccountID, Equal<Vendor.bAccountID>,
                            And<Address.addressID, Equal<Vendor.defAddressID>>>>>,
                    Where<
                        FSxVendor.sDEnabled, Equal<True>,
                        And<Vendor.status, NotEqual<Vendor.status.inactive>>>>),
                typeof(Vendor.acctCD),
                new Type[]
                {
                    typeof(Vendor.acctCD),
                    typeof(Vendor.status),
                    typeof(Vendor.acctName),
                    typeof(Vendor.classID),                
                    typeof(Contact.phone1),
                    typeof(Address.city),
                    typeof(Address.countryID)
                })
        {
            DescriptionField = typeof(Vendor.acctName);
            DirtyRead = true;
        }
    }
    #endregion


    public class FSINLotSerialNbrAttribute : PXCustomSelectorAttribute
    {
        private readonly Type InventoryType;
        private readonly Type SubItemType;
        private readonly Type LocationType;

        public FSINLotSerialNbrAttribute(Type InventoryType, Type SubItemType, Type LocationType)
            : base(
                    typeof(Search2<INLotSerialStatus.lotSerialNbr,
                                        InnerJoin<INSiteLotSerial,
                                            On<INLotSerialStatus.inventoryID, Equal<INSiteLotSerial.inventoryID>,
                                            And<INLotSerialStatus.siteID, Equal<INSiteLotSerial.siteID>,
                                            And<INLotSerialStatus.lotSerialNbr, Equal<INSiteLotSerial.lotSerialNbr>>>>>>),
                new Type[]
                {
                    typeof(INLotSerialStatus.lotSerialNbr),
                    typeof(INLotSerialStatus.siteID),
                    typeof(INLotSerialStatus.locationID),
                    typeof(INLotSerialStatus.qtyOnHand),
                    typeof(INSiteLotSerial.qtyAvail),
                    typeof(INLotSerialStatus.expireDate)
                })
        {            
            this.InventoryType = InventoryType;
            this.SubItemType = SubItemType;
            this.LocationType = LocationType;
        }

        public override void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
        {
            if (string.IsNullOrEmpty((string)e.NewValue) == true)
            {
                e.NewValue = null;
            }

            base.FieldVerifying(sender, e);
        }
        protected virtual IEnumerable GetRecords()
        {
            FSAppointmentDetPart currentRow = null;
            PXCache currentCache = this._Graph.Caches[typeof(FSAppointmentDetPart)];

            foreach (object item in PXView.Currents)
            {
                if (item != null && (item.GetType() == typeof(FSAppointmentDetPart)))
                {
                    currentRow = item as FSAppointmentDetPart;
                    break;
                }
            }

            if (currentRow == null)
            {
                currentRow = currentCache.Current as FSAppointmentDetPart;
                if (currentRow == null)
                {
                    return null;
                }
            }

            PXResultset<FSSODetPartSplit> splitLines = PXSelectJoin<FSSODetPartSplit,
                                                    InnerJoin<FSSODet,
                                                            On<FSSODet.srvOrdType, Equal<FSSODetPartSplit.srvOrdType>,
                                                            And<FSSODet.refNbr, Equal<FSSODetPartSplit.refNbr>,
                                                            And<FSSODet.lineNbr, Equal<FSSODetPartSplit.lineNbr>>>>>,
                                                    Where<FSSODet.sODetID, Equal<Required<FSSODet.sODetID>>>>
                                                    .Select(_Graph, currentRow.SODetID);

            List<PXResult<FSSODetPartSplit, FSSODet>> linesWithLotSerial = splitLines
                                                                        .Where(x => string.IsNullOrEmpty(((FSSODetPartSplit)((PXResult<FSSODetPartSplit, FSSODet>)x)).LotSerialNbr) == false)
                                                                        .Select(y => (PXResult<FSSODetPartSplit, FSSODet>)y).ToList();

            if (linesWithLotSerial.Count > 0)
            {
                FSSODet currentFSSODet = (FSSODet)((PXResult<FSSODetPartSplit, FSSODet>)linesWithLotSerial.First());
                BqlCommand bqlCommand = BqlTemplate.OfCommand<
                                        Search2<INLotSerialStatus.lotSerialNbr,
                                        InnerJoin<INSiteLotSerial,
                                            On<INLotSerialStatus.inventoryID, Equal<INSiteLotSerial.inventoryID>,
                                            And<INLotSerialStatus.siteID, Equal<INSiteLotSerial.siteID>,
                                            And<INLotSerialStatus.lotSerialNbr, Equal<INSiteLotSerial.lotSerialNbr>>>>,
                                         InnerJoin<FSSODetPartSplit,
                                            On<FSSODetPartSplit.lotSerialNbr, Equal<INLotSerialStatus.lotSerialNbr>>>>,
                                        Where<
                                            INLotSerialStatus.inventoryID, Equal<Optional<BqlPlaceholder1>>,
                                            And<INLotSerialStatus.subItemID, Equal<Optional<BqlPlaceholder2>>,
                                            And<INLotSerialStatus.qtyOnHand, Greater<decimal0>,
                                            And<FSSODetPartSplit.srvOrdType, Equal<Required<FSSODetPartSplit.srvOrdType>>,
                                            And<FSSODetPartSplit.refNbr, Equal<Required<FSSODetPartSplit.refNbr>>,
                                            And<FSSODetPartSplit.lineNbr, Equal<Required<FSSODetPartSplit.lineNbr>>,
                                            And<
                                                Where<INLotSerialStatus.locationID, Equal<Optional<BqlPlaceholder3>>,
                                                   Or<Required<INLotSerialStatus.locationID>, IsNull>>>>>>>>>>>
                                        .Replace<BqlPlaceholder1>(this.InventoryType)
                                        .Replace<BqlPlaceholder2>(this.SubItemType)
                                        .Replace<BqlPlaceholder3>(this.LocationType)
                                        .ToCommand();

                PXView auxView = new PXView(_Graph, true, bqlCommand);

                return auxView.SelectMulti(
                    currentRow.InventoryID,
                    currentRow.SubItemID,
                    currentFSSODet.SrvOrdType,
                    currentFSSODet.RefNbr,
                    currentFSSODet.LineNbr,
                    currentRow.SiteLocationID,
                    currentRow.SiteLocationID);
            }
            else
            {
                var bqlCommand = BqlTemplate.OfCommand<
                                        Search2<INLotSerialStatus.lotSerialNbr,
                                        InnerJoin<INSiteLotSerial,
                                            On<INLotSerialStatus.inventoryID, Equal<INSiteLotSerial.inventoryID>,
                                            And<INLotSerialStatus.siteID, Equal<INSiteLotSerial.siteID>,
                                            And<INLotSerialStatus.lotSerialNbr, Equal<INSiteLotSerial.lotSerialNbr>>>>>,
                                         Where<
                                            INLotSerialStatus.inventoryID, Equal<Optional<BqlPlaceholder1>>,
                                            And<INLotSerialStatus.subItemID, Equal<Optional<BqlPlaceholder2>>,
                                            And<INLotSerialStatus.qtyOnHand, Greater<decimal0>,
                                            And<
                                                Where<INLotSerialStatus.locationID, Equal<Optional<BqlPlaceholder3>>,
                                                    Or<Optional<BqlPlaceholder4>, IsNull>>>>>>>>
                                        .Replace<BqlPlaceholder1>(this.InventoryType)
                                        .Replace<BqlPlaceholder2>(this.SubItemType)
                                        .Replace<BqlPlaceholder3>(this.LocationType)
                                        .Replace<BqlPlaceholder4>(this.LocationType)
                                        .ToCommand();

                PXView auxView = new PXView(_Graph, true, bqlCommand);
                var a  = auxView.SelectMulti();

                return a;
            }
        }
    }


    [PXDBInt()]
    [PXUIField(DisplayName = "Warehouse", Visibility = PXUIVisibility.Visible, FieldClass = SiteAttribute.DimensionName)]
    public class FSSiteAvailAttribute : SiteAvailAttribute
    {
        #region Ctor
        public FSSiteAvailAttribute(Type InventoryType, Type SubItemType)
            : base(InventoryType, SubItemType)
        {
        }
        #endregion
        #region Implementation
        public override void FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
        {
            base.FieldDefaulting(sender, e);

            FSSrvOrdType fsSrvOrdTypeRow = null;
            FSBranchLocation fsBranchLocationRow = null;

            if (sender.Graph is ServiceOrderEntry)
            {
                fsSrvOrdTypeRow = ((ServiceOrderEntry)sender.Graph).ServiceOrderTypeSelected.Current;
                fsBranchLocationRow = ((ServiceOrderEntry)sender.Graph).CurrentBranchLocation.Current;
            }
            else if (sender.Graph is AppointmentEntry)
            {
                fsSrvOrdTypeRow = ((AppointmentEntry)sender.Graph).ServiceOrderTypeSelected.Current;
                fsBranchLocationRow = ((AppointmentEntry)sender.Graph).CurrentBranchLocation.Current;
            }

            if (fsSrvOrdTypeRow?.PostTo == ID.SrvOrdType_PostTo.ACCOUNTS_RECEIVABLE_MODULE)
            {
                e.NewValue = null;
            }
            else if (fsSrvOrdTypeRow?.PostTo != ID.SrvOrdType_PostTo.ACCOUNTS_RECEIVABLE_MODULE && e.NewValue == null)
            {
                e.NewValue = fsBranchLocationRow?.DfltSiteID != null ? fsBranchLocationRow.DfltSiteID : e.NewValue;
            }
        }
        #endregion
    }
}
