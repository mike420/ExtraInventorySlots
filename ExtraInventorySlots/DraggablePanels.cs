using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExtraInventorySlots;

/// <summary>
/// Lets the player inventory panel and the container panel be moved with the mouse. Where each was dragged to is kept
/// as an offset from where the game puts it, so turning the setting off (or removing the mod) leaves the game's layout.
/// </summary>
internal static class DraggablePanels
{
	internal static void Attach(InventoryGui gui)
	{
		if (gui == null)
		{
			return;
		}
		Add(gui.m_player, MainConfig.inventoryPanelX, MainConfig.inventoryPanelY);
		Add(gui.m_container, MainConfig.containerPanelX, MainConfig.containerPanelY);
	}

	internal static void Detach(InventoryGui gui)
	{
		if (gui == null)
		{
			return;
		}
		foreach (RectTransform panel in new[] { gui.m_player, gui.m_container })
		{
			DraggablePanel drag = panel != null ? panel.GetComponent<DraggablePanel>() : null;
			if (drag != null)
			{
				Object.Destroy(drag);
			}
		}
	}

	internal static void ResetPositions()
	{
		MainConfig.inventoryPanelX.Value = 0f;
		MainConfig.inventoryPanelY.Value = 0f;
		MainConfig.containerPanelX.Value = 0f;
		MainConfig.containerPanelY.Value = 0f;
	}

	private static void Add(RectTransform panel, ConfigEntry<float> x, ConfigEntry<float> y)
	{
		if (panel == null || panel.GetComponent<DraggablePanel>() != null)
		{
			return;
		}
		DraggablePanel drag = panel.gameObject.AddComponent<DraggablePanel>();
		drag.OffsetX = x;
		drag.OffsetY = y;
	}
}

/// <summary>
/// Drags its panel by any part that is not a slot, button, scroll bar or text field, so moving items is unaffected.
/// </summary>
internal sealed class DraggablePanel : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	// How much of the panel stays on screen, so it can always be grabbed again
	private const float Keep = 80f;

	internal ConfigEntry<float> OffsetX;

	internal ConfigEntry<float> OffsetY;

	private RectTransform m_rect;

	private Canvas m_canvas;

	// Where the game puts the panel, and what this last set it to: anything else that moves it changes the first
	private Vector2 m_base;

	private Vector2 m_written;

	private bool m_hasWritten;

	private Vector2 m_lastWanted;

	private Vector2Int m_lastScreen;

	private bool m_dragging;

	private Vector2 m_dragOffset;

	private Vector2 m_dragStartOffset;

	private Vector2 m_dragStartPointer;

	private readonly Vector3[] m_corners = new Vector3[4];

	private Vector2 Offset
	{
		get
		{
			if (m_dragging)
			{
				return m_dragOffset;
			}
			if (!MainConfig.draggablePanels.Value || OffsetX == null || OffsetY == null)
			{
				return Vector2.zero;
			}
			return new Vector2(OffsetX.Value, OffsetY.Value);
		}
	}

	private void Awake()
	{
		m_rect = (RectTransform)transform;
	}

	// After the game and its animator have placed the panel for this frame
	private void LateUpdate()
	{
		Vector2 current = m_rect.anchoredPosition;
		bool moved = !m_hasWritten || current != m_written;
		if (moved)
		{
			m_base = current;
		}
		Vector2 wanted = Offset;
		Vector2Int screen = new Vector2Int(Screen.width, Screen.height);
		if (moved || wanted != m_lastWanted || screen != m_lastScreen)
		{
			m_lastWanted = wanted;
			m_lastScreen = screen;
			Place(wanted);
		}
	}

	private void OnDestroy()
	{
		if (m_rect != null && m_hasWritten && m_rect.anchoredPosition == m_written)
		{
			m_rect.anchoredPosition = m_base;
		}
	}

	public void OnInitializePotentialDrag(PointerEventData eventData)
	{
		if (!MainConfig.draggablePanels.Value || eventData.button != PointerEventData.InputButton.Left || IsControl(eventData.pointerCurrentRaycast.gameObject))
		{
			// Nothing gets the drag, and slots and buttons get their clicks as usual
			eventData.pointerDrag = null;
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (!PointerInParent(eventData, out m_dragStartPointer))
		{
			return;
		}
		m_dragStartOffset = m_dragOffset = Offset;
		m_dragging = true;
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!m_dragging || !PointerInParent(eventData, out Vector2 pointer))
		{
			return;
		}
		m_dragOffset = Place(m_dragStartOffset + pointer - m_dragStartPointer);
		m_lastWanted = m_dragOffset;
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (!m_dragging)
		{
			return;
		}
		m_dragging = false;
		OffsetX.Value = m_dragOffset.x;
		OffsetY.Value = m_dragOffset.y;
		m_lastWanted = Offset;
		ExtraInventorySlotsPlugin.ConfigFile.Save();
	}

	// Slots, buttons, scroll bars and text fields, between where the pointer is and this panel
	private bool IsControl(GameObject target)
	{
		if (target == null)
		{
			return true;
		}
		for (Transform t = target.transform; t != null; t = t.parent)
		{
			if (t == transform)
			{
				return false;
			}
			if (t.GetComponent<InventoryGrid>() != null || t.GetComponent<Selectable>() != null || t.GetComponent<UIInputHandler>() != null)
			{
				return true;
			}
		}
		return true;
	}

	private bool PointerInParent(PointerEventData eventData, out Vector2 point)
	{
		point = default;
		RectTransform parent = m_rect.parent as RectTransform;
		return parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out point);
	}

	/// <summary>Moves the panel to the game's place plus the offset, kept on screen; returns the offset used.</summary>
	private Vector2 Place(Vector2 offset)
	{
		SetPosition(m_base + offset);
		if (m_canvas == null)
		{
			m_canvas = GetComponentInParent<Canvas>()?.rootCanvas;
		}
		RectTransform canvasRect = m_canvas != null ? m_canvas.transform as RectTransform : null;
		RectTransform parent = m_rect.parent as RectTransform;
		if (canvasRect == null || parent == null || offset == Vector2.zero)
		{
			return offset;
		}
		m_rect.GetWorldCorners(m_corners);
		Vector3 min = canvasRect.InverseTransformPoint(m_corners[0]);
		Vector3 max = canvasRect.InverseTransformPoint(m_corners[2]);
		Rect screen = canvasRect.rect;
		Vector2 shift = Vector2.zero;
		if (max.x < screen.xMin + Keep)
		{
			shift.x = screen.xMin + Keep - max.x;
		}
		else if (min.x > screen.xMax - Keep)
		{
			shift.x = screen.xMax - Keep - min.x;
		}
		// The top edge stays on screen: it is where the panel is easiest to grab
		if (max.y > screen.yMax)
		{
			shift.y = screen.yMax - max.y;
		}
		else if (max.y < screen.yMin + Keep)
		{
			shift.y = screen.yMin + Keep - max.y;
		}
		if (shift != Vector2.zero)
		{
			offset += (Vector2)parent.InverseTransformVector(canvasRect.TransformVector(shift));
			SetPosition(m_base + offset);
		}
		return offset;
	}

	private void SetPosition(Vector2 position)
	{
		if (m_rect.anchoredPosition != position)
		{
			m_rect.anchoredPosition = position;
		}
		m_written = m_rect.anchoredPosition;
		m_hasWritten = true;
	}
}
