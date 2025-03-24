using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    // Scoring system
    private int currentScore = 0;            // Current player score

    // Rotation tracking
    private float rotationTextDuration = 2.0f;   // How long to show rotation text
    private float rotationTextTimer = 0f;
    private bool isShowingRotationText = false;

    // Combo system
    private int comboCount = 0;              // Current combo count
    private bool lastJumpHadRotation = false; // Did the last jump have a rotation reward
    private float comboTextDuration = 2.0f;   // How long to show combo text
    private float comboTextTimer = 0f;
    private bool isShowingComboText = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TextManager.Instance.SetTextFieldActive(TextType.rotation, false);
        TextManager.Instance.SetTextFieldActive(TextType.combo, false);

        TextManager.Instance.SetText(TextType.score, "Score: " + currentScore);

        // Initialize timers
        isShowingComboText = false;
    }

    // Add score to the currentScore and return the updated score
    public int AddScore(int score)
    {
        currentScore += score;
        TextManager.Instance.SetText(TextType.score, "Score: " + currentScore);

        return currentScore;
    }

    public void ResetText()
    {
        TextManager.Instance.SetTextFieldActive(TextType.rotation, false);
        isShowingRotationText = false;

        // Reset combo
        comboCount = 0;
        lastJumpHadRotation = false;
        TextManager.Instance.SetTextFieldActive(TextType.combo, false);

        isShowingComboText = false;

        // Reset score when all lives are lost
        currentScore = 0;
        TextManager.Instance.SetText(TextType.score, "Score: " + currentScore);
    }

    public void HandleScoreTimers()
    {
        // Rotation text timer
        if (isShowingRotationText)
        {
            rotationTextTimer += Time.fixedDeltaTime;
            if (rotationTextTimer >= rotationTextDuration)
            {
                TextManager.Instance.SetTextFieldActive(TextType.rotation, false);
                isShowingRotationText = false;
            }
        }

        // Combo text timer
        if (isShowingComboText)
        {
            comboTextTimer += Time.fixedDeltaTime;
            if (comboTextTimer >= comboTextDuration)
            {
                TextManager.Instance.SetTextFieldActive(TextType.combo, false);
                isShowingComboText = false;
            }
        }
    }

    public void ShowRotationText(string message)
    {
        TextManager.Instance.SetText(TextType.rotation, message);
        TextManager.Instance.SetTextFieldActive(TextType.rotation, true);
        isShowingRotationText = true;
        rotationTextTimer = 0f;
    }

    public void UpdateComboText()
    {
        if (comboCount > 1)
        {
            TextManager.Instance.SetText(TextType.combo, "COMBO x" + comboCount);
            TextManager.Instance.SetTextFieldActive(TextType.combo, true);
            isShowingComboText = true;
            comboTextTimer = 0f;
        }
        else
        {
            TextManager.Instance.SetTextFieldActive(TextType.combo, false);
            isShowingComboText = false;
        }
    }

    public void CalculateRotationRewards(float totalRotation)
    {
        // First rotation (270+ degrees counts as complete)
        if (totalRotation >= 270f)
        {
            int rotationScore = 0;
            string rotationMessage = "";

            // Third rotation (990 degrees = 270 + 360 + 360)
            if (totalRotation >= 990f)
            {
                rotationScore = 1000;
                rotationMessage = "TRIPLE 360!";
            }
            // Second rotation (630 degrees = 270 + 360)
            else if (totalRotation >= 630f)
            {
                rotationScore = 100;
                rotationMessage = "DOUBLE 360!";
            }
            // First rotation (270+ degrees)
            else
            {
                rotationScore = 10;
                rotationMessage = "360!";
            }

            // Increment combo if we had a rotation in the last jump
            if (lastJumpHadRotation)
            {
                comboCount++;
            }
            else
            {
                // First rotation in a new combo
                comboCount = 1;
            }

            // Apply combo multiplier to score
            int comboMultiplier = comboCount;
            int finalScore = rotationScore * comboMultiplier;

            // Add rotation score to total score
            currentScore += finalScore;
            TextManager.Instance.SetText(TextType.score, "Score: " + currentScore);

            // Update combo text
            UpdateComboText();

            // Show rotation text with combo info
            if (comboCount > 1)
            {
                rotationMessage += "\nCOMBO x" + comboCount + "!";
                rotationMessage += "\n+" + finalScore + " POINTS!";
            }
            else
            {
                rotationMessage += "\n+" + finalScore + " POINTS!";
            }

            // Show rotation text
            ShowRotationText(rotationMessage);

            // Mark that this jump had a rotation
            lastJumpHadRotation = true;
        }
        else
        {
            // No rotation this jump, break the combo
            comboCount = 0;
            lastJumpHadRotation = false;
            UpdateComboText(); // This will hide the combo text
        }

        // Reset rotation tracking
        totalRotation = 0f;
    }
}
