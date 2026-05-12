#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DebugDuo.EditorTools
{
    /// <summary>
    /// One-click finalize for FinishedScene. Applies visual polish (lighting + UI hover
    /// + camera follow) AND wires the Enemy Tile system (encounter components, visuals,
    /// question popup with a coding-question pool).
    ///
    /// Idempotent — safe to run multiple times. Skips anything already present.
    ///
    /// Does NOT touch tile cells, players, RunManager, command queues, ready system,
    /// movement logic, drag-and-drop, repeat/loop, or scene layout.
    /// </summary>
    public static class DebugDuoApplyPolish
    {
        private const string TargetSceneName = "FinishedScene";

        // ---------- coding question pool ----------

        private static readonly EnemyQuestion[] CodingQuestionPool = new[]
        {
            new EnemyQuestion {
                question = "What is a variable?",
                answers = new[] { "A place to store data", "A type of loop", "A button in Unity", "A sound effect" },
                correctAnswer = 1
            },
            new EnemyQuestion {
                question = "What does a loop do?",
                answers = new[] { "Plays music", "Repeats code multiple times", "Closes the program", "Opens a file" },
                correctAnswer = 2
            },
            new EnemyQuestion {
                question = "What is a function?",
                answers = new[] { "A type of variable", "A reusable block of code", "A game character", "A color" },
                correctAnswer = 2
            },
            new EnemyQuestion {
                question = "Which one is a programming language?",
                answers = new[] { "Photoshop", "Python", "Excel", "Word" },
                correctAnswer = 2
            },
            new EnemyQuestion {
                question = "What does an 'if' statement do?",
                answers = new[] { "Always runs the next line", "Asks the user a question", "Runs code only when a condition is true", "Loops forever" },
                correctAnswer = 3
            },
            new EnemyQuestion {
                question = "What is a 'bug' in programming?",
                answers = new[] { "A type of insect in the game", "A mistake in the code", "A new feature", "A user" },
                correctAnswer = 2
            },
            new EnemyQuestion {
                question = "What does it mean to 'compile' code?",
                answers = new[] { "Turn code into a runnable program", "Delete the code", "Save the file", "Print to the screen" },
                correctAnswer = 1
            },
            new EnemyQuestion {
                question = "Which symbol starts a single-line comment in C#?",
                answers = new[] { "#", "//", ";", "!" },
                correctAnswer = 2
            },
            new EnemyQuestion {
                question = "What is an array?",
                answers = new[] { "A single value", "A list of values stored together", "A button", "An error message" },
                correctAnswer = 2
            },
            new EnemyQuestion {
                question = "What does 'true' or 'false' represent?",
                answers = new[] { "Numbers", "Strings of text", "A boolean value", "A color" },
                correctAnswer = 3
            },
        };

        // ---------- entry point ----------

        [MenuItem("Debug Duo/Apply Polish to FinishedScene")]
        public static void ApplyToFinishedScene()
        {
            string path = ResolveScenePath(TargetSceneName);
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog(
                    "Debug Duo",
                    $"Could not find a scene asset named '{TargetSceneName}' in the project.",
                    "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Debug Duo", "Failed to open " + path, "OK");
                return;
            }

            int lightsRemoved   = ApplyLighting();
            int camerasPolished = ApplyCameraFollow();
            var ui              = ApplyUiPolish();
            var enemy           = WireEnemySystem();
            bool rulesAdded     = WireRulesPopup();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            string summary =
                $"Polish + enemy wiring applied to '{scene.name}':\n" +
                $"  Lighting: removed {lightsRemoved} pre-existing directional light(s); added Key + Fill; set Trilight ambient.\n" +
                $"  Camera follow: ensured on {camerasPolished} camera(s).\n" +
                $"  UI hover/click: {ui.hovers} hover, {ui.sounds} click-sound, {ui.transitions} transition→None on Selectables; {ui.blockHovers} hover on CommandBlocks.\n" +
                $"  Enemy system: {enemy.encountersAdded} EnemyEncounter(s) added, {enemy.visualsAdded} visual(s) added, " +
                $"question manager {(enemy.managerExisted ? "already present (pool refreshed)" : "created")}.\n" +
                $"  Rules popup: {(rulesAdded ? "built" : "already present")}.";

            Debug.Log("[DebugDuoApplyPolish] " + summary);
            EditorUtility.DisplayDialog("Debug Duo", summary, "OK");
        }

        // ---------- scene path ----------

        private static string ResolveScenePath(string sceneName)
        {
            var guids = AssetDatabase.FindAssets(sceneName + " t:Scene");
            var paths = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => System.IO.Path.GetFileNameWithoutExtension(p) == sceneName)
                .ToArray();
            return paths.Length > 0 ? paths[0] : null;
        }

        // ---------- lighting ----------

        private static int ApplyLighting()
        {
            int removed = 0;
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l == null) continue;
                if (l.type != LightType.Directional) continue;
                Undo.DestroyObjectImmediate(l.gameObject);
                removed++;
            }

            var keyGO = new GameObject("Key Light");
            Undo.RegisterCreatedObjectUndo(keyGO, "Add Key Light");
            var key = keyGO.AddComponent<Light>();
            key.type             = LightType.Directional;
            key.color            = new Color(1.00f, 0.94f, 0.84f);
            key.intensity        = 1.15f;
            key.shadows          = LightShadows.Soft;
            key.shadowStrength   = 0.65f;
            key.shadowBias       = 0.05f;
            key.shadowNormalBias = 0.4f;
            keyGO.transform.rotation = Quaternion.Euler(38f, -42f, 0f);

            var fillGO = new GameObject("Fill Light");
            Undo.RegisterCreatedObjectUndo(fillGO, "Add Fill Light");
            var fill = fillGO.AddComponent<Light>();
            fill.type      = LightType.Directional;
            fill.color     = new Color(0.70f, 0.82f, 1.00f);
            fill.intensity = 0.35f;
            fill.shadows   = LightShadows.None;
            fillGO.transform.rotation = Quaternion.Euler(20f, 138f, 0f);

            RenderSettings.ambientMode         = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.85f, 0.78f, 0.65f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.52f, 0.48f);
            RenderSettings.ambientGroundColor  = new Color(0.18f, 0.20f, 0.25f);
            RenderSettings.ambientIntensity    = 1.0f;

            return removed;
        }

        // ---------- camera ----------

        private static int ApplyCameraFollow()
        {
            int polished = 0;
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                if (cam == null) continue;
                if (cam.GetComponent<CameraMouseFollow>() != null) continue;
                Undo.AddComponent<CameraMouseFollow>(cam.gameObject);
                polished++;
            }
            return polished;
        }

        // ---------- UI polish ----------

        private struct UiPolishResult
        {
            public int hovers, sounds, transitions, blockHovers;
        }

        private static UiPolishResult ApplyUiPolish()
        {
            var r = new UiPolishResult();

            // Every Selectable: Button, Toggle, Slider, Scrollbar, Dropdown, etc.
            var selectables = Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None);
            foreach (var sel in selectables)
            {
                if (sel == null) continue;

                // UIButtonHover owns the visual, so disable the built-in transition.
                if (sel.transition != Selectable.Transition.None)
                {
                    Undo.RecordObject(sel, "Disable Selectable Transition");
                    sel.transition = Selectable.Transition.None;
                    r.transitions++;
                }

                if (sel.GetComponent<Image>() != null && sel.GetComponent<UIButtonHover>() == null)
                {
                    Undo.AddComponent<UIButtonHover>(sel.gameObject);
                    r.hovers++;
                }

                if (sel.GetComponent<UIButtonClickSound>() == null)
                {
                    Undo.AddComponent<UIButtonClickSound>(sel.gameObject);
                    r.sounds++;
                }
            }

            // CommandBlocks aren't Selectables, but they're clickable and should glow.
            var blocks = Object.FindObjectsByType<CommandBlockUI>(FindObjectsSortMode.None);
            foreach (var b in blocks)
            {
                if (b == null) continue;
                if (b.GetComponent<Image>() == null) continue;
                if (b.GetComponent<UIButtonHover>() != null) continue;
                Undo.AddComponent<UIButtonHover>(b.gameObject);
                r.blockHovers++;
            }

            return r;
        }

        // ---------- enemy system ----------

        private struct EnemyWireResult
        {
            public int encountersAdded;
            public int visualsAdded;
            public bool managerExisted;
        }

        private static EnemyWireResult WireEnemySystem()
        {
            var r = new EnemyWireResult();

            // 1. Per Enemy tile: ensure EnemyEncounter + visual + tile.enemy ref
            var tiles = Object.FindObjectsByType<Tile>(FindObjectsSortMode.None);
            foreach (var tile in tiles)
            {
                if (tile == null) continue;
                if (tile.Type != TileType.Enemy) continue;

                var enc = tile.GetComponent<EnemyEncounter>();
                if (enc == null)
                {
                    enc = Undo.AddComponent<EnemyEncounter>(tile.gameObject);
                    r.encountersAdded++;
                }

                Transform visual = tile.transform.Find("EnemyVisual");
                if (visual == null)
                {
                    var vGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    vGO.name = "EnemyVisual";
                    vGO.transform.SetParent(tile.transform, false);
                    vGO.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                    vGO.transform.localScale    = new Vector3(0.5f, 0.4f, 0.5f);
                    Object.DestroyImmediate(vGO.GetComponent<Collider>());
                    ApplyMaterial(vGO, new Color(0.85f, 0.15f, 0.25f));
                    Undo.RegisterCreatedObjectUndo(vGO, "Add Enemy Visual");
                    visual = vGO.transform;
                    r.visualsAdded++;
                }

                enc.SetVisual(visual.gameObject);

                // Wire the tile.enemy back-link the player movement uses.
                if (tile.enemy != enc)
                {
                    Undo.RecordObject(tile, "Wire Enemy Encounter");
                    tile.enemy = enc;
                    EditorUtility.SetDirty(tile);
                }
            }

            // 2. Ensure the question UI + manager exist on the screen-space canvas.
            var canvas = FindMainCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("[DebugDuoApplyPolish] No screen-space Canvas found — skipping question UI build.");
                return r;
            }

            var manager = canvas.GetComponent<EnemyQuestionManager>();
            if (manager != null) r.managerExisted = true;
            else
            {
                manager = Undo.AddComponent<EnemyQuestionManager>(canvas.gameObject);
                BuildQuestionPanel(canvas, manager);
            }

            // 3. Always (re)set the question pool — keeps it fresh on re-runs.
            Undo.RecordObject(manager, "Set Question Pool");
            manager.SetQuestionPool(CodingQuestionPool);
            EditorUtility.SetDirty(manager);

            return r;
        }

        // ---------- rules popup ----------

        private const string RulesBody =
            "Welcome to <b>Debug Duo</b>!\n" +
            "A 2-player co-op command puzzle game.\n\n" +
            "<b>How to play:</b>\n" +
            "•  Drag command blocks into each player's sequence.\n" +
            "•  Commands: Walk, Run, Walk Back, Turn Left, Turn Right, Wait, Press Button.\n" +
            "•  Use the <b>Repeat / Loop</b> toggle on a block to run it multiple times.\n" +
            "•  Both players must press <b>READY</b> before the run starts.\n" +
            "•  Press <b>RUN</b> to execute both sequences at the same time.\n" +
            "•  If the plan fails, fix your commands and try again.\n" +
            "•  Reach the <b>End Tiles</b> together to win.\n" +
            "•  <b>Enemy Tiles</b> trigger coding questions — answer correctly to pass.";

        private static bool WireRulesPopup()
        {
            var canvas = FindMainCanvas();
            if (canvas == null) return false;

            // Idempotent — keep the existing popup if there is one.
            if (canvas.GetComponent<RulesPopup>() != null) return false;

            // Backdrop covers the whole canvas and BLOCKS raycasts so nothing under it
            // is clickable until Start is pressed.
            var panel = NewUI(canvas.transform, "RulesPopup");
            Undo.RegisterCreatedObjectUndo(panel, "Add Rules Popup");
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.78f);
            // Make sure this draws above any other UI when active
            panel.transform.SetAsLastSibling();

            // Centered card
            var card = NewUI(panel.transform, "Card");
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(880f, 740f);
            var cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.13f, 0.14f, 0.17f, 0.98f);

            // Title
            var title = NewText(card.transform, "Title", "HOW TO PLAY",
                                 new Vector2(0f, 310f), new Vector2(840f, 60f), 38, TextAlignmentOptions.Center);
            title.color = new Color(1f, 0.9f, 0.55f);
            title.fontStyle = FontStyles.Bold;

            // Body — big block of bullet points
            var body = NewText(card.transform, "Body", RulesBody,
                                new Vector2(0f, -10f), new Vector2(820f, 540f), 22, TextAlignmentOptions.TopLeft);
            body.color = Color.white;
            body.enableWordWrapping = true;
            body.richText = true;

            // Start button
            var startBtn = NewButton(card.transform, "StartButton", "START",
                                      new Vector2(0f, -320f), new Vector2(280f, 70f), new Color(0.20f, 0.70f, 0.30f));

            var rules = Undo.AddComponent<RulesPopup>(canvas.gameObject);
            rules.SetReferences(panel, startBtn);

            // Visible at edit time too so designers see it; runtime Awake re-asserts.
            panel.SetActive(true);
            return true;
        }

        // ---------- canvas lookup ----------

        private static Canvas FindMainCanvas()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            // Prefer screen-space-overlay; fall back to first.
            foreach (var c in canvases)
                if (c != null && c.renderMode == RenderMode.ScreenSpaceOverlay) return c;
            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static void BuildQuestionPanel(Canvas canvas, EnemyQuestionManager manager)
        {
            // Full-screen backdrop
            var panel = NewUI(canvas.transform, "EnemyQuestionPanel");
            Undo.RegisterCreatedObjectUndo(panel, "Add Enemy Question Panel");
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            // Centered card
            var card = NewUI(panel.transform, "Card");
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(720f, 620f);
            var cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.13f, 0.14f, 0.17f, 0.98f);

            var qText = NewText(card.transform, "Question", "?", new Vector2(0f, 230f), new Vector2(680f, 110f), 28, TextAlignmentOptions.Center);
            qText.color = Color.white;

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

            manager.SetReferences(panel, qText, btns, lbls, feedback);
            EditorUtility.SetDirty(manager);
        }

        // ---------- UI primitives (mirrors DebugDuoSceneBuilder) ----------

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
