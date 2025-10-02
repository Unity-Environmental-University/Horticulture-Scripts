using System;
using _project.Scripts.GameState;
using UnityEngine;

namespace _project.Scripts.Core
{
    public class SaveManager : MonoBehaviour
    {
        public void Save()
        {
            try
            {
                GameStateManager.SaveGame();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
            }
        }

        public void Load()
        {
            try
            {
                GameStateManager.LoadGame();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
            }
        }
    }
}