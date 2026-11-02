using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using UnityEngine;

namespace ExtraInventorySlots;

/// <summary>
/// The player inventory is made taller by <see cref="MainConfig.extraRows"/>. The game keeps its own row count (4, more
/// once bought from the trader) in the player's "invrows" key; the extra rows are only ever added on top of it and never
/// written into that key, so removing the mod gives the game's own size back.
/// </summary>
internal static class ExtraRows
{
	internal const int MaxExtraRows = 6;

	// The game clamps its own row count to 0-9 (Player.SetInventorySize)
	private const int MaxVanillaRows = 9;

	private const int DefaultRows = 4;

	private const string RowsKey = "invrows";

	internal static ManualLogSource Log;

	internal static bool Active;

	// Player inventories (the only ones that get the extra rows); set while this mod sets a height itself
	private static readonly ConditionalWeakTable<Inventory, Player> PlayerInventories = new ConditionalWeakTable<Inventory, Player>();

	private static bool settingHeight;

	internal static int Extra => Active ? Mathf.Clamp(MainConfig.extraRows.Value, 0, MaxExtraRows) : 0;

	internal static void Register(Player player)
	{
		Inventory inventory = player?.m_inventory;
		if (inventory != null && !PlayerInventories.TryGetValue(inventory, out _))
		{
			PlayerInventories.Add(inventory, player);
		}
	}

	internal static bool IsPlayerInventory(Inventory inventory)
	{
		return inventory != null && PlayerInventories.TryGetValue(inventory, out _);
	}

	/// <summary>The height the game itself gives this player: 4, or what was bought from the trader.</summary>
	internal static int VanillaRows(Player player)
	{
		if (player != null && player.TryGetUniqueKeyValue(RowsKey, out string value) && int.TryParse(value, out int rows))
		{
			return Mathf.Clamp(rows, 0, MaxVanillaRows);
		}
		return DefaultRows;
	}

	/// <summary>Inventory.SetHeight on a player inventory: the game's rows plus the extra ones.</summary>
	internal static int AdjustHeight(Inventory inventory, int height)
	{
		if (settingHeight || !IsPlayerInventory(inventory))
		{
			return height;
		}
		return height + Extra;
	}

	/// <summary>Sets the player's inventory to the game's rows plus the extra ones.</summary>
	internal static void Apply(Player player)
	{
		Inventory inventory = player?.m_inventory;
		if (inventory == null)
		{
			return;
		}
		int oldHeight = inventory.m_height;
		int newHeight = VanillaRows(player) + Extra;
		if (newHeight == oldHeight)
		{
			return;
		}
		settingHeight = true;
		try
		{
			inventory.SetHeight(newHeight);
		}
		finally
		{
			settingHeight = false;
		}
		if (newHeight < oldHeight)
		{
			MoveOutOfRemovedRows(player, inventory, newHeight, oldHeight);
		}
		inventory.Changed();
	}

	internal static void ApplyToLocalPlayer()
	{
		if (Player.m_localPlayer != null)
		{
			Apply(Player.m_localPlayer);
		}
	}

	/// <summary>
	/// Items left in rows that were just removed: into a free cell, or dropped at the player's feet. Only those rows are
	/// looked at, so items other mods keep below the grid (BetterEquipSlots' slots, from row 20) are left alone.
	/// </summary>
	private static void MoveOutOfRemovedRows(Player player, Inventory inventory, int newHeight, int oldHeight)
	{
		List<ItemDrop.ItemData> stranded = new List<ItemDrop.ItemData>();
		foreach (ItemDrop.ItemData item in inventory.m_inventory)
		{
			if (item.m_gridPos.y >= newHeight && item.m_gridPos.y < oldHeight && item.m_gridPos.x >= 0 && item.m_gridPos.x < inventory.m_width)
			{
				stranded.Add(item);
			}
		}
		int dropped = 0;
		foreach (ItemDrop.ItemData item in stranded)
		{
			Vector2i free = inventory.FindEmptySlot(inventory.TopFirst(item));
			if (free.x >= 0)
			{
				item.m_gridPos = free;
			}
			else if (player.DropItem(inventory, item, item.m_stack))
			{
				dropped++;
			}
		}
		if (dropped > 0)
		{
			Log?.LogInfo($"Inventory made smaller: {dropped} item(s) that did not fit were dropped.");
		}
	}

	/// <summary>Grows the inventory panel's frame to the rows the player inventory has now.</summary>
	internal static void FitPanel(InventoryGui gui, Inventory inventory)
	{
		if (gui == null || inventory == null || gui.m_player == null || gui.m_invGridHeight <= 0f)
		{
			return;
		}
		float height = gui.m_playerHeight + (inventory.m_height - DefaultRows) * gui.m_invGridHeight;
		Vector2 size = gui.m_player.sizeDelta;
		if (!Mathf.Approximately(size.y, height))
		{
			gui.m_player.sizeDelta = new Vector2(size.x, height);
		}
	}
}
