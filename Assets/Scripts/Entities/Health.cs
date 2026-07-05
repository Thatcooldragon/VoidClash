using System;
using UnityEngine;

namespace VoidClash
{
    public class Health : MonoBehaviour
    {
        public float Current { get; private set; }
        public int Max { get; private set; }
        public int Armor { get; private set; }
        public ArmorClass ArmorClass { get; private set; }
        public Entity Owner { get; private set; }

        /// <summary>(attacker) — attacker may be null.</summary>
        public event Action<Entity> Damaged;
        public event Action<Entity> Died;
        public event Action Changed;

        bool _dead;

        public void Init(Entity owner, int max, int armor, ArmorClass ac)
        {
            Owner = owner;
            Max = max;
            Current = max;
            Armor = armor;
            ArmorClass = ac;
            _dead = false;
        }

        public float Fraction => Max > 0 ? Mathf.Clamp01(Current / Max) : 0f;

        public void TakeDamage(float baseDamage, DamageClass dc, Entity attacker)
        {
            if (_dead) return;
            float dmg = DamageTable.Compute(baseDamage, dc, ArmorClass, Armor);
            Current -= dmg;
            Changed?.Invoke();
            Damaged?.Invoke(attacker);
            if (Current <= 0f)
            {
                _dead = true;
                Died?.Invoke(attacker);
            }
        }

        public void Heal(float amount)
        {
            if (_dead) return;
            Current = Mathf.Min(Max, Current + amount);
            Changed?.Invoke();
        }

        /// <summary>Used when a building finishes construction with scaled HP.</summary>
        public void SetCurrent(float value)
        {
            Current = Mathf.Clamp(value, 1f, Max);
            Changed?.Invoke();
        }
    }
}
