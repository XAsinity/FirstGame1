// Place this file at Assets/Editor/FindMissingScripts.cs
// Usage (in Unity Editor): Tools -> Find Missing Scripts -> Find In Scene / Find In Project
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public static class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts/Find In Scene")]
    public static void FindInScene()
    {
        int count = 0;
        // Search through all root objects in all open scenes
        for (int s = 0; s < UnityEngine.SceneManagement.SceneManager.sceneCount; s++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
                count += FindInGameObject(root);
        }
        Debug.Log($"FindMissingScripts: Finished scanning scenes. Found {count} GameObjects with missing scripts.");
    }

    [MenuItem("Tools/Find Missing Scripts/Find In Project (Prefabs)")]
    public static void FindInProject()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int total = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            // Inspect the prefab contents (non-instantiated)
            if (PrefabHasMissing(prefab))
            {
                total++;
                Debug.Log($"Missing scripts found in prefab: {path}", prefab);
            }
        }
        Debug.Log($"FindMissingScripts: Finished scanning prefabs. {total} prefabs contained missing scripts.");
    }

    [MenuItem("Tools/Find Missing Scripts/Remove Missing Scripts From Selected Prefabs")]
    public static void RemoveMissingFromSelectedPrefabs()
    {
        var objs = Selection.objects;
        if (objs == null || objs.Length == 0)
        {
            EditorUtility.DisplayDialog("Remove Missing Scripts", "Select one or more prefabs in Project window first.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Remove Missing Scripts", "This will remove Missing (Mono Script) components from selected prefabs. This change is permanent to the asset. Make a backup if unsure. Proceed?", "Yes", "No"))
            return;

        int removedCount = 0;
        foreach (var o in objs)
        {
            var path = AssetDatabase.GetAssetPath(o);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            // Load contents and create an editable instance
            GameObject instance = PrefabUtility.LoadPrefabContents(path);
            if (instance == null) continue;

            // Remove MonoBehaviours with missing scripts
            // This method is available in UnityEditor.GameObjectUtility
            removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(instance);

            // Save back
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            PrefabUtility.UnloadPrefabContents(instance);

            Debug.Log($"Removed missing scripts from prefab: {path}");
        }

        Debug.Log($"FindMissingScripts: Completed removal. Removed missing components from {removedCount} GameObjects across selected prefabs.");
    }

    // Recursively search for missing MonoBehaviours on a GameObject and its children.
    // Returns number of GameObjects that had missing scripts.
    private static int FindInGameObject(GameObject go)
    {
        int found = 0;
        var components = go.GetComponents<Component>();
        bool hasMissing = false;
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                // Log the GameObject (pass it as context so the Console entry is clickable)
                Debug.Log($"Missing script on GameObject: {GetGameObjectPath(go)}", go);
                hasMissing = true;
                break;
            }
        }
        if (hasMissing) found++;

        // Recurse children
        foreach (Transform child in go.transform)
            found += FindInGameObject(child.gameObject);

        return found;
    }

    private static bool PrefabHasMissing(GameObject prefab)
    {
        var allTransforms = prefab.GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            var comps = t.gameObject.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c == null) return true;
            }
        }
        return false;
    }

    private static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}