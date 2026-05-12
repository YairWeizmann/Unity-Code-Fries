using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DebugDuo
{
    public class RunManager : MonoBehaviour
    {
        [Header("Players")]
        [SerializeField] private PlayerCommandExecutor playerA;
        [SerializeField] private PlayerCommandExecutor playerB;

        [Header("Sequences")]
        [SerializeField] private CommandSequenceArea sequenceA;
        [SerializeField] private CommandSequenceArea sequenceB;

        [Header("UI")]
        [SerializeField] private Button runButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Ready")]
        [SerializeField] private Button readyButtonA;
        [SerializeField] private Button readyButtonB;
        [SerializeField] private TMP_Text readyLabelA;
        [SerializeField] private TMP_Text readyLabelB;

        [Header("Resettable")]
        [SerializeField] private DoorController[] doors;
        [SerializeField] private ButtonTrigger[] buttons;

        private static readonly Color ReadyColor    = new Color(0.20f, 0.70f, 0.30f);
        private static readonly Color NotReadyColor = new Color(0.45f, 0.45f, 0.50f);

        private bool isRunning;
        private bool readyA;
        private bool readyB;

        public void SetReferences(PlayerCommandExecutor a, PlayerCommandExecutor b,
                                  CommandSequenceArea seqA, CommandSequenceArea seqB,
                                  Button run, Button reset, TMP_Text status,
                                  DoorController[] doors, ButtonTrigger[] buttons)
        {
            playerA = a; playerB = b;
            sequenceA = seqA; sequenceB = seqB;
            runButton = run; resetButton = reset; statusText = status;
            this.doors = doors; this.buttons = buttons;
        }

        public void SetReadyControls(Button btnA, Button btnB, TMP_Text labelA, TMP_Text labelB)
        {
            readyButtonA = btnA; readyButtonB = btnB;
            readyLabelA  = labelA; readyLabelB  = labelB;
        }

        private void Awake()
        {
            if (runButton    != null) runButton.onClick.AddListener(OnRun);
            if (resetButton  != null) resetButton.onClick.AddListener(OnReset);
            if (readyButtonA != null) readyButtonA.onClick.AddListener(() => ToggleReady(true));
            if (readyButtonB != null) readyButtonB.onClick.AddListener(() => ToggleReady(false));
        }

        private void Start()
        {
            UpdateReadyVisuals();
            UpdateRunInteractable();
        }

        private void Update()
        {
            if (isRunning)
            {
                GoalZone.EvaluateAll();
                if (GoalZone.AllReached()) WinUI.Show();
            }
        }

        private void ToggleReady(bool isPlayerA)
        {
            if (isRunning) return;
            if (isPlayerA) readyA = !readyA;
            else           readyB = !readyB;
            UpdateReadyVisuals();
            UpdateRunInteractable();
        }

        private void UpdateReadyVisuals()
        {
            ApplyReadyVisual(readyButtonA, readyLabelA, readyA);
            ApplyReadyVisual(readyButtonB, readyLabelB, readyB);
        }

        private static void ApplyReadyVisual(Button btn, TMP_Text label, bool ready)
        {
            if (btn != null)
            {
                var img = btn.GetComponent<Image>();
                if (img != null) img.color = ready ? ReadyColor : NotReadyColor;

                // Sync the hover script's base color so its hover/press math stays correct
                var hover = btn.GetComponent<UIButtonHover>();
                if (hover != null) hover.RefreshBaseColor();
            }
            if (label != null) label.text = ready ? "READY" : "NOT READY";
        }

        private void UpdateRunInteractable()
        {
            if (runButton != null) runButton.interactable = !isRunning && readyA && readyB;
        }

        private void OnRun()
        {
            if (isRunning) return;
            if (!(readyA && readyB)) return;
            StartCoroutine(RunBoth());
        }

        private IEnumerator RunBoth()
        {
            isRunning = true;
            SetStatus("Running...");
            SetButtonsInteractable(false);
            UpdateRunInteractable();

            var cmdsA = sequenceA != null ? sequenceA.GetCommands() : null;
            var cmdsB = sequenceB != null ? sequenceB.GetCommands() : null;

            Coroutine a = (playerA != null && cmdsA != null) ? StartCoroutine(playerA.RunSequence(cmdsA)) : null;
            Coroutine b = (playerB != null && cmdsB != null) ? StartCoroutine(playerB.RunSequence(cmdsB)) : null;
            if (a != null) yield return a;
            if (b != null) yield return b;

            GoalZone.EvaluateAll();
            isRunning = false;

            // Clear sequence panels (palette is untouched — different content roots)
            if (sequenceA != null) sequenceA.ClearAll();
            if (sequenceB != null) sequenceB.ClearAll();

            // Reset ready flags so players must re-ready for the next run
            readyA = false;
            readyB = false;
            UpdateReadyVisuals();

            SetButtonsInteractable(true);
            UpdateRunInteractable();

            SetStatus(GoalZone.AllReached()
                ? "Both reached the goal!"
                : "Done. Plan again — both players must press Ready before Run.");
        }

        private void OnReset()
        {
            if (isRunning) return;

            if (playerA != null) playerA.ResetToStart();
            if (playerB != null) playerB.ResetToStart();

            if (doors != null)
                for (int i = 0; i < doors.Length; i++) if (doors[i] != null) doors[i].ResetDoor();
            if (buttons != null)
                for (int i = 0; i < buttons.Length; i++) if (buttons[i] != null) buttons[i].ResetButton();

            GoalZone.ResetAll();
            WinUI.Hide();

            // Reset all enemy encounters in the scene (no manual wiring needed)
            var enemies = FindObjectsByType<EnemyEncounter>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++) if (enemies[i] != null) enemies[i].ResetEncounter();
            if (EnemyQuestionManager.Instance != null) EnemyQuestionManager.Instance.Close();

            // Reset also drops ready state
            readyA = false;
            readyB = false;
            UpdateReadyVisuals();
            UpdateRunInteractable();

            SetStatus("Ready. Drag commands into your sequence, then both press Ready.");
        }

        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }

        private void SetButtonsInteractable(bool value)
        {
            if (resetButton  != null) resetButton.interactable = value;
            if (readyButtonA != null) readyButtonA.interactable = value;
            if (readyButtonB != null) readyButtonB.interactable = value;
            // runButton interactability is managed by UpdateRunInteractable
        }
    }
}
