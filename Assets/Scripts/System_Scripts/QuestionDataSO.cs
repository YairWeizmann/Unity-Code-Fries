using UnityEngine;


[CreateAssetMenu(fileName = "NewQuestion", menuName = "Game/Question")]
public class QuestionDataSO : ScriptableObject
{
    [Header("Question Variables")]
    public string question;

    public string[] answers; // size 4
    public int correctAnswerIndex; // 1-4

}
