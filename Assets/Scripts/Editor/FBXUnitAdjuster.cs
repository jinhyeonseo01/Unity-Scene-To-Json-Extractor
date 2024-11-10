// Assets/Editor/FBXUnitAdjuster.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Formats.Fbx.Exporter; // FBX Exporter 패키지 네임스페이스
using UnityEngine.Formats.Fbx.Exporter;
using System.Collections.Generic;

public class FBXUnitAdjuster : EditorWindow
{
    // 기본 설정
    private string folderPath = "Assets/Models"; // FBX 파일이 위치한 기본 폴더
    private float originalUnitScale = 0.01f; // 원본 단위 스케일 (예: 1 단위 = 1cm)

    [MenuItem("Tools/FBX Unit Adjuster")]
    public static void ShowWindow()
    {
        GetWindow<FBXUnitAdjuster>("FBX Unit Adjuster");
    }

    private void OnGUI()
    {
        GUILayout.Label("FBX Unit Adjuster", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 폴더 경로 입력
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Folder Path:", GUILayout.Width(80));
        folderPath = EditorGUILayout.TextField(folderPath);
        EditorGUILayout.EndHorizontal();

        // 원본 단위 스케일 입력
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Original Unit Scale:", GUILayout.Width(120));
        originalUnitScale = EditorGUILayout.FloatField(originalUnitScale);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);

        // 실행 버튼
        if (GUILayout.Button("Adjust Units"))
        {
            AdjustUnits();
        }
    }

    private void AdjustUnits()
    {
        // 폴더 경로 유효성 검사
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Error", "Invalid folder path.", "OK");
            return;
        }

        // 폴더 내 모든 FBX 파일 찾기
        string[] fbxFiles = Directory.GetFiles(folderPath, "*.fbx", SearchOption.AllDirectories);
        if (fbxFiles.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", "No FBX files found in the specified folder.", "OK");
            return;
        }

        // 진행 상황 표시를 위한 프로그레스 바 설정
        int totalFiles = fbxFiles.Length;
        int currentFile = 0;

        // 원본 단위를 1로 변경하기 위한 스케일 팩터 계산
        float targetUnitScale = 1.0f; // 목표 단위 스케일 (1 단위 = 1m)
        float scaleFactor = targetUnitScale / originalUnitScale;

        // 처리된 파일 목록 저장 (성공 및 실패)
        List<string> successFiles = new List<string>();
        List<string> failedFiles = new List<string>();

        foreach (string filePath in fbxFiles)
        {
            currentFile++;
            EditorUtility.DisplayProgressBar("Adjusting FBX Units", $"Processing {Path.GetFileName(filePath)} ({currentFile}/{totalFiles})", (float)currentFile / totalFiles);

            string assetPath = filePath;
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;

            if (importer == null)
            {
                Debug.LogWarning($"Failed to get ModelImporter for {assetPath}");
                failedFiles.Add(assetPath);
                continue;
            }
            
            // 기존 글로벌 스케일 가져오기
            float existingScale = importer.globalScale;
            importer.globalScale = 1.0f;

            // 변경 사항 저장 및 재임포트
            importer.SaveAndReimport();

            // FBX Exporter를 사용하여 수정된 모델을 임포트한 GameObject로부터 다시 FBX로 익스포트
            GameObject gos = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            GameObject go = gos;
            if (go == null)
            {
                Debug.LogWarning($"Failed to load GameObject for {assetPath}");
                failedFiles.Add(assetPath);
                continue;
            }

            // 임시 경로 설정
            string tempExportPath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + ".fbx");

            // 익스포트 시도
            go.transform.localScale = go.transform.localScale * existingScale;
            ModelExporter.ExportObject(tempExportPath, go);
            successFiles.Add(filePath);
        }

        // 프로그레스 바 해제
        EditorUtility.ClearProgressBar();

        // 결과 요약
        string summary = $"Unit adjustment completed.\n\nTotal Files: {totalFiles}\nSuccessfully Adjusted: {successFiles.Count}\nFailed: {failedFiles.Count}";
        if (failedFiles.Count > 0)
        {
            summary += "\n\nFailed Files:\n";
            foreach (string fail in failedFiles)
            {
                summary += $"- {fail}\n";
            }
        }

        EditorUtility.DisplayDialog("FBX Unit Adjuster", summary, "OK");
    }
}