using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-80)]
public sealed class DropFactory : MonoBehaviour
{
    [Serializable]
    public struct Entry
    {
        [Tooltip("코어가 브로드캐스트하는 weaponKey (예: \"sword\", \"bow\", \"wand\")")]
        public string key;

        [Tooltip("해당 드랍 프리팹 (반드시 DropHandle 포함!)")]
        public GameObject prefab;

        [Tooltip("(선택) 바닥 스냅 적용 여부")]
        public bool snapToGround;
    }

    [Header("Drop Table")]
    [Tooltip("weaponKey ↔ 프리팹 매핑 테이블")]
    public Entry[] table;

    [Header("Ground Snap Settings")]
    [Tooltip("snapToGround=true일 때 바닥으로 투사할 레이어 마스크")]
    public LayerMask groundMask = ~0;

    [Tooltip("위로 얼마나 올려서 쏠지")]
    public float groundProbeUp = 2f;

    [Tooltip("아래로 얼마나 내릴지")]
    public float groundProbeDown = 5f;

    // 내부 매핑 (key 대소문자 무시)
    private readonly Dictionary<string, Entry> _map = new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        _map.Clear();
        if (table == null) return;

        foreach (var e in table)
        {
            if (string.IsNullOrWhiteSpace(e.key) || e.prefab == null) continue;
            if (_map.ContainsKey(e.key))
                Debug.LogWarning($"[DropFactory] Duplicate key '{e.key}' last one wins.");

            // 프리팹에 DropHandle 유무 체크(강제는 아님, 권장)
            if (!e.prefab.TryGetComponent<DropHandle>(out _))
                Debug.LogWarning($"[DropFactory] Prefab for key='{e.key}' has no DropHandle. " +
                                 $"It is recommended to add DropHandle on the prefab.");

            _map[e.key] = e;
        }
    }

    /// <summary>
    /// weaponKey로 드랍 오브젝트를 스폰한다(로컬 Instantiate).
    /// </summary>
    /// <returns>생성된 GameObject (실패 시 null)</returns>
    public GameObject SpawnDrop(string weaponKey, Vector3 pos, Quaternion rot)
    {
        if (string.IsNullOrWhiteSpace(weaponKey))
        {
            Debug.LogError("[DropFactory] SpawnDrop failed: empty weaponKey");
            return null;
        }

        if (!_map.TryGetValue(weaponKey, out var entry) || entry.prefab == null)
        {
            Debug.LogError($"[DropFactory] SpawnDrop failed: key '{weaponKey}' not found.");
            return null;
        }

        // 바닥 스냅(선택)
        // if (entry.snapToGround)
        // {
        //     var origin = pos + Vector3.up * groundProbeUp;
        //     var dist = groundProbeUp + groundProbeDown;
        //     if (Physics.Raycast(origin, Vector3.down, out var hit, dist, groundMask, QueryTriggerInteraction.Ignore))
        //     {
        //         pos = hit.point;
        //     }
        // }

        var go = Instantiate(entry.prefab, pos, rot);

        if (!go.TryGetComponent<DropHandle>(out _))
        {
            Debug.LogWarning($"[DropFactory] Spawned object (key='{weaponKey}') has no DropHandle component.");
        }

        return go;
    }
}
