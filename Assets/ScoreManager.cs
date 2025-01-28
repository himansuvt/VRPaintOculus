using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public Collider[] sequentialColliders;
    private int currentColliderIndex = 0; 

    [Header("UI References")]
    public TMPro.TextMeshProUGUI scoreText;
    public TMPro.TextMeshProUGUI missesText;

    public BallMovement ball;
    private int score = 0;
    private int misses = 0;

    public void UpdateScoreUI()
    {
        scoreText.text = $"Score: {score}";
        missesText.text = $"Misses: {misses}";
    }

    private void Start()
    {
        UpdateScoreUI();
    }

    public void CheckGoal(Collider hitCollider)
    {
        if (currentColliderIndex < sequentialColliders.Length &&
            hitCollider == sequentialColliders[currentColliderIndex])
        {
            currentColliderIndex++;

            if (currentColliderIndex == sequentialColliders.Length)
            {
                HandleSuccess();
            }
        }
    }

    public void ResetSequence()
    {
        currentColliderIndex = 0;
        ball.successTriggered = false;
    }

    public void HandleSuccess()
    {
        score++;
        ball.successTriggered = true;
        ball.StopAllCoroutines();
        UpdateScoreUI();
        ResetSequence();
    }

    public void HandleMiss()
    {
        misses++;
        UpdateScoreUI();
        ResetSequence();
        ball.successTriggered = false;
    }
}
