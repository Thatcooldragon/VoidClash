using UnityEngine;
using UnityEngine.AI;

namespace VoidClash
{
    public static class UnitFactory
    {
        public static Unit Spawn(UnitData data, Faction faction, Vector3 position)
        {
            if (data == null) return null;
            if (NavMesh.SamplePosition(position, out var hit, 4f, NavMesh.AllAreas))
                position = hit.position;

            var go = new GameObject($"{faction}_{data.id}");
            go.transform.position = position;
            VisualFactory.BuildUnitVisual(go.transform, data.id, faction, data.bodyScale);

            Unit unit = data.isWorker ? go.AddComponent<WorkerUnit>() : go.AddComponent<Unit>();
            unit.Init(data, faction);
            if (unit.Agent != null) unit.Agent.Warp(position);
            return unit;
        }
    }

    public static class BuildingFactory
    {
        /// <summary>Places a building. preBuilt = instantly complete (used for starting bases).</summary>
        public static Building Place(BuildingData data, Faction faction, Vector3 position, bool preBuilt)
        {
            if (data == null) return null;
            var go = new GameObject($"{faction}_{data.id}");
            position.y = MapBuilder.GroundHeight(position.x, position.z) - 0.05f; // settle into terrain
            go.transform.position = position;
            VisualFactory.BuildBuildingVisual(go.transform, data.id, faction);
            var b = go.AddComponent<Building>();
            b.Init(data, faction, preBuilt);
            if (!preBuilt && G.Audio != null && faction == Faction.Player) G.Audio.PlayAt("build_place", position);
            return b;
        }
    }
}
