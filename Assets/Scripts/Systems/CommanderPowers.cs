using System.Collections;
using UnityEngine;

namespace VoidClash
{
    public enum CommanderPower { Airstrike, HealWave, Freeze }

    /// <summary>Player commander powers — Airstrike, Heal Wave, Freeze — on one shared cooldown,
    /// plus a race Overdrive that boosts the selected army. Player-only for now.</summary>
    public class CommanderPowers : MonoBehaviour
    {
        public const float PowerCooldown = 42f;
        public const float OverdriveCooldown = 26f;
        public const float OverdriveDuration = 8f;

        public const float StrikeRadius = 6.5f;
        public const float StrikeDamage = 55f;
        public const float HealRadius = 7f;
        public const float HealAmount = 90f;
        public const float FreezeRadius = 6.5f;
        public const float FreezeDuration = 5f;

        float _powerReadyAt;
        float _overdriveReadyAt;

        void Start()
        {
            // a short warm-up so powers aren't castable on the very first second
            _powerReadyAt = Time.time + 18f;
            _overdriveReadyAt = Time.time + 8f;
        }

        /// <summary>Test/debug hook: make all powers instantly castable.</summary>
        public void DebugMakeReady() { _powerReadyAt = 0f; _overdriveReadyAt = 0f; }

        public bool PowerReady => Time.time >= _powerReadyAt && (G.Game == null || !G.Game.IsOver);
        public float PowerCharge => Mathf.Clamp01(1f - (_powerReadyAt - Time.time) / PowerCooldown);
        public bool OverdriveReady => Time.time >= _overdriveReadyAt && (G.Game == null || !G.Game.IsOver);
        public float OverdriveCharge => Mathf.Clamp01(1f - (_overdriveReadyAt - Time.time) / OverdriveCooldown);

        /// <summary>Casts a targeted commander power. Returns true if it fired.</summary>
        public bool CastPower(CommanderPower power, Vector3 point)
        {
            if (!PowerReady) { Deny("Commander power is recharging"); return false; }
            _powerReadyAt = Time.time + PowerCooldown;
            switch (power)
            {
                case CommanderPower.Airstrike: StartCoroutine(AirstrikeRoutine(point)); break;
                case CommanderPower.HealWave: HealWave(point); break;
                case CommanderPower.Freeze: FreezeArea(point); break;
            }
            return true;
        }

        IEnumerator AirstrikeRoutine(Vector3 point)
        {
            if (G.Hud != null) G.Hud.Notify("Airstrike inbound!");
            if (G.Effects != null) G.Effects.SpawnMoveMarker(point, true);
            yield return new WaitForSeconds(1.3f);

            for (int i = 0; i < 6; i++)
            {
                Vector3 p = point + Random.insideUnitSphere * StrikeRadius; p.y = 0f;
                if (G.Effects != null) G.Effects.SpawnExplosion(p + Vector3.up * 0.5f, 1.4f, Faction.Player);
                if (G.Audio != null) G.Audio.PlayAt("explosion", p, 0.6f);
                yield return new WaitForSeconds(0.1f);
            }
            foreach (var e in Entity.All)
                if (e != null && !e.IsDead && e.Faction == Faction.Enemy && e.Health != null
                    && Vector3.Distance(e.Position, point) <= StrikeRadius)
                    e.Health.TakeDamage(StrikeDamage, DamageClass.Siege, null);
        }

        void HealWave(Vector3 point)
        {
            if (G.Hud != null) G.Hud.Notify("Healing wave!");
            foreach (var e in Entity.All)
                if (e != null && !e.IsDead && e.Faction == Faction.Player && e.Health != null
                    && Vector3.Distance(e.Position, point) <= HealRadius)
                {
                    e.Health.Heal(HealAmount);
                    if (G.Effects != null) G.Effects.SpawnHealBurst(e.Position + Vector3.up * 0.6f);
                }
            if (G.Audio != null) G.Audio.Play("build_done", 0.5f);
        }

        void FreezeArea(Vector3 point)
        {
            if (G.Hud != null) G.Hud.Notify("Freeze ray!");
            foreach (var e in Entity.All)
                if (e is Unit u && !u.IsDead && u.Faction == Faction.Enemy
                    && Vector3.Distance(u.Position, point) <= FreezeRadius)
                    u.Freeze(FreezeDuration);
            if (G.Effects != null) G.Effects.SpawnFrostBurst(point);
            if (G.Audio != null) G.Audio.PlayAt("deposit", point, 0.6f);
        }

        /// <summary>Race Overdrive — boosts the currently selected player combat units.</summary>
        public bool TryOverdrive()
        {
            if (!OverdriveReady) { Deny("Overdrive is recharging"); return false; }
            int n = 0;
            foreach (var e in G.Selection.Selected)
                if (e is Unit u && u.Faction == Faction.Player && !u.IsDead && u.Data.canAttack && !(u is WorkerUnit))
                { u.ApplyOverdrive(OverdriveDuration); n++; }
            if (n == 0) { Deny("Select combat units to Overdrive"); return false; }
            _overdriveReadyAt = Time.time + OverdriveCooldown;
            if (G.Hud != null) G.Hud.Notify($"Overdrive! {n} units boosted for {OverdriveDuration:0}s");
            if (G.Audio != null) G.Audio.Play("train_done", 0.6f);
            return true;
        }

        static void Deny(string msg)
        {
            if (G.Hud != null) G.Hud.Notify(msg);
            if (G.Audio != null) G.Audio.Play("error", 0.5f);
        }
    }
}
