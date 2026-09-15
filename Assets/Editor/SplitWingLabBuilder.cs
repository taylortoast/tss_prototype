using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

internal static class SplitWingLabBuilder
{
    private const float BuildingWidth = 36f;
    private const float BuildingDepth = 30f;
    private const float WallThickness = 0.25f;
    private const float WallHeight = 2.8f;
    private const float FloorHeight = 3f;
    private const float DoorWidth = 1.2f;
    private const float DoorHeight = 2.1f;
    private const float StairWidth = 2.05f;

    private enum DoorSide { None, North, South, East, West }

    private struct Room
    {
        public string Name;
        public Rect Bounds;
        public DoorSide Door;

        public Room(string name, Rect bounds, DoorSide door)
        {
            Name = name;
            Bounds = bounds;
            Door = door;
        }
    }

    private static readonly string[] OptionNames = { "Option1_CentralSpine", "Option2_LoopHallway", "Option3_SplitWing" };
    private static readonly string[] OptionLabels = { "Central Spine", "Loop Hallway", "Split Wing" };
    private static readonly Dictionary<string, GameObject> Modules = new Dictionary<string, GameObject>();

    [MenuItem("TSS/Build Three Floor-Plan Buildings")]
    public static void BuildAll()
    {
        EnsureFolders();
        CreateModules();
        CreateStaircasePrefabs();
        for (int i = 0; i < OptionNames.Length; i++) BuildOption(i);
        BuildGallery();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built all three floor-plan building prefabs and preview scenes.");
    }

    [MenuItem("TSS/Build Option 3 Split Wing")]
    public static void BuildOption3Only()
    {
        EnsureFolders();
        CreateModules();
        CreateStaircasePrefabs();
        BuildOption(2);
        BuildGallery();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built the Option 3 Split Wing prefab, preview scene, and gallery.");
    }

    private static void EnsureFolders()
    {
        string[] folders = { "Assets/Prefabs", "Assets/Prefabs/Architecture", "Assets/Prefabs/Buildings", "Assets/Scenes", "Assets/Scripts", "Assets/Editor" };
        foreach (string folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = folder.Substring(0, folder.LastIndexOf('/'));
                AssetDatabase.CreateFolder(parent, folder.Substring(folder.LastIndexOf('/') + 1));
            }
        }
    }

    private static void CreateModules()
    {
        Modules.Clear();
        CreateModule("WallModule", new Vector3(3f, WallHeight, WallThickness));
        CreateModule("FloorModule", new Vector3(3f, 0.2f, 3f));
        CreateModule("CeilingModule", new Vector3(3f, 0.2f, 3f));
        CreateModule("RoofModule", new Vector3(3f, 0.2f, 3f));
        CreateModule("StairStepModule", new Vector3(StairWidth, 0.167f, 0.3f));
    }

    private static void CreateModule(string name, Vector3 size)
    {
        string path = "Assets/Prefabs/Architecture/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            Modules[name] = existing;
            return;
        }
        GameObject module = GameObject.CreatePrimitive(PrimitiveType.Cube);
        module.name = name;
        module.transform.localScale = size;
        SetStatic(module);
        PrefabUtility.SaveAsPrefabAsset(module, path);
        UnityEngine.Object.DestroyImmediate(module);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        Modules[name] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void CreateStaircasePrefabs()
    {
        CreateStaircasePrefab("StaircaseLower", false);
        CreateStaircasePrefab("StaircaseUpper", true);
    }

    private static void CreateStaircasePrefab(string name, bool upper)
    {
        string path = "Assets/Prefabs/Architecture/" + name + ".prefab";
        bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = existing ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
        while (root.transform.childCount > 0) UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
        for (int i = 0; i < 9; i++)
        {
            float height = (i + 1) * (FloorHeight / 18f);
            float z = upper ? -0.35f + i * 0.3f : -2.75f + i * 0.3f;
            float y = upper ? 1.5f + height * 0.5f : height * 0.5f;
            float x = upper ? StairWidth * 0.5f : -StairWidth * 0.5f;
            AddModule("StairStepModule", root.transform, new Vector3(x, y, z), new Vector3(StairWidth, height, 0.3f), (upper ? "UpperStep_" : "LowerStep_") + i);
        }
        MarkHierarchyStatic(root.transform);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        if (existing) PrefabUtility.UnloadPrefabContents(root);
        else UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void BuildOption(int option)
    {
        string key = OptionNames[option];
        string label = OptionLabels[option];
        string prefabPath = "Assets/Prefabs/Buildings/Building_" + key + ".prefab";
        string scenePath = "Assets/Scenes/" + key + ".unity";
        AssetDatabase.DeleteAsset(prefabPath);
        AssetDatabase.DeleteAsset(scenePath);
        AssetDatabase.DeleteAsset("Assets/Scenes/" + key);

        GameObject root = new GameObject("Building_" + key);
        root.name = "Building_" + key;
        Transform structure = NewChild(root, "Structure");
        Transform floor1 = NewChild(root, "Floor1");
        Transform floor2 = NewChild(root, "Floor2");
        Transform roof = NewChild(root, "Roof");
        Transform lighting = NewChild(root, "Lighting");
        Transform spawn = NewChild(root, "Spawn");
        spawn.position = new Vector3(0f, 0.05f, -11f);

        AddSlab(floor1, 0f, false);
        AddSlab(floor2, FloorHeight, true);
        AddExterior(structure);
        AddStairCore(structure, option);
        AddRooms(option, floor1, 0f, GetRooms(option, 0));
        AddRooms(option, floor2, FloorHeight, GetRooms(option, 1));
        AddRoof(roof);
        AddLights(option, lighting, GetRooms(option, 0), GetRooms(option, 1));
        MarkHierarchyStatic(root.transform);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject building = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
        building.name = "Building_" + key;
        CreatePlayer(new Vector3(0f, 1f, -11f));
        ConfigureEnvironment();
        EditorSceneManager.SaveScene(preview, scenePath);
        BakeAndSave(preview);
        Debug.Log("Built " + label + " layout: " + scenePath);
    }

    private static void BuildGallery()
    {
        string scenePath = "Assets/Scenes/BuildingGallery.unity";
        AssetDatabase.DeleteAsset(scenePath);
        Scene gallery = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        for (int i = 0; i < OptionNames.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Building_" + OptionNames[i] + ".prefab");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3((i - 1) * 48f, 0f, 0f);
            instance.name = "Building_" + OptionNames[i];
            foreach (Light light in instance.GetComponentsInChildren<Light>(true)) light.enabled = false;
        }
        GameObject cameraObject = new GameObject("Gallery Camera");
        cameraObject.transform.position = new Vector3(0f, 42f, -38f);
        cameraObject.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 52f;
        camera.farClipPlane = 300f;
        cameraObject.tag = "MainCamera";
        ConfigureEnvironment();
        EditorSceneManager.SaveScene(gallery, scenePath);
        EditorBuildSettings.scenes = BuildSceneList();
        EditorSceneManager.OpenScene(scenePath);
    }

    private static EditorBuildSettingsScene[] BuildSceneList()
    {
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene("Assets/Scenes/BuildingGallery.unity", true) };
        foreach (string name in OptionNames) scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/" + name + ".unity", true));
        return scenes.ToArray();
    }

    private static void BakeAndSave(Scene scene)
    {
        EditorSceneManager.SaveScene(scene);
        try { Lightmapping.Bake(); }
        catch (Exception ex) { Debug.LogWarning("Light bake deferred: " + ex.Message); }
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureEnvironment()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.025f, 0.03f, 0.04f, 1f);
        RenderSettings.ambientIntensity = 0.15f;
        RenderSettings.reflectionIntensity = 0f;
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional) UnityEngine.Object.DestroyImmediate(light.gameObject);
        }
    }

    private static void AddSlab(Transform parent, float baseY, bool stairOpening)
    {
        if (!stairOpening)
        {
            AddModule("FloorModule", parent, new Vector3(0f, baseY - 0.1f, 0f), new Vector3(BuildingWidth, 0.2f, BuildingDepth), "FloorSlab");
            return;
        }
        float holeX = 6f, holeZ = 8f;
        float southDepth = (BuildingDepth - holeZ) * 0.5f;
        float sideWidth = (BuildingWidth - holeX) * 0.5f;
        AddModule("FloorModule", parent, new Vector3(0f, baseY - 0.1f, -BuildingDepth * 0.5f + southDepth * 0.5f), new Vector3(BuildingWidth, 0.2f, southDepth), "FloorSlab_South");
        AddModule("FloorModule", parent, new Vector3(0f, baseY - 0.1f, BuildingDepth * 0.5f - southDepth * 0.5f), new Vector3(BuildingWidth, 0.2f, southDepth), "FloorSlab_North");
        AddModule("FloorModule", parent, new Vector3(-BuildingWidth * 0.5f + sideWidth * 0.5f, baseY - 0.1f, 0f), new Vector3(sideWidth, 0.2f, holeZ), "FloorSlab_West");
        AddModule("FloorModule", parent, new Vector3(BuildingWidth * 0.5f - sideWidth * 0.5f, baseY - 0.1f, 0f), new Vector3(sideWidth, 0.2f, holeZ), "FloorSlab_East");
    }

    private static void AddExterior(Transform parent)
    {
        AddWall(parent, new Vector3(0f, 1.4f, -15f), new Vector3(BuildingWidth, WallHeight, WallThickness), "Exterior_South");
        AddWall(parent, new Vector3(0f, 1.4f, 15f), new Vector3(BuildingWidth, WallHeight, WallThickness), "Exterior_North");
        AddWall(parent, new Vector3(-18f, 1.4f, 0f), new Vector3(WallThickness, WallHeight, BuildingDepth), "Exterior_West");
        AddWall(parent, new Vector3(18f, 1.4f, 0f), new Vector3(WallThickness, WallHeight, BuildingDepth), "Exterior_East");
    }

    private static void AddRoof(Transform parent)
    {
        AddModule("RoofModule", parent, new Vector3(0f, 5.9f, 0f), new Vector3(BuildingWidth, 0.2f, BuildingDepth), "RoofSlab");
        AddWall(parent, new Vector3(0f, 6.3f, -15f), new Vector3(BuildingWidth, 0.6f, WallThickness), "Parapet_South");
        AddWall(parent, new Vector3(0f, 6.3f, 15f), new Vector3(BuildingWidth, 0.6f, WallThickness), "Parapet_North");
        AddWall(parent, new Vector3(-18f, 6.3f, 0f), new Vector3(WallThickness, 0.6f, BuildingDepth), "Parapet_West");
        AddWall(parent, new Vector3(18f, 6.3f, 0f), new Vector3(WallThickness, 0.6f, BuildingDepth), "Parapet_East");
    }

    private static void AddStairCore(Transform parent, int option)
    {
        AddWallWithDoor(parent, new Vector3(0f, 1.4f, -4f), 6f, false, DoorWidth, "StairCore_South");
        AddWall(parent, new Vector3(-3f, 1.4f, 0f), new Vector3(WallThickness, WallHeight, 8f), "StairCore_West");
        AddWall(parent, new Vector3(3f, 1.4f, 0f), new Vector3(WallThickness, WallHeight, 8f), "StairCore_East");
        AddWall(parent, new Vector3(0f, 1.4f, 4f), new Vector3(6f, WallHeight, WallThickness), "StairCore_North");
        Transform stairs = NewChild(parent.gameObject, "Staircase");
        GameObject lowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Architecture/StaircaseLower.prefab");
        GameObject upperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Architecture/StaircaseUpper.prefab");
        if (lowerPrefab != null) PrefabUtility.InstantiatePrefab(lowerPrefab, stairs);
        if (upperPrefab != null) PrefabUtility.InstantiatePrefab(upperPrefab, stairs);
        if (option == 2)
        {
            AddModule("FloorModule", stairs, new Vector3(0f, 1.5f, -0.35f), new Vector3(4.1f, 0.2f, 1.5f), "StairLandingMid");
            AddModule("FloorModule", stairs, new Vector3(0f, 1.5f, 1.874f), new Vector3(5.8f, 0.2f, 4.1f), "StairLanding");
            AddModule("FloorModule", stairs, new Vector3(0f, FloorHeight - 0.1f, 2.05f), new Vector3(5.8f, 0.2f, 3.9f), "StairLandingTop");
            AddUpperStairEnclosure(parent);
        }
        else
        {
            AddModule("FloorModule", stairs, new Vector3(0f, 1.5f, -0.05f), new Vector3(3f, 0.2f, 1.5f), "StairLanding");
        }
    }

    private static void AddUpperStairEnclosure(Transform parent)
    {
        float y = FloorHeight + WallHeight * 0.5f;
        AddWall(parent, new Vector3(-3f, y, 0f), new Vector3(WallThickness, WallHeight, 8f), "StairCore_Upper_West");
        AddVerticalWallWithDoor(parent, 3f, y, -4f, 4f, 2.05f, DoorWidth, "StairCore_Upper_East");
        AddWall(parent, new Vector3(0f, y, 4f), new Vector3(6f, WallHeight, WallThickness), "StairCore_Upper_North");
        AddWall(parent, new Vector3(0f, y, -4f), new Vector3(6f, WallHeight, WallThickness), "StairCore_Upper_South");
    }

    private static void AddVerticalWallWithDoor(Transform parent, float x, float y, float zMin, float zMax, float doorCenter, float gap, string name)
    {
        float southLength = Mathf.Max(0.5f, doorCenter - gap * 0.5f - zMin);
        float northLength = Mathf.Max(0.5f, zMax - (doorCenter + gap * 0.5f));
        AddWall(parent, new Vector3(x, y, zMin + southLength * 0.5f), new Vector3(WallThickness, WallHeight, southLength), name + "_South");
        AddWall(parent, new Vector3(x, y, zMax - northLength * 0.5f), new Vector3(WallThickness, WallHeight, northLength), name + "_North");
        float headerHeight = WallHeight - DoorHeight;
        AddWall(parent, new Vector3(x, y + DoorHeight + headerHeight * 0.5f - WallHeight * 0.5f, doorCenter), new Vector3(WallThickness, headerHeight, gap), name + "_Header");
    }

    private static void AddRooms(int option, Transform parent, float baseY, List<Room> rooms)
    {
        foreach (Room room in rooms)
        {
            Transform roomRoot = NewChild(parent.gameObject, room.Name);
            AddRoomWalls(roomRoot, room, baseY);
        }
    }

    private static void AddRoomWalls(Transform parent, Room room, float baseY)
    {
        float y = baseY + WallHeight * 0.5f;
        Rect r = room.Bounds;
        if (room.Door == DoorSide.South) AddWallWithDoor(parent, new Vector3((r.xMin + r.xMax) * 0.5f, y, r.yMin), r.width, false, DoorWidth, room.Name + "_South");
        else AddWall(parent, new Vector3((r.xMin + r.xMax) * 0.5f, y, r.yMin), new Vector3(r.width, WallHeight, WallThickness), room.Name + "_South");
        if (room.Door == DoorSide.North) AddWallWithDoor(parent, new Vector3((r.xMin + r.xMax) * 0.5f, y, r.yMax), r.width, false, DoorWidth, room.Name + "_North");
        else AddWall(parent, new Vector3((r.xMin + r.xMax) * 0.5f, y, r.yMax), new Vector3(r.width, WallHeight, WallThickness), room.Name + "_North");
        if (room.Door == DoorSide.West) AddWallWithDoor(parent, new Vector3(r.xMin, y, (r.yMin + r.yMax) * 0.5f), r.height, true, DoorWidth, room.Name + "_West");
        else AddWall(parent, new Vector3(r.xMin, y, (r.yMin + r.yMax) * 0.5f), new Vector3(WallThickness, WallHeight, r.height), room.Name + "_West");
        if (room.Door == DoorSide.East) AddWallWithDoor(parent, new Vector3(r.xMax, y, (r.yMin + r.yMax) * 0.5f), r.height, true, DoorWidth, room.Name + "_East");
        else AddWall(parent, new Vector3(r.xMax, y, (r.yMin + r.yMax) * 0.5f), new Vector3(WallThickness, WallHeight, r.height), room.Name + "_East");
    }

    private static void AddWallWithDoor(Transform parent, Vector3 center, float length, bool vertical, float gap, string name)
    {
        float sideLength = Mathf.Max(0.5f, (length - gap) * 0.5f);
        Vector3 axis = vertical ? Vector3.forward : Vector3.right;
        AddWall(parent, center - axis * (gap + sideLength) * 0.5f, vertical ? new Vector3(WallThickness, WallHeight, sideLength) : new Vector3(sideLength, WallHeight, WallThickness), name + "_A");
        AddWall(parent, center + axis * (gap + sideLength) * 0.5f, vertical ? new Vector3(WallThickness, WallHeight, sideLength) : new Vector3(sideLength, WallHeight, WallThickness), name + "_B");
        float headerHeight = WallHeight - DoorHeight;
        AddWall(parent, center + Vector3.up * (DoorHeight + headerHeight * 0.5f - WallHeight * 0.5f), vertical ? new Vector3(WallThickness, headerHeight, gap) : new Vector3(gap, headerHeight, WallThickness), name + "_Header");
    }

    private static void AddWall(Transform parent, Vector3 position, Vector3 size, string name)
    {
        AddModule("WallModule", parent, position, size, name);
    }

    private static void AddModule(string moduleName, Transform parent, Vector3 position, Vector3 scale, string name)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(Modules[moduleName], parent);
        instance.name = name;
        instance.transform.localPosition = position;
        instance.transform.localScale = scale;
        SetStatic(instance);
    }

    private static Transform NewChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child.transform;
    }

    private static Transform NewChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void AddLights(int option, Transform parent, List<Room> lowerRooms, List<Room> upperRooms)
    {
        int index = 0;
        foreach (Room room in lowerRooms)
        {
            AddRoomLights(parent, room, 1.35f, ref index);
        }
        foreach (Room room in upperRooms)
        {
            AddRoomLights(parent, room, 4.35f, ref index);
        }
        for (int i = -2; i <= 2; i++)
        {
            AddPointLight(parent, new Vector3(0f, 2.35f, i * 5f), 3.5f, "HallLight_Lower_" + i);
            AddPointLight(parent, new Vector3(0f, 5.35f, i * 5f), 3.5f, "HallLight_Upper_" + i);
        }
        AddPointLight(parent, new Vector3(-1.8f, 2.35f, -1.5f), 3.5f, "StairLight_West");
        AddPointLight(parent, new Vector3(1.8f, 2.35f, -1.5f), 3.5f, "StairLight_East");
        AddPointLight(parent, new Vector3(-1.8f, 5.35f, -1.5f), 3.5f, "StairLight_Upper_West");
        AddPointLight(parent, new Vector3(1.8f, 5.35f, -1.5f), 3.5f, "StairLight_Upper_East");
    }

    private static void AddRoomLights(Transform parent, Room room, float y, ref int index)
    {
        int xCount = Mathf.Clamp(Mathf.CeilToInt(room.Bounds.width / 6f), 1, 2);
        int zCount = Mathf.Clamp(Mathf.CeilToInt(room.Bounds.height / 6f), 1, 2);
        for (int x = 0; x < xCount; x++)
            for (int z = 0; z < zCount; z++)
            {
                float px = Mathf.Lerp(room.Bounds.xMin + 1.6f, room.Bounds.xMax - 1.6f, xCount == 1 ? 0.5f : (float)x / (xCount - 1));
                float pz = Mathf.Lerp(room.Bounds.yMin + 1.6f, room.Bounds.yMax - 1.6f, zCount == 1 ? 0.5f : (float)z / (zCount - 1));
                AddPointLight(parent, new Vector3(px, y, pz), 4.5f, "RoomLight_" + index++);
            }
    }

    private static void AddPointLight(Transform parent, Vector3 position, float range, string name)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = range;
        light.intensity = 2.2f;
        light.color = new Color(0.93f, 0.97f, 1f);
        light.shadows = LightShadows.Soft;
        light.lightmapBakeType = LightmapBakeType.Baked;
    }

    private static void CreatePlayer(Vector3 position)
    {
        GameObject player = new GameObject("Player");
        player.transform.position = position;
        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 1f, 0f);
        controller.stepOffset = 0.3f;
        controller.slopeLimit = 45f;
        player.AddComponent<ThirdPersonWalker>();
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "PlayerBody";
        body.transform.SetParent(player.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
        CapsuleCollider capsule = body.GetComponent<CapsuleCollider>();
        if (capsule != null) UnityEngine.Object.DestroyImmediate(capsule);
        GameObject pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        GameObject cameraObject = new GameObject("PlayerCamera");
        cameraObject.transform.SetParent(pivot.transform, false);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 60f;
        ThirdPersonWalker walker = player.GetComponent<ThirdPersonWalker>();
        SerializedObject serialized = new SerializedObject(walker);
        serialized.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
        serialized.FindProperty("playerCamera").objectReferenceValue = camera;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static List<Room> GetRooms(int option, int floor)
    {
        float xL = -17f, xL2 = 4.5f;
        float north = 5f, midBottom = -1.8f, bottom = -13.5f;
        var rooms = new List<Room>();
        if (option == 0)
        {
            if (floor == 0)
            {
                rooms.Add(new Room("WorkstationRoomA", new Rect(xL, north, 12.5f, 9f), DoorSide.East));
                rooms.Add(new Room("InstructorArea", new Rect(xL, midBottom, 12.5f, 5.5f), DoorSide.East));
                rooms.Add(new Room("Storage", new Rect(xL, bottom, 12.5f, 10.8f), DoorSide.East));
                rooms.Add(new Room("WorkstationRoomB", new Rect(xL2, north, 12.5f, 9f), DoorSide.West));
                rooms.Add(new Room("ServerRoom", new Rect(xL2, midBottom, 12.5f, 5.5f), DoorSide.West));
                rooms.Add(new Room("NetworkCloset", new Rect(xL2, bottom, 12.5f, 10.8f), DoorSide.West));
            }
            else
            {
                rooms.Add(new Room("WorkstationRoomC", new Rect(xL, north, 12.5f, 9f), DoorSide.East));
                rooms.Add(new Room("WorkstationRoomE", new Rect(xL, bottom, 12.5f, 15f), DoorSide.East));
                rooms.Add(new Room("WorkstationRoomD", new Rect(xL2, north, 12.5f, 9f), DoorSide.West));
                rooms.Add(new Room("NetworkCloset", new Rect(xL2, midBottom, 12.5f, 5.5f), DoorSide.West));
                rooms.Add(new Room("WorkstationRoomF", new Rect(xL2, bottom, 12.5f, 10.8f), DoorSide.West));
            }
        }
        else if (option == 1)
        {
            rooms.Add(new Room("WorkstationRoomA", new Rect(-10.5f, 9f, 10.5f, 5f), DoorSide.South));
            rooms.Add(new Room("WorkstationRoomB", new Rect(-16f, -5f, 5f, 10f), DoorSide.East));
            rooms.Add(new Room("WorkstationRoomC", new Rect(11f, -5f, 5f, 10f), DoorSide.West));
            rooms.Add(new Room("ServerRoom", new Rect(-5f, 1f, 10f, 7f), DoorSide.South));
            rooms.Add(new Room("NetworkCloset", new Rect(5f, 1f, 4f, 4f), DoorSide.West));
            rooms.Add(new Room("ServerRoomAnnex", new Rect(-5f, -7f, 10f, 5f), DoorSide.North));
            rooms.Add(new Room("BreakoutArea", new Rect(-8f, -14f, 16f, 5f), DoorSide.North));
        }
        else
        {
            if (floor == 0)
            {
                rooms.Add(new Room("WorkstationLabA", new Rect(xL, 5f, 11f, 9f), DoorSide.East));
                rooms.Add(new Room("PracticeRoom", new Rect(xL, -1.8f, 11f, 5.5f), DoorSide.East));
                rooms.Add(new Room("WorkstationLabB", new Rect(xL, bottom, 11f, 10.8f), DoorSide.East));
                rooms.Add(new Room("ServerRoom", new Rect(xL2, 5f, 11f, 9f), DoorSide.West));
                rooms.Add(new Room("NetworkCloset", new Rect(xL2, -1.8f, 11f, 5.5f), DoorSide.West));
                rooms.Add(new Room("UtilityRoom", new Rect(xL2, bottom, 11f, 10.8f), DoorSide.West));
            }
            else
            {
                rooms.Add(new Room("ObservationArea", new Rect(xL, 5f, 11f, 9f), DoorSide.East));
                rooms.Add(new Room("PracticeRoom", new Rect(xL, -1.8f, 11f, 5.5f), DoorSide.East));
                rooms.Add(new Room("UpperWestRoom", new Rect(xL, bottom, 11f, 10.8f), DoorSide.East));
                rooms.Add(new Room("CoreServerRoom", new Rect(xL2, 5f, 11f, 9f), DoorSide.West));
                rooms.Add(new Room("NetworkCloset", new Rect(xL2, -1.8f, 11f, 5.5f), DoorSide.West));
                rooms.Add(new Room("UpperEastRoom", new Rect(xL2, bottom, 11f, 10.8f), DoorSide.West));
            }
        }
        return rooms;
    }

    private static List<Room> GetAllRooms(int option)
    {
        var all = GetRooms(option, 0);
        all.AddRange(GetRooms(option, 1));
        return all;
    }

    private static void SetStatic(GameObject gameObject)
    {
        StaticEditorFlags flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic;
        GameObjectUtility.SetStaticEditorFlags(gameObject, flags);
    }

    private static void MarkHierarchyStatic(Transform root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) SetStatic(child.gameObject);
    }
}
