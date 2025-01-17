﻿using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using System;

namespace PX.Objects.SO
{
	[PXCacheName(Messages.SOPickPackShipSetup, PXDacType.Config)]
	public class SOPickPackShipSetup : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<SOPickPackShipSetup>.By<branchID>
		{
			public static SOPickPackShipSetup Find(PXGraph graph, int? branchID) => FindBy(graph, branchID);
		}
		#endregion

		#region BranchID
		[PXDBInt(IsKey = true)]
		[PXDefault(typeof(Search<GL.Branch.branchID, Where<GL.Branch.branchID, Equal<Current<AccessInfo.branchID>>>>))]
		[PXUIField(DisplayName = "Branch")]
		public virtual int? BranchID { get; set; }
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		#endregion

		#region ShowPickTab
		[PXDBBool]
		[PXDefault(true)]
		[PXUIField(DisplayName = "Display the Pick Tab", Enabled = true)]
		public virtual bool? ShowPickTab { get; set; }
		public abstract class showPickTab : PX.Data.BQL.BqlBool.Field<showPickTab> { }
		#endregion
		#region ShowPackTab
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Display the Pack Tab", Enabled = true)]
		public virtual bool? ShowPackTab { get; set; }
		public abstract class showPackTab : PX.Data.BQL.BqlBool.Field<showPackTab> { }
		#endregion
		#region ShowShipTab
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Display the Ship Tab", Enabled = true)]
		public virtual bool? ShowShipTab { get; set; }
		public abstract class showShipTab : PX.Data.BQL.BqlBool.Field<showShipTab> { }
		#endregion
		#region ShowScanLogTab
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Display the Scan Log Tab", Enabled = true)]
		public virtual bool? ShowScanLogTab { get; set; }
		public abstract class showScanLogTab : PX.Data.BQL.BqlBool.Field<showScanLogTab> { }
		#endregion

		#region UseCartsForPick
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Use Carts for Picking", FieldClass = "Carts")]
		[PXUIEnabled(typeof(showPickTab))]
		[PXFormula(typeof(Switch<Case<Where<showPickTab, Equal<False>>, False>, useCartsForPick>))]
		public virtual bool? UseCartsForPick { get; set; }
		public abstract class useCartsForPick : PX.Data.BQL.BqlBool.Field<useCartsForPick> { }
		#endregion
		#region ExplicitLineConfirmation
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Use Explicit Line Confirmation")]
		public virtual bool? ExplicitLineConfirmation { get; set; }
		public abstract class explicitLineConfirmation : PX.Data.BQL.BqlBool.Field<explicitLineConfirmation> { }
		#endregion
		#region UseDefaultQty
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Use Default Quantity")]
		public virtual bool? UseDefaultQty { get; set; }
		public abstract class useDefaultQty : PX.Data.BQL.BqlBool.Field<useDefaultQty> { }
		#endregion


		#region DefaultLocationFromShipment
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Use Default Location")]
		public virtual bool? DefaultLocationFromShipment { get; set; }
		public abstract class defaultLocationFromShipment : PX.Data.BQL.BqlBool.Field<defaultLocationFromShipment> { }
		#endregion

		#region DefaultLotSerialFromShipment
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Use Default Lot/Serial Nbr.")]
		public virtual bool? DefaultLotSerialFromShipment { get; set; }
		public abstract class defaultLotSerialFromShipment : PX.Data.BQL.BqlBool.Field<defaultLotSerialFromShipment> { }
		#endregion

		#region PrintShipmentConfirmation
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Print Shipment Confirmation Automatically", FieldClass = "DeviceHub")]
		public virtual bool? PrintShipmentConfirmation { get; set; }
		public abstract class printShipmentConfirmation : PX.Data.BQL.BqlBool.Field<printShipmentConfirmation> { }
		#endregion

		#region PrintShipmentLabels
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Print Shipment Labels Automatically", FieldClass = "DeviceHub")]
		public virtual bool? PrintShipmentLabels { get; set; }
		public abstract class printShipmentLabels : PX.Data.BQL.BqlBool.Field<printShipmentLabels> { }
		#endregion

		#region EnterSizeForPackages
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Enter Length/Width/Height for Packages", Visible = false)]
		public virtual bool? EnterSizeForPackages { get; set; }
		public abstract class enterSizeForPackages : PX.Data.BQL.BqlBool.Field<enterSizeForPackages> { }
		#endregion

		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp>
		{
		}
		protected Byte[] _tstamp;
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID>
		{
		}
		protected Guid? _CreatedByID;
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID
		{
			get
			{
				return this._CreatedByID;
			}
			set
			{
				this._CreatedByID = value;
			}
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID>
		{
		}
		protected String _CreatedByScreenID;
		[PXDBCreatedByScreenID()]
		public virtual String CreatedByScreenID
		{
			get
			{
				return this._CreatedByScreenID;
			}
			set
			{
				this._CreatedByScreenID = value;
			}
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime>
		{
		}
		protected DateTime? _CreatedDateTime;
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime
		{
			get
			{
				return this._CreatedDateTime;
			}
			set
			{
				this._CreatedDateTime = value;
			}
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID>
		{
		}
		protected Guid? _LastModifiedByID;
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID
		{
			get
			{
				return this._LastModifiedByID;
			}
			set
			{
				this._LastModifiedByID = value;
			}
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID>
		{
		}
		protected String _LastModifiedByScreenID;
		[PXDBLastModifiedByScreenID()]
		public virtual String LastModifiedByScreenID
		{
			get
			{
				return this._LastModifiedByScreenID;
			}
			set
			{
				this._LastModifiedByScreenID = value;
			}
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime>
		{
		}
		protected DateTime? _LastModifiedDateTime;
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime
		{
			get
			{
				return this._LastModifiedDateTime;
			}
			set
			{
				this._LastModifiedDateTime = value;
			}
		}
		#endregion

	}
}