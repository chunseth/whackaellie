using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Raises OnDown when this UI object (or a child without its own handler) is pressed.
/// Used to let players tap the reward panel to skip the coin count-up.
/// </summary>
public class PointerDownRelay : MonoBehaviour, IPointerDownHandler
{
    public event Action OnDown;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDown?.Invoke();
    }
}
