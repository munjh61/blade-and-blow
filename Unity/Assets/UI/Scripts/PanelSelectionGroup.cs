using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PanelSelectionGroup : MonoBehaviour
{
    private readonly List<PanelSelectable> _items = new();
    private PanelSelectable _selected;

    [System.Serializable] public class IntEvent : UnityEvent<int> { }
    public IntEvent onSelectedIndexChanged = new();

    public void Register(PanelSelectable item)
    {
        if (item == null || _items.Contains(item)) return;
        _items.Add(item);

        item.SetSelected(false);
    }

    public void Unregister(PanelSelectable item)
    {
        if (item == null) return;
        _items.Remove(item);

        if (_selected == item) _selected = null;
    }
    public void Select(PanelSelectable target)
    {
        if (target == null) return;
        if (_selected == target) return;

        _selected = target;
        foreach (var it in _items)
            it.SetSelected(it == target);

        onSelectedIndexChanged.Invoke(SelectedIndex);
    }

    public void SelectFirstIfNone()
    {
        if (_selected == null && _items.Count > 0)
            Select(_items[0]);
    }

    public int SelectedIndex => _selected == null ? -1 : _items.IndexOf(_selected);
    public PanelSelectable Current => _selected;
}
