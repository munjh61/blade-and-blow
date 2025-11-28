using UnityEngine;

/// <summary>
/// 드랍 오브젝트에 컴포넌트 추가 필요
/// </summary>
[DisallowMultipleComponent]
public sealed class DropHandle : MonoBehaviour
{
    [SerializeField] private ulong _token;
    [SerializeField] private string _weaponName;

    public ulong Token
    {
        get => _token;
        set
        {
            if (_token == value) return;

            // 이전 토큰으로 등록되어 있었다면 제거
            if (_token != 0) DropRegistry.Unregister(this);

            _token = value;

            // 새 토큰으로 등록
            if (_token != 0) DropRegistry.Register(this);

            UpdateName();
        }
    }

    public string WeaponName
    {
        get => _weaponName;
        set
        {
            _weaponName = value;
            UpdateName();
        }
    }

    void Awake()
    {
        UpdateName();
    }

    void OnDestroy()
    {
        if (_token != 0) DropRegistry.Unregister(this);
    }

    private void UpdateName()
    {
        var t = _token != 0 ? _token.ToString() : "0";
        var k = !string.IsNullOrEmpty(_weaponName) ? _weaponName : "unknown";
        gameObject.name = $"drop_{t}_{k}";
    }
}