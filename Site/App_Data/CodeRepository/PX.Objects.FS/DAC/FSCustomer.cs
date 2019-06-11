using PX.Data;
using PX.Objects.AR;

namespace PX.Objects.FS
{
    [System.SerializableAttribute]
    [PXPrimaryGraph(typeof(CustomerMaintBridge))]
    public partial class FSCustomer : Customer
    {
        public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
    }
}
