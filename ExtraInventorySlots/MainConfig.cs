using BepInEx.Configuration;
using Jotunn.Extensions;

namespace ExtraInventorySlots;

internal static class MainConfig
{
	public static ConfigEntry<bool> modEnabled;

	public static ConfigEntry<int> extraRows;

	public static void InitConfigs(ConfigFile config)
	{
		modEnabled = config.BindConfigInOrder("General Settings", "Enable Plugin", defaultValue: true, "Enable plugin: extra rows in the player inventory. Off: the game as without the mod.\n(Restart required. It is read before connecting to a server, so it is a local setting).\n", synced: false, sectionOrder: true, settingOrder: true);

		extraRows = config.BindConfigInOrder("Inventory", "Extra Rows", defaultValue: 2, "Rows added to the bottom of the player inventory, on top of the game's own (4, plus any bought from the trader). Lowering it moves items out of the removed rows into free cells, or drops them at your feet when the inventory is full.", synced: true, sectionOrder: true, settingOrder: true, acceptableValues: new AcceptableValueRange<int>(0, ExtraRows.MaxExtraRows));
	}
}
