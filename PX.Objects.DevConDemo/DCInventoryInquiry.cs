using System;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.DevConDemo
{
    public class DCInventoryInquiry : PXGraph
    {
        public PXCancel<InventoryInquiryFilter> Cancel;

        public PXFilter<InventoryInquiryFilter> Filter;

        public PXSelectJoin<
            InventoryItem,
            LeftJoin<INItemClass, 
                On<InventoryItem.itemClassID, Equal<INItemClass.itemClassID>>,
            LeftJoin<INLotSerClass, 
                On<InventoryItem.lotSerClassID, Equal<INLotSerClass.lotSerClassID>>>>,
            Where2<Where<InventoryItem.itemClassID, Equal<Current<InventoryInquiryFilter.itemClassID>>,
                    Or<Current<InventoryInquiryFilter.itemClassID>, IsNull>>,
            And2<Where<InventoryItem.stkItem, Equal<True>,
                Or<Current<InventoryInquiryFilter.includeNonStkItem>, Equal<True>>>,
            And<Where<InventoryItemDCExt.usrRepSource, Equal<Current<InventoryInquiryFilter.replenishmentSource>>,
                Or<Current<InventoryInquiryFilter.replenishmentSource>, IsNull>>>>>>
            InventoryItems;

        public DCInventoryInquiry()
        {
            InventoryItems.AllowUpdate = false;
        }

        public class InventoryItemDCExt : PXCacheExtension<InventoryItem>
        {
            #region UsrRepSource
            [PXDBString(1, IsFixed = true)]
            [PXUIField(DisplayName = "Source")]
            [INReplenishmentSource.List]
            [PXDefault(INReplenishmentSource.Purchased, PersistingCheck = PXPersistingCheck.Nothing)]
            public virtual string UsrRepSource { get; set; }
            public abstract class usrRepSource : PX.Data.BQL.BqlString.Field<usrRepSource> { }
            #endregion
        }
    }

    [Serializable]
    [PXCacheName("Inventory Inquiry Filter")]
    public class InventoryInquiryFilter : IBqlTable
    {
        #region ReplenishmentSource
        public abstract class replenishmentSource : PX.Data.BQL.BqlString.Field<replenishmentSource> { }

        protected string _ReplenishmentSource;
        [PXString(1)]
        [PXUIField(DisplayName = "Replenishment Source")]
        [INReplenishmentSource.List]
        public virtual string ReplenishmentSource
        {
            get { return this._ReplenishmentSource; }
            set { this._ReplenishmentSource = value; }
        }
        #endregion

        #region ItemClassID
        public abstract class itemClassID : PX.Data.BQL.BqlInt.Field<itemClassID> { }
        protected int? _ItemClassID;

        [PXDBInt]
        [PXUIField(DisplayName = "Item Class")]
        [PXDimensionSelector(INItemClass.Dimension, typeof(Search<INItemClass.itemClassID>), typeof(INItemClass.itemClassCD), DescriptionField = typeof(INItemClass.descr))]
        public virtual int? ItemClassID
        {
            get
            {
                return this._ItemClassID;
            }
            set
            {
                this._ItemClassID = value;
            }
        }
        #endregion

        #region IncludeNonStkItem
        public abstract class includeNonStkItem : PX.Data.BQL.BqlBool.Field<includeNonStkItem> { }
        protected Boolean? _IncludeNonStkItem;

        [PXDBBool]
        [PXUnboundDefault(false)]
        [PXUIField(DisplayName = "Include Non-Stock Items")]
        public virtual Boolean? IncludeNonStkItem
        {
            get
            {
                return this._IncludeNonStkItem;
            }
            set
            {
                this._IncludeNonStkItem = value;
            }
        }
        #endregion
    }
}