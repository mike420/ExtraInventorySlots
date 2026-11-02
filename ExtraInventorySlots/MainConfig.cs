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

		draggablePanels = config.BindConfigInOrder("Panels", "Draggable Panels", defaultValue: true, "Move the inventory panel and the chest panel by dragging their frame with the left mouse button (not a slot or button). They open where you last left them. Turning this off puts both back where the game has them.", synced: false, sectionOrder: true, settingOrder: true);

		// Hidden: set by dragging. Offsets from where the game puts each panel, in UI units.
		inventoryPanelX = BindHidden(config, "Inventory Panel X", "How far the inventory panel was dragged to the right of where the game puts it.");
		inventoryPanelY = BindHidden(config, "Inventory Panel Y", "How far the inventory panel was dragged up from where the game puts it.");
		containerPanelX = BindHidden(config, "Chest Panel X", "How far the chest panel was dragged to the right of where the game puts it.");
		containerPanelY = BindHidden(config, "Chest Panel Y", "How far the chest panel was dragged up from where the game puts it.");
	}

	public static ConfigEntry<bool> draggablePanels;

	public static ConfigEntry<float> inventoryPanelX;

	public static ConfigEntry<float> inventoryPanelY;

	public static ConfigEntry<float> containerPanelX;

	public static ConfigEntry<float> containerPanelY;

	private static ConfigEntry<float> BindHidden(ConfigFile config, string key, string description)
	{
		return config.BindConfigInOrder("Panels", key, defaultValue: 0f, description, synced: false, sectionOrder: true, settingOrder: true, configAttributes: new ConfigurationManagerAttributes { Browsable = false });
	}
}
