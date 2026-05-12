#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DebugDuo.EditorTools
{
    /// <summary>
    /// One-click builder for the tile-based Debug Duo MVP.
    /// Menu: Debug Duo > Build MVP Scene
    /// </summary>
    public static class DebugDuoSceneBuilder
    {
        // ---------------------------------------------------------------------
        // Level layout (10 wide x 3 tall):
        //   y=2:  A  .  .  .  D  .  .  .  .  GA
        //   y=1:  #  #  #  #  #  #  #  #  #  #
        //   y=0:  B  .  .  Bt .  .  .  .  .  GB
        // A=PlayerA start, B=PlayerB start, D=Door, Bt=Button, GA/GB=Goals
        // ---------------------------------------------------------------------

        private const int GridW = 10;
        private const int GridH = 3;

        private static readonly Color FloorColor   = new Color(0.30f, 0.32f, 0.36f);
        private static readonly Color FloorAltColor= new Color(0.25f, 0.27f, 0.31f);
        private static readonly Color WallColor    = new Color(0.45f, 0.45f, 0.50f);
        private static readonly Color DoorColor    = new Color(0.75f, 0.55f, 0.20f);
        private static readonly Color ButtonRed    = new Color(0.85f, 0.20f, 0.20f);
        private static readonly Color GoalYellow   = new Color(1.00f, 0.85f, 0.20f);
        private static readonly Color PlayerABlue  = new Color(0.30f, 0.60f, 1.00f);
        private static readonly Color PlayerBOrange= new Color(1.00f, 0.55f, 0.30f);

        private static TileGridManager grid;
        private static Transform tilesRoot;

        [MenuItem("Debug Duo/Build MVP Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLighting();
            CreateCamera();
            CreateGrid();
            CreateAudioManager();

            // Wall row separating the two lanes
            for (int x = 0; x < GridW; x++) CreateWallTile(new Vector2Int(x, 1));

            // Top lane (y=2): Player A — Starting, floors, Door, Enemy, floors, Goal
            CreateStartingTile(new Vector2Int(0, 2), PlayerSlot.PlayerA, Direction.East, PlayerABlue);
            for (int x = 1; x < 4; x++) CreateFloorTile(new Vector2Int(x, 2), FloorAltColor);
            DoorController door = CreateDoorTile(new Vector2Int(4, 2));
            CreateEnemyTile(new Vector2Int(5, 2), new EnemyQuestion {
                question = "What does 'Walk' mean?",
                answers = new[] { "ללכת", "לרוץ", "לקפוץ", "לעצור" },
                correctAnswer = 1
            });
            for (int x = 6; x < 9; x++) CreateFloorTile(new Vector2Int(x, 2), FloorAltColor);
            (GoalZone goalA, Renderer padA) = CreateGoalTile(new Vector2Int(9, 2));

            // Bottom lane (y=0): Player B — Starting, floors, Button, floor, Enemy, floors, Goal
            CreateStartingTile(new Vector2Int(0, 0), PlayerSlot.PlayerB, Direction.East, PlayerBOrange);
            for (int x = 1; x < 3; x++) CreateFloorTile(new Vector2Int(x, 0), FloorColor);
            ButtonTrigger btn = CreateButtonTile(new Vector2Int(3, 0), door);
            CreateFloorTile(new Vector2Int(4, 0), FloorColor);
            CreateEnemyTile(new Vector2Int(5, 0), new EnemyQuestion {
                question = "What does 'Run' mean?",
                answers = new[] { "ללכת", "לרוץ", "לקפוץ", "לעצור" },
                correctAnswer = 2
            });
            for (int x = 6; x < 9; x++) CreateFloorTile(new Vector2Int(x, 0), FloorColor);
            (GoalZone goalB, Renderer padB) = CreateGoalTile(new Vector2Int(9, 0));

            // Players — slot is identity. Spawn cell/facing is auto-applied at Awake by
            // TileGridManager.AssignPlayerSpawns reading the StartingTiles above.
            var playerA = CreatePlayer("PlayerA", PlayerSlot.PlayerA, new Vector2Int(0, 2), Direction.East, PlayerABlue);
            var playerB = CreatePlayer("PlayerB", PlayerSlot.PlayerB, new Vector2Int(0, 0), Direction.East, PlayerBOrange);

            // Link goals to expected players (using the goal pad renderer, not the floor)
            goalA.SetLinks(playerA, new Vector2Int(9, 2), padA);
            goalB.SetLinks(playerB, new Vector2Int(9, 0), padB);

            // UI
            var (canvas, uiRoots) = CreateUI(playerA, playerB);

            // RunManager
            var runMgrGO = new GameObject("RunManager");
            var runMgr = runMgrGO.AddComponent<RunManager>();
            runMgr.SetReferences(
                playerA, playerB,
                uiRoots.sequenceA, uiRoots.sequenceB,
                uiRoots.runButton, uiRoots.resetButton, uiRoots.statusText,
                new[] { door },
                new[] { btn });
            runMgr.SetReadyControls(uiRoots.readyButtonA, uiRoots.readyButtonB, uiRoots.readyLabelA, uiRoots.readyLabelB);

            // Save scene
            const string scenePath = "Assets/Scenes/DebugDuo.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorUtility.DisplayDialog("Debug Duo", "MVP scene built and saved at:\n" + scenePath + "\n\nPress Play to try it.", "OK");
        }

        // ---------------- Scene parts ----------------

        private static void CreateLighting()
        {
            // Warm key light at a low cinematic angle ("late afternoon" feel)
            var keyGO = new GameObject("Key Light");
            var key = keyGO.AddComponent<Light>();
            key.type           = LightType.Directional;
            key.color          = new Color(1.00f, 0.94f, 0.84f); // warm sun
            key.intensity      = 1.15f;
            key.shadows        = LightShadows.Soft;
            key.shadowStrength = 0.65f;
            key.shadowBias     = 0.05f;
            key.shadowNormalBias = 0.4f;
            keyGO.transform.rotation = Quaternion.Euler(38f, -42f, 0f);

            // Cool fill from the opposite side, no shadows — softens dark sides
            var fillGO = new GameObject("Fill Light");
            var fill = fillGO.AddComponent<Light>();
            fill.type      = LightType.Directional;
            fill.color     = new Color(0.70f, 0.82f, 1.00f); // sky bounce
            fill.intensity = 0.35f;
            fill.shadows   = LightShadows.None;
            fillGO.transform.rotation = Quaternion.Euler(20f, 138f, 0f);

            // Trilight ambient — warm dusk sky over neutral mid over slightly cool ground
            RenderSettings.ambientMode         = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.85f, 0.78f, 0.65f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.52f, 0.48f);
            RenderSettings.ambientGroundColor  = new Color(0.18f, 0.20f, 0.25f);
            RenderSettings.ambientIntensity    = 1.0f;
        }

        private static void CreateCamera()
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.12f, 0.15f);
            cam.fieldOfView = 50f;
            camGO.AddComponent<AudioListener>();

            // Position camera to view the 10x3 grid (tileSize=1, center at (4.5, 0, 1)).
            camGO.transform.position = new Vector3(4.5f, 7.5f, -3.5f);
            camGO.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

            // Subtle interactive offset based on mouse position
            camGO.AddComponent<CameraMouseFollow>();
        }

        private static void CreateGrid()
        {
            var go = new GameObject("TileGridManager");
            grid = go.AddComponent<TileGridManager>();
            // Component default is tileSize=1 (Unity default Move snap)
            tilesRoot = new GameObject("Tiles").transform;
            tilesRoot.SetParent(go.transform, false);
        }

        private static void CreateAudioManager()
        {
            // Centralized SFX + music. Drop AudioClips into the Inspector fields after the
            // scene is built; with no clips assigned the calls are silent no-ops.
            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
        }

        // Tile visuals sized for tileSize=1 (slight gap between tiles)
        private const float TileFootprint = 0.95f;

        private static Tile CreateFloorTile(Vector2Int cell, Color color)
        {
            var tile = NewTile(cell, TileType.Floor);
            CreateBox("Floor", tile.transform, new Vector3(0f, 0f, 0f), new Vector3(TileFootprint, 0.1f, TileFootprint), color);
            return tile;
        }

        private static Tile CreateWallTile(Vector2Int cell)
        {
            var tile = NewTile(cell, TileType.Wall);
            CreateBox("Wall", tile.transform, new Vector3(0f, 0.5f, 0f), new Vector3(TileFootprint, 1.0f, TileFootprint), WallColor);
            return tile;
        }

        private static DoorController CreateDoorTile(Vector2Int cell)
        {
            var tile = NewTile(cell, TileType.Door);
            CreateBox("Floor", tile.transform, new Vector3(0f, 0f, 0f), new Vector3(TileFootprint, 0.1f, TileFootprint), FloorAltColor);
            var visual = CreateBox("DoorVisual", tile.transform, new Vector3(0f, 0.75f, 0f), new Vector3(0.85f, 1.5f, 0.3f), DoorColor);
            var door = tile.gameObject.AddComponent<DoorController>();
            door.SetVisual(visual.transform);
            tile.door = door;
            return door;
        }

        private static ButtonTrigger CreateButtonTile(Vector2Int cell, DoorController linkedDoor)
        {
            var tile = NewTile(cell, TileType.Button);
            CreateBox("Floor", tile.transform, new Vector3(0f, 0f, 0f), new Vector3(TileFootprint, 0.1f, TileFootprint), FloorColor);
            var btnGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btnGO.name = "ButtonPad";
            btnGO.transform.SetParent(tile.transform, false);
            btnGO.transform.localPosition = new Vector3(0f, 0.13f, 0f);
            btnGO.transform.localScale = new Vector3(0.6f, 0.07f, 0.6f);
            Object.DestroyImmediate(btnGO.GetComponent<Collider>());
            ApplyMaterial(btnGO, ButtonRed);

            var btn = tile.gameObject.AddComponent<ButtonTrigger>();
            btn.SetLinks(linkedDoor, btnGO.GetComponent<Renderer>());
            tile.button = btn;
            return btn;
        }

        private static Tile CreateStartingTile(Vector2Int cell, PlayerSlot spawnPlayer, Direction startFacing, Color markerColor)
        {
            var tile = NewTile(cell, TileType.Starting);
            CreateBox("Floor",  tile.transform, new Vector3(0f, 0f,    0f), new Vector3(TileFootprint, 0.1f,  TileFootprint), FloorAltColor);
            CreateBox("Marker", tile.transform, new Vector3(0f, 0.06f, 0f), new Vector3(0.4f,          0.02f, 0.4f),          markerColor);
            tile.SetSpawn(spawnPlayer, startFacing);
            return tile;
        }

        private static EnemyEncounter CreateEnemyTile(Vector2Int cell, EnemyQuestion question)
        {
            var tile = NewTile(cell, TileType.Enemy);
            CreateBox("Floor", tile.transform, new Vector3(0f, 0f, 0f), new Vector3(TileFootprint, 0.1f, TileFootprint), FloorAltColor);

            var enemyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyVisual.name = "EnemyVisual";
            enemyVisual.transform.SetParent(tile.transform, false);
            enemyVisual.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            enemyVisual.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
            Object.DestroyImmediate(enemyVisual.GetComponent<Collider>());
            ApplyMaterial(enemyVisual, new Color(0.85f, 0.15f, 0.25f));

            var enc = tile.gameObject.AddComponent<EnemyEncounter>();
            enc.SetVisual(enemyVisual);
            enc.SetQuestion(question);
            tile.enemy = enc;
            return enc;
        }

        private static (GoalZone, Renderer) CreateGoalTile(Vector2Int cell)
        {
            var tile = NewTile(cell, TileType.Goal);
            CreateBox("Floor", tile.transform, new Vector3(0f, 0f, 0f), new Vector3(TileFootprint, 0.1f, TileFootprint), FloorAltColor);
            var pad = CreateBox("GoalPad", tile.transform, new Vector3(0f, 0.11f, 0f), new Vector3(0.85f, 0.05f, 0.85f), GoalYellow);
            var goal = tile.gameObject.AddComponent<GoalZone>();
            tile.goal = goal;
            return (goal, pad.GetComponent<Renderer>());
        }

        private static Tile NewTile(Vector2Int cell, TileType type)
        {
            var go = new GameObject($"Tile_{type}_{cell.x}_{cell.y}");
            go.transform.SetParent(tilesRoot, false);
            go.transform.position = grid.CellToWorld(cell);
            var tile = go.AddComponent<Tile>();
            tile.Init(cell, type);
            grid.Register(tile);
            return tile;
        }

        private static PlayerCommandExecutor CreatePlayer(string name, PlayerSlot slot, Vector2Int startCell, Direction startFacing, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            // Scale the capsule to fit within a 1-unit tile
            go.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            ApplyMaterial(go, color);
            var exec = go.AddComponent<PlayerCommandExecutor>();
            exec.SetSlot(slot);
            // Editor preview position; runtime spawn is reapplied by TileGridManager
            exec.SetStart(startCell, startFacing);

            // Add a small "nose" cube so facing direction is visible (in local capsule space)
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(go.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.4f, 0.85f);
            nose.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            ApplyMaterial(nose, Color.white);

            // Glow/trail behind the player while it hops between tiles
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.30f;
            trail.startWidth = 0.45f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.02f;
            trail.emitting = true;
            var trailShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (trailShader != null)
            {
                var trailMat = new Material(trailShader);
                MaterialColorHelper.Set(trailMat, color);
                trail.material = trailMat;
            }
            var c0 = color;        c0.a = 0.7f;
            var c1 = color;        c1.a = 0f;
            trail.startColor = c0;
            trail.endColor   = c1;

            return exec;
        }

        // ---------------- UI ----------------

        private struct UIRefs
        {
            public CommandSequenceArea sequenceA;
            public CommandSequenceArea sequenceB;
            public Button runButton;
            public Button resetButton;
            public TMP_Text statusText;
            public Button readyButtonA;
            public Button readyButtonB;
            public TMP_Text readyLabelA;
            public TMP_Text readyLabelB;
        }

        private static (Canvas canvas, UIRefs refs) CreateUI(PlayerCommandExecutor playerA, PlayerCommandExecutor playerB)
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // EventSystem (StandaloneInputModule works with activeInputHandler=Both)
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

            // Build per-player panels
            var (sequenceA, readyBtnA, readyLblA) = BuildPlayerPanel(canvasGO.transform, "PanelA", "PLAYER A", isLeft: true,  accent: PlayerABlue);
            var (sequenceB, readyBtnB, readyLblB) = BuildPlayerPanel(canvasGO.transform, "PanelB", "PLAYER B", isLeft: false, accent: PlayerBOrange);

            // Bottom bar: Run + Reset + Status
            var bottom = NewUI(canvasGO.transform, "BottomBar");
            var bottomRT = bottom.GetComponent<RectTransform>();
            bottomRT.anchorMin = new Vector2(0.5f, 0f);
            bottomRT.anchorMax = new Vector2(0.5f, 0f);
            bottomRT.pivot = new Vector2(0.5f, 0f);
            bottomRT.anchoredPosition = new Vector2(0f, 30f);
            bottomRT.sizeDelta = new Vector2(800f, 130f);

            var runBtn   = NewButton(bottom.transform, "RunButton",   "RUN",   new Vector2(-180f, 30f), new Vector2(240f, 65f), new Color(0.20f, 0.70f, 0.30f));
            var resetBtn = NewButton(bottom.transform, "ResetButton", "RESET", new Vector2( 180f, 30f), new Vector2(240f, 65f), new Color(0.70f, 0.30f, 0.30f));
            var status   = NewText(bottom.transform, "Status", "Drag commands into your sequence, then both press Ready.", new Vector2(0f, 100f), new Vector2(800f, 40f), 22, TextAlignmentOptions.Center);

            // Win panel (centered card so Reset stays clickable)
            var winPanel = NewUI(canvasGO.transform, "WinPanel");
            var winRT = winPanel.GetComponent<RectTransform>();
            winRT.anchorMin = new Vector2(0.5f, 0.5f);
            winRT.anchorMax = new Vector2(0.5f, 0.5f);
            winRT.pivot = new Vector2(0.5f, 0.5f);
            winRT.anchoredPosition = new Vector2(0f, 100f);
            winRT.sizeDelta = new Vector2(900f, 280f);
            var winBg = winPanel.AddComponent<Image>();
            winBg.color = new Color(0f, 0f, 0f, 0.85f);
            winBg.raycastTarget = false;
            var winText = NewText(winPanel.transform, "WinText", "YOU WIN!\nBoth players reached their goals.", Vector2.zero, new Vector2(880f, 260f), 56, TextAlignmentOptions.Center);
            winText.raycastTarget = false;
            winPanel.SetActive(false);

            var winUI = canvasGO.AddComponent<WinUI>();
            winUI.SetPanel(winPanel);

            // Enemy question popup
            BuildEnemyQuestionPanel(canvasGO);

            return (canvas, new UIRefs
            {
                sequenceA = sequenceA,
                sequenceB = sequenceB,
                runButton = runBtn,
                resetButton = resetBtn,
                statusText = status,
                readyButtonA = readyBtnA,
                readyButtonB = readyBtnB,
                readyLabelA = readyLblA,
                readyLabelB = readyLblB
            });
        }

        private static void BuildEnemyQuestionPanel(GameObject canvasGO)
        {
            // Full-canvas backdrop with a centered card
            var panel = NewUI(canvasGO.transform, "EnemyQuestionPanel");
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            var card = NewUI(panel.transform, "Card");
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(720f, 620f);
            var cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.13f, 0.14f, 0.17f, 0.98f);

            var qText = NewText(card.transform, "Question", "?", new Vector2(0f, 230f), new Vector2(680f, 110f), 28, TextAlignmentOptions.Center);
            qText.color = Color.white;

            // Four answer buttons stacked vertically
            var btns = new Button[4];
            var lbls = new TMP_Text[4];
            float topY = 100f, gap = 80f;
            for (int i = 0; i < 4; i++)
            {
                var b = NewButton(card.transform, $"Answer{i + 1}", "?", new Vector2(0f, topY - i * gap), new Vector2(560f, 64f), new Color(0.25f, 0.30f, 0.45f));
                btns[i] = b;
                lbls[i] = b.GetComponentInChildren<TMP_Text>();
            }

            var feedback = NewText(card.transform, "Feedback", "", new Vector2(0f, -260f), new Vector2(680f, 50f), 24, TextAlignmentOptions.Center);
            feedback.color = new Color(1f, 0.85f, 0.3f);

            panel.SetActive(false);

            var mgr = canvasGO.AddComponent<EnemyQuestionManager>();
            mgr.SetReferences(panel, qText, btns, lbls, feedback);
        }

        private static (CommandSequenceArea seq, Button readyBtn, TMP_Text readyLabel) BuildPlayerPanel(Transform parent, string name, string title, bool isLeft, Color accent)
        {
            var panel = NewUI(parent, name);
            var rt = panel.GetComponent<RectTransform>();
            float x = isLeft ? 0f : 1f;
            rt.anchorMin = new Vector2(x, 0.5f);
            rt.anchorMax = new Vector2(x, 0.5f);
            rt.pivot     = new Vector2(x, 0.5f);
            rt.anchoredPosition = new Vector2(isLeft ? 20f : -20f, 50f);
            rt.sizeDelta = new Vector2(260f, 880f);

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            // Title
            var titleText = NewText(panel.transform, "Title", title, new Vector2(0f, -30f), new Vector2(240f, 40f), 28, TextAlignmentOptions.Center);
            titleText.color = accent;

            // Palette label
            NewText(panel.transform, "PaletteLabel", "Palette (drag)", new Vector2(0f, -75f), new Vector2(240f, 28f), 18, TextAlignmentOptions.Center).color = new Color(1f,1f,1f,0.7f);

            // Palette content
            var paletteContent = NewUI(panel.transform, "PaletteContent");
            var paletteRT = paletteContent.GetComponent<RectTransform>();
            paletteRT.anchorMin = new Vector2(0.5f, 1f);
            paletteRT.anchorMax = new Vector2(0.5f, 1f);
            paletteRT.pivot = new Vector2(0.5f, 1f);
            paletteRT.anchoredPosition = new Vector2(0f, -100f);
            paletteRT.sizeDelta = new Vector2(240f, 360f);
            var paletteVLG = paletteContent.AddComponent<VerticalLayoutGroup>();
            paletteVLG.spacing = 6f;
            paletteVLG.childAlignment = TextAnchor.UpperCenter;
            paletteVLG.childControlWidth = false;
            paletteVLG.childControlHeight = false;
            paletteVLG.childForceExpandWidth = false;
            paletteVLG.childForceExpandHeight = false;

            var paletteCmds = new[] { CommandType.Walk, CommandType.Run, CommandType.WalkBack, CommandType.TurnLeft, CommandType.TurnRight, CommandType.Wait, CommandType.PressButton };
            for (int i = 0; i < paletteCmds.Length; i++)
            {
                CommandBlockUI.Create(paletteRT, paletteCmds[i], isPaletteSource: true, owner: null, color: accent, size: new Vector2(220f, 50f), fontSize: 20f);
            }

            // Sequence label
            NewText(panel.transform, "SeqLabel", "Sequence (drop here)", new Vector2(0f, -480f), new Vector2(240f, 28f), 18, TextAlignmentOptions.Center).color = new Color(1f,1f,1f,0.7f);

            // Sequence drop zone
            var seqDropZone = NewUI(panel.transform, "SequenceDropZone");
            var seqRT = seqDropZone.GetComponent<RectTransform>();
            seqRT.anchorMin = new Vector2(0.5f, 1f);
            seqRT.anchorMax = new Vector2(0.5f, 1f);
            seqRT.pivot = new Vector2(0.5f, 1f);
            seqRT.anchoredPosition = new Vector2(0f, -510f);
            seqRT.sizeDelta = new Vector2(240f, 340f);
            var seqBg = seqDropZone.AddComponent<Image>();
            seqBg.color = new Color(1f, 1f, 1f, 0.07f);

            var seqContentGO = NewUI(seqDropZone.transform, "Content");
            var seqContentRT = seqContentGO.GetComponent<RectTransform>();
            seqContentRT.anchorMin = new Vector2(0f, 1f);
            seqContentRT.anchorMax = new Vector2(1f, 1f);
            seqContentRT.pivot = new Vector2(0.5f, 1f);
            seqContentRT.anchoredPosition = new Vector2(0f, 0f);
            seqContentRT.offsetMin = new Vector2(8f, 0f);
            seqContentRT.offsetMax = new Vector2(-8f, 0f);
            seqContentRT.sizeDelta = new Vector2(0f, 340f);
            var seqVLG = seqContentGO.AddComponent<VerticalLayoutGroup>();
            seqVLG.spacing = 4f;
            seqVLG.padding = new RectOffset(0, 0, 6, 6);
            seqVLG.childAlignment = TextAnchor.UpperCenter;
            seqVLG.childControlWidth = false;
            seqVLG.childControlHeight = false;
            seqVLG.childForceExpandWidth = false;
            seqVLG.childForceExpandHeight = false;
            // Don't intercept drops — they should land on the parent dropzone
            var seqContentCG = seqContentGO.AddComponent<CanvasGroup>();
            seqContentCG.blocksRaycasts = true; // children (sequence blocks) need to receive clicks

            var hint = NewText(seqDropZone.transform, "EmptyHint", "Drop blocks here", new Vector2(0f, 0f), new Vector2(220f, 40f), 16, TextAlignmentOptions.Center);
            hint.color = new Color(1f, 1f, 1f, 0.4f);
            hint.raycastTarget = false;

            var seqArea = seqDropZone.AddComponent<CommandSequenceArea>();
            seqArea.Configure(seqContentRT, hint, new Vector2(220f, 44f), 18f, accent);

            // Clear sequence button
            var clearBtn = NewButton(panel.transform, "ClearSeq", "Clear Sequence", new Vector2(0f, -870f), new Vector2(220f, 40f), new Color(0.55f, 0.30f, 0.30f));
            clearBtn.onClick.AddListener(() => seqArea.ClearAll());

            // Ready button — anchored to panel bottom so it's reliably visible
            var readyGO = NewUI(panel.transform, "ReadyButton");
            var readyRT = readyGO.GetComponent<RectTransform>();
            readyRT.anchorMin = new Vector2(0.5f, 0f);
            readyRT.anchorMax = new Vector2(0.5f, 0f);
            readyRT.pivot     = new Vector2(0.5f, 0f);
            readyRT.anchoredPosition = new Vector2(0f, 8f);
            readyRT.sizeDelta = new Vector2(220f, 32f);
            var readyImg = readyGO.AddComponent<Image>();
            readyImg.color = new Color(0.45f, 0.45f, 0.50f);
            var readyBtn = readyGO.AddComponent<Button>();
            readyBtn.targetGraphic = readyImg;
            readyBtn.transition = Selectable.Transition.None;
            readyGO.AddComponent<UIButtonHover>();
            readyGO.AddComponent<UIButtonClickSound>();

            var readyLabelGO = NewUI(readyGO.transform, "Label");
            var readyLabelRT = readyLabelGO.GetComponent<RectTransform>();
            readyLabelRT.anchorMin = Vector2.zero;
            readyLabelRT.anchorMax = Vector2.one;
            readyLabelRT.offsetMin = Vector2.zero;
            readyLabelRT.offsetMax = Vector2.zero;
            var readyLabel = readyLabelGO.AddComponent<TextMeshProUGUI>();
            readyLabel.text = "NOT READY";
            readyLabel.alignment = TextAlignmentOptions.Center;
            readyLabel.fontSize = 18;
            readyLabel.color = Color.white;
            readyLabel.raycastTarget = false;

            return (seqArea, readyBtn, readyLabel);
        }

        // ---------------- UI primitives ----------------

        private static GameObject NewUI(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Button NewButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bg)
        {
            var go = NewUI(parent, name);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = bg;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            // We let UIButtonHover handle visual feedback so the base color is preserved
            btn.transition = Selectable.Transition.None;
            go.AddComponent<UIButtonHover>();
            go.AddComponent<UIButtonClickSound>();

            var labelGO = NewUI(go.transform, "Label");
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero; labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero; labelRT.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 22;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return btn;
        }

        private static TextMeshProUGUI NewText(Transform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
        {
            var go = NewUI(parent, name);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            return tmp;
        }

        // ---------------- 3D primitives ----------------

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            // Strip collider — we don't need physics for grid movement
            Object.DestroyImmediate(go.GetComponent<Collider>());
            ApplyMaterial(go, color);
            return go;
        }

        private static void ApplyMaterial(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            MaterialColorHelper.Set(mat, color);
            rend.sharedMaterial = mat;
        }
    }

}
#endif
