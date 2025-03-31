using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace _project.Scripts.Data
{
    public class CaptureFrameRate : MonoBehaviour
    {
        private const float FrameReportingInterval = 60f; // 60fps
        private float _cumulativeFrameTime;
        private int _frameCount;

        private void Start()
        {
            StartCoroutine(ReportFrameRate());
        }

        private void Update()
        {
            _cumulativeFrameTime += Time.unscaledDeltaTime;
            _frameCount++;
        } // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator ReportFrameRate()
        {
            while (true)
            {
                yield return new WaitForSeconds(FrameReportingInterval);
                if (_frameCount <= 0) continue;
                var averageFPS = _cumulativeFrameTime / _frameCount;
                Debug.LogWarning("Average Frame Rate: " + averageFPS);

                Analytics.CustomEvent("averageFrameRate", new Dictionary<string, object>
                {
                    { "avg_fps", averageFPS }
                });

                Debug.LogWarning("Average Frame Rate: " + averageFPS);

                _cumulativeFrameTime = 0f;
                _frameCount = 0;
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }
}