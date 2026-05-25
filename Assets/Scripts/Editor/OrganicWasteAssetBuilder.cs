using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class OrganicWasteAssetBuilder
{
    private const string ImportedFolder = "Assets/Imported/OrganicWaste";
    private const string ActiveTrashFolder = "Assets/Prefabs/ActiveTrash";
    private const string MaterialFolder = ImportedFolder + "/Materials";
    private const string AutorunFlagPath = "Library/RunOrganicWasteAssetBuilder.flag";
    private const float TargetLargestDimension = 0.62f;

    private static readonly ImportedFoodWaste[] FoodWasteAssets =
    {
        new ImportedFoodWaste(
            "Trash_FoodWaste_Organic_Template",
            ImportedFolder + "/uploads_files_4380637_Organic_Waste.fbx",
            ImportedFolder + "/Textures",
            "OrganicWaste_PBR",
            "Organic_Waste_OrganicWaste_AlbedoTransparency.png",
            "Organic_Waste_OrganicWaste_Normal.png",
            "Organic_Waste_OrganicWaste_Ambient_Occlusion.png",
            null,
            "Organic_Waste_OrganicWaste_SpecularSmoothness.png",
            0.62f,
            TrashCategory.Food),
        new ImportedFoodWaste(
            "Trash_FoodWaste_BananaPeel_Template",
            ImportedFolder + "/uploads_files_5329536_BananaPeelFBX.fbx",
            ImportedFolder + "/Textures",
            "BananaPeel_PBR",
            "BananaFBXTextures_1001_BaseColor_ver02.png",
            "BananaFBXTextures_1001_Normal.png",
            null,
            "BananaFBXTextures_1001_Metallic.png",
            null,
            0.58f,
            TrashCategory.Food),
        new ImportedFoodWaste(
            "Trash_FoodWaste_Broccoli_Template",
            ImportedFolder + "/broccoli.fbx",
            ImportedFolder + "/Tex_Metal_Rough",
            "Broccoli_PBR",
            "broccoli_brobody_Mat_BaseColor.png",
            "broccoli_brobody_Mat_Normal.png",
            "broccoli_brobody_Mat_AO.png",
            "broccoli_brobody_Mat_Metallic.png",
            null,
            0.5f,
            TrashCategory.Food),
        new ImportedFoodWaste(
            "Trash_FoodWaste_Donut_Template",
            ImportedFolder + "/uploads_files_6122592_black+donut.fbx",
            ImportedFolder + "/Textures",
            "Donut_PBR",
            "donut base.png",
            null,
            null,
            null,
            null,
            0.42f,
            TrashCategory.Food),
        new ImportedFoodWaste(
            "Trash_Recycle_CardboardBox_Template",
            ImportedFolder + "/uploads_files_5951235_CardboardBox.fbx",
            ImportedFolder + "/CardboardBox_2kPBRTextures_TARGA",
            "CardboardBox_PBR",
            "CardboardBox_BaseColor.tga",
            "CardboardBox_Normal.tga",
            null,
            "CardboardBox_Metallic.tga",
            null,
            0.72f,
            TrashCategory.Recyclable),
        new ImportedFoodWaste(
            "Trash_Recycle_Can_Template",
            ImportedFolder + "/uploads_files_3498603_CAN3dmode+2Sizes+.fbx",
            ImportedFolder + "/Textures",
            "Can_PBR",
            "Metal Stock.jpg",
            null,
            null,
            null,
            null,
            0.46f,
            TrashCategory.Recyclable),
        new ImportedFoodWaste(
            "Trash_Recycle_MilkCarton_Template",
            ImportedFolder + "/Milk Carton.FBX",
            ImportedFolder + "/Textures",
            "MilkCarton_PBR",
            null,
            null,
            null,
            null,
            null,
            0.62f,
            TrashCategory.Recyclable,
            false,
            new Color(0.88f, 0.96f, 1f)),
        new ImportedFoodWaste(
            "Trash_Recycle_Tetrapak_Template",
            ImportedFolder + "/001_70x60x150mm tetrapak_FBX.FBX",
            ImportedFolder + "/Textures",
            "Tetrapak_PBR",
            "DKWT0003.jpg",
            null,
            null,
            null,
            null,
            0.58f,
            TrashCategory.Recyclable),
        new ImportedFoodWaste(
            "Trash_General_PaperBag_Template",
            ImportedFolder + "/Paper Bag.FBX",
            ImportedFolder + "/Textures",
            "PaperBag_PBR",
            null,
            null,
            null,
            null,
            null,
            0.66f,
            TrashCategory.General,
            false,
            new Color(0.58f, 0.48f, 0.31f)),
        new ImportedFoodWaste(
            "Trash_General_ToiletPaper_Template",
            ImportedFolder + "/uploads_files_1971501_tp.FBX",
            ImportedFolder + "/Textures",
            "ToiletPaper_PBR",
            "h0888001_10003337b_150802105125_02_515.jpg",
            null,
            null,
            null,
            null,
            0.46f,
            TrashCategory.General),
        new ImportedFoodWaste(
            "Trash_General_SnackPack_Template",
            ImportedFolder + "/uploads_files_6680653_Snack-Pack-04-3D-model.fbx",
            ImportedFolder + "/Textures",
            "SnackPack_PBR",
            "Snack Pack 04_material_PBR_Diffuse.png",
            "Snack Pack 04_material_PBR_Normal.png",
            null,
            "Snack Pack 04_material_PBR_Metalness.png",
            null,
            0.56f,
            TrashCategory.General)
    };

    static OrganicWasteAssetBuilder()
    {
        if (!File.Exists(AutorunFlagPath))
        {
            return;
        }

        EditorApplication.delayCall += RunRequestedBuild;
    }

    [MenuItem("Tools/Sorting Game/Build Imported Food Waste Assets")]
    public static void Build()
    {
        if (File.Exists(AutorunFlagPath))
        {
            File.Delete(AutorunFlagPath);
        }

        EnsureProjectFolders();

        List<GameObject> builtPrefabs = new List<GameObject>();
        foreach (ImportedFoodWaste foodWaste in FoodWasteAssets)
        {
            GameObject prefab = BuildFoodWastePrefab(foodWaste);
            if (prefab != null)
            {
                builtPrefabs.Add(prefab);
            }
        }

        UpdateOpenSceneSpawners(builtPrefabs);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void RunRequestedBuild()
    {
        if (!File.Exists(AutorunFlagPath))
        {
            return;
        }

        File.Delete(AutorunFlagPath);
        Build();
    }

    private static GameObject BuildFoodWastePrefab(ImportedFoodWaste foodWaste)
    {
        EnsureTextureImportSettings(foodWaste);
        Material material = CreateMaterial(foodWaste);
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(foodWaste.ModelPath);
        if (modelAsset == null)
        {
            Debug.LogWarning("[OrganicWasteAssetBuilder] Missing model: " + foodWaste.ModelPath);
            return null;
        }

        GameObject root = new GameObject(foodWaste.PrefabName);
        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        modelInstance.name = "ImportedTrashModel";
        modelInstance.transform.SetParent(root.transform, false);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        if (material != null)
        {
            AssignMaterial(modelInstance, material);
        }

        FitModelToTrashScale(root.transform, modelInstance.transform, foodWaste.TargetLargestDimension);
        ConfigureTrashRoot(root, foodWaste.Category, foodWaste.IsDirty);

        string prefabPath = foodWaste.PrefabPath;
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    private static void EnsureProjectFolders()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder(ImportedFolder, "Materials");
        }

        if (!AssetDatabase.IsValidFolder(ActiveTrashFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "ActiveTrash");
        }
    }

    private static void EnsureTextureImportSettings(ImportedFoodWaste foodWaste)
    {
        ConfigureTexture(foodWaste.AlbedoPath, TextureImporterType.Default, true);
        ConfigureTexture(foodWaste.NormalPath, TextureImporterType.NormalMap, false);
        ConfigureTexture(foodWaste.OcclusionPath, TextureImporterType.Default, false);
        ConfigureTexture(foodWaste.MetallicPath, TextureImporterType.Default, false);
        ConfigureTexture(foodWaste.SpecularSmoothnessPath, TextureImporterType.Default, false);
    }

    private static void ConfigureTexture(string path, TextureImporterType textureType, bool sRgb)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = importer.textureType != textureType || importer.sRGBTexture != sRgb || importer.maxTextureSize != 2048;
        importer.textureType = textureType;
        importer.sRGBTexture = sRgb;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 2048;

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static Material CreateMaterial(ImportedFoodWaste foodWaste)
    {
        if (!foodWaste.FallbackColor.HasValue &&
            string.IsNullOrEmpty(foodWaste.AlbedoPath) &&
            string.IsNullOrEmpty(foodWaste.NormalPath) &&
            string.IsNullOrEmpty(foodWaste.OcclusionPath) &&
            string.IsNullOrEmpty(foodWaste.MetallicPath) &&
            string.IsNullOrEmpty(foodWaste.SpecularSmoothnessPath))
        {
            return null;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(foodWaste.MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, foodWaste.MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        Texture2D albedo = LoadTexture(foodWaste.AlbedoPath);
        Texture2D normal = LoadTexture(foodWaste.NormalPath);
        Texture2D occlusion = LoadTexture(foodWaste.OcclusionPath);
        Texture2D metallic = LoadTexture(foodWaste.MetallicPath);
        Texture2D specularSmoothness = LoadTexture(foodWaste.SpecularSmoothnessPath);

        SetTextureIfPresent(material, "_BaseMap", albedo);
        SetTextureIfPresent(material, "_MainTex", albedo);
        SetTextureIfPresent(material, "_BumpMap", normal);
        SetTextureIfPresent(material, "_OcclusionMap", occlusion);
        SetTextureIfPresent(material, "_MetallicGlossMap", metallic);
        SetTextureIfPresent(material, "_SpecGlossMap", specularSmoothness);

        Color baseColor = foodWaste.FallbackColor ?? Color.white;
        SetColorIfPresent(material, "_BaseColor", baseColor);
        SetColorIfPresent(material, "_Color", baseColor);
        SetFloatIfPresent(material, "_BumpScale", 1f);
        SetFloatIfPresent(material, "_OcclusionStrength", 0.8f);
        SetFloatIfPresent(material, "_Metallic", metallic != null ? 0.25f : 0f);
        SetFloatIfPresent(material, "_Smoothness", specularSmoothness != null ? 0.35f : 0.22f);

        SetKeyword(material, "_NORMALMAP", normal != null);
        SetKeyword(material, "_OCCLUSIONMAP", occlusion != null);
        SetKeyword(material, "_METALLICSPECGLOSSMAP", metallic != null);
        SetKeyword(material, "_SPECGLOSSMAP", specularSmoothness != null);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D LoadTexture(string path)
    {
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void AssignMaterial(GameObject modelInstance, Material material)
    {
        foreach (Renderer renderer in modelInstance.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials.Length == 0)
            {
                renderer.sharedMaterial = material;
                continue;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private static void FitModelToTrashScale(Transform root, Transform model, float targetLargestDimension)
    {
        Bounds bounds = CalculateBounds(model.gameObject);
        float largestDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (largestDimension > 0.001f)
        {
            model.localScale = Vector3.one * (targetLargestDimension / largestDimension);
        }

        bounds = CalculateBounds(model.gameObject);
        model.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        root.position = Vector3.zero;
        root.rotation = Quaternion.identity;
    }

    private static void ConfigureTrashRoot(GameObject root, TrashCategory category, bool isDirty)
    {
        TrashItem trashItem = root.GetComponent<TrashItem>();
        if (trashItem == null)
        {
            trashItem = root.AddComponent<TrashItem>();
        }

        trashItem.itemType = category;
        trashItem.isDirty = isDirty;
        trashItem.isCompoundItem = true;

        Rigidbody body = root.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = root.AddComponent<Rigidbody>();
        }

        body.mass = 0.7f;
        body.linearDamping = 1f;
        body.angularDamping = 1f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        BoxCollider collider = root.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = root.AddComponent<BoxCollider>();
        }

        Bounds bounds = CalculateBounds(root);
        collider.size = bounds.size;
        collider.center = bounds.center;
    }

    private static void AddRealisticGarbageDetails(GameObject root, ImportedFoodWaste foodWaste)
    {
        Bounds bounds = CalculateBounds(root);
        GameObject detailRoot = new GameObject("GarbageWearDetails");
        detailRoot.transform.SetParent(root.transform, false);

        Random.State previousState = Random.state;
        Random.InitState(StableHash(foodWaste.PrefabName));

        if (foodWaste.PrefabName.Contains("PaperBag"))
        {
            AddPaperBagCreases(detailRoot.transform, bounds);
        }
        else if (foodWaste.Category == TrashCategory.Food)
        {
            AddFoodCrumbs(detailRoot.transform, bounds);
        }
        else
        {
            AddDiscardScuffs(detailRoot.transform, bounds, foodWaste.Category);
        }

        Random.state = previousState;
    }

    private static void AddPaperBagCreases(Transform parent, Bounds bounds)
    {
        Material creaseMaterial = GetGeneratedMaterial("PaperBag_Crease_Mat", new Color(0.33f, 0.25f, 0.14f), 0.18f);
        Material fiberMaterial = GetGeneratedMaterial("PaperBag_Fiber_Mat", new Color(0.74f, 0.64f, 0.43f), 0.12f);

        float frontZ = bounds.max.z + 0.003f;
        for (int i = 0; i < 7; i++)
        {
            float height = Random.Range(bounds.size.y * 0.18f, bounds.size.y * 0.45f);
            Vector3 position = new Vector3(
                Random.Range(bounds.min.x + bounds.size.x * 0.15f, bounds.max.x - bounds.size.x * 0.15f),
                Random.Range(bounds.min.y + height * 0.6f, bounds.max.y - height * 0.25f),
                frontZ);

            Vector3 scale = new Vector3(Random.Range(0.008f, 0.018f), height, 0.006f);
            Vector3 rotation = new Vector3(0f, 0f, Random.Range(-18f, 18f));
            CreateDetailCube(parent, "PaperCrease_" + i, position, rotation, scale, creaseMaterial);
        }

        for (int i = 0; i < 5; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.min.y + 0.012f,
                Random.Range(bounds.min.z, bounds.max.z));

            Vector3 scale = new Vector3(Random.Range(0.035f, 0.08f), 0.004f, Random.Range(0.008f, 0.018f));
            Vector3 rotation = new Vector3(0f, Random.Range(0f, 180f), 0f);
            CreateDetailCube(parent, "PaperFiber_" + i, position, rotation, scale, fiberMaterial);
        }
    }

    private static void AddFoodCrumbs(Transform parent, Bounds bounds)
    {
        Material crumbMaterial = GetGeneratedMaterial("FoodWaste_Crumb_Mat", new Color(0.38f, 0.25f, 0.12f), 0.2f);
        Material stainMaterial = GetGeneratedMaterial("FoodWaste_Stain_Mat", new Color(0.18f, 0.26f, 0.12f), 0.34f);

        for (int i = 0; i < 6; i++)
        {
            Vector3 position = RandomPointNearBase(bounds, 0.045f);
            Vector3 scale = Vector3.one * Random.Range(0.018f, 0.036f);
            CreateDetailCube(parent, "FoodCrumb_" + i, position, RandomEuler(), scale, crumbMaterial);
        }

        for (int i = 0; i < 3; i++)
        {
            Vector3 position = RandomPointNearBase(bounds, 0.02f);
            Vector3 scale = new Vector3(Random.Range(0.07f, 0.13f), 0.004f, Random.Range(0.025f, 0.055f));
            Vector3 rotation = new Vector3(0f, Random.Range(0f, 180f), 0f);
            CreateDetailCube(parent, "FoodStain_" + i, position, rotation, scale, stainMaterial);
        }
    }

    private static void AddDiscardScuffs(Transform parent, Bounds bounds, TrashCategory category)
    {
        Color scuffColor = category == TrashCategory.Recyclable
            ? new Color(0.18f, 0.20f, 0.20f)
            : new Color(0.26f, 0.22f, 0.18f);
        Material scuffMaterial = GetGeneratedMaterial(category + "_Scuff_Mat", scuffColor, 0.28f);

        for (int i = 0; i < 5; i++)
        {
            Vector3 position = RandomPointNearBase(bounds, 0.018f);
            Vector3 scale = new Vector3(Random.Range(0.05f, 0.12f), 0.004f, Random.Range(0.008f, 0.02f));
            Vector3 rotation = new Vector3(0f, Random.Range(0f, 180f), 0f);
            CreateDetailCube(parent, "DiscardScuff_" + i, position, rotation, scale, scuffMaterial);
        }
    }

    private static Vector3 RandomPointNearBase(Bounds bounds, float yOffset)
    {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.min.y + yOffset,
            Random.Range(bounds.min.z, bounds.max.z));
    }

    private static Vector3 RandomEuler()
    {
        return new Vector3(Random.Range(0f, 35f), Random.Range(0f, 180f), Random.Range(0f, 35f));
    }

    private static void CreateDetailCube(Transform parent, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
    {
        GameObject detail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        detail.name = name;
        detail.transform.SetParent(parent, false);
        detail.transform.localPosition = position;
        detail.transform.localRotation = Quaternion.Euler(rotation);
        detail.transform.localScale = scale;

        Collider collider = detail.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = detail.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static Material GetGeneratedMaterial(string materialName, Color color, float smoothness)
    {
        string path = MaterialFolder + "/" + materialName + ".mat";
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        SetColorIfPresent(material, "_BaseColor", color);
        SetColorIfPresent(material, "_Color", color);
        SetFloatIfPresent(material, "_Metallic", 0f);
        SetFloatIfPresent(material, "_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static int StableHash(string text)
    {
        unchecked
        {
            int hash = 23;
            for (int i = 0; i < text.Length; i++)
            {
                hash = hash * 31 + text[i];
            }

            return hash;
        }
    }

    private static void UpdateOpenSceneSpawners(List<GameObject> builtPrefabs)
    {
        if (builtPrefabs.Count == 0)
        {
            return;
        }

        bool changed = false;
        foreach (TrashSpawner spawner in Object.FindObjectsByType<TrashSpawner>(FindObjectsInactive.Include))
        {
            List<GameObject> prefabs = spawner.trashPrefabs != null
                ? new List<GameObject>(spawner.trashPrefabs)
                : new List<GameObject>();

            foreach (GameObject prefab in builtPrefabs)
            {
                if (!ContainsPrefab(prefabs, prefab.name))
                {
                    prefabs.Add(prefab);
                    changed = true;
                }
            }

            spawner.trashPrefabs = prefabs.ToArray();
            EditorUtility.SetDirty(spawner);
        }

        if (!changed)
        {
            return;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        EditorSceneManager.SaveOpenScenes();
    }

    private static bool ContainsPrefab(List<GameObject> prefabs, string prefabName)
    {
        foreach (GameObject prefab in prefabs)
        {
            if (prefab != null && prefab.name == prefabName)
            {
                return true;
            }
        }

        return false;
    }

    private static Bounds CalculateBounds(GameObject gameObject)
    {
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(Vector3.zero, Vector3.one * 0.4f);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static void SetTextureIfPresent(Material material, string propertyName, Texture texture)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, texture);
        }
    }

    private static void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (enabled)
        {
            material.EnableKeyword(keyword);
        }
        else
        {
            material.DisableKeyword(keyword);
        }
    }

    private static void SetColorIfPresent(Material material, string propertyName, Color color)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, color);
        }
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private sealed class ImportedFoodWaste
    {
        public ImportedFoodWaste(
            string prefabName,
            string modelPath,
            string textureFolder,
            string materialName,
            string albedoFile,
            string normalFile,
            string occlusionFile,
            string metallicFile,
            string specularSmoothnessFile,
            float targetLargestDimension,
            TrashCategory category,
            bool isDirty = false,
            Color? fallbackColor = null)
        {
            PrefabName = prefabName;
            ModelPath = modelPath;
            TextureFolder = textureFolder;
            MaterialName = materialName;
            AlbedoFile = albedoFile;
            NormalFile = normalFile;
            OcclusionFile = occlusionFile;
            MetallicFile = metallicFile;
            SpecularSmoothnessFile = specularSmoothnessFile;
            TargetLargestDimension = targetLargestDimension;
            Category = category;
            IsDirty = isDirty;
            FallbackColor = fallbackColor;
        }

        public string PrefabName { get; }
        public string ModelPath { get; }
        public string TextureFolder { get; }
        public string MaterialName { get; }
        public string AlbedoFile { get; }
        public string NormalFile { get; }
        public string OcclusionFile { get; }
        public string MetallicFile { get; }
        public string SpecularSmoothnessFile { get; }
        public float TargetLargestDimension { get; }
        public TrashCategory Category { get; }
        public bool IsDirty { get; }
        public Color? FallbackColor { get; }
        public string PrefabPath => ActiveTrashFolder + "/" + PrefabName + ".prefab";
        public string MaterialPath => MaterialFolder + "/" + MaterialName + ".mat";
        public string AlbedoPath => TexturePath(AlbedoFile);
        public string NormalPath => TexturePath(NormalFile);
        public string OcclusionPath => TexturePath(OcclusionFile);
        public string MetallicPath => TexturePath(MetallicFile);
        public string SpecularSmoothnessPath => TexturePath(SpecularSmoothnessFile);

        private string TexturePath(string fileName)
        {
            return string.IsNullOrEmpty(fileName) ? null : TextureFolder + "/" + fileName;
        }
    }
}
