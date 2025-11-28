using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class ResolutionController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;

    public List<Vector2Int> whitelist = new List<Vector2Int>();

    private List<Resolution> _resolutions;
    private Dictionary<Vector2Int, HashSet<int>> _hzMap;
    private List<(Vector2Int size, int hz)> _options;

    void BuildResolutionLists()
    {
        var native = Screen.resolutions;

        _hzMap = new Dictionary<Vector2Int, HashSet<int>>();

        foreach (var r in native)
        {
            var key = new Vector2Int(r.width, r.height);
            int hz = GetHz(r);
            if (!_hzMap.TryGetValue(key, out var set))
            {
                set = new HashSet<int>();
                _hzMap[key] = set;
            }
            set.Add(hz);
        }

        IEnumerable<KeyValuePair<Vector2Int, HashSet<int>>> filtered = _hzMap;
        if (whitelist != null && whitelist.Count > 0)
        {
            var allow = new HashSet<Vector2Int>(whitelist);
            filtered = _hzMap.Where(kvp => allow.Contains(kvp.Key));
        }

        _options = filtered
            .SelectMany(kvp => kvp.Value.Select(hz => (size: kvp.Key, hz)))
            .OrderBy(o => o.size.x)
            .ThenBy(o => o.size.y)
            .ThenBy(o => o.hz)
            .ToList();
    }

    void BuildResolutionDropdown()
    {
        resolutionDropdown.ClearOptions();

        var labels = _options
            .Select(o => $"{o.size.x}×{o.size.y} {o.hz}Hz")
            .ToList();

        resolutionDropdown.AddOptions(labels);
    }

    void BuildScreenModeDropdown()
    {
        if (screenModeDropdown == null) return;

        screenModeDropdown.ClearOptions();
        screenModeDropdown.AddOptions(new List<string> {
            "Fullscreen",   // FullScreenMode.ExclusiveFullScreen
            "Fullscreen (Borderless)", // FullScreenMode.FullScreenWindow
            "Windowed"                // FullScreenMode.Windowed
        });

        // 현재 모드로 값 동기화
        screenModeDropdown.value = Screen.fullScreenMode switch
        {
            FullScreenMode.ExclusiveFullScreen => 0,
            FullScreenMode.FullScreenWindow => 1,
            _ => 2,
        };
    }
    public void ApplySelected()
    {
        // 해상도×주사율 파싱
        var label = resolutionDropdown.options[resolutionDropdown.value].text; // "1920×1080 144Hz"
        ParseResolutionLabel(label, out var w, out var h, out var hz);

        // 화면 모드
        FullScreenMode mode = FullScreenMode.ExclusiveFullScreen;
        if (screenModeDropdown != null)
        {
            mode = screenModeDropdown.value switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.Windowed
            };
        }

        var rr = new RefreshRate { numerator = (uint)hz, denominator = 1 };
        Screen.SetResolution(w, h, mode, rr);
    }

    public void SelectBySaved(int w, int h, int hz, FullScreenMode mode)
    {
        // 모드 드롭다운
        if (screenModeDropdown != null)
        {
            screenModeDropdown.SetValueWithoutNotify(mode switch
            {
                FullScreenMode.ExclusiveFullScreen => 0,
                FullScreenMode.FullScreenWindow => 1,
                _ => 2,
            });
        }

        if (resolutionDropdown == null || _options == null || _options.Count == 0) return;

        // 정확 일치 우선
        int idx = _options.FindIndex(o => o.size.x == w && o.size.y == h && o.hz == hz);
        if (idx < 0)
        {
            // 해상도 일치 + Hz 가장 가까운 것
            idx = _options
                .Select((o, i) => new { o, i })
                .Where(x => x.o.size.x == w && x.o.size.y == h)
                .OrderBy(x => Mathf.Abs(x.o.hz - hz))
                .Select(x => x.i)
                .DefaultIfEmpty(-1)
                .First();
        }
        if (idx < 0)
        {
            // 그래도 없으면 전체 중 가장 근접
            idx = _options
                .Select((o, i) => new { i, d = Mathf.Abs(o.size.x - w) + Mathf.Abs(o.size.y - h) + Mathf.Abs(o.hz - hz) })
                .OrderBy(x => x.d)
                .Select(x => x.i)
                .First();
        }

        if (idx >= 0 && idx < resolutionDropdown.options.Count)
            resolutionDropdown.SetValueWithoutNotify(idx);
    }

    void SyncCurrentSelection()
    {
        var cur = Screen.currentResolution;
        int curHz = GetCurrentHz();

        int idx = _options.FindIndex(o => o.size.x == cur.width && o.size.y == cur.height && o.hz == curHz);
        if (idx < 0)
        {
            idx = _options
                .Select((o, i) => new { o, i })
                .Where(x => x.o.size.x == cur.width && x.o.size.y == cur.height)
                .OrderBy(x => Mathf.Abs(x.o.hz - curHz))
                .Select(x => x.i)
                .DefaultIfEmpty(-1)
                .First();
        }

        if (idx >= 0 && idx < resolutionDropdown.options.Count)
            resolutionDropdown.value = idx;

        // 화면 모드는 BuildScreenModeDropdown에서 이미 동기화함
    }

    static int GetCurrentHz()
    {
        var rr = Screen.currentResolution.refreshRateRatio;
        return (int)Mathf.Round(rr.numerator / (float)rr.denominator);
    }

    static int GetHz(Resolution r)
    {
        // numerator/denominator가 (144/1)처럼 오는 케이스가 일반적
        return (int)Mathf.Round(r.refreshRateRatio.numerator / (float)r.refreshRateRatio.denominator);
    }

    static void ParseResolutionLabel(string label, out int w, out int h, out int hz)
    {
        // "1920×1080 144Hz" 또는 "1920x1080 144Hz" 형태 허용
        var parts = label.Split(' ');
        var sizePart = parts[0].Replace('×', 'x');
        var wh = sizePart.Split('x');
        w = int.Parse(wh[0]);
        h = int.Parse(wh[1]);
        hz = int.Parse(parts[1].Replace("Hz", ""));
    }

    public bool TryGetSelected(out int w, out int h, out int hz, out FullScreenMode mode)
    {
        w = h = hz = 0; mode = Screen.fullScreenMode;
        if (resolutionDropdown == null || resolutionDropdown.options.Count == 0) return false;

        var label = resolutionDropdown.options[resolutionDropdown.value].text; // "1920×1080 144Hz"
        ParseResolutionLabel(label, out w, out h, out hz);

        if (screenModeDropdown != null)
        {
            mode = screenModeDropdown.value switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.Windowed
            };
        }
        return true;
    }

    public void RebuildAndSync()
    {
        BuildResolutionLists();
        BuildResolutionDropdown();
        BuildScreenModeDropdown();
        SyncCurrentSelection();
    }
}
