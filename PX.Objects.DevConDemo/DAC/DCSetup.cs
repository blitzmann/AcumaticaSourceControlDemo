using System;
using PX.Data;

namespace PX.Objects.DevConDemo
{
    [Serializable]
    public class DCSetup : IBqlTable
    {
        #region PluginOnPublished
        [PXDBString(500, IsUnicode = true, InputMask = "")]
        [PXUIField(DisplayName = "On Published Msg")]
        public virtual string PluginOnPublished { get; set; }
        public abstract class pluginOnPublished : PX.Data.BQL.BqlString.Field<pluginOnPublished> { }
        #endregion

        #region PluginUpdateDatabase
        [PXDBString(500, IsUnicode = true, InputMask = "")]
        [PXUIField(DisplayName = "Update Database Msg")]
        public virtual string PluginUpdateDatabase { get; set; }
        public abstract class pluginUpdateDatabase : PX.Data.BQL.BqlString.Field<pluginUpdateDatabase> { }
        #endregion
    }
}