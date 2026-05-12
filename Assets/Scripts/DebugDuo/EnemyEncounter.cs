using UnityEngine;

namespace DebugDuo
{
    /// <summary>
    /// Lives on an Enemy Tile. Holds one multiple-choice question that the player must
    /// answer correctly to defeat the enemy. While not defeated, stepping onto the tile
    /// pauses the player's run and opens the question UI. Once defeated, the visual is
    /// hidden and the tile becomes a normal pass-through.
    /// </summary>
    [System.Serializable]
    public class EnemyQuestion
    {
        [TextArea(2, 4)]
        public string question = "What does 'Walk' mean?";

        [Tooltip("Exactly four answers — one of them must be the correct one (1..4).")]
        public string[] answers = new[] { "ללכת", "לרוץ", "לקפוץ", "לעצור" };

        [Range(1, 4), Tooltip("Index (1..4) of the correct answer in the list above.")]
        public int correctAnswer = 1;
    }

    public class EnemyEncounter : MonoBehaviour
    {
        [SerializeField] private EnemyQuestion question = new EnemyQuestion();
        [SerializeField] private GameObject enemyVisual;

        public bool IsDefeated { get; private set; }
        public EnemyQuestion Question => question;

        public void SetVisual(GameObject visual) => enemyVisual = visual;
        public void SetQuestion(EnemyQuestion q) => question = q;

        public void OnDefeated()
        {
            IsDefeated = true;
            if (enemyVisual != null) enemyVisual.SetActive(false);
            Debug.Log($"[EnemyEncounter] '{name}' defeated.");
        }

        public void ResetEncounter()
        {
            IsDefeated = false;
            if (enemyVisual != null) enemyVisual.SetActive(true);
        }
    }
}
