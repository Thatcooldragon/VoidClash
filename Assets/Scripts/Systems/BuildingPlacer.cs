using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

namespace VoidClash
{
    /// <summary>Ghost-preview building placement with validity checking.
    /// Also used headlessly by the enemy AI via IsValidAt/PlaceAt.</summary>
    public class BuildingPlacer : MonoBehaviour
    {
        public bool IsActive { get; private set; }
        BuildingData _data;
        GameObject _ghost;
        readonly List<Renderer> _ghostRenderers = new List<Renderer>();
        readonly List<Renderer> _footprintRenderers = new List<Renderer>();
        bool _valid;
        int _blockMask;

        void Start()
        {
            _blockMask = LayerMask.GetMask("Units", "Buildings", "Minerals", "Default");
        }

        public bool Begin(BuildingData data)
        {
            if (data == null) return false;
            if (!G.PlayerBank.CanAfford(data.mineralCost))
            {
                if (G.Hud != null) G.Hud.Notify("Not enough minerals");
                if (G.Audio != null) G.Audio.Play("error");
                return false;
            }
            Cancel();
            _data = data;
            _ghost = new GameObject("PlacementGhost");
            VisualFactory.BuildBuildingVisual(_ghost.transform, data.id, Faction.Player);
            BuildFootprintGrid(data);
            _ghostRenderers.Clear();
            foreach (var r in _ghost.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (!_footprintRenderers.Contains(r)) _ghostRenderers.Add(r);
            }
            IsActive = true;
            if (G.Hud != null) G.Hud.Notify($"{data.displayName}: choose a build site");
            return true;
        }

        public void Cancel()
        {
            if (_ghost != null) Destroy(_ghost);
            _ghost = null;
            _data = null;
            IsActive = false;
        }

        /// <summary>Called every frame by InputController while in placement mode.</summary>
        public void Tick()
        {
            if (!IsActive) return;

            if (Input.GetMouseButtonDown(1)) { Cancel(); return; }

            Vector3 pos;
            var ray = G.Cam.Cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f, LayerMask.GetMask("Ground"))) pos = hit.point;
            else if (!G.Cam.ScreenToGround(Input.mousePosition, out pos)) return;
            pos = Snap(pos);
            _ghost.transform.position = pos;

            _valid = IsValidAt(_data, pos);
            var mat = MaterialLibrary.Get(_valid ? "ghost_valid" : "ghost_invalid");
            foreach (var r in _ghostRenderers) r.sharedMaterial = mat;
            foreach (var r in _footprintRenderers) r.sharedMaterial = mat;

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(0) && !overUI)
            {
                if (!_valid)
                {
                    if (G.Hud != null) G.Hud.Notify("Cannot build there");
                    if (G.Audio != null) G.Audio.Play("error");
                    return;
                }
                if (!G.PlayerBank.TrySpend(_data.mineralCost))
                {
                    if (G.Hud != null) G.Hud.Notify("Not enough minerals");
                    if (G.Audio != null) G.Audio.Play("error");
                    Cancel();
                    return;
                }
                var site = PlaceAt(_data, Faction.Player, pos);
                AssignSelectedWorker(site);
                bool keepPlacing = Input.GetKey(KeyCode.LeftShift);
                if (!keepPlacing) Cancel();
                else if (!G.PlayerBank.CanAfford(_data.mineralCost)) Cancel();
            }
        }

        static Vector3 Snap(Vector3 p)
        {
            float x = Mathf.Round(p.x), z = Mathf.Round(p.z);
            return new Vector3(x, MapBuilder.GroundHeight(x, z), z);
        }

        public static Vector3 SnapToBuildGrid(Vector3 p) => Snap(p);

        void BuildFootprintGrid(BuildingData data)
        {
            _footprintRenderers.Clear();

            int cellsX = Mathf.Max(1, Mathf.CeilToInt(data.sizeX));
            int cellsZ = Mathf.Max(1, Mathf.CeilToInt(data.sizeZ));
            float startX = -(cellsX - 1) * 0.5f;
            float startZ = -(cellsZ - 1) * 0.5f;

            var grid = new GameObject("FootprintGrid");
            grid.transform.SetParent(_ghost.transform, false);
            grid.transform.localPosition = new Vector3(0f, 0.035f, 0f);

            for (int x = 0; x < cellsX; x++)
                for (int z = 0; z < cellsZ; z++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = "FootprintTile";
                    var col = tile.GetComponent<Collider>();
                    if (col != null)
                    {
                        col.enabled = false;
                        Destroy(col);
                    }
                    tile.transform.SetParent(grid.transform, false);
                    tile.transform.localPosition = new Vector3(startX + x, 0f, startZ + z);
                    tile.transform.localScale = new Vector3(0.9f, 0.035f, 0.9f);
                    var renderer = tile.GetComponent<Renderer>();
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.sharedMaterial = MaterialLibrary.Get("ghost_valid");
                    _footprintRenderers.Add(renderer);
                }
        }

        public bool IsValidAt(BuildingData data, Vector3 pos)
        {
            if (G.Map == null || !G.Map.InBounds(pos, Mathf.Max(data.sizeX, data.sizeZ) * 0.5f + 1f))
                return false;

            // overlap vs units, buildings, minerals, cliffs/rocks
            var half = new Vector3(data.sizeX * 0.5f + 0.4f, 1.2f, data.sizeZ * 0.5f + 0.4f);
            if (Physics.CheckBox(pos + Vector3.up * 1.2f, half, Quaternion.identity, _blockMask))
                return false;

            // corners + center must be on the navmesh (i.e., open buildable ground)
            for (int i = 0; i < 5; i++)
            {
                Vector3 probe = pos;
                if (i == 1) probe += new Vector3(half.x, 0, half.z);
                else if (i == 2) probe += new Vector3(-half.x, 0, half.z);
                else if (i == 3) probe += new Vector3(half.x, 0, -half.z);
                else if (i == 4) probe += new Vector3(-half.x, 0, -half.z);
                if (!NavMesh.SamplePosition(probe, out _, 1.0f, NavMesh.AllAreas)) return false;
            }
            return true;
        }

        public Building PlaceAt(BuildingData data, Faction faction, Vector3 pos)
        {
            var b = BuildingFactory.Place(data, faction, pos, false);
            return b;
        }

        void AssignSelectedWorker(Building site)
        {
            // nearest selected worker builds; if none selected, nearest idle/harvesting worker
            WorkerUnit best = null;
            float bestD = float.MaxValue;
            foreach (var e in G.Selection.Selected)
                if (e is WorkerUnit w && w.Faction == Faction.Player)
                {
                    float d = (w.Position - site.Position).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = w; }
                }
            if (best == null)
            {
                foreach (var e in Entity.All)
                    if (e is WorkerUnit w && w.Faction == Faction.Player && !w.IsDead)
                    {
                        float d = (w.Position - site.Position).sqrMagnitude;
                        if (d < bestD) { bestD = d; best = w; }
                    }
            }
            if (best != null) best.CommandBuild(site);
            if (G.Audio != null) G.Audio.Play("build_place", 0.7f);
        }
    }
}
