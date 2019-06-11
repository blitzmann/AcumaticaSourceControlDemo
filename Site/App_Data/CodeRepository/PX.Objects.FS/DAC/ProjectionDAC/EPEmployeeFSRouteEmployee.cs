using System;
using PX.Data;
using PX.Data.EP;
using PX.Objects.CR;
using PX.Objects.EP;

namespace PX.Objects.FS
{
    [Serializable]
    [PXPrimaryGraph(typeof(EPEmployeeMaintBridge))]
    [PXProjection(typeof(
        Select2<EPEmployee,
            InnerJoin<FSRouteEmployee,
                On<FSRouteEmployee.employeeID, Equal<EPEmployee.bAccountID>>>>))]
    public class EPEmployeeFSRouteEmployee : IBqlTable
    {
        #region BAccountID
        public abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }

        [PXDBInt(IsKey = true, BqlField = typeof(EPEmployee.bAccountID))]
        [PXUIField(Visible = false, Visibility = PXUIVisibility.Invisible)]
        public virtual int? BAccountID { get; set; }
        #endregion

        #region AcctCD
        public abstract class acctCD : PX.Data.BQL.BqlString.Field<acctCD> { }

        [EmployeeRaw]
        [PXDBString(30, IsUnicode = true, IsKey = true, InputMask = "", BqlField = typeof(EPEmployee.acctCD))]
        [PXDefault]
        [PXFieldDescription]
        [PXUIField(DisplayName = "Driver ID", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual string AcctCD { get; set; }
        #endregion

        #region AcctName
        public abstract class acctName : PX.Data.BQL.BqlString.Field<acctName> { }

        [PXDBString(60, IsUnicode = true, BqlField = typeof(EPEmployee.acctName))]
        [PXDefault]
        [PXUIField(DisplayName = "Driver Name", Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
        public virtual string AcctName { get; set; }
        #endregion

        #region RouteID
        public abstract class routeID : PX.Data.BQL.BqlInt.Field<routeID> { }

        [PXDefault]
        [PXDBInt(BqlField = typeof(FSRouteEmployee.routeID))]
        public virtual int? RouteID { get; set; }
        #endregion

        #region PriorityPreference
        public abstract class priorityPreference : PX.Data.BQL.BqlInt.Field<priorityPreference> { }

        [PXDBInt(BqlField = typeof(FSRouteEmployee.priorityPreference))]
        [PXDefault(1)]
        [PXUIField(DisplayName = "Priority Preference", Required = true, Visibility = PXUIVisibility.SelectorVisible)]
        public virtual int? PriorityPreference { get; set; }
        #endregion

        #region Status
        public abstract class status : BAccount.status
        {
        }

        [PXDBString(1, IsFixed = true, BqlField = typeof(EPEmployee.status))]
        [PXUIField(DisplayName = "Status", Visibility = PXUIVisibility.SelectorVisible)]
        [EPEmployee.status.List]
        public virtual string Status { get; set; }
        #endregion

        #region DepartmentID
        public abstract class departmentID : PX.Data.BQL.BqlString.Field<departmentID> { }

        [PXDBString(10, IsUnicode = true, BqlField = typeof(EPEmployee.departmentID))]
        [PXDefault]
        [PXSelector(typeof(EPDepartment.departmentID), DescriptionField = typeof(EPDepartment.description))]
        [PXUIField(DisplayName = "Department", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual string DepartmentID { get; set; }
        #endregion


        #region Memory Helper
        #region MemDriverName
        public abstract class memDriverName : PX.Data.BQL.BqlString.Field<memDriverName>
		{
        }

        [PXString]
        [PXUIField(DisplayName = "Driver Name", Enabled = false)]
        public virtual string MemDriverName { get; set; }
        #endregion
        #endregion
    }
}
