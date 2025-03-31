using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace _project.Scripts.Core
{
    #region Classes

    [Serializable]
    public class DiseaseCollection
    {
        public int day;
        public List<GameObject> aphid = new();
        public List<GameObject> mealy = new();
        public List<GameObject> mold = new();
        public List<GameObject> thrips = new();

        public DiseaseCollection(int day)
        {
            this.day = day;
        }

        public List<GameObject> AllDiseases()
        {
            var output = new List<GameObject>();
            output.AddRange(aphid);
            output.AddRange(mealy);
            output.AddRange(mold);
            output.AddRange(thrips);
            return output;
        }
    }

    #endregion

    public class ScriptedSpread : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI currentDayText;

        public int currentDay;
        public int nextDay;


        public List<DiseaseCollection> diseases = new()
        {
            new DiseaseCollection(1),
            new DiseaseCollection(5),
            new DiseaseCollection(10)
        };

        private void Start() => SpreadDay(currentDay);

        public void SpreadDay(int day)
        {
            if (day == 0) day = 1;
            UpdateDayText(day);
            var dayDiseases = diseases.Find(d => d.day == day);
            ActivatePlants(dayDiseases.AllDiseases());
            nextDay = currentDay switch
            {
                1 => 5,
                5 => 10,
                _ => nextDay
            };
        }

        private void UpdateDayText(int day)
        {
            currentDay = day;
            currentDayText.text = "Current Day: " + day;
        }

        private static void ActivatePlants(params List<GameObject>[] plantLists)
        {
            foreach (var plantList in plantLists)
            foreach (var plant in plantList.Where(plant => plant))
                plant.SetActive(true);
        }
    }
}