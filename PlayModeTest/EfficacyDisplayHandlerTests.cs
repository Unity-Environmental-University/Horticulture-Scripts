using System.Collections;
using System.Reflection;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.Handlers;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace _project.Scripts.PlayModeTest
{
    public class EfficacyDisplayHandlerTests
    {
        [UnityTest]
        public IEnumerator UpdateInfo_ShowsZeroWhenTreatmentCannotTreatAffliction()
        {
            var plantGo = new GameObject("Plant");
            var plant = plantGo.AddComponent<PlantController>();
            plant.PlantCard = new ColeusCard();

            var affliction = new PlantAfflictions.SpiderMitesAffliction();
            plant.AddAffliction(affliction);

            var displayGo = new GameObject("EfficacyDisplay");
            var efficacyText = displayGo.AddComponent<TextMeshPro>();
            var displayHandler = displayGo.AddComponent<EfficacyDisplayHandler>();

            typeof(EfficacyDisplayHandler)
                .GetField("efficacyText", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(displayHandler, efficacyText);

            displayHandler.SetPlant(plant);
            displayHandler.SetTreatment(new PlantAfflictions.FungicideTreatment());

            displayHandler.UpdateInfo();

            Assert.AreEqual("0%", efficacyText.text);

            Object.Destroy(displayGo);
            Object.Destroy(plantGo);

            yield return null;
        }

        [UnityTest]
        public IEnumerator UpdateInfo_DoesNotShowEfficacyForLadyBugs()
        {
            var plantGo = new GameObject("Plant");
            var plant = plantGo.AddComponent<PlantController>();
            plant.PlantCard = new ColeusCard();

            var affliction = new PlantAfflictions.SpiderMitesAffliction();
            plant.AddAffliction(affliction);

            var displayGo = new GameObject("EfficacyDisplay");
            var efficacyText = displayGo.AddComponent<TextMeshPro>();
            var displayHandler = displayGo.AddComponent<EfficacyDisplayHandler>();

            typeof(EfficacyDisplayHandler)
                .GetField("efficacyText", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(displayHandler, efficacyText);

            displayHandler.SetPlant(plant);
            displayHandler.SetTreatment(new PlantAfflictions.LadyBugs());

            displayHandler.UpdateInfo();

            Assert.AreEqual(string.Empty, efficacyText.text);

            Object.Destroy(displayGo);
            Object.Destroy(plantGo);

            yield return null;
        }
    }
}
