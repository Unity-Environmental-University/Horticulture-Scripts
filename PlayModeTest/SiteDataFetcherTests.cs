using System.Collections;
using System.Reflection;
using _project.Scripts.Classes;
using _project.Scripts.Core;
using _project.Scripts.Data;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace _project.Scripts.PlayModeTest
{
    public class SiteDataFetcherTests
    {
        private SiteDataFetcher _fetcher;
        private GameObject _root;
        private TextMeshProUGUI _textGui;

        [SetUp]
        public void Setup()
        {
            // Create Canvas and TextMeshProUGUI for output
            _root = new GameObject("TestRoot");
            var canvasGo = new GameObject("Canvas", typeof(Canvas));
            canvasGo.transform.SetParent(_root.transform);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(canvasGo.transform);
            _textGui = textGo.GetComponent<TextMeshProUGUI>();

            // Create SiteDataFetcher and inject text field
            var fetcherGo = new GameObject("SiteDataFetcher", typeof(SiteDataFetcher));
            fetcherGo.transform.SetParent(_root.transform);
            _fetcher = fetcherGo.GetComponent<SiteDataFetcher>();
            var plantField = typeof(SiteDataFetcher)
                .GetField("plantSummary", BindingFlags.Instance | BindingFlags.NonPublic);
            plantField?.SetValue(_fetcher, _textGui);
            var affField = typeof(SiteDataFetcher)
                .GetField("afflictionSummary", BindingFlags.Instance | BindingFlags.NonPublic);
            affField?.SetValue(_fetcher, _textGui);
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(_root);
        }

        [UnityTest]
        public IEnumerator SetPlant_UpdatesText()
        {
            _textGui.text = string.Empty;
            _fetcher.SetPlant(PlantType.Coleus);
            // wait for coroutine to fetch
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(string.IsNullOrEmpty(_textGui.text), "Plant text should be updated after SetPlant().");
        }

        [UnityTest]
        public IEnumerator SetAffliction_UpdatesText()
        {
            _textGui.text = string.Empty;
            var aff = new PlantAfflictions.MildewAffliction();
            _fetcher.SetAffliction(aff);
            // wait for coroutine to fetch
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(string.IsNullOrEmpty(_textGui.text),
                "Affliction text should be updated after SetAffliction().");
        }
    }
}