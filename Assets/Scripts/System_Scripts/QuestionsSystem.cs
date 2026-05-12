using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
public class QuestionsSystem : MonoBehaviour
{
    [Header("Question Fields")]
    private QuestionDataSO currentQuestion;
    private TileBehaviour activeQuestionTile;
    private bool answeredCorrectly = false;


    [Header("Editor View")]
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button [] questionButton = new Button[4];


    [Header("Singleton")]
    public static QuestionsSystem Instance;

    //Unity Methods
    private void Awake()
    {
        Instance = this;    
    }


    //Question System Logic
    public void showQuestion(QuestionDataSO tile_currentQuestion , TileBehaviour currentTile)
    {
        questionPanel.SetActive(true);
        questionText.text = tile_currentQuestion.question;

        currentQuestion = tile_currentQuestion;
        activeQuestionTile = currentTile;

        for (int i = 0; i < questionButton.Length; i++) //Assigning Answers to its Buttons Text
        {
            TextMeshProUGUI answerText;
            answerText = questionButton[i].GetComponentInChildren<TextMeshProUGUI>();

            answerText.text = currentQuestion.answers[i];
        }
    }

    private void ResetQuestion()
    {
        questionPanel.SetActive(false);
        questionText.text = null;

        currentQuestion = null;
        activeQuestionTile = null;

        for (int i = 0; i < questionButton.Length; i++) 
        {
            TextMeshProUGUI answerText;
            answerText = questionButton[i].GetComponentInChildren<TextMeshProUGUI>();

            answerText.text = "0";
        }
    }

    public void onAnswerQuestion(int currentButton_index)
    {
        int correctAnswer_index = currentQuestion.correctAnswerIndex;

        if(correctAnswer_index != currentButton_index)
        {
            Debug.Log("Not Correct");   
            return;
        }

        Debug.Log("Correct");

        if (activeQuestionTile != null)
        {
            activeQuestionTile.setAnsweredCorrectly(true);
            activeQuestionTile.TeleportToTile();
            ResetQuestion();

        }
        else
            Debug.Log("Not Found Tile");


    }

}
