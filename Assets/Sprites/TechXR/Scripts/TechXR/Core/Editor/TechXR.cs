using UnityEngine;
using UnityEditor;
using TechXR.Core.Sense;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.EventSystems;

namespace TechXR.Core.Editor
{
    public class TechXR : EditorWindow
    {
        // max layers and tags allowed
        private const int MAX_LAYERS = 31;
        private const int MAX_TAGS = 10000;

        // layers and tags to be added
        private static List<string> m_Layers = new List<string>() { "Ground" };
        private static List<string> m_Tags = new List<string>() { "XRPlayerController", "SenseController", "SenseManager", "Env", "CharacterBody", "TechXRDeveloperCube", "SenseEventSystem", "GazeImage", "ISphere" };

        // GUI Styling
        private GUIStyle guiStyle = new GUIStyle();

        //
        private bool m_XRInitialized;
        private bool m_UIAssetsDisplay;
        private string m_UIAssetsButtonTitle = "Show UI Assets";
        //

        // For CheckBox
        bool m_IsCamera;
        bool m_IsController;
        bool m_IsPlayer;
        bool m_IsEnvironment;
        static bool m_TrackableWarningSystem = true;
        static bool m_BluetoothWarningSystem;

        bool m_IsCustomSceneGroupEnabled;

        // Radio Button Group
        static int selectApplicationModeIndex = 1;
        static string[] applicationModeOptions = new string[] { "  AR         ", "  VR" };

        // For Folding the elements
        protected static bool showUIElements = false;
        protected static bool showPrefabs = false;
        protected static bool showObjects = false;

        // Dropdown Controller Body Options 
        string[] controllerBodyOptions = { "Default", "Hand" };
        static int controllerBodyIndex = 0;

        // Dropdown Player Character Options
        static string[] playerCharacterOptions = { "None", "Default" };
        static int playerCharacterIndex = 0;

        // Dropdown Player Character Options
        static string[] arControllerOptions = { "InsideView", "OutsideView", "IntractableCube" };
        static int arControllerIndex = 0;

        // Dropdown Environment Options
        static int environmentIndex;
        static DirectoryInfo environmentDir = new DirectoryInfo("Assets/TechXR/Prefabs/Environments");
        static FileInfo[] environmentFileInfo;
        static string[] environmentOptions;

        // Dropdown Canvas Type Options
        static int uiElementIndex;
        static DirectoryInfo uiElementDir = new DirectoryInfo("Assets/TechXR/Prefabs/UIElements/UI");
        static FileInfo[] uiElementFilesInfo;
        string[] uiElementOptions;

        // Dropdown 3D Model Options
        static int modelIndex;
        static DirectoryInfo modelsDir = new DirectoryInfo("Assets/TechXR/3DModels");
        static FileInfo[] modelFilesInfo;
        string[] modelOptions;

        // Dropdown Skybox Options
        static int skyboxIndex;
        static DirectoryInfo skyboxDir = new DirectoryInfo("Assets/TechXR/Skybox/Mat");
        static FileInfo[] skyboxFilesInfo;
        static string[] skyboxOptions;

        Texture2D controllerBody;
        Texture2D uiElement;

        Vector2 scrollerPosition;

        // Tab System
        private GUISkin skin;
        private GUIStyle rightTabStyle, leftTabStyle, tabStyle2, tabStyle3;
        static private int selectedTab = 0;
        static string[] tabs = { "AR", "VR" };
        GUISkin originalGUISkin;

        static private int SelectedTabsOfDeveloperSDK = 0;
        static string[] DeveloperSDKOptions = { "DEVELOPER CUBE", "6 DoF CONTROLLER" };

        // Skin color alternate
        Texture2D headerSectionTexture;
        Texture2D middleSectionTexture;
        Color headerSectionColor = new Color(0.76f, 0.76f, 0.76f, 1);
        Rect headerSection;
        Rect middleSection;
        //

        private void OnEnable()
        {
            // save references to skin and style
            skin = (GUISkin)Resources.Load("CustomSkin");
            rightTabStyle = skin.GetStyle("RightTab");
            leftTabStyle = skin.GetStyle("LeftTab");
            tabStyle2 = skin.GetStyle("Tab2");
            tabStyle3 = skin.GetStyle("Tab3");

            // Update Layers and Tags
            AddLayersAndTags();

            // Update the dropdown lists
            FillEnvironmentOptions();
            FillModelOptions();
            FillSkyboxOptions();
            FillUIElementOptions();

            //
            InitTextures();
            //
        }

        void InitTextures()
        {
            headerSectionTexture = new Texture2D(1, 1);
            headerSectionTexture.SetPixel(0, 0, headerSectionColor);
            headerSectionTexture.Apply();
        }

        void DrawLayouts()
        {
            headerSection.x = 0;
            headerSection.y = 0;
            headerSection.width = Screen.width;
            headerSection.height = Screen.height;

            GUI.DrawTexture(headerSection, headerSectionTexture);
        }

        [MenuItem("TechXR/Preferences")]
        public static void ShowWindow()
        {
            TechXR window = (TechXR)EditorWindow.GetWindow<TechXR>("TechXR Sense Action Panel");
            window.minSize = new Vector2(600, 620);
            window.maxSize = new Vector2(600, 640);

            // Give path to the TechXR Icon here
            //var texture = Resources.Load<Texture>("Icon/TechXR");
            //window.titleContent = new GUIContent("Nice icon, eh?", texture, "Just the tip");
        }

        [MenuItem("TechXR/Set-Up Controller VR Scene")]
        public static void SetUpVRScene()
        {
            if (EditorUtility.DisplayDialog("Warning...!!", "TechXR-Default VR Scene will be loaded, it may remove the Existing Camera, XRPlayers, XRController and DeveloperCube.", "Ok", "Close"))
            {
                selectApplicationModeIndex = 1;

                // Reset Dropdown Selection
                controllerBodyIndex = 0;
                playerCharacterIndex = 0;
                environmentIndex = 0;

                // Delete TechXR-Environments
                GameObject[] envs = GameObject.FindGameObjectsWithTag("Env");
                foreach (GameObject e in envs)
                    DestroyImmediate(e);

                // Add Items
                Dictionary<string, Action> functionMap = new Dictionary<string, Action>()
                {
                    { "AddLayersAndTags", AddLayersAndTags },
                    { "XRPlayerController", AddXRPlayerController },
                    { "Environment", AddEnvironment },
                    { "DirectionalLight", AddDirectionalLight}
                };

                foreach (var item in functionMap)
                {
                    item.Value();
                }

                // Camera settings
                Camera.main.clearFlags = CameraClearFlags.Skybox;

                // Notify
                foreach (SceneView scene in SceneView.sceneViews)
                {
                    scene.ShowNotification(new GUIContent("Updated..!!"));
                }
            }
        }

        // Validate the menu item defined by the function above.
        // The menu item will be disabled if this function returns false.
        [MenuItem("TechXR/Set-Up Controller VR Scene", true)]
        static bool ValidateSetUpVRScene()
        {
            // Return false if no transform is selected.
            return false;
        }

        [MenuItem("TechXR/Set-Up Controller AR Scene")]
        public static void SetUpARScene()
        {
            if (EditorUtility.DisplayDialog("Warning...!!", "TechXR-Default AR Scene will be loaded, it may remove the Existing TechXR-Environment, XRPlayer, XRController, DeveloperCube and Cameras.", "Ok", "Close"))
            {
                // Reset Dropdown Selection
                controllerBodyIndex = 0;
                playerCharacterIndex = 0;
                environmentIndex = 0;

                // Add Tags & Layers
                AddLayersAndTags();

                // Delete all un-necessary things for AR camera configuration
                GameObject[] grounds = GameObject.FindGameObjectsWithTag("Env");
                foreach (GameObject ground in grounds)
                    DestroyImmediate(ground);

                GameObject[] players = GameObject.FindGameObjectsWithTag("XRPlayerController");
                foreach (GameObject player in players)
                    DestroyImmediate(player);

                // Add Items
                AddSenseCamera();
                AddController();
                AddSenseManager();
                AddDirectionalLight();

                // Camera settings
                Camera.main.clearFlags = CameraClearFlags.SolidColor;

                // Notify
                foreach (SceneView scene in SceneView.sceneViews)
                {
                    scene.ShowNotification(new GUIContent("Updated..!!"));
                }
            }
        }

        [MenuItem("TechXR/Set-Up Controller AR Scene", true)]
        static bool ValidateSetUpARScene()
        {
            // Return false if no transform is selected.
            return false;
        }

        [MenuItem("TechXR/Set-Up Cube VR Scene")]
        public static void SetUpCubeVRScene()
        {
            if (EditorUtility.DisplayDialog("Warning...!!", "TechXR-Default VR Scene will be loaded, it may remove the Existing TechXR-Environment, DeveloperCube and Cameras.", "Ok", "Close"))
            {
                // Reset Dropdown Selection
                controllerBodyIndex = 0;
                playerCharacterIndex = 0;
                environmentIndex = 0;

                // Add Tags & Layers
                AddLayersAndTags();

                // Delete all un-necessary things for AR camera configuration
                GameObject[] grounds = GameObject.FindGameObjectsWithTag("Env");
                foreach (GameObject ground in grounds)
                    DestroyImmediate(ground);

                GameObject[] players = GameObject.FindGameObjectsWithTag("XRPlayerController");
                foreach (GameObject player in players)
                    DestroyImmediate(player);

                // Add Items
                AddSenseCamera();
                AddDeveloperCube("Intractable");
                if (!GameObject.Find("MenuCanvas"))
                    AddCanvas("MenuCanvas");
                AddEnvironment();
                AddDirectionalLight();

                // Camera settings
                Camera.main.clearFlags = CameraClearFlags.Skybox;

                // Notify
                foreach (SceneView scene in SceneView.sceneViews)
                {
                    scene.ShowNotification(new GUIContent("Updated..!!"));
                }
            }
        }

        [MenuItem("TechXR/Set-Up Cube AR Scene")]
        public static void SetUpCubeARScene()
        {
            if (EditorUtility.DisplayDialog("Warning...!!", "TechXR-Default AR Scene will be loaded, it may remove the Existing TechXR-Environment, DeveloperCube and Cameras.", "Ok", "Close"))
            {
                // Reset Dropdown Selection
                controllerBodyIndex = 0;
                playerCharacterIndex = 0;
                environmentIndex = 0;


                // Add Items
                AddLayersAndTags();
                AddSenseCamera();
                AddDeveloperCube("InsideView");
                AddDirectionalLight();

                // Delete all un-necessary things for AR camera configuration
                GameObject[] grounds = GameObject.FindGameObjectsWithTag("Env");
                foreach (GameObject ground in grounds)
                    DestroyImmediate(ground);

                GameObject[] players = GameObject.FindGameObjectsWithTag("XRPlayerController");
                foreach (GameObject player in players)
                    DestroyImmediate(player);


                // Camera settings
                Camera.main.clearFlags = CameraClearFlags.SolidColor;

                // Notify
                foreach (SceneView scene in SceneView.sceneViews)
                {
                    scene.ShowNotification(new GUIContent("Updated..!!"));
                }
            }
        }
        /*
        [MenuItem("TechXR/Set-Up FPS Game")]
        public static void SetUpFPSScene()
        {
            if (EditorUtility.DisplayDialog("Warning...!!", "TechXR-Default FPS Scene will be loaded, it may remove active GameObjects from your scene. Make a new scene or Click on Set-Up", "Set-Up", "Close"))
            {
                // Remove all the Items from Scene
                GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (UnityEngine.Object go in allObjects)
                    DestroyImmediate(go);

                // Instantiate the FPS prefab
                UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/FPS/Prefabs/FPS.prefab", typeof(GameObject));
                var fps = Instantiate(prefab) as GameObject;
                fps.name = "FPS";

                // Set the Skybox Material
                Material m = AssetDatabase.LoadAssetAtPath("Assets/TechXR/FPS/Skybox/Material/FPS.mat", typeof(Material)) as Material;
                RenderSettings.skybox = m;

                // Notify
                foreach (SceneView scene in SceneView.sceneViews)
                {
                    scene.ShowNotification(new GUIContent("Updated..!!"));
                }
            }
        }
        */


        /*// Item ports the current scene to the new oculus scene if oculus sdk is imported, the function was made only for testing, need to repair a lot
        [MenuItem("TechXR/Port To Oculus")]
        static void PortToOculus()
        {
            // Duplicate Scene
            string[] path = EditorApplication.currentScene.Split(char.Parse("/"));
            path[path.Length - 1] = "Oculus_" + path[path.Length - 1];
            EditorApplication.SaveScene(string.Join("/", path), true);
            Debug.Log("Saved Scene");

            // Open Scene
            EditorApplication.OpenScene(string.Join("/", path));

            // Add scene to Build Setting
            var original = EditorBuildSettings.scenes;
            var newSettings = new EditorBuildSettingsScene[original.Length + 1];
            System.Array.Copy(original, newSettings, original.Length);
            var sceneToAdd = new EditorBuildSettingsScene(string.Join("/", path), true);
            newSettings[newSettings.Length - 1] = sceneToAdd;
            EditorBuildSettings.scenes = newSettings;

            // Instantiate the Oculus PlayerPrefab
            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/Oculus/VR/Prefabs/OVRPlayerController.prefab", typeof(GameObject));
            GameObject player = Instantiate(prefab) as GameObject;
            Vector3 pos = player.transform.position;
            pos.y = 1f;
            player.transform.position = pos;
            player.name = "OVRPlayerController";

            // Move the Container
            GameObject container = GameObject.Find("SenseContainer");
            container.transform.parent = GameObject.Find("RightHandAnchor").transform;
            container.transform.localPosition = Vector3.zero;

            // Add Controller Prefabs
            GameObject lha = GameObject.Find("LeftHandAnchor");
            GameObject rha = GameObject.Find("RightHandAnchor");

            UnityEngine.Object lc = AssetDatabase.LoadAssetAtPath("Assets/Oculus/VR/Meshes/OculusTouchForQuest2/OculusTouchForQuest2_Left.fbx", typeof(GameObject));
            GameObject left = Instantiate(lc) as GameObject;
            UnityEngine.Object rc = AssetDatabase.LoadAssetAtPath("Assets/Oculus/VR/Meshes/OculusTouchForQuest2/OculusTouchForQuest2_Right.fbx", typeof(GameObject));
            GameObject right = Instantiate(rc) as GameObject;

            left.transform.SetParent(lha.transform);
            right.transform.SetParent(rha.transform);

            Vector3 defaultpos = Vector3.zero;
            left.transform.localPosition = defaultpos;
            right.transform.localPosition = defaultpos;

            // Destroy
            DestroyImmediate(GameObject.FindGameObjectWithTag("XRPlayerController"));
        }
        */

        [MenuItem("TechXR/Configuration")]
        public static void TechXRConfiguration()
        {
            // Create the asset
            TechXRConfiguration techXRConfiguration = new TechXRConfiguration();
            AssetDatabase.CreateAsset(techXRConfiguration, "Assets/Resources/TechXRConfiguration.asset");

            // Print the path of the created asset
            string path = AssetDatabase.GetAssetPath(techXRConfiguration);
            Debug.Log(path);

            // Open the ConfigurationFile
            AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(path));
        }
        [MenuItem("TechXR/Configuration", true)]
        static bool ValidateTechXRConfiguration()
        {
            // Return false if no transform is selected.
            return false;
        }


        private void OnGUI()
        {
            if (!headerSectionTexture)
            {
                Close();
            }
            DrawLayouts();
            //Texture banner = (Texture)AssetDatabase.LoadAssetAtPath("Assets/TechXR/Logo/file.png", typeof(Texture));
            //GUILayout.Box(banner, skin.GetStyle("Logo"));
            //=============Developer Cube Tabs=================
            originalGUISkin = GUI.skin; // store original gui skin
            GUI.skin = skin; // use our custom skin

            // Create toolbar using custom tab style

            if (DeveloperSDKOptions[SelectedTabsOfDeveloperSDK].Contains("CONTROLLER"))
            {
                SelectedTabsOfDeveloperSDK = GUILayout.Toolbar(SelectedTabsOfDeveloperSDK, DeveloperSDKOptions, leftTabStyle);
            }
            else
            {
                SelectedTabsOfDeveloperSDK = GUILayout.Toolbar(SelectedTabsOfDeveloperSDK, DeveloperSDKOptions, rightTabStyle);
            }

            // Set Back original skin
            GUI.skin = originalGUISkin;
            //X============Developer Cube Tabs================X



            //=============AR and VR Tabs======================
            originalGUISkin = GUI.skin; // store original gui skin
            GUI.skin = skin; // use our custom skin

            // Create toolbar using custom tab style
            if (DeveloperSDKOptions[SelectedTabsOfDeveloperSDK].Contains("CONTROLLER"))
            {
                selectedTab = GUILayout.Toolbar(selectedTab, tabs, tabStyle3);
            }
            else
            {
                selectedTab = GUILayout.Toolbar(selectedTab, tabs, tabStyle2);
            }
            // Set Back original skin
            GUI.skin = originalGUISkin;
            //X============AR and VR Tabs=====================X


            // Set GUI Style
            guiStyle.fontSize = 15; // Change the font size
            guiStyle.richText = true; // Bold
            guiStyle.fontStyle = FontStyle.Bold;
            guiStyle.alignment = TextAnchor.MiddleCenter; // Align Text to the center

            // Space
            GUILayout.Space(8);

            // Start designing Window Vertically
            GUILayout.BeginVertical();

            // Add Scroller / Begin scroll view
            scrollerPosition = GUILayout.BeginScrollView(scrollerPosition, false, true);

            // Space
            GUILayout.Space(7);

            float originalValue = EditorGUIUtility.labelWidth;

            // Developer Cube
            if (DeveloperSDKOptions[SelectedTabsOfDeveloperSDK].Contains("DEVELOPER"))
            {
                // AR Tab
                if (tabs[selectedTab].Contains("AR"))
                {
                    // Space
                    GUILayout.Space(10);

                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("XR Camera", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    // Space
                    GUILayout.Space(10);

                    // Check for camera
                    Vuforia.VuforiaBehaviour techxrCamera = GameObject.FindObjectOfType<Vuforia.VuforiaBehaviour>();

                    // Camera
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("XRCamera", skin.GetStyle("SubHeading"));
                    GUILayout.Space(10);
                    EditorGUI.BeginDisabledGroup(techxrCamera);
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        AddSenseCamera();
                        
                        //
                        AddDirectionalLight();
                        this.ShowNotification(new GUIContent("Updated..!!"));
                    }
                    EditorGUI.EndDisabledGroup();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // HelpBox
                    if (techxrCamera)
                    {
                        // Horizontal Line Separation
                        GUILayout.Space(10);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(100);
                        GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                        GUILayout.Space(100);
                        GUILayout.EndHorizontal();
                        GUILayout.Space(10);


                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUILayout.Space(40);
                        EditorGUILayout.HelpBox("To switch to the Setreo-mode (Split Screen-mode), Install XR Plugin Management under the Player Settings and select Mock HMD Loader checkbox.", MessageType.Info);
                        GUILayout.Space(40);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }



                    // Check for controller
                    //SenseController senseController = GameObject.FindObjectOfType<SenseController>();
                    // Check for DeveloperCube
                    GameObject developerCube = GameObject.FindGameObjectWithTag("TechXRDeveloperCube");

                    // HelpBox
                    /*if (senseController || developerCube)
                    {
                        // Horizontal Line Separation
                        GUILayout.Space(10);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(100);
                        GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                        GUILayout.Space(100);
                        GUILayout.EndHorizontal();
                        GUILayout.Space(10);

                        // Help Box
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(50);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("Either Controller or DeveloperCube can be added at a time", MessageType.Info);
                        GUILayout.FlexibleSpace();
                        GUILayout.Space(50);
                        GUILayout.EndHorizontal();
                    }*/

                    // Horizontal Line Separation
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(100);
                    GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                    GUILayout.Space(100);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);

                    // Space
                    GUILayout.Space(10);


                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("XR Developer Cube", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    // Space
                    GUILayout.Space(20);


                    // XR Developer Cube
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Cube Type :", skin.GetStyle("SubHeading"));
                    GUILayout.Space(10);

                    // Start a code block to check for GUI changes
                    EditorGUI.BeginChangeCheck();

                    // Dropdown
                    arControllerIndex = EditorGUILayout.Popup("", arControllerIndex, arControllerOptions, GUILayout.Width(100));

                    // End the code block and update the label if a change occurred
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (developerCube)
                        {
                            // turn off inside/outside view
                            foreach (string s in arControllerOptions)
                            {
                                Transform go = developerCube.transform.Find(s);
                                if (go) go.gameObject.SetActive(false);
                            }

                            // if intractable cube is selected add laser pointer and input module
                            if (arControllerOptions[arControllerIndex].Contains("Intractable"))
                            {
                                // add intractable sphere
                                UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/Intractable Cube/Intractable.prefab", typeof(GameObject));
                                var sphere = Instantiate(prefab) as GameObject;
                                sphere.transform.SetParent(developerCube.transform);
                                sphere.transform.localPosition = Vector3.zero;

                                // add laserpointer if not present
                                LaserPointer laserPointer = developerCube.GetComponent<LaserPointer>();
                                if (!laserPointer)
                                    laserPointer = developerCube.AddComponent<LaserPointer>();

                                // Set Color
                                laserPointer.Color = Color.white;

                                // add laserpointerinputmodule in eventsystem, if not prenent add eventsystem
                                GameObject eventSystem = GameObject.FindGameObjectWithTag("SenseEventSystem");
                                if (eventSystem != null)
                                {
                                    // add laserpointerinputmodule
                                    LaserPointerInputModule lpim = GameObject.FindObjectOfType<LaserPointerInputModule>();
                                    if (lpim) lpim.enabled = true;
                                    else eventSystem.AddComponent<LaserPointerInputModule>();

                                    // turn off StandaloneInputModule
                                    StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
                                    if (standaloneInputModule) standaloneInputModule.enabled = false;
                                }
                                else
                                {
                                    UnityEngine.Object es_prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/SenseEventSystem.prefab", typeof(GameObject));
                                    var es = Instantiate(es_prefab) as GameObject;
                                    es.name = "SenseEventSystem";

                                    GameObject te = GetTechXREssentialGO();
                                    es.transform.SetParent(te.transform);
                                }
                            }
                            else
                            {
                                // remove the sphere
                                GameObject i_Sphere = GameObject.FindWithTag("ISphere");
                                if (i_Sphere) DestroyImmediate(i_Sphere);

                                // turn on inside/outside view
                                developerCube.transform.Find(arControllerOptions[arControllerIndex]).gameObject.SetActive(true);

                                // remove the laserpointer
                                LaserPointer laserPointer = developerCube.GetComponent<LaserPointer>();
                                if (laserPointer) DestroyImmediate(laserPointer);

                                // remove the LaserPointerInputModule and enable StandaloneInputModule
                                LaserPointerInputModule lpim = GameObject.FindObjectOfType<LaserPointerInputModule>();
                                if (lpim)
                                {
                                    GameObject eventSystem = lpim.gameObject;

                                    DestroyImmediate(eventSystem.GetComponent<LaserPointerInputModule>());

                                    StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
                                    if (standaloneInputModule) standaloneInputModule.enabled = true;
                                }
                            }
                            this.ShowNotification(new GUIContent("Developer Cube Updated to " + arControllerOptions[arControllerIndex] + "..!!"));
                        }
                    }

                    GUILayout.Space(15);
                    if (arControllerIndex >= 0)
                    {
                        string icon = arControllerOptions[arControllerIndex];

                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Sprites/Icon/" + icon + ".png", typeof(Texture2D));
                        controllerBody = obj as Texture2D;
                        GUILayout.Label(controllerBody, GUILayout.Width(80), GUILayout.Height(80));
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    //GUILayout.Space(15);


                    // Button
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginDisabledGroup(developerCube);
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        // Instantiate the prefab
                        UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/TechXR_DeveloperCube/Prefabs/DeveloperCube" + ".prefab", typeof(GameObject));
                        var devCube = Instantiate(prefab) as GameObject;


                        // turn off inside/outside view
                        foreach (string s in arControllerOptions)
                        {
                            Transform go = devCube.transform.Find(s);
                            if (go) go.gameObject.SetActive(false);
                        }

                        // if intractable cube is selected add laser pointer and input module
                        if (arControllerOptions[arControllerIndex].Contains("Intractable"))
                        {
                            // add laserpointer
                            LaserPointer laserPointer = devCube.AddComponent<LaserPointer>();

                            if (!laserPointer)
                                laserPointer = devCube.AddComponent<LaserPointer>();

                            // Set Color
                            laserPointer.Color = Color.white;

                            // laserpointerinputmodule
                            AddEventSystem();

                            // add intractable sphere
                            UnityEngine.Object i_sphere = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/Intractable Cube/Intractable.prefab", typeof(GameObject));
                            var sphere = Instantiate(i_sphere) as GameObject;
                            sphere.transform.SetParent(devCube.transform);
                            sphere.transform.localPosition = Vector3.zero;
                        }
                        else
                        {
                            // turn on inside/outside view
                            devCube.transform.Find(arControllerOptions[arControllerIndex]).gameObject.SetActive(true);

                            // remove the sphere
                            GameObject i_Sphere = GameObject.FindWithTag("ISphere");
                            if (i_Sphere) DestroyImmediate(i_Sphere);
                        }

                        // Focus
                        Selection.activeGameObject = devCube;
                        SceneView.lastActiveSceneView.FrameSelected();

                        AddDirectionalLight();

                        this.ShowNotification(new GUIContent("Updated..!!"));
                    }
                    EditorGUI.EndDisabledGroup();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);

                    //=====================Assets==============================

                    // Horizontal Line Separation
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(100);
                    GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(2) });
                    GUILayout.Space(100);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);

                    // Space
                    GUILayout.Space(10);

                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Assets", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(15);


                    // UI Elements Options
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Select UI Element :", skin.GetStyle("SubHeading"));
                    GUILayout.Space(20);
                    if (uiElementOptions[0] == null) FillUIElementOptions();
                    uiElementIndex = EditorGUILayout.Popup("", uiElementIndex, uiElementOptions, GUILayout.Width(100)); // Dropdown
                    GUILayout.Space(80);
                    if (uiElementIndex >= 0)
                    {
                        GUILayout.BeginVertical();
                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Sprites/Icon/" + uiElementOptions[uiElementIndex] + ".png", typeof(Texture2D));
                        uiElement = obj as Texture2D;

                        //uiElement = Resources.Load("Icon/" + uiElementOptions[uiElementIndex], typeof(Texture2D)) as Texture2D;

                        //GUILayout.Label(uiElement);

                        // Button
                        if (GUILayout.Button("Add", GUILayout.Width(75)))
                        {
                            AddCanvas(uiElementOptions[uiElementIndex]);
                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.Space(20);
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(15);


                    // 3D Models Options
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Select 3D Model  :", skin.GetStyle("SubHeading"));
                    GUILayout.Space(20);
                    if (modelOptions[0] == null) FillModelOptions();
                    modelIndex = EditorGUILayout.Popup("", modelIndex, modelOptions, GUILayout.Width(100)); // Dropdown
                    GUILayout.Space(80);
                    if (modelIndex >= 0)
                    {
                        GUILayout.BeginVertical();
                        if (GUILayout.Button("Add", GUILayout.Width(75))) // Button
                        {
                            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/3DModels/" + modelOptions[modelIndex] + "/" + "model.obj", typeof(GameObject));

                            if (!prefab)
                            {
                                prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/3DModels/" + modelOptions[modelIndex] + "/" + modelOptions[modelIndex] + ".obj", typeof(GameObject));
                            }

                            GameObject obj = Instantiate(prefab) as GameObject;
                            if (developerCube)
                            {
                                obj.transform.SetParent(developerCube.transform);
                                obj.transform.localPosition = Vector3.zero;
                            }

                            // remove the sphere
                            GameObject i_Sphere = GameObject.FindWithTag("ISphere");
                            if (i_Sphere) i_Sphere.transform.Find("Sphere").gameObject.SetActive(false);

                            // Focus
                            Selection.activeGameObject = obj;
                            SceneView.lastActiveSceneView.FrameSelected();

                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.Space(20);
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);

                    //=====================Assets==============================
                }

                // VR Tab
                if (tabs[selectedTab].Contains("VR"))
                {
                    // Space
                    GUILayout.Space(10);

                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("XR Camera", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    // Space
                    GUILayout.Space(10);

                    // Check for camera
                    Vuforia.VuforiaBehaviour techxrCamera = GameObject.FindObjectOfType<Vuforia.VuforiaBehaviour>();

                    // Camera
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("XRCamera", skin.GetStyle("SubHeading"));
                    GUILayout.Space(10);
                    EditorGUI.BeginDisabledGroup(techxrCamera);
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        AddSenseCamera();

                        // Camera settings
                        Camera.main.clearFlags = CameraClearFlags.Skybox;

                        AddDirectionalLight();
                        this.ShowNotification(new GUIContent("Updated..!!"));
                    }
                    EditorGUI.EndDisabledGroup();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // HelpBox
                    if (techxrCamera)
                    {
                        // Horizontal Line Separation
                        GUILayout.Space(10);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(100);
                        GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                        GUILayout.Space(100);
                        GUILayout.EndHorizontal();
                        GUILayout.Space(10);


                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUILayout.Space(40);
                        EditorGUILayout.HelpBox("To switch to the Setreo-mode (Split Screen-mode), Install XR Plugin Management under the Player Settings and select Mock HMD Loader checkbox.", MessageType.Info);
                        GUILayout.Space(40);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }


                    // Check for controller
                    //SenseController senseController = GameObject.FindObjectOfType<SenseController>();
                    // Check for DeveloperCube
                    GameObject developerCube = GameObject.FindGameObjectWithTag("TechXRDeveloperCube");

                    // HelpBox
                    //if (senseController || developerCube)
                    /*{
                        // Horizontal Line Separation
                        GUILayout.Space(10);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(100);
                        GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                        GUILayout.Space(100);
                        GUILayout.EndHorizontal();
                        GUILayout.Space(10);

                        // Help Box
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(50);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("Either Controller or DeveloperCube can be added at a time", MessageType.Info);
                        GUILayout.FlexibleSpace();
                        GUILayout.Space(50);
                        GUILayout.EndHorizontal();
                    }*/

                    // Horizontal Line Separation
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(100);
                    GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                    GUILayout.Space(100);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);

                    // Space
                    GUILayout.Space(10);


                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("XR Developer Cube", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    // Space
                    GUILayout.Space(20);


                    // XR Developer Cube
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Cube Type :", skin.GetStyle("SubHeading"));
                    GUILayout.Space(10);

                    // Start a code block to check for GUI changes
                    EditorGUI.BeginChangeCheck();

                    // Dropdown
                    arControllerIndex = EditorGUILayout.Popup("", arControllerIndex, arControllerOptions, GUILayout.Width(100));

                    // End the code block and update the label if a change occurred
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (developerCube)
                        {
                            // turn off inside/outside view
                            foreach (string s in arControllerOptions)
                            {
                                Transform go = developerCube.transform.Find(s);
                                if (go) go.gameObject.SetActive(false);
                            }

                            // if intractable cube is selected add laser pointer and input module
                            if (arControllerOptions[arControllerIndex].Contains("Intractable"))
                            {
                                // add intractable sphere
                                UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/Intractable Cube/Intractable.prefab", typeof(GameObject));
                                var sphere = Instantiate(prefab) as GameObject;
                                sphere.transform.SetParent(developerCube.transform);
                                sphere.transform.localPosition = Vector3.zero;

                                // add laserpointer if not present
                                LaserPointer laserPointer = developerCube.GetComponent<LaserPointer>();
                                if (!laserPointer)
                                    laserPointer = developerCube.AddComponent<LaserPointer>();

                                // Set Color
                                laserPointer.Color = Color.white;

                                // add laserpointerinputmodule in eventsystem, if not prenent add eventsystem
                                GameObject eventSystem = GameObject.FindGameObjectWithTag("SenseEventSystem");
                                if (eventSystem != null)
                                {
                                    // add laserpointerinputmodule
                                    LaserPointerInputModule lpim = GameObject.FindObjectOfType<LaserPointerInputModule>();
                                    if (lpim) lpim.enabled = true;
                                    else eventSystem.AddComponent<LaserPointerInputModule>();

                                    // turn off StandaloneInputModule
                                    StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
                                    if (standaloneInputModule) standaloneInputModule.enabled = false;
                                }
                                else
                                {
                                    UnityEngine.Object es_prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/SenseEventSystem.prefab", typeof(GameObject));
                                    var es = Instantiate(es_prefab) as GameObject;
                                    es.name = "SenseEventSystem";

                                    GameObject te = GetTechXREssentialGO();
                                    es.transform.SetParent(te.transform);
                                }
                            }
                            else
                            {
                                // remove the sphere
                                GameObject i_Sphere = GameObject.FindWithTag("ISphere");
                                if (i_Sphere) DestroyImmediate(i_Sphere);

                                // turn on inside/outside view
                                developerCube.transform.Find(arControllerOptions[arControllerIndex]).gameObject.SetActive(true);

                                // remove the laserpointer
                                LaserPointer laserPointer = developerCube.GetComponent<LaserPointer>();
                                if (laserPointer) DestroyImmediate(laserPointer);

                                // remove the LaserPointerInputModule and enable StandaloneInputModule
                                LaserPointerInputModule lpim = GameObject.FindObjectOfType<LaserPointerInputModule>();
                                if (lpim)
                                {
                                    GameObject eventSystem = lpim.gameObject;

                                    DestroyImmediate(eventSystem.GetComponent<LaserPointerInputModule>());

                                    StandaloneInputModule standaloneInputModule = eventSystem.GetComponent<StandaloneInputModule>();
                                    if (standaloneInputModule) standaloneInputModule.enabled = true;
                                }
                            }
                            this.ShowNotification(new GUIContent("Developer Cube Updated to " + arControllerOptions[arControllerIndex] + "..!!"));
                        }
                    }

                    GUILayout.Space(15);
                    if (arControllerIndex >= 0)
                    {
                        string icon = arControllerOptions[arControllerIndex];

                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Sprites/Icon/" + icon + ".png", typeof(Texture2D));
                        controllerBody = obj as Texture2D;
                        GUILayout.Label(controllerBody, GUILayout.Width(80), GUILayout.Height(80));
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    //GUILayout.Space(15);


                    // Button
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginDisabledGroup(developerCube);
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        // Instantiate the prefab
                        UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/TechXR_DeveloperCube/Prefabs/DeveloperCube" + ".prefab", typeof(GameObject));
                        var devCube = Instantiate(prefab) as GameObject;


                        // turn off inside/outside view
                        foreach (string s in arControllerOptions)
                        {
                            Transform go = devCube.transform.Find(s);
                            if (go) go.gameObject.SetActive(false);
                        }

                        // if intractable cube is selected add laser pointer and input module
                        if (arControllerOptions[arControllerIndex].Contains("Intractable"))
                        {
                            // add laserpointer
                            LaserPointer laserPointer = devCube.AddComponent<LaserPointer>();

                            if (!laserPointer)
                                laserPointer = devCube.AddComponent<LaserPointer>();

                            // Set Color
                            laserPointer.Color = Color.white;

                            // laserpointerinputmodule
                            AddEventSystem();

                            // add intractable sphere
                            UnityEngine.Object i_sphere = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/Intractable Cube/Intractable.prefab", typeof(GameObject));
                            var sphere = Instantiate(i_sphere) as GameObject;
                            sphere.transform.SetParent(devCube.transform);
                            sphere.transform.localPosition = Vector3.zero;
                        }
                        else
                        {
                            // turn on inside/outside view
                            devCube.transform.Find(arControllerOptions[arControllerIndex]).gameObject.SetActive(true);

                            // remove the sphere
                            GameObject i_Sphere = GameObject.FindWithTag("ISphere");
                            if (i_Sphere) DestroyImmediate(i_Sphere);
                        }

                        // Focus
                        Selection.activeGameObject = devCube;
                        SceneView.lastActiveSceneView.FrameSelected();

                        AddDirectionalLight();

                        this.ShowNotification(new GUIContent("Updated..!!"));
                    }
                    EditorGUI.EndDisabledGroup();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);

                    // Horizontal Line Separation
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(100);
                    GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                    GUILayout.Space(100);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);


                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Configure VR Scene", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(15);


                    // Add Environment
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (environmentOptions[0] == null) FillEnvironmentOptions();
                    EditorGUIUtility.labelWidth = 100;
                    GUILayout.Label("Environment :", skin.GetStyle("SubHeading"));
                    EditorGUIUtility.labelWidth = originalValue;
                    environmentIndex = EditorGUILayout.Popup("", environmentIndex, environmentOptions);
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        GameObject[] envs = GameObject.FindGameObjectsWithTag("Env");

                        if (envs.Length > 0)
                        {
                            if (EditorUtility.DisplayDialog("Warning...!!", "TechXR-Environment already present", "Delete Existing and Add new one", "Close"))
                            {
                                foreach (GameObject e in envs)
                                    DestroyImmediate(e);
                                AddEnvironment();
                                this.ShowNotification(new GUIContent("Updated..!!"));
                            }
                        }
                        else
                        {
                            AddEnvironment();
                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                        AddDirectionalLight();
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);


                    // Add Skybox
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (environmentOptions[0] == null) FillEnvironmentOptions();
                    EditorGUIUtility.labelWidth = 100;
                    GUILayout.Label("Skybox         :", skin.GetStyle("SubHeading"));
                    EditorGUIUtility.labelWidth = originalValue;
                    skyboxIndex = EditorGUILayout.Popup("", skyboxIndex, skyboxOptions);
                    if (GUILayout.Button("Set", GUILayout.Width(60)))
                    {
                        SetSkybox();
                        this.ShowNotification(new GUIContent("Updated..!!"));
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);

                    //=====================Assets==============================

                    // Horizontal Line Separation
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(100);
                    GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(2) });
                    GUILayout.Space(100);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);

                    // Space
                    GUILayout.Space(10);

                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Assets", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(15);


                    // UI Elements Options
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Select UI Element :", skin.GetStyle("SubHeading"));
                    GUILayout.Space(20);
                    if (uiElementOptions[0] == null) FillUIElementOptions();
                    uiElementIndex = EditorGUILayout.Popup("", uiElementIndex, uiElementOptions, GUILayout.Width(100)); // Dropdown
                    GUILayout.Space(80);
                    if (uiElementIndex >= 0)
                    {
                        GUILayout.BeginVertical();
                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Sprites/Icon/" + uiElementOptions[uiElementIndex] + ".png", typeof(Texture2D));
                        uiElement = obj as Texture2D;

                        //uiElement = Resources.Load("Icon/" + uiElementOptions[uiElementIndex], typeof(Texture2D)) as Texture2D;

                        //GUILayout.Label(uiElement);

                        // Button
                        if (GUILayout.Button("Add", GUILayout.Width(75)))
                        {
                            AddCanvas(uiElementOptions[uiElementIndex]);
                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.Space(20);
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(15);


                    // 3D Models Options
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Select 3D Model  :", skin.GetStyle("SubHeading"));
                    GUILayout.Space(20);
                    if (modelOptions[0] == null) FillModelOptions();
                    modelIndex = EditorGUILayout.Popup("", modelIndex, modelOptions, GUILayout.Width(100)); // Dropdown
                    GUILayout.Space(80);
                    if (modelIndex >= 0)
                    {
                        GUILayout.BeginVertical();
                        if (GUILayout.Button("Add", GUILayout.Width(75))) // Button
                        {
                            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/3DModels/" + modelOptions[modelIndex] + "/" + "model.obj", typeof(GameObject));

                            if (!prefab)
                            {
                                prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/3DModels/" + modelOptions[modelIndex] + "/" + modelOptions[modelIndex] + ".obj", typeof(GameObject));
                            }

                            GameObject obj = Instantiate(prefab) as GameObject;
                            if (developerCube)
                            {
                                obj.transform.SetParent(developerCube.transform);
                                obj.transform.localPosition = Vector3.zero;
                            }

                            // remove the sphere
                            GameObject i_Sphere = GameObject.FindWithTag("ISphere");
                            if (i_Sphere)
                            {
                                i_Sphere.transform.Find("Sphere").gameObject.SetActive(false);
                            }

                            // Focus
                            Selection.activeGameObject = obj;
                            SceneView.lastActiveSceneView.FrameSelected();

                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.Space(20);
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);

                    //=====================Assets==============================
                }
            }

            // 6DoF Controller
            if (DeveloperSDKOptions[SelectedTabsOfDeveloperSDK].Contains("CONTROLLER"))
            {

                // AR Tab
                if (tabs[selectedTab].Contains("AR"))
                {
                    EditorGUI.BeginDisabledGroup(true);

                    // Space
                    GUILayout.Space(10);

                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("XR Camera", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    // Space
                    GUILayout.Space(10);

                    // Check for camera
                    Vuforia.VuforiaBehaviour techxrCamera = GameObject.FindObjectOfType<Vuforia.VuforiaBehaviour>();

                    // Camera
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("XRCamera", skin.GetStyle("SubHeading"));
                    GUILayout.Space(10);
                    EditorGUI.BeginDisabledGroup(techxrCamera);
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        AddSenseCamera();
                        AddDirectionalLight();
                        this.ShowNotification(new GUIContent("Updated..!!"));
                    }
                    EditorGUI.EndDisabledGroup();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);


                    // HelpBox
                    /*
                    if (techxrCamera)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(50);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("To switch to the Stereo-mode (Split Screen-mode), Install XR Plugin Management under the Player Settings and select Mock HMD Loader checkbox.", MessageType.Info);
                        GUILayout.FlexibleSpace();
                        GUILayout.Space(50);
                        GUILayout.EndHorizontal();
                    }
                    */

                    // Space
                    //GUILayout.Space(10);


                    // Horizontal Line Separation
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(100);
                    GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                    GUILayout.Space(100);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);


                    // Space
                    //GUILayout.Space(10);

                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("XR Controller", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(20);


                    // Check for controller
                    //SenseController senseController = GameObject.FindObjectOfType<SenseController>();
                    // Check for DeveloperCube
                    GameObject developerCube = GameObject.FindGameObjectWithTag("TechXRDeveloperCube");


                    // XRController
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Controller Type :", skin.GetStyle("SubHeading"));
                    GUILayout.Space(10);

                    // Start a code block to check for GUI changes
                    EditorGUI.BeginChangeCheck();

                    // Dropdown
                    controllerBodyIndex = EditorGUILayout.Popup("", controllerBodyIndex, controllerBodyOptions, GUILayout.Width(100));

                    // End the code block and update the label if a change occurred
                    if (EditorGUI.EndChangeCheck())
                    {
                        //if (senseController) AddController();
                    }

                    GUILayout.Space(15);
                    if (controllerBodyIndex >= 0)
                    {
                        string icon = "";

                        if (controllerBodyIndex == 0) icon = "Default";
                        if (controllerBodyIndex == 1) icon = "Hand";

                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Sprites/Icon/" + icon + ".png", typeof(Texture2D));
                        controllerBody = obj as Texture2D;
                        GUILayout.Label(controllerBody, GUILayout.Width(80), GUILayout.Height(80));
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    //GUILayout.Space(15);


                    // Add Button
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    //EditorGUI.BeginDisabledGroup(senseController || developerCube);
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        AddController();
                        AddSenseManager();
                        AddDirectionalLight();
                        this.ShowNotification(new GUIContent("Updated..!!"));
                    }
                    //EditorGUI.EndDisabledGroup();
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    //GUILayout.Space(10);

                    // HelpBox
                    //if (senseController || developerCube)
                    /*{
                        // Horizontal Line Separation
                        GUILayout.Space(10);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(100);
                        GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                        GUILayout.Space(100);
                        GUILayout.EndHorizontal();
                        GUILayout.Space(10);

                        // Help Box
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(50);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("Either Controller or DeveloperCube can be added at a time", MessageType.Info);
                        GUILayout.FlexibleSpace();
                        GUILayout.Space(50);
                        GUILayout.EndHorizontal();
                    }*/


                    //// Horizontal Line Separation
                    //GUILayout.Space(10);
                    //GUILayout.BeginHorizontal();
                    //GUILayout.Space(100);
                    //GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                    //GUILayout.Space(100);
                    //GUILayout.EndHorizontal();
                    //GUILayout.Space(10);


                    //// Space
                    //GUILayout.Space(10);


                    //// Heading
                    //GUILayout.BeginHorizontal();
                    //GUILayout.FlexibleSpace();
                    //GUILayout.Label("XR Developer Cube", skin.GetStyle("Heading"));
                    //GUILayout.FlexibleSpace();
                    //GUILayout.EndHorizontal();

                    //// Space
                    //GUILayout.Space(20);


                    //// XR Developer Cube
                    //GUILayout.BeginHorizontal();
                    //GUILayout.FlexibleSpace();
                    //GUILayout.Label("Cube Type :", skin.GetStyle("SubHeading"));
                    //GUILayout.Space(10);

                    //// Start a code block to check for GUI changes
                    //EditorGUI.BeginChangeCheck();

                    //// Dropdown
                    //arControllerIndex = EditorGUILayout.Popup("", arControllerIndex, arControllerOptions, GUILayout.Width(100));

                    //// End the code block and update the label if a change occurred
                    //if (EditorGUI.EndChangeCheck())
                    //{
                    //    if (developerCube)
                    //    {
                    //        foreach (string s in arControllerOptions)
                    //            developerCube.transform.Find(s).gameObject.SetActive(false);

                    //        developerCube.transform.Find(arControllerOptions[arControllerIndex]).gameObject.SetActive(true);
                    //    }
                    //}

                    //GUILayout.Space(15);
                    //if (arControllerIndex >= 0)
                    //{
                    //    string icon = arControllerOptions[arControllerIndex];

                    //    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Sprites/Icon/" + icon + ".png", typeof(Texture2D));
                    //    controllerBody = obj as Texture2D;
                    //    GUILayout.Label(controllerBody, GUILayout.Width(80), GUILayout.Height(80));
                    //}
                    //GUILayout.FlexibleSpace();
                    //GUILayout.EndHorizontal();


                    // Space
                    //GUILayout.Space(15);


                    // Button
                    //GUILayout.BeginHorizontal();
                    //GUILayout.FlexibleSpace();
                    ////EditorGUI.BeginDisabledGroup(senseController || developerCube);
                    //if (GUILayout.Button("Add", GUILayout.Width(60)))
                    //{
                    //    // Instantiate the prefab
                    //    UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/TechXR_DeveloperCube/Prefabs/DeveloperCube" + ".prefab", typeof(GameObject));
                    //    var devCube = Instantiate(prefab) as GameObject;

                    //    foreach (string s in arControllerOptions)
                    //        devCube.transform.Find(s).gameObject.SetActive(false);

                    //    devCube.transform.Find(arControllerOptions[arControllerIndex]).gameObject.SetActive(true);

                    //    // Focus
                    //    Selection.activeGameObject = devCube;
                    //    SceneView.lastActiveSceneView.FrameSelected();

                    //    AddDirectionalLight();

                    //    this.ShowNotification(new GUIContent("Updated..!!"));
                    //}
                    ////EditorGUI.EndDisabledGroup();
                    //GUILayout.FlexibleSpace();
                    //GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);

                    //=====================Assets==============================

                    // Horizontal Line Separation
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(100);
                    GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(2) });
                    GUILayout.Space(100);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);

                    // Space
                    GUILayout.Space(10);

                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Assets", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(15);


                    // UI Elements Options
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Select UI Element :", skin.GetStyle("SubHeading"));
                    GUILayout.Space(20);
                    if (uiElementOptions[0] == null) FillUIElementOptions();
                    uiElementIndex = EditorGUILayout.Popup("", uiElementIndex, uiElementOptions, GUILayout.Width(100)); // Dropdown
                    GUILayout.Space(80);
                    if (uiElementIndex >= 0)
                    {
                        GUILayout.BeginVertical();
                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Sprites/Icon/" + uiElementOptions[uiElementIndex] + ".png", typeof(Texture2D));
                        uiElement = obj as Texture2D;

                        //uiElement = Resources.Load("Icon/" + uiElementOptions[uiElementIndex], typeof(Texture2D)) as Texture2D;

                        //GUILayout.Label(uiElement);

                        // Button
                        if (GUILayout.Button("Add", GUILayout.Width(75)))
                        {
                            AddCanvas(uiElementOptions[uiElementIndex]);
                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.Space(20);
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(15);


                    // 3D Models Options
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Select 3D Model  :", skin.GetStyle("SubHeading"));
                    GUILayout.Space(20);
                    if (modelOptions[0] == null) FillModelOptions();
                    modelIndex = EditorGUILayout.Popup("", modelIndex, modelOptions, GUILayout.Width(100)); // Dropdown
                    GUILayout.Space(80);
                    if (modelIndex >= 0)
                    {
                        GUILayout.BeginVertical();
                        if (GUILayout.Button("Add", GUILayout.Width(75))) // Button
                        {
                            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/3DModels/" + modelOptions[modelIndex] + "/" + "model.obj", typeof(GameObject));

                            if (!prefab)
                            {
                                prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/3DModels/" + modelOptions[modelIndex] + "/" + modelOptions[modelIndex] + ".obj", typeof(GameObject));
                            }

                            GameObject obj = Instantiate(prefab) as GameObject;

                            // Focus
                            Selection.activeGameObject = obj;
                            SceneView.lastActiveSceneView.FrameSelected();

                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.Space(20);
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);

                    //=====================Assets==============================

                    EditorGUI.EndDisabledGroup();

                }

                // VR Tab
                if (tabs[selectedTab].Contains("VR"))
                {
                    EditorGUI.BeginDisabledGroup(true);

                    // Space
                    GUILayout.Space(10);

                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("XR Player", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    // Space
                    GUILayout.Space(20);


                    // Check for the player controller
                    GameObject playerController = GameObject.FindWithTag("XRPlayerController");


                    // Player Body Options
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(80);
                    GUILayout.Label("Character Type :", skin.GetStyle("SubHeading"));
                    // Start a code block to check for GUI changes
                    EditorGUI.BeginChangeCheck();


                    playerCharacterIndex = EditorGUILayout.Popup("", playerCharacterIndex, playerCharacterOptions);

                    // End the code block and update the label if a change occurred
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (playerController)
                        {
                            Transform characterBodyContainer = playerController.transform.Find("CharacterBodyContainer");

                            foreach (Transform child in characterBodyContainer)
                            {
                                child.gameObject.SetActive(false);
                            }

                            characterBodyContainer.Find(playerCharacterOptions[playerCharacterIndex]).gameObject.SetActive(true);
                        }


                    }

                    GUILayout.Space(40);
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);


                    // Controller Body Options
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(80);
                    GUILayout.Label("Controller Type :", skin.GetStyle("SubHeading"));
                    // Check for controller
                    //SenseController senseController = GameObject.FindObjectOfType<SenseController>();


                    // Start a code block to check for GUI changes
                    EditorGUI.BeginChangeCheck();

                    // Dropdown
                    controllerBodyIndex = EditorGUILayout.Popup("", controllerBodyIndex, controllerBodyOptions);

                    // End the code block and update the label if a change occurred
                    if (EditorGUI.EndChangeCheck())
                    {
                        //if (senseController) AddController();
                    }


                    if (controllerBodyIndex >= 0)
                    {
                        string icon = "";

                        if (controllerBodyIndex == 0) icon = "Default";
                        if (controllerBodyIndex == 1) icon = "Hand";

                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Sprites/Icon/" + icon + ".png", typeof(Texture2D));
                        controllerBody = obj as Texture2D;
                        GUILayout.Label(controllerBody, GUILayout.Width(80), GUILayout.Height(80));
                    }
                    GUILayout.Space(40);
                    GUILayout.EndHorizontal();


                    // Space
                    //GUILayout.Space(10);


                    // Space
                    //GUILayout.Space(10);


                    // Search for player
                    //GameObject playerController = GameObject.FindWithTag("XRPlayerController");


                    // Add Player
                    EditorGUI.BeginDisabledGroup(playerController);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Add", GUILayout.Width(80), GUILayout.Height(30)))
                    {
                        if (EditorUtility.DisplayDialog("Warning...!!", "Existing Camera and Controller/DeveloperCube will be removed and a Player will be added to the scene.", "Ok", "Close"))
                        {
                            AddXRPlayerController();
                            AddDirectionalLight();
                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    EditorGUI.EndDisabledGroup();


                    // HelpBox
                    if (playerController)
                    {
                        // Space
                        GUILayout.Space(10);

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(50);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.HelpBox("XRCamera and the XRController are inside XRPlayerController", MessageType.Info);
                        GUILayout.FlexibleSpace();
                        GUILayout.Space(50);
                        GUILayout.EndHorizontal();
                    }


                    // Space
                    //GUILayout.Space(10);


                    // Horizontal Line Separation
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(100);
                    GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                    GUILayout.Space(100);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);


                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Configure VR Scene", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(15);


                    // Add Environment
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (environmentOptions[0] == null) FillEnvironmentOptions();
                    EditorGUIUtility.labelWidth = 100;
                    GUILayout.Label("Environment :", skin.GetStyle("SubHeading"));
                    EditorGUIUtility.labelWidth = originalValue;
                    environmentIndex = EditorGUILayout.Popup("", environmentIndex, environmentOptions);
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        GameObject[] envs = GameObject.FindGameObjectsWithTag("Env");

                        if (envs.Length > 0)
                        {
                            if (EditorUtility.DisplayDialog("Warning...!!", "TechXR-Environment already present", "Delete Existing and Add new one", "Close"))
                            {
                                foreach (GameObject e in envs)
                                    DestroyImmediate(e);
                                AddEnvironment();
                                this.ShowNotification(new GUIContent("Updated..!!"));
                            }
                        }
                        else
                        {
                            AddEnvironment();
                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                        AddDirectionalLight();
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);


                    // Add Skybox
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (environmentOptions[0] == null) FillEnvironmentOptions();
                    EditorGUIUtility.labelWidth = 100;
                    GUILayout.Label("Skybox         :", skin.GetStyle("SubHeading"));
                    EditorGUIUtility.labelWidth = originalValue;
                    skyboxIndex = EditorGUILayout.Popup("", skyboxIndex, skyboxOptions);
                    if (GUILayout.Button("Set", GUILayout.Width(60)))
                    {
                        SetSkybox();
                        this.ShowNotification(new GUIContent("Updated..!!"));
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);


                    // Add Trackable Warning System
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUIUtility.labelWidth = 100;
                    GUILayout.Label("Controller Trackable Red Alert Warning System :", skin.GetStyle("SubHeading"));
                    EditorGUIUtility.labelWidth = originalValue;
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        GameObject pc = GameObject.FindWithTag("XRPlayerController");
                        if (!pc)
                        {
                            if (EditorUtility.DisplayDialog("Warning...!!", "TechXR :: No XRPlayer in the Scene , First add the XRPlayer", "Ok", "Close")) { }
                            Debug.Log("TechXR :: No XRPlayer in the Scene , First add the XRPlayer");
                        }
                        else
                        {
                            AddTrackableWarningSystem();
                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }

                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);


                    // Add Bluetooth Warning System
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    EditorGUIUtility.labelWidth = 100;
                    GUILayout.Label("Controller Bluetooth Warning System :", skin.GetStyle("SubHeading"));
                    EditorGUIUtility.labelWidth = originalValue;
                    if (GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        GameObject sc = GameObject.FindWithTag("SenseController");
                        if (!sc)
                        {
                            if (EditorUtility.DisplayDialog("Warning...!!", "TechXR :: No XRController in the Scene , First add the XRController/XRPlayer", "Ok", "Close")) { }
                            Debug.Log("TechXR :: No XRController in the Scene , First add the XRController/XRPlayer");
                        }
                        else
                        {
                            AddBluetoothWarningSystem();
                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);

                    //=====================Assets==============================

                    // Horizontal Line Separation
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(100);
                    GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(2) });
                    GUILayout.Space(100);
                    GUILayout.EndHorizontal();
                    GUILayout.Space(10);

                    // Space
                    GUILayout.Space(10);

                    // Heading
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Assets", skin.GetStyle("Heading"));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(15);


                    // UI Elements Options
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Select UI Element :", skin.GetStyle("SubHeading"));
                    GUILayout.Space(20);
                    if (uiElementOptions[0] == null) FillUIElementOptions();
                    uiElementIndex = EditorGUILayout.Popup("", uiElementIndex, uiElementOptions, GUILayout.Width(100)); // Dropdown
                    GUILayout.Space(80);
                    if (uiElementIndex >= 0)
                    {
                        GUILayout.BeginVertical();
                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Sprites/Icon/" + uiElementOptions[uiElementIndex] + ".png", typeof(Texture2D));
                        uiElement = obj as Texture2D;

                        //uiElement = Resources.Load("Icon/" + uiElementOptions[uiElementIndex], typeof(Texture2D)) as Texture2D;

                        //GUILayout.Label(uiElement);

                        // Button
                        if (GUILayout.Button("Add", GUILayout.Width(75)))
                        {
                            AddCanvas(uiElementOptions[uiElementIndex]);
                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.Space(20);
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(15);


                    // 3D Models Options
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Select 3D Model  :", skin.GetStyle("SubHeading"));
                    GUILayout.Space(20);
                    if (modelOptions[0] == null) FillModelOptions();
                    modelIndex = EditorGUILayout.Popup("", modelIndex, modelOptions, GUILayout.Width(100)); // Dropdown
                    GUILayout.Space(80);
                    if (modelIndex >= 0)
                    {
                        GUILayout.BeginVertical();
                        if (GUILayout.Button("Add", GUILayout.Width(75))) // Button
                        {
                            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/3DModels/" + modelOptions[modelIndex] + "/" + "model.obj", typeof(GameObject));

                            if (!prefab)
                            {
                                prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/3DModels/" + modelOptions[modelIndex] + "/" + modelOptions[modelIndex] + ".obj", typeof(GameObject));
                            }

                            GameObject obj = Instantiate(prefab) as GameObject;

                            // Focus
                            Selection.activeGameObject = obj;
                            SceneView.lastActiveSceneView.FrameSelected();

                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.Space(20);
                    GUILayout.EndHorizontal();


                    // Space
                    GUILayout.Space(10);

                    //=====================Assets==============================
                    EditorGUI.EndDisabledGroup();
                }
            }


            GUILayout.EndScrollView(); // End Scroll View

            

            Texture banner = (Texture)AssetDatabase.LoadAssetAtPath("Assets/TechXR/Logo/file.png", typeof(Texture));
            GUILayout.Box(banner, skin.GetStyle("Logo"));

            // Space
            GUILayout.Space(10);

            // Link to TechXR Store
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("To Pre-Book your TechXR 6DoF Controller (SenseXR) Click");
            if (GUILayout.Button("Here", skin.GetStyle("Button")))
            {
                Application.OpenURL("https://forms.gle/jxNumHU3ZwVZVwGi6");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            /*
            // Horizontal Line Separation
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(100);
            GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(100);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            */


            // Space
            GUILayout.Space(10);

            //=====================Assets==============================
            /*
            // Horizontal Line Separation
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Space(100);
            GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(2) });
            GUILayout.Space(100);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Space
            GUILayout.Space(10);

            // Heading
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Assets", skin.GetStyle("Heading"));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            // Space
            GUILayout.Space(15);


            // UI Elements Options
            GUILayout.BeginHorizontal();
            GUILayout.Label("Select UI Element :", skin.GetStyle("SubHeading"));
            GUILayout.Space(20);
            if (uiElementOptions[0] == null) FillUIElementOptions();
            uiElementIndex = EditorGUILayout.Popup("", uiElementIndex, uiElementOptions, GUILayout.Width(100)); // Dropdown
            GUILayout.Space(80);
            if (uiElementIndex >= 0)
            {
                GUILayout.BeginVertical();
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Sprites/Icon/" + uiElementOptions[uiElementIndex] + ".png", typeof(Texture2D));
                uiElement = obj as Texture2D;

                //uiElement = Resources.Load("Icon/" + uiElementOptions[uiElementIndex], typeof(Texture2D)) as Texture2D;

                //GUILayout.Label(uiElement);

                // Button
                if (GUILayout.Button("Add", GUILayout.Width(75)))
                {
                    AddCanvas(uiElementOptions[uiElementIndex]);
                    this.ShowNotification(new GUIContent("Updated..!!"));
                }
                GUILayout.EndVertical();
            }
            GUILayout.Space(20);
            GUILayout.EndHorizontal();


            // Space
            GUILayout.Space(15);


            // 3D Models Options
            GUILayout.BeginHorizontal();
            GUILayout.Label("Select 3D Model  :", skin.GetStyle("SubHeading"));
            GUILayout.Space(20);
            if (modelOptions[0] == null) FillModelOptions();
            modelIndex = EditorGUILayout.Popup("", modelIndex, modelOptions, GUILayout.Width(100)); // Dropdown
            GUILayout.Space(80);
            if (modelIndex >= 0)
            {
                GUILayout.BeginVertical();
                if (GUILayout.Button("Add", GUILayout.Width(75))) // Button
                {
                    UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/3DModels/" + modelOptions[modelIndex] + "/" + "model.obj", typeof(GameObject));

                    if (!prefab)
                    {
                        prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/3DModels/" + modelOptions[modelIndex] + "/" + modelOptions[modelIndex] + ".obj", typeof(GameObject));
                    }

                    GameObject obj = Instantiate(prefab) as GameObject;

                    // Focus
                    Selection.activeGameObject = obj;
                    SceneView.lastActiveSceneView.FrameSelected();

                    this.ShowNotification(new GUIContent("Updated..!!"));
                }
                GUILayout.EndVertical();
            }
            GUILayout.Space(20);
            GUILayout.EndHorizontal();


            // Space
            GUILayout.Space(10);
            */
            //=====================Assets==============================

            /*

                        // Heading (Horizontally)
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("Select Application Mode", guiStyle);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        // Space
                        GUILayout.Space(15);

                        // Radio Buttons
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        selectApplicationModeIndex = GUILayout.SelectionGrid(selectApplicationModeIndex, applicationModeOptions, applicationModeOptions.Length, EditorStyles.radioButton);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        // Space
                        GUILayout.Space(5);

            */



            /*
                        // Space
                        GUILayout.Space(5);

                        // Horizontal Line Separation
                        GUILayout.Space(10);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(100);
                        GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                        GUILayout.Space(100);
                        GUILayout.EndHorizontal();
                        GUILayout.Space(10);

                        // Space
                        GUILayout.Space(7);

                        // Heading
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("Configure XRPlayer/XRController", guiStyle);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        // Space
                        GUILayout.Space(15);

                        // --------------------------Begin Disable Group---------------------

                        EditorGUI.BeginDisabledGroup(applicationModeOptions[selectApplicationModeIndex].Contains("AR"));
                        if (applicationModeOptions[selectApplicationModeIndex].Contains("AR"))
                            m_IsPlayer = false;

                        // Add Player Toggle
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUIUtility.labelWidth = 180;
                        m_IsPlayer = EditorGUILayout.ToggleLeft("Add XRPlayer", m_IsPlayer);
                        EditorGUIUtility.labelWidth = originalValue;
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        // Help Box
                        if (m_IsPlayer && applicationModeOptions[selectApplicationModeIndex].Contains("VR"))
                        {
                            GUILayout.Space(3);
                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("XRCamera along with the XRController will move inside XRPlayerController", MessageType.Info);
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }

                        EditorGUI.EndDisabledGroup();

                        // --------------------------End Disable Group---------------------


                        // Space
                        GUILayout.Space(5);


                        // --------------------------Begin Disable Group---------------------

                        EditorGUI.BeginDisabledGroup(applicationModeOptions[selectApplicationModeIndex].Contains("VR"));
                        if(applicationModeOptions[selectApplicationModeIndex].Contains("VR"))
                            m_IsController = m_IsPlayer;

                        // Add Controller
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        originalValue = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 180;
                        m_IsController = EditorGUILayout.ToggleLeft("Add XR Controller ( Recommended )", m_IsController);
                        EditorGUIUtility.labelWidth = originalValue;
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        EditorGUI.EndDisabledGroup();

                        // --------------------------End Disable Group---------------------

                        // Space
                        GUILayout.Space(5);

                        // Add Camera
                        m_IsCamera = m_IsController;

                        // Help Box If Controller is present without the Camera give warning
                        if (m_IsController && GameObject.FindGameObjectWithTag("SenseController") != null && GameObject.Find("SenseCamera") == null)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("XR Camera must be present in the Scene with the XR Controller", MessageType.Warning);
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }

                        // Space
                        GUILayout.Space(7);


                        // --------------------------Begin Disable Group---------------------

                        EditorGUI.BeginDisabledGroup(applicationModeOptions[selectApplicationModeIndex].Contains("AR"));

                        // Player Body Options
                        GUILayout.BeginHorizontal();
                        playerCharacterIndex = EditorGUILayout.Popup("Character Type : ", playerCharacterIndex, playerCharacterOptions);
                        GUILayout.Space(40);
                        GUILayout.EndHorizontal();

                        EditorGUI.EndDisabledGroup();

                        // --------------------------End Disable Group---------------------


                        // Controller Body Options
                        GUILayout.BeginHorizontal();
                        controllerBodyIndex = EditorGUILayout.Popup("Select Controller Body : ", controllerBodyIndex, controllerBodyOptions);
                        if (m_IsController)
                        {
                            string icon = "";

                            if (controllerBodyIndex == 0) icon = "Default";
                            if (controllerBodyIndex == 1) icon = "Hand";

                            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Sprites/Icon/" + icon + ".png", typeof(Texture2D));
                            controllerBody = obj as Texture2D;
                            GUILayout.Label(controllerBody);
                        }
                        GUILayout.Space(40);
                        GUILayout.EndHorizontal();

                        // Space
                        GUILayout.Space(5);

                        // Horizontal Line Separation
                        GUILayout.Space(10);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(100);
                        GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                        GUILayout.Space(100);
                        GUILayout.EndHorizontal();
                        GUILayout.Space(10);

                        // Heading
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("Configure XR Scene", guiStyle);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        // Space
                        GUILayout.Space(15);

                        // --------------------------Begin Disable Group---------------------

                        EditorGUI.BeginDisabledGroup(applicationModeOptions[selectApplicationModeIndex].Contains("AR"));

                        // Add Environment
                        GUILayout.Label("*Optional");
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (environmentOptions[0] == null) FillEnvironmentOptions();
                        EditorGUIUtility.labelWidth = 100;
                        m_IsEnvironment = EditorGUILayout.ToggleLeft("Add Environment", m_IsEnvironment);
                        EditorGUIUtility.labelWidth = originalValue;
                        environmentIndex = EditorGUILayout.Popup("", environmentIndex, environmentOptions);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        // Add Warning System
                        GUILayout.Label("*Optional");
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUIUtility.labelWidth = 310;
                        m_TrackableWarningSystem = EditorGUILayout.ToggleLeft("Add XRController ( Trackable Red Alert ) Warning System", m_TrackableWarningSystem);
                        EditorGUIUtility.labelWidth = originalValue;
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        // HelpBox Add Trackable Warning System Info
                        if (m_TrackableWarningSystem && !m_IsPlayer)
                        {
                            GameObject controller = GameObject.FindWithTag("SenseController");
                            if (!controller)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.FlexibleSpace();
                                EditorGUILayout.HelpBox("No XRPlayer in the scene, Check the XRPlayer Checkbox", MessageType.Warning);
                                GUILayout.FlexibleSpace();
                                GUILayout.EndHorizontal();
                            }
                        }

                        // Add Warning System
                        GUILayout.Label("*Optional");
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        EditorGUIUtility.labelWidth = 310;
                        m_BluetoothWarningSystem = EditorGUILayout.ToggleLeft("Add XRController Bluetooth Alert", m_BluetoothWarningSystem);
                        EditorGUIUtility.labelWidth = originalValue;
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        EditorGUI.EndDisabledGroup();

                        // --------------------------End Disable Group---------------------


                        // HelpBox
                        if (applicationModeOptions[selectApplicationModeIndex].Contains("AR"))
                        {
                            GUILayout.Space(5);
                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.HelpBox("Environment & Warning System Are not enabled for AR Mode.", MessageType.Info);
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }



                        // Horizontal Line Separation
                        GUILayout.Space(18);
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(100);
                        GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                        GUILayout.Space(100);
                        GUILayout.EndHorizontal();


                        // Check if any Options are checked
                        m_IsCustomSceneGroupEnabled = m_IsCamera || m_IsController || m_IsPlayer || m_IsEnvironment || m_TrackableWarningSystem || m_BluetoothWarningSystem;


                        // --------------------------Begin Disable Group---------------------

                        EditorGUI.BeginDisabledGroup(!m_IsCustomSceneGroupEnabled);

                        // Space
                        GUILayout.Space(18);

                        // Button
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Add Selected Items to Scene", GUILayout.Height(40)))
                        {
                            AddLayersAndTags();

                            AddDirectionalLight();

                            if (applicationModeOptions[selectApplicationModeIndex].Contains("AR"))
                            {
                                if (m_IsCamera) AddSenseCamera();
                                if (m_IsController)
                                {
                                    AddController();
                                    AddSenseManager();
                                }
                            } 
                            //if (m_IsPlayer && applicationModeOptions[selectApplicationModeIndex].Contains("VR")) //AddXRPlayerController();
                            //if (m_IsPlayer) AddXRPlayerController();
                            //if (m_IsPlayer && !m_IsCamera) AddXRPlayerController();
                            //if (m_IsPlayer) AddXRPlayerController();
                            //if (m_IsFloor) AddFloor();
                            if (applicationModeOptions[selectApplicationModeIndex].Contains("VR"))
                            {
                                if (m_IsPlayer) AddXRPlayerController();
                                if (m_IsEnvironment) AddEnvironment();
                                if (m_TrackableWarningSystem) AddTrackableWarningSystem();
                                if (m_BluetoothWarningSystem) AddBluetoothWarningSystem();
                            }

                            // Delete all un-necessary things for AR camera configuration if the camera is selected
                            if (m_IsCamera && applicationModeOptions[selectApplicationModeIndex].Contains("AR"))
                            {
                                GameObject[] grounds = GameObject.FindGameObjectsWithTag("Env");
                                foreach (GameObject ground in grounds)
                                    DestroyImmediate(ground);

                                GameObject[] players = GameObject.FindGameObjectsWithTag("XRPlayerController");
                                foreach (GameObject player in players)
                                    DestroyImmediate(player);
                            }

                            this.ShowNotification(new GUIContent("Updated..!!"));
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();


                        EditorGUI.EndDisabledGroup();

                        // --------------------------End Disable Group---------------------


                        // Horizontal Line Separation
                        GUILayout.Space(18);
                        GUILayout.BeginHorizontal();
                        GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                        GUILayout.EndHorizontal();

                        // Space
                        GUILayout.Space(30);

                        // UI Elements Options
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        if(uiElementOptions[0] == null) FillUIElementOptions();
                        uiElementIndex = EditorGUILayout.Popup("Select UI Element : ", uiElementIndex, uiElementOptions,GUILayout.Width(300)); // Dropdown
                        GUILayout.Space(80);
                        if (uiElementIndex >= 0)
                        {
                            GUILayout.BeginVertical();
                            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Sprites/Icon/" + uiElementOptions[uiElementIndex] + ".png", typeof(Texture2D));
                            uiElement = obj as Texture2D;

                            //uiElement = Resources.Load("Icon/" + uiElementOptions[uiElementIndex], typeof(Texture2D)) as Texture2D;

                            GUILayout.Label(uiElement);

                            // Button
                            if (GUILayout.Button("Add", GUILayout.Width(75)))
                            {
                                AddCanvas(uiElementOptions[uiElementIndex]);
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.Space(20);
                        GUILayout.EndHorizontal();

                        // Space
                        GUILayout.Space(30);

                        // 3D Models Options
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        if (modelOptions[0] == null) FillModelOptions();
                        modelIndex = EditorGUILayout.Popup("Select 3D Model : ", modelIndex, modelOptions, GUILayout.Width(300)); // Dropdown
                        GUILayout.Space(80);
                        if (modelIndex >= 0)
                        {
                            GUILayout.BeginVertical();
                            if (GUILayout.Button("Add", GUILayout.Width(75))) // Button
                            {
                                UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/3DModels/"+ modelOptions[modelIndex] +"/"+"model.obj", typeof(GameObject));

                                if (!prefab)
                                {
                                    prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/3DModels/" + modelOptions[modelIndex] + "/" + modelOptions[modelIndex] + ".obj", typeof(GameObject));
                                }

                                GameObject obj = Instantiate(prefab) as GameObject;

                                // Focus
                                Selection.activeGameObject = obj;
                                SceneView.lastActiveSceneView.FrameSelected();
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.Space(20);
                        GUILayout.EndHorizontal();

                        // Space
                        GUILayout.Space(30);

                        // Skybox Options
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        if (skyboxOptions[0] == null) FillSkyboxOptions();
                        skyboxIndex = EditorGUILayout.Popup("Select Skybox : ", skyboxIndex, skyboxOptions, GUILayout.Width(300)); // Dropdown
                        GUILayout.Space(80);
                        if (modelIndex >= 0)
                        {
                            GUILayout.BeginVertical();
                            if (GUILayout.Button("Set", GUILayout.Width(75))) // Button
                            {
                                SetSkybox();
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.Space(20);
                        GUILayout.EndHorizontal();
            */

            /*
            // Space
            GUILayout.Space(35);

            // Button
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset To Default", GUILayout.Height(25))) 
            {
                Reset();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            */


            /*
                        //Middle Section------------------------------------------------------------------------------------

                        // Label for the Menu
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("B. Add UI Elements", guiStyle);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                        GUILayout.Space(15);



                        showUIElements = EditorGUILayout.Foldout(showUIElements, "UI Element List");
                        if (showUIElements)
                        {
                            if (GUILayout.Button("Add Dialogue Canvas"))
                            {
                                // Check if Canvas is present in scene -> Display a dialog box, else add the Canvas
                                if (CheckIfTheCanvasIsPresentInScene("Dialogue Canvas"))
                                {
                                    if (EditorUtility.DisplayDialog("Dialogue Canvas is already present in your scene", "To add one more Dialogue Canvas click on ok button.", "Ok", "Close"))
                                    {
                                        AddCanvas("Dialogue Canvas");
                                    }
                                }
                                else
                                {
                                    AddCanvas("Dialogue Canvas");
                                }

                            }

                            if (GUILayout.Button("Add Scroll Canvas"))
                            {
                                // Check if Canvas is present in scene -> Display a dialog box, else add the Canvas
                                if (CheckIfTheCanvasIsPresentInScene("Scroll Canvas"))
                                {
                                    if (EditorUtility.DisplayDialog("Scroll Canvas is already present in your scene", "To add one more Scroll Canvas click on ok button.", "Ok", "Close"))
                                    {
                                        AddCanvas("Scroll Canvas");
                                    }
                                }
                                else
                                {
                                    AddCanvas("Scroll Canvas");
                                }

                            }

                            if (GUILayout.Button("Add Slider Canvas"))
                            {
                                // Check if Canvas is present in scene -> Display a dialog box, else add the Canvas
                                if (CheckIfTheCanvasIsPresentInScene("Slider Canvas"))
                                {
                                    if (EditorUtility.DisplayDialog("Slider Canvas is already present in your scene", "To add one more Slider Canvas click on ok button.", "Ok", "Close"))
                                    {
                                        AddCanvas("Slider Canvas");
                                    }
                                }
                                else
                                {
                                    AddCanvas("Slider Canvas");
                                }

                            }
                        }

                        showPrefabs = EditorGUILayout.Foldout(showPrefabs, "Prefabs List");
                        showObjects = EditorGUILayout.Foldout(showObjects, "3D Objects List");

                        GUILayout.Space(15);
                        // Horizontal Line Seperation
                        //EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                        GUILayout.Box(GUIContent.none, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
                        GUILayout.Space(15);
            */


            //GUILayout.EndScrollView(); // End Scroll View
            GUILayout.EndVertical(); // End Vertical Window design

        }

        /// <summary>
        /// Update the Dropdown List of Environment prefabs present in the Given folder
        /// </summary>
        static void FillEnvironmentOptions()
        {
            environmentFileInfo = environmentDir.GetFiles("*.*");
            environmentOptions = new string[environmentFileInfo.Length];

            int index = 0;
            foreach (FileInfo f in environmentFileInfo)
            {
                environmentOptions[index] = f.Name.Split('.')[0];
                index++;
            }
        }

        /// <summary>
        /// Update the Dropdown List of UI Elements present in the Given folder
        /// </summary>
        void FillUIElementOptions()
        {
            uiElementFilesInfo = uiElementDir.GetFiles("*.*");
            uiElementOptions = new string[uiElementFilesInfo.Length];

            int index = 0;
            foreach (FileInfo f in uiElementFilesInfo)
            {
                uiElementOptions[index] = f.Name.Split('.')[0];
                index++;
            }
        }

        /// <summary>
        /// Update the Dropdown List of 3D Models present in the Given folder
        /// </summary>
        void FillModelOptions()
        {
            modelFilesInfo = modelsDir.GetFiles("*.*");
            modelOptions = new string[modelFilesInfo.Length];

            int index = 0;
            foreach (FileInfo f in modelFilesInfo)
            {
                modelOptions[index] = f.Name.Split('.')[0];
                index++;
            }
        }

        /// <summary>
        /// Update the Dropdown List of Skybox materials present in the Given folder
        /// </summary>
        void FillSkyboxOptions()
        {
            skyboxFilesInfo = skyboxDir.GetFiles("*.*");
            skyboxOptions = new string[skyboxFilesInfo.Length / 2];

            int index = 0;
            foreach (FileInfo f in skyboxFilesInfo)
            {
                if (f.Name.Contains(".mat.")) continue;

                skyboxOptions[index] = f.Name.Split('.')[0];
                index++;
            }
        }

        /// <summary>
        /// Set the options to default states
        /// </summary>
        private void Reset()
        {
            selectApplicationModeIndex = 1;

            m_IsController = false;
            controllerBodyIndex = 0;

            m_IsPlayer = false;
            playerCharacterIndex = 0;

            m_IsEnvironment = false;
            environmentIndex = 0;

            m_TrackableWarningSystem = true;
            m_BluetoothWarningSystem = false;
        }

        private void ToggleUIAssetsButtonTitle()
        {
            m_UIAssetsButtonTitle = m_UIAssetsDisplay ? "Hide UI Assets" : "Show UI Assets";
        }

        private void ToggleUIAssetsDisplay()
        {
            if (m_UIAssetsDisplay)
            {
                DisplayUIAssetsPanel();
            }
            else
            {
                HideUIAssetsPanel();
            }
        }

        private void DisplayUIAssetsPanel()
        {
            DrawUILine(Color.grey, 10, 20);
        }

        private void HideUIAssetsPanel()
        {
            throw new NotImplementedException();
        }


        private void OpenUIElementsWindow()
        {
            //GetWindow(typeof(SenseUIElements));
        }

        /// <summary>
        /// Add the Camera to the scene
        /// </summary>
        private static void AddSenseCamera()
        {

            // Destroy All cameras
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in cameras)
                DestroyImmediate(cam.gameObject);

            // Instantiate the SenseCamera
            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/SenseCamera.prefab", typeof(GameObject));
            var xrcam = Instantiate(prefab) as GameObject;
            xrcam.name = "SenseCamera";

            // SetUp Clear Flags
            if (tabs[selectedTab].Contains("VR"))
            {
                xrcam.GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
            }
            else if (tabs[selectedTab].Contains("AR"))
            {
                xrcam.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            }

            // Focus
            Selection.activeGameObject = xrcam;
            SceneView.lastActiveSceneView.FrameSelected();


        }

        /// <summary>
        /// Add XR Controller to the scene
        /// </summary>

        private static void AddController()
        {
            /*
            // Return if SenseController is present
            GameObject[] controllers = GameObject.FindGameObjectsWithTag("SenseController");

            if (controllers.Length > 0)
            {
                controllers[0].GetComponent<SenseController>().SetPointerType(controllerBodyIndex);
                Debug.Log("SenseController is already present in your scene..!");
                return;
            }

            // Instantiate the XR Controller
            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/SenseController.prefab", typeof(GameObject));
            var xr = Instantiate(prefab) as GameObject;
            xr.name = "SenseController";

            // Set Active the GameObject
            xr.GetComponent<SenseController>().SetPointerType(controllerBodyIndex);

            AddEventSystem();*/
        }

        /// <summary>
        /// Add the Player to the scene
        /// </summary>

        private static void AddXRPlayerController()
        {
            /*
            // Destroy All Players
            GameObject[] players = GameObject.FindGameObjectsWithTag("XRPlayerController");
            foreach (GameObject p in players)
                DestroyImmediate(p);

            // Delete all Cameras
            DeleteCameras();

            // Delete all XRControllers
            GameObject[] controllers = GameObject.FindGameObjectsWithTag("SenseController");
            foreach (GameObject controller in controllers)
                DestroyImmediate(controller);

            // Delete all DeveloperCube
            GameObject[] devCubes = GameObject.FindGameObjectsWithTag("TechXRDeveloperCube");
            foreach (GameObject devCube in devCubes)
                DestroyImmediate(devCube);

            // Instantiate the XRPlayerController
            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/T_XRPlayerController.prefab", typeof(GameObject));
            var playerController = Instantiate(prefab) as GameObject;
            playerController.name = "XRPlayerController";

            // Turn on the Player Body
            playerController.transform.Find("CharacterBodyContainer").Find(playerCharacterOptions[playerCharacterIndex]).gameObject.SetActive(true);

            // Set active the controller body
            playerController.GetComponentInChildren<SenseController>().SetPointerType(controllerBodyIndex);

            // Focus
            Selection.activeGameObject = playerController;
            SceneView.lastActiveSceneView.FrameSelected();

            // Adding SenseManager and EventSystem
            AddEventSystem();
            AddSenseManager();
            */
        }

        /// <summary>
        /// Add Trackable Warning System
        /// </summary>

        private static void AddTrackableWarningSystem()
        {
            /*GameObject warningCanvas = GameObject.Find("Warning Canvas");
            if (warningCanvas)
                warningCanvas.transform.Find("TrackableWarningImage").gameObject.SetActive(true);
            else
            {
                UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/UIElements/Optional/Warning Canvas.prefab", typeof(GameObject));
                var xrcanvas = Instantiate(prefab) as GameObject;
                xrcanvas.name = "Warning Canvas";

                xrcanvas.transform.Find("TrackableWarningImage").gameObject.SetActive(true);

                GameObject te = GetTechXREssentialGO();
                xrcanvas.transform.SetParent(te.transform);
            }

            GameObject playerController = GameObject.FindWithTag("XRPlayerController");

            if (!playerController)
            {
                Debug.Log("TechXR :: No XRPlayer in the Scene , Check the XRPlayer Checkbox");
            }
            else
            {
                DestroyImmediate(playerController.GetComponentInChildren<DefaultTrackableEventHandler>());

                GameObject controller = playerController.GetComponentInChildren<SenseController>().gameObject;
                controller.AddComponent<SenseXRTrackingStatus>();
            }*/
        }


        /// <summary>
        /// Add Bluetooth warning system
        /// </summary>

        private static void AddBluetoothWarningSystem()
        {

            /*GameObject btWarning = GameObject.Find("Bluetooth Warning");
            if (!btWarning)
            {
                GameObject go = new GameObject("Bluetooth Warning");
                go.AddComponent<SenseXRConnectivityStatus>();

                GameObject te = GetTechXREssentialGO();
                go.transform.SetParent(te.transform);
            }
            */
        }


        /// <summary>
        /// Check if eventsystem is not present in the scene and add
        /// </summary>
        private static void AddEventSystem()
        {
            GameObject eventSystem = GameObject.FindGameObjectWithTag("SenseEventSystem");
            if (eventSystem != null) return;

            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/SenseEventSystem.prefab", typeof(GameObject));
            var es = Instantiate(prefab) as GameObject;
            es.name = "SenseEventSystem";

            GameObject te = GetTechXREssentialGO();
            es.transform.SetParent(te.transform);
        }

        /// <summary>
        /// Add SenseManager prefab
        /// </summary>
        private static void AddSenseManager()
        {
            if (GameObject.FindGameObjectWithTag("SenseManager") != null) return;

            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/SenseManager.prefab", typeof(GameObject));
            var senseManager = Instantiate(prefab) as GameObject;
            senseManager.name = "SenseManager";

            GameObject te = GetTechXREssentialGO();
            senseManager.transform.SetParent(te.transform);
        }

        /// <summary>
        /// Add the Floor prefab
        /// </summary>
        private static void AddEnvironment()
        {
            FillEnvironmentOptions();
            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/Environments/" + environmentOptions[environmentIndex] + ".prefab", typeof(GameObject));
            var env = Instantiate(prefab) as GameObject;
            env.name = environmentOptions[environmentIndex];

            GameObject te = GetTechXREssentialGO();
            env.transform.SetParent(te.transform);
        }

        /// <summary>
        /// Set Skybox
        /// </summary>
        private static void SetSkybox()
        {
            Material m = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Skybox/Mat/" + skyboxOptions[skyboxIndex] + ".mat", typeof(Material)) as Material;

            RenderSettings.skybox = m;
        }

        /// <summary>
        /// Add Directional Light
        /// </summary>
        private static void AddDirectionalLight()
        {
            // If Directional light is there then return
            Light dl = GameObject.FindObjectOfType<Light>();
            if (dl) if (dl.type == LightType.Directional) return;

            // If Directional light is not there create new object and give the name
            GameObject lightGameObject = new GameObject("Directional Light");

            // Assign The transform
            lightGameObject.transform.position = new Vector3(0, 3, 0);
            lightGameObject.transform.eulerAngles = new Vector3(50, -30, 0);

            // Add light component and do the settings
            Light lightComp = lightGameObject.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.color = new Color(255f / 255f, 244f / 255f, 214f / 255f);
            lightComp.shadows = LightShadows.Soft;
        }

        /// <summary>
        /// Add Canvas
        /// </summary>
        /// <param name="canvasName"></param>
        public static void AddCanvas(string canvasName)
        {
            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/UIElements/UI/" + canvasName + ".prefab", typeof(GameObject));
            var xrcanvas = Instantiate(prefab) as GameObject;
            xrcanvas.name = canvasName;

            // Check and assign the MainCamera (Sense Camera) to the Canvas - EventCamera
            if (Camera.main)
                xrcanvas.GetComponent<Canvas>().worldCamera = Camera.main;
            else
                Debug.LogWarning("TechXR : Add SenseCamera to the scene, and assign the SenseCamera to the EventCamera of the Canvas component");

            // Focus
            Selection.activeGameObject = xrcanvas;
            SceneView.lastActiveSceneView.FrameSelected();

            //CheckEventSystem();
        }


        /// <summary>
        /// Get the TechXREssential gameobject
        /// </summary>
        /// <returns></returns>
        private static GameObject GetTechXREssentialGO()
        {
            GameObject go = GameObject.Find("TechXREssentials");
            if (go) return go;

            go = new GameObject("TechXREssentials");
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            return go;
        }

        public static void AddDeveloperCube(string cubeType)
        {
            GameObject _go = GameObject.FindWithTag("TechXRDeveloperCube");
            if (_go) return;

            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath("Assets/TechXR/TechXR_DeveloperCube/Prefabs/DeveloperCube" + ".prefab", typeof(GameObject));
            var devCube = Instantiate(prefab) as GameObject;


            // turn off inside/outside view
            foreach (string s in arControllerOptions)
            {
                Transform go = devCube.transform.Find(s);
                if (go) go.gameObject.SetActive(false);
            }

            // if intractable cube is selected add laser pointer and input module
            if (cubeType == "Intractable")
            {
                // add laserpointer
                LaserPointer laserPointer = devCube.AddComponent<LaserPointer>();

                if (!laserPointer)
                    laserPointer = devCube.AddComponent<LaserPointer>();

                // Set Color
                laserPointer.Color = Color.white;

                // laserpointerinputmodule
                AddEventSystem();

                // add intractable sphere
                UnityEngine.Object i_sphere = AssetDatabase.LoadAssetAtPath("Assets/TechXR/Prefabs/Intractable Cube/Intractable.prefab", typeof(GameObject));
                var sphere = Instantiate(i_sphere) as GameObject;
                sphere.transform.SetParent(devCube.transform);
                sphere.transform.localPosition = Vector3.zero;
            }
            else
            {
                // turn on inside/outside view
                devCube.transform.Find(cubeType).gameObject.SetActive(true);

                // remove the sphere
                GameObject i_Sphere = GameObject.FindWithTag("ISphere");
                if (i_Sphere) DestroyImmediate(i_Sphere);
            }

            // Focus
            Selection.activeGameObject = devCube;
            SceneView.lastActiveSceneView.FrameSelected();

            AddDirectionalLight();
        }

        /// <summary>
        /// To check if camera is present in the scene
        /// </summary>
        /// <returns></returns>
        private bool CheckIfCameraIsPresentInScene()
        {
            Camera[] cameras = FindObjectsOfType<Camera>();

            foreach (Camera camera in cameras)
                if (camera.gameObject.name == "SenseCamera")
                    return true;

            return false;
        }

        /// <summary>
        /// To check if controller is present in the scene
        /// </summary>
        /// <returns></returns>
        private bool CheckIfControllerIsPresentInScene()
        {
            IUILaserPointer[] controllers = FindObjectsOfType<IUILaserPointer>();

            if (controllers.Length > 0)
                return true;

            return false;
        }

        /// <summary>
        /// To check if XRPlayerController is present in the scene
        /// </summary>
        /// <returns></returns>
        private bool CheckIfXRPlayerControllerIsPresentInScene()
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("XRPlayerController");

            if (players.Length > 0)
                return true;

            return false;
        }

        /// <summary>
        /// To check if the canvas is present in the scene
        /// </summary>
        /// <param name="canvasName"></param>
        /// <returns></returns>
        private bool CheckIfTheCanvasIsPresentInScene(string canvasName)
        {
            Canvas[] canvasGroup = GameObject.FindObjectsOfType<Canvas>();

            foreach (Canvas canvas in canvasGroup)
                if (canvas.gameObject.name == canvasName)
                    return true;

            return false;
        }

        /// <summary>
        /// Delete All Cameras from the scene
        /// </summary>
        private static void DeleteCameras()
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in cameras)
                DestroyImmediate(cam.gameObject);
        }

        /// <summary>
        /// Create Layer
        /// </summary>
        /// <param name="name"></param>
        public static void CreateLayer(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new System.ArgumentNullException("name", "New layer name string is either null or empty.");

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layerProps = tagManager.FindProperty("layers");
            var propCount = layerProps.arraySize;

            SerializedProperty firstEmptyProp = null;

            for (var i = 0; i < propCount; i++)
            {
                var layerProp = layerProps.GetArrayElementAtIndex(i);

                var stringValue = layerProp.stringValue;

                if (stringValue == name) return;

                if (i < 8 || stringValue != string.Empty) continue;

                if (firstEmptyProp == null)
                    firstEmptyProp = layerProp;
            }

            if (firstEmptyProp == null)
            {
                UnityEngine.Debug.LogError("Maximum limit of " + propCount + " layers exceeded. Layer \"" + name + "\" not created.");
                return;
            }

            firstEmptyProp.stringValue = name;
            tagManager.ApplyModifiedProperties();

            Debug.Log(name + " Layer is Successfully added to your project..!!");
        }

        /// <summary>
        /// Add layers and tags
        /// </summary>
        private static void AddLayersAndTags()
        {
            AddLayersAndTags(m_Layers, m_Tags);
        }

        /// <summary>
        /// Add the layers and tags from the passed lists
        /// </summary>
        /// <param name="layers"></param>
        /// <param name="tags"></param>
        private static void AddLayersAndTags(List<string> layers, List<string> tags)
        {
            // add layers
            foreach (var item in layers)
            {
                CreateLayer(item);
            }

            // add tags
            foreach (var item in tags)
            {
                AddTag(item);
            }
        }

        /// <summary>
        /// Add tag to the tag list
        /// </summary>
        /// <param name="tagName"></param>
        private static void AddTag(string tagName)
        {
            // Open tag manager
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            // Tags Property
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            if (tagsProp.arraySize >= MAX_TAGS)
            {
                Debug.Log("No more tags can be added to the Tags property. You have " + tagsProp.arraySize + " tags");
            }
            // if not found, add it
            if (!PropertyExists(tagsProp, 0, tagsProp.arraySize, tagName))
            {
                int index = tagsProp.arraySize;
                // Insert new array element
                tagsProp.InsertArrayElementAtIndex(index);
                SerializedProperty sp = tagsProp.GetArrayElementAtIndex(index);
                // Set array element to tagName
                sp.stringValue = tagName;
                Debug.Log("Tag: " + tagName + " has been added");
                // Save settings
                tagManager.ApplyModifiedProperties();
            }
            else
            {
                //Debug.Log("Tag: " + tagName + " already exists");
            }
        }


        /// <summary>
        /// Checks if the value exists in the property.
        /// </summary>
        /// <returns><c>true</c>, if exists was propertyed, <c>false</c> otherwise.</returns>
        /// <param name="property">Property.</param>
        /// <param name="start">Start.</param>
        /// <param name="end">End.</param>
        /// <param name="value">Value.</param>
        private static bool PropertyExists(SerializedProperty property, int start, int end, string value)
        {
            for (int i = start; i < end; i++)
            {
                SerializedProperty t = property.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(value))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Draw UI Line
        /// </summary>
        /// <param name="color"></param>
        /// <param name="thickness"></param>
        /// <param name="padding"></param>
        private void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            //
            EditorGUI.DrawRect(r, color);
        }
    }
}