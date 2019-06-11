using System;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.DevConDemo
{
    public class DCInventoryInquiry : PXGraph
    {
        public PXCancel<InventoryInquiryFilter> Cancel;

        public PXFilter<InventoryInquiryFilter> Filter;

        public PXSelectJoin<InventoryItem,
            LeftJoin<INItemClass, On<InventoryItem.itemClassID, Equal<INItemClass.itemClassID>>,
            LeftJoin<INLotSerClass, On<InventoryItem.lotSerClassID, Equal<INLotSerClass.lotSerClassID>>>>> InventoryItems;
    }

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
    }
}