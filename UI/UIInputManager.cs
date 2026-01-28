using _project.Scripts.Card_Core;
using UnityEngine;

namespace _project.Scripts.UI
{
    /// <summary>
    ///     Reason codes for forcing UIInput state, bypassing the ownership model
    /// </summary>
    public enum ForcedStateReason
    {
        SceneTransition,
        CriticalError,
        GamePause,
        SaveLoad
    }

    /// <summary>
    ///     Centralized manager for UIInput state to prevent race conditions between systems.
    ///     Tracks ownership to ensure only the controlling system can disable UIInput.
    ///     All methods are null-safe and will log warnings if CardGameMaster.Instance
    ///     or uiInputModule is not available. Callers do not need to perform null checks.
    /// </summary>
    public static class UIInputManager
    {
        private static readonly object OwnershipLock = new();
        private static string _currentOwner;

        /// <summary>
        ///     Get current UIInput enabled state
        /// </summary>
        public static bool IsEnabled
        {
            get
            {
                lock (OwnershipLock)
                {
                    return CardGameMaster.Instance?.uiInputModule && CardGameMaster.Instance.uiInputModule.enabled;
                }
            }
        }

        /// <summary>
        ///     Get current owner of UIInput state
        /// </summary>
        public static string CurrentOwner
        {
            get
            {
                lock (OwnershipLock)
                {
                    return _currentOwner;
                }
            }
        }

        /// <summary>
        ///     Request to enable UIInput. Takes ownership of the UIInput state.
        /// </summary>
        /// <param name="owner">The requesting system identifier</param>
        /// <remarks>
        ///     IMPORTANT: Calling this method immediately transfers ownership to the new owner,
        ///     even if another system currently holds ownership. The previous owner will NOT
        ///     be notified of the ownership transfer. Previous owners attempting to disable
        ///     UIInput will be blocked and receive a warning log.
        ///     If you need to coordinate ownership transfer with cleanup, consider:
        ///     1. Call ReleaseOwnership() before the new owner calls RequestEnable()
        ///     2. Or ensure OnDisable() handlers check ownership before attempting to disable
        /// </remarks>
        public static void RequestEnable(string owner)
        {
            lock (OwnershipLock)
            {
                if (!CardGameMaster.Instance?.uiInputModule)
                {
                    Debug.LogWarning(
                        $"[UIInputManager] Cannot enable UIInput - uiInputModule not found. Requested by: {owner}");
                    return;
                }

                _currentOwner = owner;
                CardGameMaster.Instance.uiInputModule.enabled = true;
            }
        }

        /// <summary>
        ///     Request to disable UIInput. Only succeeds if the caller is the current owner or no owner is set.
        /// </summary>
        /// <param name="owner">The requesting system identifier</param>
        public static void RequestDisable(string owner)
        {
            lock (OwnershipLock)
            {
                if (!CardGameMaster.Instance?.uiInputModule)
                {
                    Debug.LogWarning(
                        $"[UIInputManager] Cannot disable UIInput - uiInputModule not found. Requested by: {owner}");
                    return;
                }

                // Only allow disabling if this system is the current owner or if no owner is set
                if (_currentOwner == owner || string.IsNullOrEmpty(_currentOwner))
                {
                    CardGameMaster.Instance.uiInputModule.enabled = false;
                    _currentOwner = null;
                }
                else
                {
                    Debug.LogWarning(
                        $"[UIInputManager] Cannot disable UIInput - owned by '{_currentOwner}', requested by '{owner}'");
                }
            }
        }

        /// <summary>
        ///     Force UIInput state regardless of ownership. Use sparingly for critical state recovery.
        /// </summary>
        /// <param name="enabled">Desired state</param>
        /// <param name="newOwner">New owner (only used if enabled is true)</param>
        /// <param name="reason">Reason for forcing state change</param>
        public static void ForceState(bool enabled, string newOwner, ForcedStateReason reason)
        {
            lock (OwnershipLock)
            {
                if (!CardGameMaster.Instance?.uiInputModule)
                {
                    Debug.LogWarning("[UIInputManager] Cannot force UIInput state - uiInputModule not found");
                    return;
                }

                CardGameMaster.Instance.uiInputModule.enabled = enabled;
                _currentOwner = enabled ? newOwner : null;
            }
        }

        /// <summary>
        ///     Release ownership without changing the enabled state. Useful for cleanup.
        /// </summary>
        /// <param name="owner">The owner releasing control</param>
        public static void ReleaseOwnership(string owner)
        {
            lock (OwnershipLock)
            {
                if (_currentOwner != owner) return;
                _currentOwner = null;
            }
        }
    }
}