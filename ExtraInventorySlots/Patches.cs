using HarmonyLib;

namespace ExtraInventorySlots;

[HarmonyPatch]
internal static class Patches
{
	// Every player inventory starts 8 x 4 (Humanoid): it gets the extra rows straight away, before the save is loaded
	[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
	[HarmonyPostfix]
	private static void PlayerAwake(Player __instance)
	{
		ExtraRows.Register(__instance);
		ExtraRows.Apply(__instance);
	}

	// The game sets the player's height itself (on spawn, when rows are bought from the trader, the inventorysize
	// command): the extra rows stay on top of it. Raising the height first also keeps the game's DropInvalidItems,
	// which runs right after, from dropping what is in the extra rows.
	[HarmonyPatch(typeof(Inventory), nameof(Inventory.SetHeight))]
	[HarmonyPrefix]
	private static void SetHeight(Inventory __instance, ref int height)
	{
		height = ExtraRows.AdjustHeight(__instance, height);
	}

	// The inventory and chest panels can be dragged around
	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
	[HarmonyPostfix]
	private static void InventoryGuiAwake(InventoryGui __instance)
	{
		DraggablePanels.Attach(__instance);
	}

	// The panel's frame follows the rows the inventory has, whatever set them
	[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateInventory))]
	[HarmonyPostfix]
	private static void UpdateInventory(InventoryGui __instance, Player player)
	{
		ExtraRows.FitPanel(__instance, player?.m_inventory);
	}
}
