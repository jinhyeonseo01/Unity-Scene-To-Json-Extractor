using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BakeSetting))]
public class BakeSettingEditor : Editor
{

    private string ConvertToRelativePath(string absolutePath)
    {
        if (absolutePath.StartsWith(Application.dataPath))
        {
            return "Assets" + absolutePath.Substring(Application.dataPath.Length);
        }
        else
        {
            return absolutePath;
        }
    }

    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 표시
        DrawDefaultInspector();

        // 대상 스크립트 가져오기
        BakeSetting myComponent = (BakeSetting)target;
        EditorGUILayout.BeginHorizontal();

        myComponent.jsonExportPath = EditorGUILayout.TextField("Export Path", myComponent.jsonExportPath);

        if (GUILayout.Button("찾아보기", GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("파일 선택", "", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                myComponent.jsonExportPath = ConvertToRelativePath(selectedPath);
            }
        }
        EditorGUILayout.EndHorizontal();


        GUILayout.Space(20);

        // 버튼 추가
        if (GUILayout.Button("Path Info Update"))
        {
            myComponent.PathUpdate();
        }
    }
}