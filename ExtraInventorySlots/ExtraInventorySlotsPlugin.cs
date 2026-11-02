using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;

namespace ExtraInventorySlots;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("MagicMike.BetterEquipSlots", BepInDependency.DependencyFlags.SoftDependency)]
[NetworkCompatibility(CompatibilityLevel.ClientMustHaveMod, VersionStrictness.Minor)]
public class ExtraInventorySlotsPlugin : BaseUnityPlugin
{
	public const string PluginName = "ExtraInventorySlots";
	public const string PluginVersion = "1.0.0";
	public const string PluginAuthor = "MagicMike";
	internal const string PluginGUID = "MagicMike.ExtraInventorySlots";
	internal const string PluginCopy = "Copyright © MagicMike 2026";
	public const string PluginLink = "https://discord.gg/mnr36G6J4x";

	public static ExtraInventorySlotsPlugin plugininstance;

	internal static ConfigFile ConfigFile;
	internal static ConfigFileWatcher ConfigFileWatcher;

	private Harmony harmony;

	public void Awake()
	{
		plugininstance = this;
		ExtraRows.Log = Logger;
		ConfigFile = Config;
		ConfigFileWatcher = new ConfigFileWatcher(Config, 1000L);
		Config.SaveOnConfigSet = false;
		MainConfig.InitConfigs(Config);

		MagicMike.ModSettings.ModSettings.Init(this);

		UpdatePlugin();

		if (!MainConfig.modEnabled.Value)
		{
			return;
		}

		ExtraRows.Active = true;
		harmony = new Harmony(PluginGUID);
		harmony.PatchAll(Assembly.GetExecutingAssembly());

		// The server's value arrives through config sync, so this also covers joining a server
		MainConfig.extraRows.SettingChanged += (_, _) => ExtraRows.ApplyToLocalPlayer();

		ConfigFileWatcher.OnConfigFileReloaded += delegate
		{
			UpdatePlugin(saveConfig: false);
		};
		SynchronizationManager.OnConfigurationWindowClosed += delegate
		{
			UpdatePlugin();
		};

		Game.isModded = true;

		// Reloaded with a world already running: resize the player that is already there
		if (Player.m_localPlayer != null)
		{
			ExtraRows.Register(Player.m_localPlayer);
			ExtraRows.ApplyToLocalPlayer();
		}
	}

	private void OnDestroy()
	{
		if (!ExtraRows.Active)
		{
			return;
		}
		harmony?.UnpatchSelf();
	}

	private void UpdatePlugin(bool saveConfig = true)
	{
		if (saveConfig)
		{
			base.Config.Save();
		}
	}
}
