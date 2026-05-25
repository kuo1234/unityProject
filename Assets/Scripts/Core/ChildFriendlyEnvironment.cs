using UnityEngine;

public class ChildFriendlyEnvironment : MonoBehaviour
{
    public bool useRealisticHdriSkybox;

    private Material grassMaterial;
    private Material skyMaterial;
    private Material hillMaterial;
    private Material farHillMaterial;
    private Material cloudMaterial;
    private Material sunMaterial;
    private Material trunkMaterial;
    private Material treeMaterial;
    private Material hdriSkyboxMaterial;
    private bool usingHdriSky;

    private void Start()
    {
        CreateMaterials();
        RemoveOldRuntimeScenery();
        usingHdriSky = useRealisticHdriSkybox && TryApplyHdriSkybox();
        ApplyWorldLightingAndSky();
        RecolorExistingFloor();
        EnsureNaturalGround();
        if (!usingHdriSky)
        {
            EnsureSkyDome();
        }
        EnsureDistantHills();
        EnsureTreeLine();
        if (!usingHdriSky)
        {
            EnsureSunAndClouds();
        }
    }

    private void CreateMaterials()
    {
        grassMaterial = CreateMaterial("Runtime_Cartoon_Grass", new Color(0.38f, 0.78f, 0.46f));
        skyMaterial = CreateMaterial("Runtime_Cartoon_Sky", new Color(0.46f, 0.78f, 1f), true);
        hillMaterial = CreateMaterial("Runtime_Cartoon_Near_Hills", new Color(0.5f, 0.86f, 0.38f));
        farHillMaterial = CreateMaterial("Runtime_Cartoon_Far_Hills", new Color(0.38f, 0.7f, 0.5f));
        cloudMaterial = CreateMaterial("Runtime_Cartoon_Clouds", new Color(1f, 1f, 0.96f));
        sunMaterial = CreateMaterial("Runtime_Cartoon_Sun", new Color(1f, 0.82f, 0.18f));
        trunkMaterial = CreateMaterial("Runtime_Cartoon_Tree_Trunk", new Color(0.52f, 0.34f, 0.18f));
        treeMaterial = CreateMaterial("Runtime_Cartoon_Tree_Canopy", new Color(0.18f, 0.68f, 0.3f));
    }

    private void RemoveOldRuntimeScenery()
    {
        DestroyIfExists("ChildFriendlyBackdrop");
        DestroyIfExists("ChildFriendlyLeftWall");
        DestroyIfExists("ChildFriendlyRightWall");
        DestroyIfExists("ChildFriendlyFrontWall");
        DestroyIfExists("ChildFriendlyPlayMat");
        DestroyIfExists("ChildFriendlySkyDome");

        for (int i = 0; i < 20; i++)
        {
            DestroyIfExists("ChildFlower_" + i);
        }

        DestroyIfExists("ChildFriendlySun");
        DestroyIfExists("ChildFriendlyCloudLeft");
        DestroyIfExists("ChildFriendlyCloudRight");
        DestroyIfExists("ChildFriendlyCloudFront");
        DestroyIfExists("ChildFriendlyCloudHighBack");
        DestroyIfExists("ChildFriendlyCloudFarLeft");
        DestroyIfExists("ChildFriendlyCloudFarRight");
        DestroyIfExists("ChildFriendlyCloudOverhead");
        DestroyIfExists("ChildFriendlyCloudBackLeft");
        DestroyIfExists("ChildFriendlyCloudBackRight");
        DestroyIfExists("ChildFriendlyCloudLeftLow");
        DestroyIfExists("ChildFriendlyCloudRightLow");
        DestroyIfExists("ChildFriendlyCloudFrontLeft");
        DestroyIfExists("ChildFriendlyCloudFrontRight");
        DestroyIfExists("ChildFriendlyCloudHighLeft");
        DestroyIfExists("ChildFriendlyCloudHighRight");
        DestroyIfExists("ChildFriendlyRainbowRed");
        DestroyIfExists("ChildFriendlyRainbowYellow");
        DestroyIfExists("ChildFriendlyRainbowBlue");
    }

    private bool TryApplyHdriSkybox()
    {
        Texture skyTexture = Resources.Load<Texture>("Sky/cloud_layers_1k");
        if (skyTexture == null)
        {
            return false;
        }

        Shader skyboxShader = Shader.Find("Skybox/Panoramic");
        if (skyboxShader == null)
        {
            return false;
        }

        hdriSkyboxMaterial = new Material(skyboxShader);
        hdriSkyboxMaterial.name = "Runtime_PolyHaven_CloudLayers_Skybox";
        hdriSkyboxMaterial.SetTexture("_MainTex", skyTexture);
        hdriSkyboxMaterial.SetFloat("_Exposure", 1.05f);
        hdriSkyboxMaterial.SetFloat("_Rotation", 115f);
        RenderSettings.skybox = hdriSkyboxMaterial;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.Skybox;
        }

        DynamicGI.UpdateEnvironment();
        return true;
    }

    private void ApplyWorldLightingAndSky()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            if (!usingHdriSky)
            {
                RenderSettings.skybox = null;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.52f, 0.8f, 1f);
            }
        }

        RenderSettings.ambientLight = new Color(0.86f, 0.9f, 0.88f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.64f, 0.84f, 0.98f);
        RenderSettings.fogDensity = 0.006f;

        Light sunLight = RenderSettings.sun;
        if (sunLight == null)
        {
            foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (light.type == LightType.Directional)
                {
                    sunLight = light;
                    break;
                }
            }
        }

        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            sunLight.color = new Color(1f, 0.95f, 0.76f);
            sunLight.intensity = 1.05f;
            RenderSettings.sun = sunLight;
        }
    }

    private void RecolorExistingFloor()
    {
        GameObject floor = GameObject.Find("SortingFloor");
        if (floor != null && floor.TryGetComponent(out Renderer renderer))
        {
            renderer.sharedMaterial = grassMaterial;
        }
    }

    private void EnsureNaturalGround()
    {
        if (GameObject.Find("ChildFriendlyWholeFloor") == null)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "ChildFriendlyWholeFloor";
            floor.transform.position = new Vector3(0f, 0.004f, 1.1f);
            floor.transform.localScale = new Vector3(18f, 0.012f, 18f);
            SetRendererMaterial(floor, grassMaterial);
            DestroyCollider(floor);
        }
        else
        {
            GameObject.Find("ChildFriendlyWholeFloor").transform.localScale = new Vector3(18f, 0.012f, 18f);
        }
    }

    private void EnsureSkyDome()
    {
        if (GameObject.Find("ChildFriendlySkyDome") != null)
        {
            return;
        }

        GameObject sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sky.name = "ChildFriendlySkyDome";
        sky.transform.position = new Vector3(0f, 4f, 1f);
        sky.transform.localScale = new Vector3(34f, 18f, 34f);
        SetRendererMaterial(sky, skyMaterial);
        DestroyCollider(sky);
    }

    private void EnsureDistantHills()
    {
        EnsureHill("ChildHillBackA", new Vector3(-4.7f, -0.12f, 8.0f), new Vector3(7.5f, 1.55f, 1.2f), farHillMaterial);
        EnsureHill("ChildHillBackB", new Vector3(2.6f, -0.2f, 8.4f), new Vector3(8.8f, 1.9f, 1.25f), hillMaterial);
        EnsureHill("ChildHillLeft", new Vector3(-7.0f, -0.18f, 1.2f), new Vector3(1.3f, 1.65f, 7.8f), farHillMaterial);
        EnsureHill("ChildHillRight", new Vector3(7.0f, -0.18f, 1.2f), new Vector3(1.3f, 1.65f, 7.8f), hillMaterial);
        EnsureHill("ChildHillFront", new Vector3(0f, -0.28f, -6.6f), new Vector3(10f, 1.55f, 1.1f), farHillMaterial);
    }

    private void EnsureHill(string objectName, Vector3 position, Vector3 scale, Material material)
    {
        if (GameObject.Find(objectName) != null)
        {
            return;
        }

        GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hill.name = objectName;
        hill.transform.position = position;
        hill.transform.localScale = scale;
        SetRendererMaterial(hill, material);
        DestroyCollider(hill);
    }

    private void EnsureTreeLine()
    {
        EnsureTree("ChildTreeBackLeft", new Vector3(-4.9f, 0f, 6.5f), 1.1f);
        EnsureTree("ChildTreeBackRight", new Vector3(4.8f, 0f, 6.4f), 1.0f);
        EnsureTree("ChildTreeLeftNear", new Vector3(-5.4f, 0f, -1.8f), 0.9f);
        EnsureTree("ChildTreeLeftFar", new Vector3(-5.6f, 0f, 3.2f), 1.05f);
        EnsureTree("ChildTreeRightNear", new Vector3(5.4f, 0f, -1.3f), 0.9f);
        EnsureTree("ChildTreeRightFar", new Vector3(5.6f, 0f, 3.4f), 1.08f);
    }

    private void EnsureTree(string objectName, Vector3 position, float scale)
    {
        if (GameObject.Find(objectName) != null)
        {
            return;
        }

        GameObject root = new GameObject(objectName);
        root.transform.position = position;
        root.transform.localScale = Vector3.one * scale;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        trunk.transform.localScale = new Vector3(0.16f, 0.45f, 0.16f);
        SetRendererMaterial(trunk, trunkMaterial);
        DestroyCollider(trunk);

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.transform.SetParent(root.transform, false);
        canopy.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        canopy.transform.localScale = new Vector3(0.85f, 0.75f, 0.85f);
        SetRendererMaterial(canopy, treeMaterial);
        DestroyCollider(canopy);
    }

    private void EnsureSunAndClouds()
    {
        EnsureSun();
        EnsureCloud("ChildFriendlyCloudLeft", new Vector3(-4.2f, 5.35f, 4.6f), 1.15f, 0.18f);
        EnsureCloud("ChildFriendlyCloudRight", new Vector3(3.8f, 4.95f, 4.2f), 1.05f, -0.12f);
        EnsureCloud("ChildFriendlyCloudFront", new Vector3(1.6f, 4.75f, -4.2f), 0.95f, 0.05f);
        EnsureCloud("ChildFriendlyCloudHighBack", new Vector3(0.1f, 6.15f, 6.2f), 1.35f, -0.22f);
        EnsureCloud("ChildFriendlyCloudFarLeft", new Vector3(-6.0f, 4.7f, 0.1f), 0.9f, 0.3f);
        EnsureCloud("ChildFriendlyCloudFarRight", new Vector3(6.1f, 5.05f, 0.6f), 0.95f, -0.28f);
        EnsureCloud("ChildFriendlyCloudOverhead", new Vector3(-0.9f, 6.35f, 0.2f), 1.0f, 0.12f);
        EnsureCloud("ChildFriendlyCloudBackLeft", new Vector3(-5.6f, 5.45f, 7.0f), 0.85f, 0.4f);
        EnsureCloud("ChildFriendlyCloudBackRight", new Vector3(5.5f, 5.7f, 7.2f), 0.9f, -0.35f);
        EnsureCloud("ChildFriendlyCloudLeftLow", new Vector3(-7.1f, 3.95f, -2.4f), 0.72f, 0.25f);
        EnsureCloud("ChildFriendlyCloudRightLow", new Vector3(7.1f, 4.15f, -2.1f), 0.76f, -0.2f);
        EnsureCloud("ChildFriendlyCloudFrontLeft", new Vector3(-3.8f, 4.35f, -5.3f), 0.82f, -0.15f);
        EnsureCloud("ChildFriendlyCloudFrontRight", new Vector3(4.6f, 4.55f, -5.0f), 0.88f, 0.18f);
        EnsureCloud("ChildFriendlyCloudHighLeft", new Vector3(-4.4f, 6.55f, 1.8f), 0.86f, -0.32f);
        EnsureCloud("ChildFriendlyCloudHighRight", new Vector3(4.2f, 6.7f, 2.2f), 0.92f, 0.34f);
    }

    private void EnsureSun()
    {
        GameObject sun = GameObject.Find("ChildFriendlySun");
        if (sun == null)
        {
            sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sun.name = "ChildFriendlySun";
            DestroyCollider(sun);
        }

        sun.transform.position = new Vector3(-5.8f, 5.8f, 3.4f);
        sun.transform.localScale = Vector3.one * 0.86f;
        SetRendererMaterial(sun, sunMaterial);
    }

    private void EnsureCloud(string objectName, Vector3 position, float scale, float yawOffset)
    {
        GameObject cloudRoot = GameObject.Find(objectName);
        if (cloudRoot != null)
        {
            return;
        }

        cloudRoot = new GameObject(objectName);
        cloudRoot.transform.position = position;
        cloudRoot.transform.rotation = Quaternion.Euler(0f, yawOffset * 60f, 0f);
        cloudRoot.transform.localScale = Vector3.one * scale;

        CreateCloudPart(cloudRoot.transform, new Vector3(-0.56f, -0.02f, 0f), new Vector3(0.8f, 0.34f, 0.22f));
        CreateCloudPart(cloudRoot.transform, new Vector3(-0.18f, 0.12f, 0.02f), new Vector3(0.9f, 0.46f, 0.24f));
        CreateCloudPart(cloudRoot.transform, new Vector3(0.28f, 0.06f, -0.01f), new Vector3(0.86f, 0.4f, 0.22f));
        CreateCloudPart(cloudRoot.transform, new Vector3(0.74f, -0.03f, 0f), new Vector3(0.66f, 0.32f, 0.2f));
        CreateCloudPart(cloudRoot.transform, new Vector3(0.02f, -0.14f, 0.03f), new Vector3(1.36f, 0.26f, 0.18f));
    }

    private void CreateCloudPart(Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        SetRendererMaterial(part, cloudMaterial);
        DestroyCollider(part);
    }

    private static Material CreateMaterial(string materialName, Color color, bool cullOff = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (cullOff)
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        return material;
    }

    private static void SetRendererMaterial(GameObject target, Material material)
    {
        if (target.TryGetComponent(out Renderer renderer))
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void DestroyCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            Destroy(target);
        }
    }
}
