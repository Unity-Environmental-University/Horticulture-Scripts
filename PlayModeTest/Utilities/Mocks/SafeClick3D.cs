using _project.Scripts.Card_Core;

namespace _project.Scripts.PlayModeTest.Utilities.Mocks
{
    /// <summary>
    ///     Safe subclass of Click3D that disables Start logic for testing.
    ///     Prevents self-destruction checks and other initialization that requires scene setup.
    /// </summary>
    public class SafeClick3D : Click3D
    {
        // ReSharper disable once Unity.RedundantEventFunction
        private void Start()
        {
            // No-op to prevent self-destruction check from running
        }
    }
}