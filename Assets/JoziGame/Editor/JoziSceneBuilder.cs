using System.IO;
using JoziGame;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JoziGame.Editor
{
    public static class JoziSceneBuilder
    {
        const string ScenePath = "Assets/JoziGame/Scenes/JohannesburgStreetRun.unity";
        const string ApkPath = "Builds/JohannesburgStreetRun.apk";

        [MenuItem("Jozi Game/Build Scene")]
        public static void BuildScene()
        {
            Directory.CreateDirectory("Assets/JoziGame/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "JohannesburgStreetRun";

            Material asphalt = Material("Jozi Asphalt", new Color(0.08f, 0.085f, 0.085f));
            Material pavement = Material("Pavement", new Color(0.44f, 0.42f, 0.38f));
            Material gold = Material("Gold Token", new Color(1f, 0.66f, 0.05f));
            Material glass = Material("City Glass", new Color(0.15f, 0.33f, 0.47f));
            Material brick = Material("Warm Brick", new Color(0.58f, 0.22f, 0.14f));
            Material concrete = Material("Concrete", new Color(0.58f, 0.58f, 0.53f));
            Material green = Material("Jacaranda Green", new Color(0.22f, 0.48f, 0.25f));
            Material purple = Material("Jacaranda Purple", new Color(0.5f, 0.34f, 0.83f));
            Material marker = Material("Finish Blue", new Color(0.05f, 0.36f, 0.84f));
            Material runnerBlue = Material("Runner Blue", new Color(0.04f, 0.28f, 0.92f));
            Material runnerGold = Material("Runner Gold", new Color(1f, 0.68f, 0.08f));

            RenderSettings.skybox = null;
            RenderSettings.ambientLight = new Color(0.58f, 0.64f, 0.7f);

            CreateLight();
            CreateGround(asphalt, pavement);
            CreateStreetGrid(asphalt, pavement);
            CreateBuildings(glass, brick, concrete);
            CreateLandmarks(concrete, glass, purple, marker);
            CreateTrees(green, purple);

            GameObject player = CreatePlayer(runnerBlue, runnerGold);
            GameObject finish = CreateFinish(marker);
            CreateTokens(gold);
            CreateUi(player.GetComponent<JoziPlayerController>(), player.transform, finish.transform);
            CreateLabels();

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Jozi Game/Build Android APK")]
        public static void BuildAndroidApk()
        {
            BuildScene();
            Directory.CreateDirectory("Builds");

            PlayerSettings.productName = "Johannesburg Street Run";
            PlayerSettings.companyName = "Jozi Game";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.jozigame.streetrun");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
        }

        static void SetBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        static void CreateLight()
        {
            var sun = new GameObject("Highveld Sun");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.94f, 0.82f);
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }

        static void CreateGround(Material asphalt, Material pavement)
        {
            Cube("City Ground", new Vector3(0f, -0.1f, 0f), new Vector3(130f, 0.2f, 130f), pavement);
            Cube("Main Street", new Vector3(0f, 0.01f, 0f), new Vector3(12f, 0.1f, 120f), asphalt);
            Cube("Commissioner Street", new Vector3(0f, 0.02f, 22f), new Vector3(110f, 0.1f, 11f), asphalt);
            Cube("Fox Street", new Vector3(0f, 0.03f, -22f), new Vector3(105f, 0.1f, 10f), asphalt);

            for (int i = -5; i <= 5; i++)
            {
                Cube("Lane Stripe", new Vector3(0f, 0.09f, i * 10f), new Vector3(0.35f, 0.04f, 4.2f), Material("Street Stripe", Color.yellow));
            }
        }

        static void CreateStreetGrid(Material asphalt, Material pavement)
        {
            for (int x = -42; x <= 42; x += 28)
            {
                Cube("Side Street", new Vector3(x, 0.04f, 0f), new Vector3(8f, 0.1f, 105f), asphalt);
                Cube("Sidewalk West/East", new Vector3(x - 6f, 0.12f, 0f), new Vector3(3f, 0.18f, 105f), pavement);
                Cube("Sidewalk West/East", new Vector3(x + 6f, 0.12f, 0f), new Vector3(3f, 0.18f, 105f), pavement);
            }
        }

        static void CreateBuildings(Material glass, Material brick, Material concrete)
        {
            Material[] palette = { glass, brick, concrete };
            int index = 0;

            for (int x = -52; x <= 52; x += 13)
            {
                for (int z = -52; z <= 52; z += 18)
                {
                    if (Mathf.Abs(x) < 12 || Mathf.Abs(z - 22) < 10 || Mathf.Abs(z + 22) < 9)
                    {
                        continue;
                    }

                    float height = 8f + Mathf.Abs((x * 3 + z) % 16);
                    Cube("City Block", new Vector3(x, height * 0.5f, z), new Vector3(8f, height, 10f), palette[index % palette.Length]);
                    index++;
                }
            }
        }

        static void CreateLandmarks(Material concrete, Material glass, Material purple, Material marker)
        {
            Cylinder("Ponte Tower", new Vector3(-34f, 14f, 40f), new Vector3(7f, 14f, 7f), concrete);
            Cylinder("Ponte Inner Core", new Vector3(-34f, 16f, 40f), new Vector3(3.4f, 16f, 3.4f), glass);
            Cylinder("Hillbrow Tower", new Vector3(36f, 22f, 42f), new Vector3(2.2f, 22f, 2.2f), concrete);
            Sphere("Hillbrow Observation Deck", new Vector3(36f, 34f, 42f), new Vector3(8f, 2.4f, 8f), glass);
            Cube("Nelson Mandela Bridge Deck", new Vector3(0f, 5f, -42f), new Vector3(54f, 1.2f, 4f), marker);
            Cube("Bridge Pylon", new Vector3(-24f, 10f, -42f), new Vector3(1.2f, 12f, 1.2f), concrete);
            Cube("Bridge Pylon", new Vector3(24f, 10f, -42f), new Vector3(1.2f, 12f, 1.2f), concrete);
            Cube("Jacaranda Art Wall", new Vector3(24f, 2.5f, -7f), new Vector3(1f, 5f, 14f), purple);
        }

        static void CreateTrees(Material green, Material purple)
        {
            for (int i = -4; i <= 4; i++)
            {
                Vector3 left = new Vector3(-11f, 1.7f, i * 11f);
                Vector3 right = new Vector3(11f, 1.7f, i * 11f + 5f);
                Cylinder("Tree Trunk", left + Vector3.down * 0.7f, new Vector3(0.55f, 1f, 0.55f), Material("Tree Trunk Brown", new Color(0.35f, 0.19f, 0.09f)));
                Sphere("Jacaranda Crown", left + Vector3.up * 1.1f, new Vector3(4f, 2.4f, 4f), i % 2 == 0 ? purple : green);
                Cylinder("Tree Trunk", right + Vector3.down * 0.7f, new Vector3(0.55f, 1f, 0.55f), Material("Tree Trunk Brown", new Color(0.35f, 0.19f, 0.09f)));
                Sphere("Jacaranda Crown", right + Vector3.up * 1.1f, new Vector3(4f, 2.4f, 4f), i % 2 == 0 ? green : purple);
            }
        }

        static GameObject CreatePlayer(Material runnerBlue, Material runnerGold)
        {
            GameObject player = new GameObject("Player");
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1.1f, -52f);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.center = new Vector3(0f, 1f, 0f);

            GameObject visualRoot = new GameObject("Visible Runner");
            visualRoot.transform.SetParent(player.transform);
            visualRoot.transform.localPosition = Vector3.zero;

            GameObject body = CapsulePart("Runner Body", visualRoot.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.55f, 0.72f, 0.55f), runnerBlue);
            GameObject head = Sphere("Runner Head", player.transform.position + new Vector3(0f, 2.05f, 0f), new Vector3(0.42f, 0.42f, 0.42f), runnerGold);
            head.transform.SetParent(visualRoot.transform);
            head.transform.localPosition = new Vector3(0f, 2.05f, 0f);
            Object.DestroyImmediate(head.GetComponent<Collider>());

            Transform leftArm = Limb("Left Arm", visualRoot.transform, new Vector3(-0.46f, 1.25f, 0f), runnerGold);
            Transform rightArm = Limb("Right Arm", visualRoot.transform, new Vector3(0.46f, 1.25f, 0f), runnerGold);
            Transform leftLeg = Limb("Left Leg", visualRoot.transform, new Vector3(-0.2f, 0.42f, 0f), runnerBlue);
            Transform rightLeg = Limb("Right Leg", visualRoot.transform, new Vector3(0.2f, 0.42f, 0f), runnerBlue);

            JoziRunnerVisual runnerVisual = visualRoot.AddComponent<JoziRunnerVisual>();
            SetPrivateField(runnerVisual, "controller", controller);
            SetPrivateField(runnerVisual, "body", body.transform);
            SetPrivateField(runnerVisual, "leftArm", leftArm);
            SetPrivateField(runnerVisual, "rightArm", rightArm);
            SetPrivateField(runnerVisual, "leftLeg", leftLeg);
            SetPrivateField(runnerVisual, "rightLeg", rightLeg);

            GameObject pivot = new GameObject("Camera Pivot");
            pivot.transform.SetParent(player.transform);
            pivot.transform.localPosition = new Vector3(0f, 1.8f, 0f);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 68f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.SetParent(pivot.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1.0f, -6.2f);
            cameraObject.transform.localRotation = Quaternion.identity;

            JoziPlayerController movement = player.AddComponent<JoziPlayerController>();
            SetPrivateField(movement, "cameraPivot", pivot.transform);
            return player;
        }

        static GameObject CapsulePart(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }

        static Transform Limb(string name, Transform parent, Vector3 localPosition, Material material)
        {
            GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            limb.name = name;
            limb.transform.SetParent(parent);
            limb.transform.localPosition = localPosition;
            limb.transform.localScale = new Vector3(0.12f, 0.42f, 0.12f);
            limb.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(limb.GetComponent<Collider>());
            return limb.transform;
        }

        static GameObject CreateFinish(Material marker)
        {
            GameObject finish = Cylinder("Maboneng Finish Marker", new Vector3(0f, 1.1f, 54f), new Vector3(4f, 1.1f, 4f), marker);
            finish.GetComponent<Collider>().isTrigger = true;
            Cube("Maboneng Sign", new Vector3(0f, 5f, 57f), new Vector3(12f, 2.2f, 0.6f), marker);
            return finish;
        }

        static void CreateTokens(Material gold)
        {
            Vector3[] positions =
            {
                new Vector3(-18f, 1.4f, -30f),
                new Vector3(16f, 1.4f, -8f),
                new Vector3(-20f, 1.4f, 16f),
                new Vector3(22f, 1.4f, 28f),
                new Vector3(0f, 1.4f, 43f)
            };

            foreach (Vector3 position in positions)
            {
                GameObject token = Cylinder("Taxi Token", position, new Vector3(1.7f, 0.18f, 1.7f), gold);
                token.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                token.GetComponent<Collider>().isTrigger = true;
                token.AddComponent<JoziPickup>();
            }
        }

        static void CreateUi(JoziPlayerController playerController, Transform player, Transform finish)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            GameObject canvasObject = new GameObject("Mobile HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            TextMeshProUGUI hud = Text(canvasObject.transform, "HUD", "Taxi tokens: 0/5", 26, TextAlignmentOptions.Left);
            Anchor(hud.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(420f, 48f));

            TextMeshProUGUI message = Text(canvasObject.transform, "Message", "Collect 5 taxi tokens, then reach Maboneng.", 24, TextAlignmentOptions.Center);
            Anchor(message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(720f, 60f));

            GameObject joystick = Panel(canvasObject.transform, "Move Joystick", new Color(1f, 1f, 1f, 0.18f));
            RectTransform joystickRect = joystick.GetComponent<RectTransform>();
            Anchor(joystickRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(120f, 112f), new Vector2(170f, 170f));

            GameObject knob = Panel(joystick.transform, "Knob", new Color(1f, 1f, 1f, 0.55f));
            RectTransform knobRect = knob.GetComponent<RectTransform>();
            Anchor(knobRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 72f));

            JoziMobileJoystick mobileJoystick = joystick.AddComponent<JoziMobileJoystick>();
            SetPrivateField(mobileJoystick, "player", playerController);
            SetPrivateField(mobileJoystick, "knob", knobRect);

            GameObject lookPadObject = Panel(canvasObject.transform, "Look Pad", new Color(1f, 1f, 1f, 0.01f));
            RectTransform lookPadRect = lookPadObject.GetComponent<RectTransform>();
            Anchor(lookPadRect, new Vector2(0.42f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
            JoziLookPad lookPad = lookPadObject.AddComponent<JoziLookPad>();
            SetPrivateField(lookPad, "player", playerController);

            GameObject sprintObject = Panel(canvasObject.transform, "Sprint Button", new Color(1f, 0.74f, 0.18f, 0.78f));
            RectTransform sprintRect = sprintObject.GetComponent<RectTransform>();
            Anchor(sprintRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-108f, 105f), new Vector2(142f, 76f));
            Button sprint = sprintObject.AddComponent<Button>();
            TextMeshProUGUI sprintText = Text(sprintObject.transform, "Sprint Text", "SPRINT", 22, TextAlignmentOptions.Center);
            Anchor(sprintText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            SetPrivateField(playerController, "sprintButton", sprint);

            GameObject managerObject = new GameObject("Game Manager");
            JoziGameManager manager = managerObject.AddComponent<JoziGameManager>();
            SetPrivateField(manager, "hudText", hud);
            SetPrivateField(manager, "messageText", message);
            SetPrivateField(manager, "player", player);
            SetPrivateField(manager, "finishPoint", finish);
        }

        static void CreateLabels()
        {
            Label("PONTE", new Vector3(-34f, 28.5f, 40f), 5f);
            Label("HILLBROW", new Vector3(36f, 39f, 42f), 4f);
            Label("MABONENG", new Vector3(0f, 6.2f, 56.5f), 3.6f);
        }

        static TextMeshProUGUI Text(Transform parent, string name, string text, int size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        static void Label(string text, Vector3 position, float size)
        {
            GameObject label = new GameObject(text + " Label");
            TextMeshPro tmp = label.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            label.transform.position = position;
            label.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        static GameObject Panel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        static GameObject Cylinder(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        static GameObject Sphere(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        static Material Material(string name, Color color)
        {
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (material.shader == null)
            {
                material = new Material(Shader.Find("Standard"));
            }

            material.name = name;
            material.color = color;
            return material;
        }

        static void SetPrivateField<T>(Object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }
    }
}
