using System.Runtime.CompilerServices;
using UnityEngine;

public class TileBehaviour : MonoBehaviour
{
    [Header("Tile Fiields")]
    private Vector3 tilePosition;

    private bool isWalkedOn = false;
    private bool answeredCorrectly { get; set; } = false;
    [Header("Player Scripts")]
    private playerMovement playerMovementScript;

    [Header("Questions Scripts")]
    [SerializeField] QuestionDataSO tileQuestion;


    //Unity Functions
    private void Start()
    {
       getComponenets();
    }

    private void OnMouseDown()
    {
        Debug.Log("Clicked Tile");

        showQuestion();

      
        
    }

    public void TeleportToTile()
    {
        bool shouldTeleport = playerMovementScript != null && !isWalkedOn && answeredCorrectly;

        if (shouldTeleport)
        {
            playerMovementScript.teleportTile(this);
            isWalkedOn = true;
        }
    }


    //Helper Methods
    private void getComponenets()
    {
       tilePosition = transform.position;
       playerMovementScript = FindFirstObjectByType<playerMovement>();

       if (playerMovementScript == null)
       {
            Debug.LogError("PlayerMovement not found in scene!");
       }

    }

    public void setAnsweredCorrectly(bool newAnswer)
    {
        answeredCorrectly = newAnswer;
    }

    public Vector3 getCenter()
    {
        return tilePosition;
    }

    //Tile Logic
    private void showQuestion()
    {
        if(QuestionsSystem.Instance != null)
        {
            QuestionsSystem.Instance.showQuestion(tile_currentQuestion: tileQuestion, currentTile: this);
            return;
        }

        Debug.Log("Question System Not Found");


    }
}
