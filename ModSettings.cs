using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;

namespace ZoningToolkit
{
    [FileLocation(Mod.ModName)]
    [SettingsUIMouseAction(ApplyActionName, "TestUsage")]
    [SettingsUIMouseAction(CancelActionName, "TestUsage")]
    internal class ModSettings : ModSetting
    {
        internal const string ApplyActionName = "ZoningToolkitApply";
        internal const string CancelActionName = "ZoningToolkitCancel";
        public ModSettings(IMod mod) : base(mod)
        {
        }

        public override void SetDefaults()
        {
            //TODO: Add defaults here
        }

        /// <summary>
        /// Gets or sets the Line Tool apply action (copied from game action).
        /// </summary>
        [SettingsUIMouseBinding(ApplyActionName)]
        [SettingsUIHidden]
        public ProxyBinding ZoningToolkitApply { get; set; }

        /// <summary>
        /// Gets or sets the Line Tool apply action (copied from game action).
        /// </summary>
        [SettingsUIMouseBinding(CancelActionName)]
        [SettingsUIHidden]
        public ProxyBinding ZoningToolkitCancel { get; set; }

    }
}
