using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DebugDuo
{
    /// <summary>
    /// Singleton-style controller for the enemy question popup. PlayerCommandExecutor
    /// calls Open(encounter) when stepping onto an active Enemy Tile. The panel stays
    /// open until the player picks the correct answer; wrong answers update the
    /// feedback text but keep the panel/encounter active.
    ///
    /// Only one encounter is active at a time. If a second player hits another enemy
    /// while a question is already open, that second call is ignored — the second
    /// player's run also halts (its own runInterrupted flag), so on the next Run the
    /// player will hit that enemy again and the question will open then.
    /// </summary>
    public class EnemyQuestionManager : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Button[] answerButtons; // 4 buttons, indices 0..3
        [SerializeField] private TMP_Text[] answerLabels; // labels parallel to answerButtons
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private float closeDelayOnCorrect = 1f;

        [Tooltip("Pool of questions. When non-empty, Open() picks one at random and uses it instead of the encounter's stored Question.")]
        [SerializeField] private List<EnemyQuestion> questionPool = new List<EnemyQuestion>();

        private static EnemyQuestionManager instance;
        public static EnemyQuestionManager Instance => instance;

        private EnemyEncounter active;
        // The question being shown for the current encounter. Locked when Open is called
        // so that wrong-answer retries keep the same question (re-rolling on retry would
        // be a worse UX — player would feel they're being penalized with new content).
        private EnemyQuestion currentQuestion;

        public bool IsOpen => active != null;

        public void SetQuestionPool(IList<EnemyQuestion> pool)
        {
            questionPool.Clear();
            if (pool != null) questionPool.AddRange(pool);
        }

        private void Awake()
        {
            instance = this;
            if (panel != null) panel.SetActive(false);
            if (feedbackText != null) feedbackText.text = "";

            // Wire onClick listeners at runtime. AddListener creates non-persistent
            // listeners which are NOT serialized into the scene, so editor-time wiring
            // (e.g. from the scene builder / apply menu) doesn't survive save/load.
            // Doing it here guarantees buttons respond every time the scene plays.
            WireAnswerListeners();
        }

        public void SetReferences(GameObject panel, TMP_Text questionText,
                                  Button[] answerButtons, TMP_Text[] answerLabels,
                                  TMP_Text feedbackText)
        {
            this.panel = panel;
            this.questionText = questionText;
            this.answerButtons = answerButtons;
            this.answerLabels = answerLabels;
            this.feedbackText = feedbackText;

            // If we're already running (Awake fired), refresh the listeners now —
            // edit-time callers don't need this branch (Awake will handle it).
            if (Application.isPlaying) WireAnswerListeners();
        }

        private void WireAnswerListeners()
        {
            if (answerButtons == null) return;
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int idx1Based = i + 1; // capture for the closure
                var btn = answerButtons[i];
                if (btn == null) continue;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnAnswer(idx1Based));
            }
        }

        public void Open(EnemyEncounter encounter)
        {
            if (active != null)
            {
                Debug.Log($"[EnemyQuestionManager] Already showing a question — ignoring new request from '{encounter.name}'.");
                return;
            }
            if (encounter == null) return;

            active = encounter;

            // Pick from the pool if one is configured; otherwise use the encounter's
            // own stored question. Either way it's locked into currentQuestion until
            // the encounter resolves, so wrong-answer retries see the same question.
            currentQuestion = (questionPool != null && questionPool.Count > 0)
                ? questionPool[Random.Range(0, questionPool.Count)]
                : encounter.Question;

            if (panel != null) panel.SetActive(true);
            if (questionText != null) questionText.text = currentQuestion.question;
            if (feedbackText != null) feedbackText.text = "";

            var answers = currentQuestion.answers;
            for (int i = 0; i < answerButtons.Length; i++)
            {
                bool hasAnswer = answers != null && i < answers.Length;
                if (answerButtons[i] != null) answerButtons[i].gameObject.SetActive(hasAnswer);
                if (hasAnswer && answerLabels != null && i < answerLabels.Length && answerLabels[i] != null)
                {
                    answerLabels[i].text = answers[i];
                }
            }

            Debug.Log($"[EnemyQuestionManager] Opened for '{encounter.name}': \"{currentQuestion.question}\"");
        }

        private void OnAnswer(int oneBasedIndex)
        {
            if (active == null) return;

            if (oneBasedIndex == currentQuestion.correctAnswer)
            {
                if (feedbackText != null) feedbackText.text = "Correct!";
                Debug.Log($"[EnemyQuestionManager] Correct answer for '{active.name}'.");
                active.OnDefeated();
                StartCoroutine(CloseAfterDelay(closeDelayOnCorrect));
            }
            else
            {
                if (feedbackText != null) feedbackText.text = "Wrong answer, try again.";
                Debug.Log($"[EnemyQuestionManager] Wrong answer ({oneBasedIndex}) for '{active.name}'.");
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFail();
            }
        }

        private IEnumerator CloseAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Close();
        }

        public void Close()
        {
            active = null;
            if (panel != null) panel.SetActive(false);
            if (feedbackText != null) feedbackText.text = "";
        }
    }
}
