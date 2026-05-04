using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SortingSceneBuilder
{
    private const string GeneratedFolder = "Assets/GeneratedSortingGame";
    private const string MaterialsFolder = GeneratedFolder + "/Materials";
    private const string MeshesFolder = GeneratedFolder + "/Meshes";
    private const string PrefabsFolder = GeneratedFolder + "/Prefabs";
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Build Sorting Scene")]
    public static void BuildSortingScene()
    {
        EditorSceneManager.OpenScene(GameplayScenePath);
        EnsureGeneratedFolders();
        ClearGeneratedSceneObjects();

        Material generalMaterial = CreateMaterial("General_Green", new Color(0.2f, 0.75f, 0.35f));
        Material plasticMaterial = CreateMaterial("Plastic_Blue", new Color(0.1f, 0.45f, 0.95f));
        Material paperMaterial = CreateMaterial("Paper_Yellow", new Color(0.95f, 0.78f, 0.18f));
        Material metalBinMaterial = CreateMaterial("Metal_Silver", new Color(0.68f, 0.72f, 0.7f), 0.35f, 0.42f);
        Material glassBinMaterial = CreateMaterial("Glass_Teal", new Color(0.15f, 0.72f, 0.78f), 0f, 0.72f);
        Material foodBinMaterial = CreateMaterial("FoodWaste_Orange", new Color(0.9f, 0.38f, 0.1f), 0f, 0.32f);
        Material dirtyMaterial = CreateMaterial("Dirty_Brown", new Color(0.45f, 0.25f, 0.12f));
        Material cleanMaterial = CreateMaterial("Clean_White", new Color(0.92f, 0.95f, 0.9f));
        Material floorMaterial = CreateMaterial("Floor_Dark", new Color(0.18f, 0.19f, 0.2f));

        BuildEnvironment(floorMaterial);

        GameObject gameManager = new GameObject("GameManager");
        gameManager.AddComponent<ScoreManager>();
        TrashSpawner trashSpawner = gameManager.AddComponent<TrashSpawner>();
        trashSpawner.spawnInterval = 1.25f;
        trashSpawner.maxActiveTrash = 12;

        GameObject spawnPoint = new GameObject("SpawnPoint");
        spawnPoint.transform.position = new Vector3(0f, 1.2f, -5f);
        spawnPoint.transform.rotation = Quaternion.identity;
        trashSpawner.spawnPoint = spawnPoint.transform;
        trashSpawner.trashPrefabs = CreateTrashPrefabs(generalMaterial, plasticMaterial, paperMaterial, dirtyMaterial, metalBinMaterial, glassBinMaterial, foodBinMaterial);

        CreateBin("Bin_General", TrashCategory.General, generalMaterial, new Vector3(-4.5f, 0.6f, 4f));
        CreateBin("Bin_Plastic", TrashCategory.Recyclable_Plastic, plasticMaterial, new Vector3(-2.7f, 0.6f, 4f));
        CreateBin("Bin_Paper", TrashCategory.Recyclable_Paper, paperMaterial, new Vector3(-0.9f, 0.6f, 4f));
        CreateBin("Bin_Metal", TrashCategory.Recyclable_Metal, metalBinMaterial, new Vector3(0.9f, 0.6f, 4f));
        CreateBin("Bin_Glass", TrashCategory.Recyclable_Glass, glassBinMaterial, new Vector3(2.7f, 0.6f, 4f));
        CreateBin("Bin_Food", TrashCategory.FoodWaste_Raw, foodBinMaterial, new Vector3(4.5f, 0.6f, 4f));

        CreateWashingStation(cleanMaterial, new Vector3(-4.2f, 0.35f, 0.3f));
        CreatePlayerRig();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[SortingSceneBuilder] Playable sorting scene built successfully.");
    }

    private static void EnsureGeneratedFolders()
    {
        EnsureFolder("Assets", "GeneratedSortingGame");
        EnsureFolder(GeneratedFolder, "Materials");
        EnsureFolder(GeneratedFolder, "Meshes");
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
            "Bin_Plastic",
            "Bin_Paper",
            "Bin_Metal",
            "Bin_Glass",
            "Bin_Food",
            "WashingStation",
            "SortingFloor",
            "SortingPlayer",
            "HoldPoint",
            "OVRCameraRig",
            "BinLabel_General",
            "BinLabel_Plastic",
            "BinLabel_Paper",
            "BinLabel_Metal",
            "BinLabel_Glass",
            "BinLabel_Food"
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

    private static Material CreateMaterial(string materialName, Color color, float metallic = 0f, float smoothness = 0.35f)
    {
        string materialPath = $"{MaterialsFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (color.a < 0.99f)
        {
            material.SetFloat("_Surface", 1f);
            material.renderQueue = 3000;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildEnvironment(Material floorMaterial)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "SortingFloor";
        floor.transform.position = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(12f, 0.1f, 12f);
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

    private static GameObject[] CreateTrashPrefabs(Material generalMaterial, Material plasticMaterial, Material paperMaterial, Material dirtyMaterial, Material metalMaterial, Material glassMaterial, Material foodMaterial)
    {
        Material translucentPlastic = CreateMaterial("Bottle_Translucent_Blue", new Color(0.42f, 0.74f, 1f, 0.52f), 0f, 0.88f);
        Material bottleLabel = CreateMaterial("Bottle_Label_Scuffed", new Color(0.92f, 0.93f, 0.86f), 0f, 0.28f);
        Material bottleCap = CreateMaterial("Bottle_Cap_Red", new Color(0.86f, 0.12f, 0.08f), 0f, 0.55f);
        Material grimyPlastic = CreateMaterial("Bottle_Grimy_Plastic", new Color(0.3f, 0.22f, 0.15f, 0.72f), 0f, 0.2f);
        Material paperCrease = CreateMaterial("Paper_Crease_Grey", new Color(0.38f, 0.35f, 0.31f), 0f, 0.18f);
        Material trayMaterial = CreateMaterial("Takeout_Tray_Stained_Fiber", new Color(0.74f, 0.66f, 0.48f), 0f, 0.22f);
        Material greasyFoodMaterial = CreateMaterial("Food_Waste_Greasy", new Color(0.58f, 0.24f, 0.08f), 0f, 0.48f);
        Material bananaMaterial = CreateMaterial("Banana_Peel_Yellow_Brown", new Color(0.86f, 0.68f, 0.18f), 0f, 0.35f);
        Material cardboardMaterial = CreateMaterial("Cardboard_Fiber_Brown", new Color(0.62f, 0.45f, 0.26f), 0f, 0.24f);
        Material clearGlassMaterial = CreateMaterial("Glass_Clear_Green", new Color(0.52f, 0.95f, 0.84f, 0.46f), 0f, 0.9f);
        Material metalCanMaterial = CreateMaterial("Crushed_Can_Dull_Metal", new Color(0.72f, 0.74f, 0.7f), 0.55f, 0.31f);

        return new[]
        {
            CreateGeneralGarbagePrefab("Trash_General", generalMaterial, trayMaterial, greasyFoodMaterial, paperCrease, metalCanMaterial),
            CreatePlasticBottlePrefab("Trash_Plastic", TrashCategory.Recyclable_Plastic, false, translucentPlastic, bottleLabel, bottleCap, plasticMaterial),
            CreateCrumpledPaperPrefab("Trash_Paper", paperMaterial, paperCrease),
            CreatePlasticBottlePrefab("Trash_DirtyPlastic", TrashCategory.Recyclable_Plastic, true, grimyPlastic, bottleLabel, dirtyMaterial, dirtyMaterial),
            CreateMetalCanPrefab("Trash_CrushedCan", metalCanMaterial, bottleLabel),
            CreateGlassBottlePrefab("Trash_GlassBottle", clearGlassMaterial, glassMaterial),
            CreateCardboardPrefab("Trash_Cardboard", cardboardMaterial, paperCrease),
            CreateBananaPeelPrefab("Trash_BananaPeel", bananaMaterial, foodMaterial, dirtyMaterial)
        };
    }

    private static GameObject CreateGeneralGarbagePrefab(string prefabName, Material generalMaterial, Material trayMaterial, Material foodMaterial, Material paperCrease, Material metalMaterial)
    {
        GameObject trash = CreateTrashRoot(prefabName, TrashCategory.General, false, 0.85f);

        GameObject tray = CreatePrimitiveChild(trash.transform, "GreasyTakeoutTray", PrimitiveType.Cube, trayMaterial);
        tray.transform.localPosition = new Vector3(0f, -0.08f, 0f);
        tray.transform.localScale = new Vector3(0.72f, 0.08f, 0.52f);

        CreatePrimitiveChild(trash.transform, "TrayFrontRim", PrimitiveType.Cube, trayMaterial, new Vector3(0f, 0.02f, -0.29f), new Vector3(0.78f, 0.16f, 0.05f));
        CreatePrimitiveChild(trash.transform, "TrayBackRim", PrimitiveType.Cube, trayMaterial, new Vector3(0f, 0.02f, 0.29f), new Vector3(0.78f, 0.16f, 0.05f));
        CreatePrimitiveChild(trash.transform, "TrayLeftRim", PrimitiveType.Cube, trayMaterial, new Vector3(-0.41f, 0.02f, 0f), new Vector3(0.05f, 0.16f, 0.52f));
        CreatePrimitiveChild(trash.transform, "TrayRightRim", PrimitiveType.Cube, trayMaterial, new Vector3(0.41f, 0.02f, 0f), new Vector3(0.05f, 0.16f, 0.52f));

        CreatePrimitiveChild(trash.transform, "GreaseStainA", PrimitiveType.Cylinder, foodMaterial, new Vector3(-0.11f, 0.005f, 0.02f), new Vector3(0.22f, 0.008f, 0.15f));
        CreatePrimitiveChild(trash.transform, "GreaseStainB", PrimitiveType.Cylinder, foodMaterial, new Vector3(0.23f, 0.008f, -0.11f), new Vector3(0.14f, 0.006f, 0.09f));
        CreatePrimitiveChild(trash.transform, "FoodScrapA", PrimitiveType.Sphere, foodMaterial, new Vector3(-0.23f, 0.08f, -0.06f), new Vector3(0.11f, 0.06f, 0.08f));
        CreatePrimitiveChild(trash.transform, "FoodScrapB", PrimitiveType.Sphere, generalMaterial, new Vector3(0.17f, 0.08f, 0.13f), new Vector3(0.08f, 0.05f, 0.06f));

        Mesh napkinMesh = CreateCrumpledMeshAsset("Trash_General_NapkinMesh", 0.18f, 8);
        GameObject napkin = CreateMeshChild(trash.transform, "CrumpledNapkin", napkinMesh, paperCrease);
        napkin.transform.localPosition = new Vector3(0.05f, 0.12f, -0.02f);
        napkin.transform.localRotation = Quaternion.Euler(14f, 32f, -9f);

        Mesh canMesh = CreateCrushedBottleMeshAsset("Trash_General_CrushedCanMesh", 0.42f, 0.16f, 0.78f);
        GameObject can = CreateMeshChild(trash.transform, "CrushedCan", canMesh, metalMaterial);
        can.transform.localPosition = new Vector3(-0.18f, 0.16f, 0.12f);
        can.transform.localRotation = Quaternion.Euler(76f, 0f, 28f);

        return SaveTrashPrefab(trash);
    }

    private static GameObject CreatePlasticBottlePrefab(string prefabName, TrashCategory category, bool isDirty, Material bodyMaterial, Material labelMaterial, Material capMaterial, Material accentMaterial)
    {
        GameObject trash = CreateTrashRoot(prefabName, category, isDirty, 0.72f);

        Mesh bottleMesh = CreateCrushedBottleMeshAsset(prefabName + "_BottleMesh", 0.88f, 0.22f, isDirty ? 0.58f : 0.72f);
        GameObject body = CreateMeshChild(trash.transform, "CrushedBottleBody", bottleMesh, bodyMaterial);
        body.transform.localRotation = Quaternion.Euler(84f, -10f, 18f);

        GameObject label = CreatePrimitiveChild(trash.transform, "TornPaperLabel", PrimitiveType.Cylinder, labelMaterial);
        label.transform.localPosition = new Vector3(0.02f, 0.01f, 0f);
        label.transform.localRotation = Quaternion.Euler(84f, -10f, 18f);
        label.transform.localScale = new Vector3(0.43f, 0.042f, 0.31f);

        for (int i = 0; i < 5; i++)
        {
            float offset = -0.28f + (i * 0.14f);
            GameObject rib = CreatePrimitiveChild(trash.transform, "CompressedRib_" + i, PrimitiveType.Cylinder, accentMaterial);
            rib.transform.localPosition = new Vector3(offset * 0.12f, offset, 0f);
            rib.transform.localRotation = Quaternion.Euler(84f, -10f, 18f);
            rib.transform.localScale = new Vector3(0.39f - (Mathf.Abs(offset) * 0.17f), 0.008f, 0.28f);
        }

        GameObject cap = CreatePrimitiveChild(trash.transform, "BottleCap", PrimitiveType.Cylinder, capMaterial);
        cap.transform.localPosition = new Vector3(0.02f, 0.49f, 0.01f);
        cap.transform.localRotation = Quaternion.Euler(84f, -10f, 18f);
        cap.transform.localScale = new Vector3(0.13f, 0.07f, 0.13f);

        Material stainMaterial = isDirty ? accentMaterial : labelMaterial;
        CreatePrimitiveChild(trash.transform, "ScuffPatchA", PrimitiveType.Cube, stainMaterial, new Vector3(-0.18f, 0.04f, -0.18f), new Vector3(0.18f, 0.01f, 0.08f), new Vector3(0f, 21f, -17f));
        CreatePrimitiveChild(trash.transform, "ScuffPatchB", PrimitiveType.Cube, stainMaterial, new Vector3(0.16f, -0.13f, 0.17f), new Vector3(0.14f, 0.01f, 0.06f), new Vector3(0f, -35f, 16f));

        return SaveTrashPrefab(trash);
    }

    private static GameObject CreateCrumpledPaperPrefab(string prefabName, Material paperMaterial, Material creaseMaterial)
    {
        GameObject trash = CreateTrashRoot(prefabName, TrashCategory.Recyclable_Paper, false, 0.52f);

        Mesh paperMesh = CreateCrumpledMeshAsset(prefabName + "_CrumpledMesh", 0.34f, 17);
        GameObject paper = CreateMeshChild(trash.transform, "CrumpledPaperBall", paperMesh, paperMaterial);
        paper.transform.localRotation = Quaternion.Euler(-8f, 21f, 13f);

        for (int i = 0; i < 13; i++)
        {
            float angle = i * 37f;
            float angleRadians = angle * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Cos(angleRadians) * 0.11f, Mathf.Sin(angleRadians * 0.7f) * 0.08f, Mathf.Sin(angleRadians) * 0.12f);
            Vector3 rotation = new Vector3(18f + (i * 11f), angle, -24f + (i * 7f));
            Vector3 scale = new Vector3(0.23f + ((i % 3) * 0.05f), 0.012f, 0.01f);
            CreatePrimitiveChild(trash.transform, "DarkCrease_" + i, PrimitiveType.Cube, creaseMaterial, position, scale, rotation);
        }

        return SaveTrashPrefab(trash);
    }

    private static GameObject CreateMetalCanPrefab(string prefabName, Material metalMaterial, Material labelMaterial)
    {
        GameObject trash = CreateTrashRoot(prefabName, TrashCategory.Recyclable_Metal, false, 0.62f);

        Mesh canMesh = CreateCrushedBottleMeshAsset(prefabName + "_CanMesh", 0.58f, 0.2f, 0.48f);
        GameObject body = CreateMeshChild(trash.transform, "FlattenedAluminumCan", canMesh, metalMaterial);
        body.transform.localRotation = Quaternion.Euler(82f, 0f, -22f);

        CreatePrimitiveChild(trash.transform, "CanTopRim", PrimitiveType.Cylinder, metalMaterial, new Vector3(-0.05f, 0.31f, 0f), new Vector3(0.18f, 0.025f, 0.18f), new Vector3(82f, 0f, -22f));
        CreatePrimitiveChild(trash.transform, "CanBottomRim", PrimitiveType.Cylinder, metalMaterial, new Vector3(0.05f, -0.31f, 0f), new Vector3(0.2f, 0.025f, 0.2f), new Vector3(82f, 0f, -22f));
        CreatePrimitiveChild(trash.transform, "TornCanLabel", PrimitiveType.Cube, labelMaterial, new Vector3(0f, 0.02f, -0.17f), new Vector3(0.38f, 0.18f, 0.012f), new Vector3(10f, 0f, -18f));
        CreatePrimitiveChild(trash.transform, "PullTab", PrimitiveType.Cube, metalMaterial, new Vector3(-0.09f, 0.34f, 0.03f), new Vector3(0.09f, 0.018f, 0.036f), new Vector3(0f, 0f, 17f));

        return SaveTrashPrefab(trash);
    }

    private static GameObject CreateGlassBottlePrefab(string prefabName, Material glassMaterial, Material accentMaterial)
    {
        GameObject trash = CreateTrashRoot(prefabName, TrashCategory.Recyclable_Glass, false, 0.68f);

        Mesh bottleMesh = CreateCrushedBottleMeshAsset(prefabName + "_BottleMesh", 0.82f, 0.2f, 0.86f);
        GameObject bottle = CreateMeshChild(trash.transform, "GreenGlassBottle", bottleMesh, glassMaterial);
        bottle.transform.localRotation = Quaternion.Euler(84f, 6f, 12f);

        CreatePrimitiveChild(trash.transform, "BottleNeckRing", PrimitiveType.Cylinder, accentMaterial, new Vector3(0.03f, 0.43f, 0f), new Vector3(0.12f, 0.025f, 0.12f), new Vector3(84f, 6f, 12f));
        CreatePrimitiveChild(trash.transform, "PaperBottleLabel", PrimitiveType.Cylinder, accentMaterial, new Vector3(0f, -0.04f, 0f), new Vector3(0.35f, 0.03f, 0.27f), new Vector3(84f, 6f, 12f));
        CreatePrimitiveChild(trash.transform, "GlassHighlightShardA", PrimitiveType.Cube, glassMaterial, new Vector3(-0.18f, -0.03f, -0.14f), new Vector3(0.18f, 0.012f, 0.04f), new Vector3(16f, 34f, -20f));
        CreatePrimitiveChild(trash.transform, "GlassHighlightShardB", PrimitiveType.Cube, glassMaterial, new Vector3(0.16f, 0.16f, 0.12f), new Vector3(0.13f, 0.012f, 0.035f), new Vector3(-12f, -28f, 29f));

        return SaveTrashPrefab(trash);
    }

    private static GameObject CreateCardboardPrefab(string prefabName, Material cardboardMaterial, Material creaseMaterial)
    {
        GameObject trash = CreateTrashRoot(prefabName, TrashCategory.Recyclable_Paper, false, 0.62f);

        GameObject box = CreatePrimitiveChild(trash.transform, "FlattenedCardboardBox", PrimitiveType.Cube, cardboardMaterial, Vector3.zero, new Vector3(0.7f, 0.055f, 0.48f), new Vector3(4f, 0f, -8f));
        CreatePrimitiveChild(box.transform, "FoldLineCenter", PrimitiveType.Cube, creaseMaterial, new Vector3(0f, 0.52f, 0f), new Vector3(0.028f, 0.03f, 1.05f), new Vector3(0f, 0f, 0f));
        CreatePrimitiveChild(box.transform, "FoldLineLeft", PrimitiveType.Cube, creaseMaterial, new Vector3(-0.28f, 0.53f, 0f), new Vector3(0.018f, 0.03f, 0.82f), new Vector3(0f, 0f, 0f));
        CreatePrimitiveChild(box.transform, "ShippingTape", PrimitiveType.Cube, creaseMaterial, new Vector3(0f, 0.54f, 0.02f), new Vector3(0.13f, 0.035f, 0.86f), new Vector3(0f, 0f, 0f));

        return SaveTrashPrefab(trash);
    }

    private static GameObject CreateBananaPeelPrefab(string prefabName, Material peelMaterial, Material foodMaterial, Material bruiseMaterial)
    {
        GameObject trash = CreateTrashRoot(prefabName, TrashCategory.FoodWaste_Raw, false, 0.58f);

        CreatePrimitiveChild(trash.transform, "BananaPeelLeft", PrimitiveType.Capsule, peelMaterial, new Vector3(-0.16f, 0f, 0f), new Vector3(0.09f, 0.3f, 0.06f), new Vector3(74f, 0f, 34f));
        CreatePrimitiveChild(trash.transform, "BananaPeelRight", PrimitiveType.Capsule, peelMaterial, new Vector3(0.16f, 0f, 0f), new Vector3(0.09f, 0.3f, 0.06f), new Vector3(74f, 0f, -34f));
        CreatePrimitiveChild(trash.transform, "BananaPeelCenter", PrimitiveType.Capsule, peelMaterial, new Vector3(0f, 0.03f, 0.08f), new Vector3(0.08f, 0.32f, 0.055f), new Vector3(78f, 0f, 0f));
        CreatePrimitiveChild(trash.transform, "SoftPulp", PrimitiveType.Sphere, foodMaterial, new Vector3(0f, 0.05f, 0f), new Vector3(0.14f, 0.08f, 0.09f));
        CreatePrimitiveChild(trash.transform, "BrownBruiseA", PrimitiveType.Cube, bruiseMaterial, new Vector3(-0.2f, 0.08f, 0.04f), new Vector3(0.07f, 0.012f, 0.035f), new Vector3(12f, 0f, 28f));
        CreatePrimitiveChild(trash.transform, "BrownBruiseB", PrimitiveType.Cube, bruiseMaterial, new Vector3(0.18f, 0.06f, -0.03f), new Vector3(0.06f, 0.012f, 0.032f), new Vector3(-18f, 0f, -22f));

        return SaveTrashPrefab(trash);
    }

    private static GameObject CreateTrashRoot(string prefabName, TrashCategory category, bool isDirty, float colliderRadius)
    {
        GameObject trash = new GameObject(prefabName);

        TrashItem trashItem = trash.AddComponent<TrashItem>();
        trashItem.itemType = category;
        trashItem.isDirty = isDirty;

        Rigidbody rigidbody = trash.AddComponent<Rigidbody>();
        rigidbody.mass = 0.7f;
        rigidbody.linearDamping = 1f;
        rigidbody.angularDamping = 1f;

        SphereCollider collider = trash.AddComponent<SphereCollider>();
        collider.radius = colliderRadius;

        return trash;
    }

    private static GameObject SaveTrashPrefab(GameObject trash)
    {
        string prefabPath = $"{PrefabsFolder}/{trash.name}.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(trash, prefabPath);
        Object.DestroyImmediate(trash);
        return savedPrefab;
    }

    private static GameObject CreatePrimitiveChild(Transform parent, string childName, PrimitiveType primitiveType, Material material)
    {
        return CreatePrimitiveChild(parent, childName, primitiveType, material, Vector3.zero, Vector3.one, Vector3.zero);
    }

    private static GameObject CreatePrimitiveChild(Transform parent, string childName, PrimitiveType primitiveType, Material material, Vector3 localPosition, Vector3 localScale)
    {
        return CreatePrimitiveChild(parent, childName, primitiveType, material, localPosition, localScale, Vector3.zero);
    }

    private static GameObject CreatePrimitiveChild(Transform parent, string childName, PrimitiveType primitiveType, Material material, Vector3 localPosition, Vector3 localScale, Vector3 localEulerAngles)
    {
        GameObject child = GameObject.CreatePrimitive(primitiveType);
        child.name = childName;
        child.transform.SetParent(parent);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = Quaternion.Euler(localEulerAngles);
        child.transform.localScale = localScale;
        child.GetComponent<Renderer>().sharedMaterial = material;
        return child;
    }

    private static GameObject CreateMeshChild(Transform parent, string childName, Mesh mesh, Material material)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        child.AddComponent<MeshFilter>().sharedMesh = mesh;
        child.AddComponent<MeshRenderer>().sharedMaterial = material;
        return child;
    }

    private static Mesh CreateCrushedBottleMeshAsset(string meshName, float height, float radius, float flattening)
    {
        int rings = 11;
        int segments = 20;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int ring = 0; ring < rings; ring++)
        {
            float t = ring / (float)(rings - 1);
            float y = Mathf.Lerp(-height * 0.5f, height * 0.5f, t);
            float neck = t > 0.77f ? Mathf.Lerp(1f, 0.46f, (t - 0.77f) / 0.23f) : 1f;
            float waist = 1f - (0.16f * Mathf.Sin(t * Mathf.PI * 2f + 0.7f));
            float ringRadius = radius * neck * waist;

            for (int segment = 0; segment < segments; segment++)
            {
                float angle = (segment / (float)segments) * Mathf.PI * 2f;
                float crumple = 1f + (0.08f * Mathf.Sin((segment * 2.1f) + (ring * 1.7f)));
                float sideDent = Mathf.Clamp01(1f - Mathf.Abs(Mathf.DeltaAngle(angle * Mathf.Rad2Deg, 18f)) / 52f);
                float dent = crumple - (0.2f * sideDent * Mathf.Sin(t * Mathf.PI));
                float x = Mathf.Cos(angle) * ringRadius * dent;
                float z = Mathf.Sin(angle) * ringRadius * flattening * dent;
                vertices.Add(new Vector3(x, y, z));
            }
        }

        for (int ring = 0; ring < rings - 1; ring++)
        {
            for (int segment = 0; segment < segments; segment++)
            {
                int nextSegment = (segment + 1) % segments;
                int current = (ring * segments) + segment;
                int next = (ring * segments) + nextSegment;
                int above = ((ring + 1) * segments) + segment;
                int aboveNext = ((ring + 1) * segments) + nextSegment;

                triangles.Add(current);
                triangles.Add(above);
                triangles.Add(next);
                triangles.Add(next);
                triangles.Add(above);
                triangles.Add(aboveNext);
            }
        }

        Mesh mesh = new Mesh
        {
            name = meshName,
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return SaveMeshAsset(meshName, mesh);
    }

    private static Mesh CreateCrumpledMeshAsset(string meshName, float radius, int seed)
    {
        int latitudes = 8;
        int longitudes = 14;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int lat = 0; lat <= latitudes; lat++)
        {
            float v = lat / (float)latitudes;
            float theta = v * Mathf.PI;
            for (int lon = 0; lon < longitudes; lon++)
            {
                float u = lon / (float)longitudes;
                float phi = u * Mathf.PI * 2f;
                float crumple = 1f + (0.22f * Mathf.Sin((lon * 1.9f) + seed)) + (0.16f * Mathf.Cos((lat * 2.7f) - seed));
                crumple = Mathf.Clamp(crumple, 0.62f, 1.28f);
                Vector3 point = new Vector3(
                    Mathf.Sin(theta) * Mathf.Cos(phi),
                    Mathf.Cos(theta),
                    Mathf.Sin(theta) * Mathf.Sin(phi));
                vertices.Add(point * radius * crumple);
            }
        }

        for (int lat = 0; lat < latitudes; lat++)
        {
            for (int lon = 0; lon < longitudes; lon++)
            {
                int nextLon = (lon + 1) % longitudes;
                int current = (lat * longitudes) + lon;
                int next = (lat * longitudes) + nextLon;
                int below = ((lat + 1) * longitudes) + lon;
                int belowNext = ((lat + 1) * longitudes) + nextLon;

                triangles.Add(current);
                triangles.Add(below);
                triangles.Add(next);
                triangles.Add(next);
                triangles.Add(below);
                triangles.Add(belowNext);
            }
        }

        Mesh mesh = new Mesh
        {
            name = meshName,
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return SaveMeshAsset(meshName, mesh);
    }

    private static Mesh SaveMeshAsset(string meshName, Mesh mesh)
    {
        string meshPath = $"{MeshesFolder}/{meshName}.asset";
        Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existingMesh != null)
        {
            EditorUtility.CopySerialized(mesh, existingMesh);
            EditorUtility.SetDirty(existingMesh);
            Object.DestroyImmediate(mesh);
            return existingMesh;
        }

        AssetDatabase.CreateAsset(mesh, meshPath);
        return mesh;
    }

    private static void CreateBin(string binName, TrashCategory targetCategory, Material material, Vector3 position)
    {
        GameObject bin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bin.name = binName;
        bin.transform.position = position;
        bin.transform.localScale = new Vector3(1.6f, 1.2f, 1.6f);
        bin.GetComponent<Renderer>().sharedMaterial = material;

        BoxCollider boxCollider = bin.GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        BinValidator binValidator = bin.AddComponent<BinValidator>();
        binValidator.targetCategory = targetCategory;

        CreateLabel("BinLabel_" + binName.Replace("Bin_", string.Empty), binName.Replace("Bin_", string.Empty), position + new Vector3(0f, 1.1f, 0f));
    }

    private static void CreateLabel(string objectName, string text, Vector3 position)
    {
        GameObject label = new GameObject(objectName);
        label.transform.position = position;
        label.transform.rotation = Quaternion.Euler(65f, 0f, 0f);

        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.characterSize = 0.28f;
        textMesh.fontSize = 72;
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
