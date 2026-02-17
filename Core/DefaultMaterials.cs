using UnityEngine;

namespace _project.Scripts.Core
{
    /// <summary>
    ///     Provides simple fallback materials for runtime visuals.
    /// </summary>
    public static class DefaultMaterials
    {
        private static Material _white;

        private static Material _foilOverlay;

        public static Material White
        {
            get
            {
                if (_white) return _white;

                // Try to load a project-defined default first
                var res = Resources.Load<Material>("Materials/Cards/DefaultWhite");
                if (res)
                {
                    _white = res;
                    return _white;
                }

                // Fallback: create a basic white material at runtime
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                _white = new Material(shader) { color = Color.white };
                return _white;
            }
        }

        /// <summary>
        ///     The additive holographic material used by the foil card overlay quad.
        ///     Loads from Resources first; creates one at runtime from the shader if no asset exists.
        /// </summary>
        public static Material FoilOverlay
        {
            get
            {
                if (_foilOverlay) return _foilOverlay;

                var res = Resources.Load<Material>("Materials/Cards/FoilOverlay");
                if (res)
                {
                    _foilOverlay = res;
                    return _foilOverlay;
                }

                // Fallback: create material from the shader at runtime
                var shader = Shader.Find("Custom/FoilCard");
                if (shader == null)
                {
                    Debug.LogWarning("DefaultMaterials: Shader 'Custom/FoilCard' not found.");
                    return null;
                }

                _foilOverlay = new Material(shader);
                _foilOverlay.SetFloat("_RainbowSpeed", 1.0f);
                _foilOverlay.SetFloat("_SparkleIntensity", 0.5f);
                _foilOverlay.SetFloat("_FoilIntensity", 0.8f);
                _foilOverlay.SetFloat("_NoiseScale", 12.0f);
                return _foilOverlay;
            }
        }
    }
}