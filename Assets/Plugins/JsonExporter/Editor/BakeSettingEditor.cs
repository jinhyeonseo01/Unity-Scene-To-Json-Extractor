using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BakeSetting))]
public class BakeSettingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 표시
        DrawDefaultInspector();

        // 대상 스크립트 가져오기
        BakeSetting myComponent = (BakeSetting)target;

        // 버튼 추가
        if (GUILayout.Button("Path Info Update"))
        {
            myComponent.PathUpdate();
        }
    }
}