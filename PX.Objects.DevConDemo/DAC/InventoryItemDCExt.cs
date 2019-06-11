using PX.Data;

namespace PX.Objects.IN
{
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