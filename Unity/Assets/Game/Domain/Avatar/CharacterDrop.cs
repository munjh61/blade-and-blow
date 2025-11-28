using System;
using System.Collections.Generic;
using Game.Domain;
using UnityEngine;

public class CharacterDrop : MonoBehaviour
{
    [Header("Drop Prefabs")]
    public GameObject[] dropPrefabs;

    [Header("Runtime Registry (Read-only)")]
    [SerializeField] private List<DroppedItem> _items = new();
    public IReadOnlyList<DroppedItem> Items => _items;

    public event Action<IReadOnlyList<DroppedItem>> OnListChanged;

    private static readonly Dictionary<string, GameObject> Catalog = new();


    [Serializable]
    public struct DroppedItem
    {
        public int equipId;
        public string prefabName;
        public Vector3 position;
        public bool pending;
        public int viewId;
    }

    public string ResolvePrefabName(int equipId)
    {
        if (dropPrefabs == null || equipId < 0 || equipId >= dropPrefabs.Length) return null;
        var go = dropPrefabs[equipId];
        return go != null ? go.name : null;
    }

    public void AddPending(int equipId, string prefabName, Vector3 pos)
    {
        _items.Add(new DroppedItem
        {
            equipId = equipId,
            prefabName = prefabName,
            position = pos,
            pending = true,
            viewId = 0
        });
        OnListChanged?.Invoke(_items);
    }

    public void ConfirmSpawnedNearest(string prefabName, Vector3 spawnedPos, int viewId, float maxDist = 1.0f)
    {
        int best = -1; float bestD = maxDist;
        for (int i = 0; i < _items.Count; i++)
        {
            if (!_items[i].pending) continue;
            if (_items[i].prefabName != prefabName) continue;
            float d = Vector3.Distance(_items[i].position, spawnedPos);
            if (d < bestD) { bestD = d; best = i; }
        }
        if (best >= 0)
        {
            var it = _items[best];
            it.pending = false;
            it.viewId = viewId;
            _items[best] = it;
            OnListChanged?.Invoke(_items);
        }
    }

    public void RemoveByApproxPosition(Vector3 pos, float maxDist = 1.5f)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (Vector3.Distance(_items[i].position, pos) <= maxDist)
            {
                _items.RemoveAt(i);
                OnListChanged?.Invoke(_items);
                return;
            }
        }
    }

    public bool RemoveByViewId(int viewId)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].viewId == viewId)
            {
                _items.RemoveAt(i);
                OnListChanged?.Invoke(_items);
                return true;
            }
        }
        return false;
    }
}