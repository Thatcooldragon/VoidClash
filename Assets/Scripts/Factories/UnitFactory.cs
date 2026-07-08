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
            var visual = VisualFactory.BuildUnitVisual(go.transform, data.id, faction, data.bodyScale);
            if ((data.id == "bubble" || data.id == "poison_bubble") && visual != null)
                visual.gameObject.AddComponent<BubbleWobble>();
            else if ((data.id == "dot" || data.id == "dot_core" || data.id == "dot_giant") && visual != null)
                visual.gameObject.AddComponent<DotPulse>();
            else if (visual != null)
            {
                var motion = visual.gameObject.AddComponent<UnitIdleMotion>();
                if (data.flying) { motion.BobHeight = 0.12f; motion.Rate = 2.7f; motion.SwayDegrees = 3.5f; }
                else if (data.id == "heavy") { motion.BobHeight = 0.018f; motion.SwayDegrees = 0.7f; }
            }
            if (data.id == "dot_core" && visual != null)
                visual.gameObject.AddComponent<DotPowerRing>();
            if ((data.id == "dot_core" || data.id == "dot_giant") && visual != null)
                visual.gameObject.AddComponent<OrbitingDots>().MaterialName = "dots_orbit";
            if (visual != null && data.canAttack && visual.gameObject.GetComponent<AttackRecoil>() == null)
                visual.gameObject.AddComponent<AttackRecoil>().Distance = data.damageClass == DamageClass.Siege ? 0.32f : 0.16f;

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
            var visual = VisualFactory.BuildBuildingVisual(go.transform, data.id, faction);
            AttachBuildingAccents(visual);
            var b = go.AddComponent<Building>();
            b.Init(data, faction, preBuilt);
            if (!preBuilt && G.Audio != null && faction == Faction.Player) G.Audio.PlayAt("build_place", position);
            return b;
        }

        static void AttachBuildingAccents(Transform visual)
        {
            if (visual == null) return;
            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.sharedMaterial == null) continue;
                string mat = r.sharedMaterial.name.ToLowerInvariant();
                if (!(mat.Contains("accent") || mat.Contains("rally") || mat.Contains("crystal"))) continue;
                if (r.gameObject.GetComponent<WorldPulse>() == null)
                {
                    var pulse = r.gameObject.AddComponent<WorldPulse>();
                    pulse.Amount = 0.035f;
                    pulse.Rate = 2.1f;
                }
            }
        }
    }
}
