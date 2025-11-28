using Game.Domain;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StaminaUI : MonoBehaviour
{
    [Header("UI")]
    public Slider slider;
    public bool autoFindSlider = true;

    [Header("Display")]
    [Tooltip("슬라이더 표시값 보간 속도(단위/초). 0이면 즉시 반영")]
    public float smoothSpeed = 250f;

    [Tooltip("캐시에 내 상태가 없을 때 바를 숨길지 여부")]
    public bool hideWhenUnknown = true;

    [Header("Config")]
    [Tooltip("Core의 stamina 최대치와 동일하게 맞추세요")]
    public int staminaMax = 100;

    private PlayerManagerPunBehaviour _pm;
    private int _localActor = -1;

    private float _display;
    private bool _subscribedInit;

    private void Awake()
    {
        if (autoFindSlider && slider == null)
            slider = GetComponentInChildren<Slider>(true);
    }

    private void OnEnable()
    {
        TryBindPM();
        PrimeFromCache();
    }

    private void OnDisable()
    {
        UnsubscribePM();
    }

    private void Update()
    {
        if (slider == null) return;

        if ((_pm == null) || _localActor <= 0)
        {
            TryBindPM();
            if (_pm == null || _localActor <= 0)
            {
                if (hideWhenUnknown) slider.gameObject.SetActive(false);
                return;
            }
        }

        if (_pm.TryGetState(_localActor, out PlayerInfoData d))
        {
            if (hideWhenUnknown && !slider.gameObject.activeSelf)
                slider.gameObject.SetActive(true);

            if (!Mathf.Approximately(slider.maxValue, staminaMax))
            {
                slider.maxValue = Mathf.Max(1, staminaMax);
                _display = Mathf.Clamp(_display, 0, slider.maxValue);
            }

            float target = Mathf.Clamp(d.stamina, 0, (int)slider.maxValue);

            if (smoothSpeed > 0f)
                _display = Mathf.MoveTowards(_display, target, smoothSpeed * Time.deltaTime);
            else
                _display = target;

            slider.SetValueWithoutNotify(_display);
        }
        else
        {
            if (hideWhenUnknown) slider.gameObject.SetActive(false);
        }
    }

    // ---------------- helpers ----------------

    private void TryBindPM()
    {
        if (_pm == null)
            _pm = FindFirstObjectByType<PlayerManagerPunBehaviour>();

        if (_pm == null) return;

        if (_pm.LocalId.Value > 0)
            _localActor = _pm.LocalId.Value;

        if (!_subscribedInit)
        {
            _pm.OnInitialized += OnPMInitialized;
            _subscribedInit = true;
        }
    }

    private void UnsubscribePM()
    {
        if (_pm != null && _subscribedInit)
        {
            _pm.OnInitialized -= OnPMInitialized;
            _subscribedInit = false;
        }
    }

    private void OnPMInitialized()
    {
        if (_pm != null && _pm.LocalId.Value > 0)
            _localActor = _pm.LocalId.Value;

        PrimeFromCache();
    }

    private void PrimeFromCache()
    {
        if (slider == null || _pm == null || _pm.LocalId.Value <= 0) return;

        if (_pm.TryGetState(_pm.LocalId.Value, out PlayerInfoData d))
        {
            slider.maxValue = Mathf.Max(1, staminaMax);
            _display = Mathf.Clamp(d.stamina, 0, (int)slider.maxValue);
            slider.SetValueWithoutNotify(_display);

            if (hideWhenUnknown) slider.gameObject.SetActive(true);
        }
        else
        {
            if (hideWhenUnknown) slider.gameObject.SetActive(false);
        }
    }
}