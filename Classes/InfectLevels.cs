using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _project.Scripts.Classes
{
    [Serializable]
    public class InfectLevel
    {
        [Serializable]
        public struct InfectData
        {
            public int infect;
            public int eggs;
        }

        private readonly Dictionary<string, InfectData> _bySource = new();

        // Totals across all sources
        public int InfectTotal => _bySource.Values.Sum(v => v.infect);
        public int EggTotal => _bySource.Values.Sum(v => v.eggs);

        // Snapshot for iteration/copying
        public IEnumerable<KeyValuePair<string, InfectData>> All => _bySource;

        private InfectData GetInfectData(string source)
        {
            return _bySource.GetValueOrDefault(source);
        }

        private void SetInfectData(string source, InfectData a)
        {
            _bySource[source] = a;
        }

        // Infect helpers
        public int GetInfect(string source) => GetInfectData(source).infect;
        public void SetInfect(string source, int amount)
        {
            var a = GetInfectData(source);
            a.infect = Mathf.Max(0, amount);
            SetInfectData(source, a);
        }
        public void AddInfect(string source, int delta)
        {
            var a = GetInfectData(source);
            a.infect = Mathf.Max(0, a.infect + delta);
            SetInfectData(source, a);
        }
        public void RemoveInfect(string source, int delta) => AddInfect(source, -delta);

        // Egg helpers
        public int GetEggs(string source) => GetInfectData(source).eggs;
        public void SetEggs(string source, int amount)
        {
            var a = GetInfectData(source);
            a.eggs = Mathf.Max(0, amount);
            SetInfectData(source, a);
        }
        public void AddEggs(string source, int delta)
        {
            var a = GetInfectData(source);
            a.eggs = Mathf.Max(0, a.eggs + delta);
            SetInfectData(source, a);
        }
        public void RemoveEggs(string source, int delta) => AddEggs(source, -delta);

        public void ClearSource(string source)
        {
            _bySource.Remove(source);
        }

        public void ClearAll()
        {
            _bySource.Clear();
        }

        public InfectLevel Clone()
        {
            var clone = new InfectLevel();
            foreach (var kvp in _bySource) clone._bySource[kvp.Key] = kvp.Value;
            return clone;
        }
    }
}
