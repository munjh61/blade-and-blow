#if UNITY_EDITOR
using Game.Domain;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ColorController))]
public class TeamColorApplierEditor : Editor
{
    private TeamId _previewTeam = TeamId.None;

    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 먼저 그리기
        DrawDefaultInspector();

        var ctrl = (ColorController)target;
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── Test Tools ──", EditorStyles.boldLabel);

        // 팀 프리뷰 선택
        _previewTeam = (TeamId)EditorGUILayout.EnumPopup("Preview Team", _previewTeam);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply Preview Team"))
        {
            ctrl.ApplyTeam(_previewTeam);
            MarkDirty(ctrl);
        }
        if (GUILayout.Button("Clear Override"))
        {
            ctrl.ClearOverride();
            MarkDirty(ctrl);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Quick Apply", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Neutral")) { ctrl.ApplyTeam(TeamId.None); MarkDirty(ctrl); }
        if (GUILayout.Button("Red"))     { ctrl.ApplyTeam(TeamId.Red);     MarkDirty(ctrl); }
        if (GUILayout.Button("Blue"))    { ctrl.ApplyTeam(TeamId.Blue);    MarkDirty(ctrl); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Ping Renderer"))
        {
            if (ctrl && ctrl.GetComponentInChildren<Renderer>())
                EditorGUIUtility.PingObject(ctrl.GetComponentInChildren<Renderer>());
        }
    }

    private static void MarkDirty(Object obj)
    {
        if (!Application.isPlaying)
            EditorUtility.SetDirty(obj);
        SceneView.RepaintAll();
    }
}
#endif