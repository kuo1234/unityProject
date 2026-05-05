using UnityEditor;
using UnityEngine;

public static class SortingSceneBuilder
{
    private const string GeneratedFolder = "Assets/GeneratedSortingGame";
    private const string MaterialsFolder = GeneratedFolder + "/Materials";
    private const string PrefabsFolder = GeneratedFolder + "/Prefabs";
    private const float DashboardTextSize = 0.07f;
    private const float FeedbackTextSize = 0.075f;
    private const float BinLabelTextSize = 0.075f;

    [MenuItem("Tools/Build Sorting Scene")]
    public static void BuildSortingScene()
    {
        EnsureGeneratedFolders();
        ClearGeneratedSceneObjects();

        Material generalMaterial = CreateMaterial("General_Green", new Color(0.2f, 0.75f, 0.35f));
        Material recyclableMaterial = CreateMaterial("Recyclable_Blue", new Color(0.1f, 0.45f, 0.95f));
        Material foodMaterial = CreateMaterial("Food_Orange", new Color(0.95f, 0.48f, 0.12f));
        Material dirtyMaterial = CreateMaterial("Dirty_Brown", new Color(0.45f, 0.25f, 0.12f));
        Material cleanMaterial = CreateMaterial("Clean_White", new Color(0.92f, 0.95f, 0.9f));
        Material floorMaterial = CreateMaterial("Floor_Dark", new Color(0.18f, 0.19f, 0.2f));
        Material acceptedMaterial = CreateMaterial("Feedback_Accepted", new Color(0.35f, 1f, 0.45f));
        Material rejectedMaterial = CreateMaterial("Feedback_Rejected", new Color(1f, 0.18f, 0.14f));

        BuildEnvironment(floorMaterial);

        GameObject gameManager = new GameObject("GameManager");
        gameManager.AddComponent<ScoreManager>();
        GameSessionManager sessionManager = gameManager.AddComponent<GameSessionManager>();
        sessionManager.roundDurationSeconds = 180f;
        sessionManager.completeSceneName = "CompleteScene";

        TrashSpawner trashSpawner = gameManager.AddComponent<TrashSpawner>();
        trashSpawner.spawnInterval = 1.6f;
        trashSpawner.maxActiveTrash = 12;

        GameObject spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.transform.position = new Vector3(0f, 1.2f, -5f);
        spawnPoint.transform.rotation = Quaternion.identity;
        trashSpawner.spawnPoint = spawnPoint.transform;
        trashSpawner.trashPrefabs = CreateTrashPrefabs(
            generalMaterial,
            recyclableMaterial,
            foodMaterial,
            dirtyMaterial);

        CreateBin("Bin_General", "General", TrashCategory.General, generalMaterial, acceptedMaterial, rejectedMaterial, new Vector3(-2.6f, 0.6f, 4f));
        CreateBin("Bin_Recyclable", "Recyclable", TrashCategory.Recyclable, recyclableMaterial, acceptedMaterial, rejectedMaterial, new Vector3(0f, 0.6f, 4f));
        CreateBin("Bin_Food", "Food", TrashCategory.Food, foodMaterial, acceptedMaterial, rejectedMaterial, new Vector3(2.6f, 0.6f, 4f));

        CreateWashingStation(cleanMaterial, new Vector3(-4.2f, 0.35f, 0.3f));
        CreateSessionUi(sessionManager);
        CreatePlayerRig();

        Debug.Log("[SortingSceneBuilder] Playable sorting scene built successfully.");
    }

    private static void EnsureGeneratedFolders()
    {
        EnsureFolder("Assets", "GeneratedSortingGame");
        EnsureFolder(GeneratedFolder, "Materials");
        EnsureFolder(GeneratedFolder, "Prefabs");
    }

    private static void EnsureFolder(string parentFolder, string newFolder)
    {
        string fullPath = parentFolder + "/" + newFolder;
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parentFolder, newFolder);
        }
    }

    private static void ClearGeneratedSceneObjects()
    {
        string[] generatedObjectNames =
        {
            "GameManager",
            "SpawnPoint",
            "Bin_General",
            "Bin_Recyclable",
            "Bin_Food",
            "Bin_Plastic",
            "Bin_Paper",
            "Bin_RawFood",
            "Bin_CookedFood",
            "WashingStation",
            "SortingFloor",
            "SortingPlayer",
            "HoldPoint",
            "OVRCameraRig",
            "BinLabel_General",
            "BinLabel_Recyclable",
            "BinLabel_Food",
            "BinLabel_Plastic",
            "BinLabel_Paper",
            "BinLabel_RawFood",
            "BinLabel_CookedFood",
            "SessionUI"
        };

        foreach (string objectName in generatedObjectNames)
        {
            GameObject existingObject = GameObject.Find(objectName);
            if (existingObject != null)
            {
                Object.DestroyImmediate(existingObject);
            }
        }

        foreach (TrashItem trashItem in Object.FindObjectsByType<TrashItem>(FindObjectsInactive.Exclude))
        {
            if (trashItem.gameObject.scene.IsValid())
            {
                Object.DestroyImmediate(trashItem.gameObject);
            }
        }
    }

    private static Material CreateMaterial(string materialName, Color color)
    {
        string materialPath = $"{MaterialsFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildEnvironment(Material floorMaterial)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "SortingFloor";
        floor.transform.position = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(11f, 0.1f, 12f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;

        Light light = Object.FindAnyObjectByType<Light>();
        if (light == null)
        {
            GameObject lightObject = new GameObject("Directional Light");
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
        }

        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.intensity = 1.2f;
    }

    private static GameObject[] CreateTrashPrefabs(
        Material generalMaterial,
        Material recyclableMaterial,
        Material foodMaterial,
        Material dirtyMaterial)
    {
        return new[]
        {
            CreateTrashPrefab("Trash_General", PrimitiveType.Capsule, TrashCategory.General, false, generalMaterial),
            CreateTrashPrefab("Trash_Plastic", PrimitiveType.Sphere, TrashCategory.Recyclable, false, recyclableMaterial),
            CreateTrashPrefab("Trash_Paper", PrimitiveType.Cube, TrashCategory.Recyclable, false, recyclableMaterial),
            CreateTrashPrefab("Trash_RawFood", PrimitiveType.Capsule, TrashCategory.Food, false, foodMaterial),
            CreateTrashPrefab("Trash_CookedFood", PrimitiveType.Capsule, TrashCategory.Food, false, foodMaterial),
            CreateTrashPrefab("Trash_DirtyPlastic", PrimitiveType.Sphere, TrashCategory.Recyclable, true, dirtyMaterial),
            CreateTrashPrefab("Trash_DirtyPaper", PrimitiveType.Cube, TrashCategory.Recyclable, true, dirtyMaterial)
        };
    }

    private static GameObject CreateTrashPrefab(string prefabName, PrimitiveType primitiveType, TrashCategory category, bool isDirty, Material material)
    {
        string prefabPath = $"{PrefabsFolder}/{prefabName}.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            ConfigureTrashObject(prefabContents, category, isDirty, material);
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabContents);
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        GameObject trash = GameObject.CreatePrimitive(primitiveType);
        trash.name = prefabName;
        trash.transform.localScale = Vector3.one * 0.45f;
        ConfigureTrashObject(trash, category, isDirty, material);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(trash, prefabPath);
        Object.DestroyImmediate(trash);
        return savedPrefab;
    }

    private static void ConfigureTrashObject(GameObject trash, TrashCategory category, bool isDirty, Material material)
    {
        if (trash.TryGetComponent(out Renderer renderer))
        {
            renderer.sharedMaterial = material;
        }

        TrashItem trashItem = trash.GetComponent<TrashItem>();
        if (trashItem == null)
        {
            trashItem = trash.AddComponent<TrashItem>();
        }

        trashItem.itemType = category;
        trashItem.isDirty = isDirty;

        Rigidbody rigidbody = trash.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = trash.AddComponent<Rigidbody>();
        }

        rigidbody.mass = 0.7f;
        rigidbody.linearDamping = 1f;
        rigidbody.angularDamping = 1f;
    }

    private static void CreateBin(
        string binName,
        string label,
        TrashCategory targetCategory,
        Material material,
        Material acceptedMaterial,
        Material rejectedMaterial,
        Vector3 position)
    {
        GameObject bin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bin.name = binName;
        bin.transform.position = position;
        bin.transform.localScale = new Vector3(1.45f, 1.2f, 1.45f);
        bin.GetComponent<Renderer>().sharedMaterial = material;

        BoxCollider boxCollider = bin.GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        BinValidator binValidator = bin.AddComponent<BinValidator>();
        binValidator.targetCategory = targetCategory;
        binValidator.acceptedFlashMaterial = acceptedMaterial;
        binValidator.rejectedFlashMaterial = rejectedMaterial;

        CreateLabel("BinLabel_" + binName.Replace("Bin_", string.Empty), label, position + new Vector3(0f, 1.05f, -0.82f));
    }

    private static void CreateLabel(string objectName, string text, Vector3 position)
    {
        GameObject label = new GameObject(objectName);
        label.transform.position = position;
        label.transform.rotation = Quaternion.identity;

        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.characterSize = BinLabelTextSize;
        textMesh.fontSize = 64;
        textMesh.color = Color.white;
    }

    private static void CreateWashingStation(Material cleanMaterial, Vector3 position)
    {
        GameObject station = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        station.name = "WashingStation";
        station.transform.position = position;
        station.transform.localScale = new Vector3(0.8f, 0.18f, 0.8f);

        Collider collider = station.GetComponent<Collider>();
        collider.isTrigger = true;

        WashingStation washingStation = station.AddComponent<WashingStation>();
        washingStation.cleanMaterial = cleanMaterial;
    }

    private static void CreateSessionUi(GameSessionManager sessionManager)
    {
        GameObject uiRoot = new GameObject("SessionUI");
        uiRoot.transform.position = new Vector3(0f, 2.85f, 1.8f);
        uiRoot.transform.rotation = Quaternion.identity;

        sessionManager.uiAnchor = uiRoot.transform;
        sessionManager.dashboardText = CreateUiText(uiRoot.transform, "DashboardText", new Vector3(0f, 0.7f, 0f), DashboardTextSize, TextAnchor.MiddleCenter);
        sessionManager.feedbackText = CreateUiText(uiRoot.transform, "FeedbackText", new Vector3(0f, 0.08f, 0f), FeedbackTextSize, TextAnchor.MiddleCenter);
        sessionManager.feedbackText.gameObject.SetActive(false);
    }

    private static TextMesh CreateUiText(Transform parent, string objectName, Vector3 localPosition, float characterSize, TextAnchor anchor)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.identity;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = anchor;
        textMesh.characterSize = characterSize;
        textMesh.fontSize = 72;
        textMesh.color = Color.white;
        return textMesh;
    }

    private static void CreatePlayerRig()
    {
        Camera camera = CreateMetaCameraRig();
        if (camera == null)
        {
            camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            camera.transform.position = new Vector3(0f, 5f, -7.5f);
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        }

        GameObject player = new GameObject("SortingPlayer");
        PlayerTrashInteractor interactor = player.AddComponent<PlayerTrashInteractor>();
        interactor.playerCamera = camera;

        GameObject holdPoint = new GameObject("HoldPoint");
        holdPoint.transform.SetParent(camera.transform);
        holdPoint.transform.localPosition = new Vector3(0f, -0.35f, 2.2f);
        holdPoint.transform.localRotation = Quaternion.identity;
        interactor.holdPoint = holdPoint.transform;

        DisableExtraAudioListeners(camera);
    }

    private static Camera CreateMetaCameraRig()
    {
        string[] cameraRigGuids = AssetDatabase.FindAssets("OVRCameraRig t:Prefab");
        foreach (string cameraRigGuid in cameraRigGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(cameraRigGuid);
            if (!path.EndsWith("OVRCameraRig.prefab"))
            {
                continue;
            }

            GameObject cameraRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (cameraRigPrefab == null)
            {
                continue;
            }

            GameObject cameraRig = (GameObject)PrefabUtility.InstantiatePrefab(cameraRigPrefab);
            cameraRig.name = "OVRCameraRig";
            cameraRig.transform.position = new Vector3(0f, 1.6f, -4.5f);
            cameraRig.transform.rotation = Quaternion.identity;

            Transform centerEyeAnchor = FindChildRecursive(cameraRig.transform, "CenterEyeAnchor");
            if (centerEyeAnchor != null && centerEyeAnchor.TryGetComponent(out Camera centerEyeCamera))
            {
                return centerEyeCamera;
            }

            return cameraRig.GetComponentInChildren<Camera>();
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform foundChild = FindChildRecursive(child, childName);
            if (foundChild != null)
            {
                return foundChild;
            }
        }

        return null;
    }

    private static void DisableExtraAudioListeners(Camera activeCamera)
    {
        foreach (AudioListener audioListener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
        {
            audioListener.enabled = activeCamera != null && audioListener.gameObject == activeCamera.gameObject;
        }
    }
}
