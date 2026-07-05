using System;
using UnityEngine;

namespace VoidClash
{
    /// <summary>Minerals + supply for one faction.</summary>
    public class ResourceBank
    {
        public readonly Faction Faction;
        public int Minerals { get; private set; }
        public int SupplyUsed { get; private set; }
        public int SupplyCap { get; private set; }
        public const int MaxSupplyCap = 200;

        public event Action Changed;

        public ResourceBank(Faction faction, int startMinerals)
        {
            Faction = faction;
            Minerals = startMinerals;
        }

        public int SupplyLeft => SupplyCap - SupplyUsed;

        public bool CanAfford(int minerals, int supply = 0)
            => Minerals >= minerals && (supply == 0 || SupplyUsed + supply <= SupplyCap);

        public bool TrySpend(int minerals, int supply = 0)
        {
            if (!CanAfford(minerals, supply)) return false;
            Minerals -= minerals;
            SupplyUsed += supply;
            Changed?.Invoke();
            return true;
        }

        public void AddMinerals(int amount)
        {
            Minerals += amount;
            Changed?.Invoke();
        }

        public void Refund(int minerals, int supply = 0)
        {
            Minerals += minerals;
            SupplyUsed = Mathf.Max(0, SupplyUsed - supply);
            Changed?.Invoke();
        }

        public void ReleaseSupply(int supply)
        {
            SupplyUsed = Mathf.Max(0, SupplyUsed - supply);
            Changed?.Invoke();
        }

        public void AddSupplyCap(int amount)
        {
            SupplyCap = Mathf.Min(MaxSupplyCap, SupplyCap + amount);
            Changed?.Invoke();
        }

        public void RemoveSupplyCap(int amount)
        {
            SupplyCap = Mathf.Max(0, SupplyCap - amount);
            Changed?.Invoke();
        }
    }
}
