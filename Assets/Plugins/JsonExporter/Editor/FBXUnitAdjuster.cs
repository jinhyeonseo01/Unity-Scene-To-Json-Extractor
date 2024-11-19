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

    [MenuItem("Tools/FBX Unit Adjuster")]
    public static void ShowWindow()
    {
        GetWindow<FBXUnitAdjuster>("FBX Unit Adjuster");
    }

    private string ConvertToRelativePath(string absolutePath)
    {
        if (absolutePath.StartsWith(Application.dataPath))
        {
            return "Assets" + absolutePath.Substring(Application.dataPath.Length);
        }
        else
        {
            Debug.LogWarning("선택된 경로가 프로젝트 폴더 내에 없습니다.");
            return "";
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("FBX Unit Adjuster", EditorStyles.boldLabel);
        GUILayout.Space(10);


        EditorGUILayout.BeginHorizontal();
        folderPath = EditorGUILayout.TextField("파일 경로", folderPath);

        if (GUILayout.Button("찾아보기", GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("파일 선택", "", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                folderPath = ConvertToRelativePath(selectedPath);
            }
        }
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


            //GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            //var prefabs = FindPrefabsUsingFBX(assetPath);
            //Debug.Log(assetPath);
            //Debug.Log(prefabs.Count);


            // 기존 글로벌 스케일 가져오기
            float existingScale = importer.globalScale;
            //importer.globalScale = 1.0f;
            importer.globalScale = 1.0f;
            //importer.useFileUnits = true;

            // 변경 사항 저장 및 재임포트
            importer.SaveAndReimport();

            // FBX Exporter를 사용하여 수정된 모델을 임포트한 GameObject로부터 다시 FBX로 익스포트
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (go == null)
            {
                Debug.LogWarning($"Failed to load GameObject for {assetPath}");
                failedFiles.Add(assetPath);
                continue;
            }

            // 임시 경로 설정
            string tempExportPath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + ".fbx");


            string metaFilePath = tempExportPath + ".meta";

            // 기존 .meta 파일 백업
            string backupMetaFilePath = metaFilePath + ".bak";
            if (File.Exists(metaFilePath))
            {
                File.Copy(metaFilePath, backupMetaFilePath, true);
            }


            // 익스포트 시도
            go.transform.localScale = go.transform.localScale * existingScale;
            OnPostprocessModel(go);
            ExportModelOptions o = new ExportModelOptions();
            o.ExportFormat = ExportFormat.ASCII;
            o.UseMayaCompatibleNames = true;
            o.PreserveImportSettings = true;
            ModelExporter.ExportObject(tempExportPath, go, o);
            successFiles.Add(filePath);


            if (File.Exists(backupMetaFilePath))
            {
                File.Copy(backupMetaFilePath, metaFilePath, true);
                File.Delete(backupMetaFilePath);

                // Unity 에셋 데이터베이스 갱신
                AssetDatabase.ImportAsset(tempExportPath);
            }


            go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            importer.SaveAndReimport();
            //var objs = FindPrefabs(assetPath);
            //for (int i = 0; i < objs.Count; i++)
            //objs[i].transform.localScale = go.transform.localScale;
            //foreach (var prefab in prefabs)
            //{
            //UpdatePrefabWithFBX(prefab, go);
            //}
        }
        AssetDatabase.Refresh();

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

    void OnPostprocessModel(GameObject g)
    {
        // 모든 자식 객체에 대해 메쉬 이름을 수정
        MeshFilter[] meshFilters = g.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh != null)
            {
                // 원하는 메쉬 이름으로 변경
                meshFilter.gameObject.name = meshFilter.sharedMesh.name;  // GameObject 이름과 동일하게 설정
            }
        }
    }


    // FBX를 참조하는 Prefab 검색
    private List<GameObject> FindPrefabsUsingFBX(string fbxPath)
    {
        var prefabList = new List<GameObject>();
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");

        foreach (string prefabGUID in allPrefabs)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            // FBX를 참조하는 Prefab인지 확인
            if (PrefabReferencesFBX(prefab, fbxPath))
            {
                prefabList.Add(prefab);
            }
        }

        return prefabList;
    }

    // Prefab이 특정 FBX를 참조하는지 확인
    private bool PrefabReferencesFBX(GameObject prefab, string fbxPath)
    {
        var renderers = prefab.GetComponentsInChildren<MeshFilter>(true);
        foreach (var renderer in renderers)
        {
            //Debug.Log(renderer.sharedMesh);
            //Debug.Log(renderer.mesh);
            if (renderer.sharedMesh != null)
            {
                if (renderer.sharedMesh != null && AssetDatabase.GetAssetPath(renderer.sharedMesh) == AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<Object>(fbxPath)))
                {
                    return true;
                }
            }
        }

        return false;
    }


    private void UpdatePrefabWithFBX(GameObject prefab, GameObject importedModel)
    {
        // FBX의 Mesh 데이터를 Prefab에 복사
        Debug.Log(prefab);
        Debug.Log(importedModel);
        var prefabInstances = prefab.GetComponentsInChildren<Transform>(true);
        var importedInstances = importedModel.GetComponentsInChildren<Transform>(true);
        foreach (var prefabInstance in prefabInstances)
        {
            foreach (var importedInstance in importedInstances)
            {
                if (prefabInstance.name == importedInstance.name)
                {
                    // MeshRenderer 연결 업데이트
                    var prefabRenderer = prefabInstance.GetComponent<MeshFilter>();
                    var importedRenderer = importedInstance.GetComponent<MeshFilter>();
                    if (prefabRenderer != null && importedRenderer != null)
                    {
                        //prefabRenderer.sha
                        prefabRenderer.sharedMesh = importedRenderer.sharedMesh;
                    }
                }
            }
        }

        // Prefab 저장
        PrefabUtility.SavePrefabAsset(prefab);
        Debug.Log($"Updated Prefab: {prefab.name} with updated FBX: {importedModel.name}");
    }










    public static List<GameObject> FindPrefabs(string fbxPath)
    {
        // FBX 파일의 경로 설정 (Project 창의 FBX 파일 경로)
        Object fbxAsset = AssetDatabase.LoadAssetAtPath<Object>(fbxPath);
        List<GameObject> gameObjects = new List<GameObject>();
        if (fbxAsset == null)
        {
            Debug.LogError($"FBX file not found at path: {fbxPath}");
            return null;
        }

        // 모든 Prefab 검색
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");

        Debug.Log($"Searching for Prefabs using FBX: {fbxPath}");
        foreach (string prefabGUID in allPrefabs)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (PrefabUsesFBX(prefab, fbxAsset))
            {
                gameObjects.Add(prefab);
            }
        }
        return gameObjects;
    }

    private static bool PrefabUsesFBX(GameObject prefab, Object fbxAsset)
    {
        if (prefab == null || fbxAsset == null)
            return false;

        // Prefab 내부의 모든 MeshFilter 및 SkinnedMeshRenderer를 검색
        var meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh != null && AssetDatabase.GetAssetPath(meshFilter.sharedMesh) == AssetDatabase.GetAssetPath(fbxAsset))
            {
                return true;
            }
        }

        var skinnedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
        {
            if (skinnedMeshRenderer.sharedMesh != null && AssetDatabase.GetAssetPath(skinnedMeshRenderer.sharedMesh) == AssetDatabase.GetAssetPath(fbxAsset))
            {
                return true;
            }
        }

        return false;
    }
}