using Game.Domain;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CurrentWeapon : MonoBehaviour
{
    [Header("UI")]
    public Image weaponIcon;
    public Sprite defaultSprite;
    public Sprite[] spriteByEquipId;

    [Header("Lookup")]
    [SerializeField] private float lookupInterval = 0.5f;

    private PlayerManagerPunBehaviour _pm; // 외부 연결 금지: 동적 탐색 전용(프라이빗)
    private Coroutine _lookupCo;
    private int _shownEquipId = -1;

    private void Awake()
    {
        if (weaponIcon == null)
        {
            var t = transform.Find("WeaponIcon");
            if (t) weaponIcon = t.GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        // 낙관적 미리보기(현재 선택값) 즉시 반영
        ApplyIcon(SelectedLoadout.CurrentEquipId);

        // 이벤트 구독
        SelectedLoadout.OnChanged += OnLocalPreviewChanged;

        // 동적 탐색 루프 시작
        _lookupCo = StartCoroutine(LookupLoop());
    }

    private void OnDisable()
    {
        SelectedLoadout.OnChanged -= OnLocalPreviewChanged;
        StopLookup();

        // 구독 해제
        if (_pm != null)
        {
            _pm.EquipApplied -= OnEquipAppliedAuthoritatively;
            _pm = null;
        }
    }

    private void StopLookup()
    {
        if (_lookupCo != null)
        {
            StopCoroutine(_lookupCo);
            _lookupCo = null;
        }
    }

    private IEnumerator LookupLoop()
    {
        var wait = new WaitForSeconds(lookupInterval);

        while (true)
        {
            if (_pm == null)
            {
                // 씬에 생성된 PlayerManagerPunBehaviour를 찾는다.
                var found = FindObjectOfType<PlayerManagerPunBehaviour>();
                if (found != null)
                {
                    BindPlayerManager(found);
                    // 권위 캐시 값으로 한번 더 동기화(있다면)
                    if (_pm.TryGetEquip(_pm.LocalId, out var curEq))
                        ApplyIcon(curEq);
                }
            }

            yield return wait;
        }
    }

    private void BindPlayerManager(PlayerManagerPunBehaviour pm)
    {
        if (pm == null) return;

        // 기존 구독 제거(이중 구독 방지)
        if (_pm != null)
            _pm.EquipApplied -= OnEquipAppliedAuthoritatively;

        _pm = pm;
        _pm.EquipApplied += OnEquipAppliedAuthoritatively;
    }


    /// <summary>
    /// 로컬 선택 변경 → 즉시 UI 반영
    /// </summary>
    private void OnLocalPreviewChanged(int equipId)
    {
        ApplyIcon(equipId);
    }

    /// <summary>
    /// 권위 확정(브로드캐스트 반영) → 최종 UI 덮어쓰기
    /// 내 플레이어에 대한 확정만 반영
    /// </summary>
    private void OnEquipAppliedAuthoritatively(PlayerId id, int equipId)
    {
        if (_pm == null) return;
        if (id.Value != _pm.LocalId.Value) return; // 내 것만

        ApplyIcon(equipId);
    }

    /// <summary>
    /// 공통 아이콘 적용 로직(미리보기/확정 모두 여기로 수렴)
    /// </summary>
    private void ApplyIcon(int equipId)
    {
        _shownEquipId = equipId;

        if (weaponIcon == null)
            return;

        Sprite s = null;
        if (equipId >= 0 && spriteByEquipId != null && equipId < spriteByEquipId.Length)
            s = spriteByEquipId[equipId];

        if (s != null)
        {
            weaponIcon.enabled = true;
            weaponIcon.sprite = s;
        }
        else if (defaultSprite != null)
        {
            weaponIcon.enabled = true;
            weaponIcon.sprite = defaultSprite;
        }
        else
        {
            weaponIcon.enabled = false;
        }
    }
}