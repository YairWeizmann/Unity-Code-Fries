#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DebugDuo.EditorTools
{
    /// <summary>
    /// One-click generator for tile prefabs the level designer can drag into a scene.
    /// Each prefab has its Tile.type pre-set, so dropping it on Unity's grid just works —
    /// the Tile auto-computes its cell from world position.
    ///
    /// Cross-scene refs (DoorController.linkedDoor, GoalZone.expectedPlayer) are left null
    /// because they belong to specific scene instances; wire them up per scene.
    /// </summary>
    public static class DebugDuoTilePrefabs
    {
        private const string PrefabFolder = "Assets/Prefabs/DebugDuo";
        private const float TileFootprint = 0.95f; // matches DebugDuoSceneBuilder

        private static readonly Color FloorColor    = new Color(0.30f, 0.32f, 0.36f);
        private static readonly Color WallColor     = new Color(0.45f, 0.45f, 0.50f);
        private static readonly Color StartingColor = new Color(0.30f, 0.60f, 1.00f);
        private static readonly Color DoorColor     = new Color(0.75f, 0.55f, 0.20f);
        private static readonly Color ButtonRed     = new Color(0.85f, 0.20f, 0.20f);
        private static readonly Color GoalYellow    = new Color(1.00f, 0.85f, 0.20f);

        [MenuItem("Debug Duo/Create Tile Prefabs")]
        public static void CreateAll()
        {
            EnsureFolder();
            SavePrefab("FloorTile",    BuildFloorTile);
            SavePrefab("StartingTile", BuildStartingTile);
            SavePrefab("ObstacleTile", BuildObstacleTile);
            SavePrefab("EndTile",      BuildEndTile);
            SavePrefab("ButtonTile",   BuildButtonTile);
            SavePrefab("DoorTile",     BuildDoorTile);
            SavePrefab("EnemyTile",    BuildEnemyTile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "Debug Duo",
                "Tile prefabs created in:\n" + PrefabFolder +
                "\n\nDrag any of them into a scene that has a TileGridManager.\n" +
                "Snap to Unity's grid (default 1u Move snap) and Cell X/Y is auto-computed.",
                "OK");
        }

        // ---- folder + save helpers ----

        private static void EnsureFolder()
        {
            if (!Directory.Exists(PrefabFolder))
            {
                Directory.CreateDirectory(PrefabFolder);
                AssetDatabase.Refresh();
            }
        }

        private static void SavePrefab(string name, System.Func<GameObject> build)
        {
            var go = build();
            go.name = name;
            string path = $"{PrefabFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        // ---- per-tile builders ----

        private static GameObject NewTileGO(TileType type)
        {
            var go = new GameObject(type.ToString());
            var tile = go.AddComponent<Tile>();
            tile.SetType(type);
            return go;
        }

        private static GameObject BuildFloorTile()
        {
            var go = NewTileGO(TileType.Floor);
            AddBox(go.transform, "Floor", new Vector3(0f, 0f, 0f), new Vector3(TileFootprint, 0.1f, TileFootprint), FloorColor);
            return go;
        }

        private static GameObject BuildStartingTile()
        {
            var go = NewTileGO(TileType.Starting);
            AddBox(go.transform, "Floor",  new Vector3(0f, 0f,    0f), new Vector3(TileFootprint, 0.1f,  TileFootprint), FloorColor);
            AddBox(go.transform, "Marker", new Vector3(0f, 0.06f, 0f), new Vector3(0.4f,          0.02f, 0.4f),          StartingColor);
            return go;
        }

        private static GameObject BuildObstacleTile()
        {
            var go = NewTileGO(TileType.Wall);
            AddBox(go.transform, "Wall", new Vector3(0f, 0.5f, 0f), new Vector3(TileFootprint, 1.0f, TileFootprint), WallColor);
            return go;
        }

        private static GameObject BuildEndTile()
        {
            var go = NewTileGO(TileType.Goal);
            AddBox(go.transform, "Floor", new Vector3(0f, 0f, 0f), new Vector3(TileFootprint, 0.1f, TileFootprint), FloorColor);
            var pad = AddBox(go.transform, "GoalPad", new Vector3(0f, 0.11f, 0f), new Vector3(0.85f, 0.05f, 0.85f), GoalYellow);

            var goal = go.AddComponent<GoalZone>();
            // expectedPlayer is wired per-scene; leave null. cell is unused (we now check by Tile.Cell elsewhere if needed)
            goal.SetLinks(null, Vector2Int.zero, pad.GetComponent<Renderer>());
            go.GetComponent<Tile>().goal = goal;
            return go;
        }

        private static GameObject BuildButtonTile()
        {
            var go = NewTileGO(TileType.Button);
            AddBox(go.transform, "Floor", new Vector3(0f, 0f, 0f), new Vector3(TileFootprint, 0.1f, TileFootprint), FloorColor);

            var btnGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btnGO.name = "ButtonPad";
            btnGO.transform.SetParent(go.transform, false);
            btnGO.transform.localPosition = new Vector3(0f, 0.13f, 0f);
            btnGO.transform.localScale    = new Vector3(0.6f, 0.07f, 0.6f);
            Object.DestroyImmediate(btnGO.GetComponent<Collider>());
            ApplyMaterial(btnGO, ButtonRed);

            var btn = go.AddComponent<ButtonTrigger>();
            // linkedDoor is wired per-scene; leave null
            btn.SetLinks(null, btnGO.GetComponent<Renderer>());
            go.GetComponent<Tile>().button = btn;
            return go;
        }

        private static GameObject BuildDoorTile()
        {
            var go = NewTileGO(TileType.Door);
            AddBox(go.transform, "Floor", new Vector3(0f, 0f, 0f), new Vector3(TileFootprint, 0.1f, TileFootprint), FloorColor);
            var visual = AddBox(go.transform, "DoorVisual", new Vector3(0f, 0.75f, 0f), new Vector3(0.85f, 1.5f, 0.3f), DoorColor);

            var door = go.AddComponent<DoorController>();
            door.SetVisual(visual.transform);
            go.GetComponent<Tile>().door = door;
            return go;
        }

        private static GameObject BuildEnemyTile()
        {
            var go = NewTileGO(TileType.Enemy);
            AddBox(go.transform, "Floor", new Vector3(0f, 0f, 0f), new Vector3(TileFootprint, 0.1f, TileFootprint), FloorColor);

            // Enemy visual — red capsule sitting on the tile
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "EnemyVisual";
            enemy.transform.SetParent(go.transform, false);
            enemy.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            enemy.transform.localScale    = new Vector3(0.5f, 0.4f, 0.5f);
            Object.DestroyImmediate(enemy.GetComponent<Collider>());
            ApplyMaterial(enemy, new Color(0.85f, 0.15f, 0.25f));

            var enc = go.AddComponent<EnemyEncounter>();
            enc.SetVisual(enemy);
            // Default question — designer edits per-instance in the Inspector
            enc.SetQuestion(new EnemyQuestion
            {
                question = "What does 'Walk' mean?",
                answers = new[] { "ללכת", "לרוץ", "לקפוץ", "לעצור" },
                correctAnswer = 1
            });
            go.GetComponent<Tile>().enemy = enc;
            return go;
        }

        // ---- visual helpers ----

        private static GameObject AddBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
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
