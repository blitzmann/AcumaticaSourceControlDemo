using Customization;
using PX.Data;

namespace PX.Objects.DevConDemo
{
    //The customization plug-in is used to execute custom actions after the customization project has been published
    // To Add a Customization Plug-In to a Project:
    //      https://help-2019r1.acumatica.com/?ScreenId=ShowWiki&pageid=c69443fe-4d32-47a9-85aa-b2882aa259ef
    public class PluginTest : CustomizationPlugin
    {
        protected string TimeStampString => PX.Common.PXTimeZoneInfo.Now.ToString("yy-MM-dd hh:mm:ss");

        //This method is executed right after website files are updated, but before the website is restarted
        //The method is invoked on each cluster node in a cluster environment
        //The method is invoked only if runtime compilation is enabled
        //Do not access custom code published to bin folder; it may not be loaded yet
        public override void OnPublished()
        {
            InsertUpdateSetup<DCSetup.pluginOnPublished>($"OnPublished;{TimeStampString}");
        }

        //This method is executed after the customization has been published and the website is restarted.
        public override void UpdateDatabase()
        {
            InsertUpdateSetup<DCSetup.pluginUpdateDatabase>($"UpdateDatabase;{TimeStampString}");
        }

        protected void InsertUpdateSetup<Field>(string msg)
            where Field : IBqlField
        {
            WriteLog(msg);
            if (!PXDatabase.Update<DCSetup>(new PXDataFieldAssign<Field>($"{msg};UPDATE")))
            {
                //Insert...
                PXDatabase.Insert<DCSetup>(new PXDataFieldAssign<Field>($"{msg};INSERT"));
            }
        }
    }
}