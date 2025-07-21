using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using _project.Scripts.Handlers;
using _project.Scripts.UI;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace _project.Scripts.Core
{
    #region Classes

    public enum ScriptFlags
    {
        Inspected,
        OpenedNotebook,
        TookPicture,
        OpenedGallery,
        DayCompleted,
        Roaming,
        HasInteract
    }

    #endregion

    public class ScriptedRobotManager : MonoBehaviour
    {
        [Header("Controllers and Manager")] 
        [SerializeField] private NotebookController notebookController;
        [SerializeField] private RobotController robotController;
        [SerializeField] private ScriptedSpread scriptedSpread;
        [SerializeField] private InteractWithRobot robotInteract;
        [SerializeField] private FPSController fpsController;
        [SerializeField] private PlantManager plantManager;
        [SerializeField] private PanelFade panelFade;

        [Header("Scene Assets")] 
        [SerializeField] private GameObject playerFront;
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject robot;
        [SerializeField] private GameObject stage2Plant;
        [SerializeField] private TextAsset dialogueCsv;

        private readonly List<string[]> _dialogueData = new();
        private readonly HashSet<ScriptFlags> _flags = new();
        private GameObject _lastSelectedPlant;
        private GameObject _robotStartPosition;

        public GameObject dialogBox;
        public GameObject playerStartPosition;
        public GameObject doomedPlant;
        public TextMeshProUGUI messageText;
        public Animation messageAnim;
        public AnimationClip closeAnim;
        public AnimationClip openAnim;
        public int currentDay;

        [Header("Toggles")] [Tooltip("Turn this ON to enable robot Story progression")]
        public bool scriptedRobot = true;

        [Tooltip("Enabling this prints debug logs to console. Enable for troubleshooting.")]
        public bool debugging;

        #region Startup

        private void Start()
        {
            // Validate the script components and make sure the robot is in scripted mode
            if (!Validate()) return;
            if (!scriptedRobot)
            {
                _flags.Add(ScriptFlags.DayCompleted); //temp
                return;
            }
        
            LoadDialogueCsv();
            StartCoroutine(BeginRobotScript());
        }

        private bool Validate()
        {
            // Get all instance fields, both public and private
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                // Check if the field is serialized (public or marked with [SerializeField])
                var isSerialized = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;

                if (!isSerialized) continue;
                // Get the value of the field
                var value = field.GetValue(this);

                // Check if the value is null or, for Unity objects if it is missing
                if (value != null && (value is not Object unityObject || unityObject)) continue;
                Debug.LogError($"Field '{field.Name}' is null or missing!");
                return false;
            }

            return true;
        }

        private void LoadDialogueCsv()
        {
            if (!dialogueCsv)
            {
                Debug.LogError("Dialogue CSV not assigned!");
                return;
            }

            _dialogueData.Clear();
            var lines = dialogueCsv.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parsedLine = new List<string>(line.Split(','));
                _dialogueData.Add(parsedLine.ToArray());
            }
        }

        #endregion

        #region DayOneStory

        private IEnumerator BeginRobotScript()
        {
            currentDay = 1;
            yield return new WaitForSeconds(5f);
            robotController.SetNewFocusTarget(player);
            robotController.GoToNewLocation(playerFront.transform.position);
            //audioManager.PlayAudio(dlg1);

            // Wait for the dlg to play, then move on
            // yield return new WaitForSeconds(dlg1.length + 0.5f);
            SetDialogueText(1, 1);
            OpenDialogueBox();
            yield return new WaitForSeconds(10f); // **TEMP** simulate length of dlg

            yield return CloseDialogueBox();

            yield return StageTwoScript();
        }

        private IEnumerator StageTwoScript()
        {
            // Find Plant and Lead Player to it
            robotController.SetNewFocusTarget(stage2Plant);
            robotController.GoToNewLocation(stage2Plant.transform.position);
            SetDialogueText(2, 1);
            OpenDialogueBox();

            yield return new WaitForSeconds(5); // Inspection Time for Robot
            // Turn to look at Player
            robotController.SetNewFocusTarget(player);
            robotInteract.interactDialogue = 7;
            SetFlag(ScriptFlags.HasInteract); // **TEMP** Set Inspection Flag

            yield return StageThreeScript();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator StageThreeScript()
        {
            // Player should open the notebook, Wait for a flag to continue
            yield return new WaitUntil(() => _flags.Contains(ScriptFlags.Inspected));
            if (debugging) Debug.Log("Inspected Object");
            SetDialogueText(3, 1);

            // Player should open the notebook, Wait for a flag to continue
            yield return new WaitUntil(() => _flags.Contains(ScriptFlags.OpenedNotebook));
            if (debugging) Debug.Log("Opened Notebook");
            SetDialogueText(4, 1);

            // Robot will say something like that's cool now lets take a picture
            yield return new WaitUntil(() => _flags.Contains(ScriptFlags.TookPicture));
            if (debugging) Debug.Log("Picture Taken");
            SetDialogueText(5, 1);

            // The Robot will tell the player to open the gallery and take a look
            yield return new WaitUntil(() => _flags.Contains(ScriptFlags.OpenedGallery));
            if (debugging) Debug.Log("Opened Gallery");
            SetDialogueText(6, 1);

            yield return new WaitForSeconds(10f);
            yield return CloseDialogueBox();
            StartCoroutine(StageFourScript());
        }

        // Something should call this, perhaps the end of day 1
        private IEnumerator StageFourScript()
        {
            robotController.SetNewFocusTarget(player);
            robotController.GoToNewLocation(playerFront.transform.position);
            // play some audio about the plant
            //yield return new WaitForSeconds(dlg1.length);
            yield return new WaitForSeconds(1);
            //TEMP
            SetFlag(ScriptFlags.Roaming);
            SetFlag(ScriptFlags.DayCompleted);
            StartCoroutine(RobotRoaming());
        }

        #endregion

        #region DayFiveStory

        private IEnumerator StageFiveScript()
        {
            var random = Random.Range(0, plantManager.CachedPlants.Count);
            var selectedPlant = plantManager.CachedPlants[random];
            ClearFlag(ScriptFlags.Roaming);
            robotController.SetNewFocusTarget(selectedPlant);
            robotController.GoToNewLocation(selectedPlant.transform.position);
            yield return new WaitUntil(() => robotController.HasReachedDestination());
            OpenDialogueBox();
            SetDialogueText(8, 1);
            yield return new WaitForSeconds(5f);
            SetDialogueText(9, 1);
            yield return new WaitForSeconds(5f);
            StartCoroutine(CloseDialogueBox());

            yield return StageSixScript();
        }

        private IEnumerator StageSixScript()
        {
            yield return new WaitForSeconds(10);
            robotController.SetNewFocusTarget(doomedPlant);
            robotController.GoToNewLocation(doomedPlant.transform.position);
            yield return new WaitUntil(() => robotController.HasReachedDestination());

            //SetDialogueText(10,1);
        }

        #endregion

        #region FlagHandlers

        private void ClearFlag(ScriptFlags flag)
        {
            _flags.Remove(flag);
        }

        public void SetFlag(ScriptFlags flag)
        {
            _flags.Add(flag);
            if (!debugging) return;
            foreach (var scriptFlags in _flags) Debug.Log(scriptFlags.ToString());
        }

        public bool CheckFlag(ScriptFlags flag)
        {
            return _flags.Contains(flag);
        }

        #endregion

        #region Dialogue Functions

        public void OpenDialogueBox()
        {
            if (!dialogBox.activeInHierarchy) dialogBox.SetActive(true);
            messageAnim.clip = openAnim;
            messageAnim.Play();
        }

        public IEnumerator CloseDialogueBox()
        {
            messageAnim.clip = closeAnim;
            messageAnim.Play();
            yield return new WaitForSeconds(messageAnim.clip.length);
            dialogBox.SetActive(false);
        }

        public void SetDialogueText(int row, int col)
        {
            if (row < 0 || row >= _dialogueData.Count)
            {
                Debug.LogError($"Row index {row} is out of range. Total rows: {_dialogueData.Count}");
                return;
            }

            if (col < 0 || col >= _dialogueData[row].Length)
            {
                Debug.LogError(
                    $"Column index {col} is out of range for row {row}. Columns in this row: {_dialogueData[row].Length}");
                return;
            }

            var dialogue = _dialogueData[row][col];

            // Trim surrounding quotes if present
            if (dialogue.StartsWith("\"") && dialogue.EndsWith("\"")) dialogue = dialogue.Substring(1, dialogue.Length - 2);

            messageText.text = dialogue;
        }

        #endregion

        #region ExtraFunctions

        private IEnumerator RobotRoaming()
        {
            while (CheckFlag(ScriptFlags.Roaming))
            {
                if (plantManager.CachedPlants.Count == 0) throw new Exception("No Plants!");

                GameObject randomPlant;
                do
                {
                    randomPlant = plantManager.CachedPlants[Random.Range(0, plantManager.CachedPlants.Count)];
                } while (randomPlant == _lastSelectedPlant && plantManager.CachedPlants.Count > 1);

                _lastSelectedPlant = randomPlant;

                robotController.SetNewFocusTarget(randomPlant);
                robotController.GoToNewLocation(randomPlant.transform.position);

                yield return new WaitUntil(() => robotController.HasReachedDestination());
                yield return new WaitForSeconds(Random.Range(3f, 10f));
            }
        }

        public void EndDay()
        {
            if (CheckFlag(ScriptFlags.DayCompleted))
            {
                EndDayRoutine();
            }
            else
            {
                if (debugging) Debug.LogWarning("Day not complete!");
            }
        }

        private void EndDayRoutine()
        {
            // Code that runs while the screen is off
            panelFade.OnScreenOff += () =>
            {
                // Apply treatments
                plantManager.TriggerPlantTreatments();
                // Clears notebook of actions
                foreach (var table in notebookController.treatmentTables) table.RemoveAllTreatments();
            
                // Reset Player Position
                playerStartPosition.transform.GetPositionAndRotation(out var position, out var rotation);
                player.transform.SetPositionAndRotation(position, rotation);
            };
        
            // Select action buttons to disable
            var actionsToDisable = notebookController.fieldNotesPages
                .SelectMany(page => page.days)
                .Where(day => day.day == currentDay.ToString())
                .SelectMany(day => day.actions.SelectMany(action => action.GetToggles()))
                .Where(action => action);

            foreach (var action in actionsToDisable) action.SetActive(false);
        
            fpsController.CloseNotebook();
        
            scriptedSpread.SpreadDay(scriptedSpread.nextDay);
            var fadeDelay = 0f;
            switch (currentDay)
            {
                case 1:
                    currentDay = 5;
                    fadeDelay = 0f;
                    StartCoroutine(StageFiveScript());
                    break;
                case 5:
                    currentDay = 10;
                    fadeDelay = 5f;
                    SetDialogueText(11, 1);
                    OpenDialogueBox();
                    break;
            }

            StartCoroutine(NextDayTransition(fadeDelay));

            // Select action buttons to enable
            var actionsToEnable = notebookController.fieldNotesPages
                .SelectMany(page => page.days)
                .Where(day => day.day == currentDay.ToString())
                .SelectMany(day => day.actions.SelectMany(action => action.GetToggles()))
                .Where(action => action);

            foreach (var action in actionsToEnable) action.SetActive(true);
        
            //ClearFlag(ScriptFlags.DayCompleted);
        }

        private IEnumerator NextDayTransition(float startDelay)
        {
            // Stop player
            fpsController.enabled = false;
            // Delay before starting fade
            yield return new WaitForSeconds(startDelay);

            // Starts fade
            panelFade.StartFade();
            // If the dialogue screen is showing, turn it off
            if (dialogBox.activeInHierarchy) dialogBox.SetActive(false);
        
            yield return new WaitForSeconds(panelFade.fadeSpeed + .5f);
            fpsController.enabled = true;
        }

        #endregion
    }
}