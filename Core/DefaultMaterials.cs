using UnityEngine;

namespace _project.Scripts.Core
{
    /// <summary>
    /// Provides simple fallback materials for runtime visuals.
    /// </summary>
    public static class DefaultMaterials
    {
        private static Material _white;

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
    }
}

