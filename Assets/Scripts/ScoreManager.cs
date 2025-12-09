using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;
    // Scoring system
    private int currentScore = 0;            // Current player score
    private int highScore = 0;               // Historical high score
    private bool hasBeatenHighScore = false; // Flag to prevent multiple dragon triggers

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

    // Reward mode system (5 seconds, double score)
    private bool isRewardMode = false;
    private float rewardModeDuration = 5.0f;
    private float rewardModeTimer = 0f;

    // Milestone tracking (10000*n triggers Koi)
    private int lastMilestone = 0;            // Last triggered milestone

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        instance = this;
        TextManager.Instance.SetTextFieldActive(TextType.rotation, false);
        TextManager.Instance.SetTextFieldActive(TextType.combo, false);

        TextManager.Instance.SetText(TextType.score, "Score: " + currentScore);

        // Initialize timers
        isShowingComboText = false;

        // Load high score from PlayerPrefs
        highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    void Update()
    {
        // Handle reward mode timer
        if (isRewardMode)
        {
            rewardModeTimer += Time.deltaTime;
            if (rewardModeTimer >= rewardModeDuration)
            {
                EndRewardMode();
            }
        }
    }

    // Add score to the currentScore and return the updated score
    public int AddScore(int score)
    {
        // Apply reward mode multiplier if active
        if (isRewardMode)
        {
            score *= 2;
        }

        int previousScore = currentScore;
        currentScore += score;
        TextManager.Instance.SetText(TextType.score, "Score: " + currentScore);

        // Check for milestones (10000*n)
        CheckMilestones(previousScore, currentScore);

        // Check for high score
        CheckHighScore();

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

        // Reset reward mode
        if (isRewardMode)
        {
            EndRewardMode();
        }

        // Reset milestone tracking
        lastMilestone = 0;

        // Reset high score flag (for new session)
        hasBeatenHighScore = false;

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
        // Debug.Log($"<color=magenta>ShowRotationText called with message: '{message}'</color>");

        if (TextManager.Instance == null)
        {
            // Debug.LogError("TextManager.Instance is NULL in ShowRotationText!");
            return;
        }

        TextManager.Instance.SetText(TextType.rotation, message);
        TextManager.Instance.SetTextFieldActive(TextType.rotation, true);
        isShowingRotationText = true;
        rotationTextTimer = 0f;

        // Debug.Log("Rotation text should now be visible");
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

    /// <summary>
    /// New circle-based scoring system
    /// Called when player lands from a jump
    /// </summary>
    public void CalculateCircleScore(float circles)
    {
        // Debug.Log($"<color=cyan>CalculateCircleScore called with circles: {circles}</color>");

        // Lower threshold: 0.3 circles (about 108 degrees) to trigger scoring
        if (circles < 0.3f)
        {
            // Debug.Log($"<color=yellow>Circles {circles} < 0.3, not scoring</color>");
            return;
        }

        int fullCircles = Mathf.FloorToInt(circles);
        int scoreToAdd = 0;
        string displayText = "";
        bool triggerGodMode = false;

        // Debug.Log($"<color=green>Full circles: {fullCircles}</color>");

        if (fullCircles == 1)
        {
            // 1 circle: +100 points
            scoreToAdd = 100;
            displayText = "+100";
            // Debug.Log("1 circle detected: +100");
        }
        else if (fullCircles == 2)
        {
            // 2 circles: +300 points
            scoreToAdd = 300;
            displayText = "+300";
            // Debug.Log("2 circles detected: +300");
        }
        else if (fullCircles >= 3)
        {
            // 3+ circles: +circles^2 * 100 points
            scoreToAdd = fullCircles * fullCircles * 100;
            displayText = $"+{scoreToAdd}";
            triggerGodMode = true;
            // Debug.Log($"3+ circles detected: +{scoreToAdd}, will trigger God Mode");
        }

        // Show score without freeze
        StartCoroutine(ShowScoreWithoutFreeze(displayText, scoreToAdd, triggerGodMode));
    }

    /// <summary>
    /// Show score without freezing the scene
    /// </summary>
    private System.Collections.IEnumerator ShowScoreWithoutFreeze(string scoreText, int scoreToAdd, bool isGodMode)
    {
        // Check if TextManager exists
        if (TextManager.Instance == null)
        {
            yield break;
        }

        // Prepare display text
        string displayText;
        if (isGodMode)
        {
            // 3+ circles: show "拖鞋战神"
            displayText = "拖鞋战神\n" + scoreText;
        }
        else
        {
            // 1-2 circles: just show score
            displayText = scoreText;
        }

        TextManager.Instance.SetText(TextType.rotation, displayText);
        TextManager.Instance.SetTextFieldActive(TextType.rotation, true);

        // Add the score immediately
        AddScore(scoreToAdd);

        // Start reward mode only for 3+ circles (God Mode)
        if (isGodMode)
        {
            StartRewardMode();
        }

        // Keep showing text for 2 seconds
        yield return new WaitForSeconds(2f);
        TextManager.Instance.SetTextFieldActive(TextType.rotation, false);
    }

    // FREEZE SYSTEM DISABLED - Uncomment below if needed
    /*
    // Store frozen objects state for restoration
    private List<Rigidbody> frozenRigidbodies = new List<Rigidbody>();
    private List<Vector3> frozenVelocities = new List<Vector3>();
    private List<Vector3> frozenAngularVelocities = new List<Vector3>();
    private List<Animator> frozenAnimators = new List<Animator>();
    private List<float> frozenAnimatorSpeeds = new List<float>();
    private List<ParticleSystem> frozenParticles = new List<ParticleSystem>();

    private System.Collections.IEnumerator FreezeAndShowScore(string scoreText, int scoreToAdd, bool isGodMode)
    {
        if (TextManager.Instance == null)
        {
            yield break;
        }

        // Comprehensive freeze
        FreezeEverything();

        // Prepare display text
        string displayText;
        if (isGodMode)
        {
            displayText = "拖鞋战神\n" + scoreText;
        }
        else
        {
            displayText = scoreText;
        }

        TextManager.Instance.SetText(TextType.rotation, displayText);
        TextManager.Instance.SetTextFieldActive(TextType.rotation, true);

        // Wait for 1 second (real time, not affected by timeScale)
        yield return new WaitForSecondsRealtime(1f);

        // Unfreeze the scene
        UnfreezeEverything();

        // Add the score AFTER unfreezing
        AddScore(scoreToAdd);

        // Start reward mode only for 3+ circles (God Mode)
        if (isGodMode)
        {
            StartRewardMode();
        }

        // Keep showing text for a bit longer
        yield return new WaitForSeconds(2f);
        TextManager.Instance.SetTextFieldActive(TextType.rotation, false);
    }

    private void FreezeEverything()
    {
        frozenRigidbodies.Clear();
        frozenVelocities.Clear();
        frozenAngularVelocities.Clear();
        frozenAnimators.Clear();
        frozenAnimatorSpeeds.Clear();
        frozenParticles.Clear();

        Time.timeScale = 0f;

        Rigidbody[] allRigidbodies = FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in allRigidbodies)
        {
            if (rb != null && !rb.isKinematic)
            {
                frozenRigidbodies.Add(rb);
                frozenVelocities.Add(rb.linearVelocity);
                frozenAngularVelocities.Add(rb.angularVelocity);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        Animator[] allAnimators = FindObjectsOfType<Animator>();
        foreach (Animator animator in allAnimators)
        {
            if (animator != null && animator.enabled)
            {
                frozenAnimators.Add(animator);
                frozenAnimatorSpeeds.Add(animator.speed);
                animator.speed = 0f;
            }
        }

        ParticleSystem[] allParticles = FindObjectsOfType<ParticleSystem>();
        foreach (ParticleSystem ps in allParticles)
        {
            if (ps != null && ps.isPlaying)
            {
                frozenParticles.Add(ps);
                ps.Pause();
            }
        }

        AudioListener.pause = true;
    }

    private void UnfreezeEverything()
    {
        Time.timeScale = 1f;

        for (int i = 0; i < frozenRigidbodies.Count; i++)
        {
            if (frozenRigidbodies[i] != null)
            {
                frozenRigidbodies[i].isKinematic = false;
                frozenRigidbodies[i].linearVelocity = frozenVelocities[i];
                frozenRigidbodies[i].angularVelocity = frozenAngularVelocities[i];
            }
        }

        for (int i = 0; i < frozenAnimators.Count; i++)
        {
            if (frozenAnimators[i] != null)
            {
                frozenAnimators[i].speed = frozenAnimatorSpeeds[i];
            }
        }

        foreach (ParticleSystem ps in frozenParticles)
        {
            if (ps != null)
            {
                ps.Play();
            }
        }

        AudioListener.pause = false;

        frozenRigidbodies.Clear();
        frozenVelocities.Clear();
        frozenAngularVelocities.Clear();
        frozenAnimators.Clear();
        frozenAnimatorSpeeds.Clear();
        frozenParticles.Clear();
    }
    */

    /// <summary>
    /// Start reward mode: 5 seconds of double score
    /// </summary>
    private void StartRewardMode()
    {
        isRewardMode = true;
        rewardModeTimer = 0f;

        // Show reward mode indicator
        TextManager.Instance.SetText(TextType.combo, "奖励模式 x2");
        TextManager.Instance.SetTextFieldActive(TextType.combo, true);

        // Debug.Log("<color=yellow>Reward Mode Started! (5 seconds, 2x score)</color>");
    }

    /// <summary>
    /// End reward mode
    /// </summary>
    private void EndRewardMode()
    {
        isRewardMode = false;
        rewardModeTimer = 0f;

        // Hide reward mode indicator
        TextManager.Instance.SetTextFieldActive(TextType.combo, false);

        // Debug.Log("<color=yellow>Reward Mode Ended</color>");
    }

    /// <summary>
    /// Check if we've crossed any 10000*n milestones
    /// </summary>
    private void CheckMilestones(int previousScore, int newScore)
    {
        int previousMilestone = previousScore / 10000;
        int currentMilestone = newScore / 10000;

        // If we crossed a milestone
        if (currentMilestone > previousMilestone && currentMilestone > lastMilestone)
        {
            lastMilestone = currentMilestone;
            TriggerKoi();
        }
    }

    /// <summary>
    /// Trigger Koi event when reaching 10000*n milestone
    /// </summary>
    private void TriggerKoi()
    {
        // Debug.Log($"<color=orange>Koi Summoned at {currentScore} points!</color>");

        // Show Koi UI text
        StartCoroutine(ShowSpecialText("锦鲤", 3f));
    }

    /// <summary>
    /// Check if current score beats high score
    /// </summary>
    private void CheckHighScore()
    {
        if (currentScore > highScore && !hasBeatenHighScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();

            hasBeatenHighScore = true;
            TriggerDragon();
        }
    }

    /// <summary>
    /// Trigger Dragon event when beating high score
    /// </summary>
    private void TriggerDragon()
    {
        // Debug.Log($"<color=red>Dragon Summoned! New High Score: {currentScore}!</color>");

        // Show Dragon UI text
        StartCoroutine(ShowSpecialText("神龙", 3f));
    }

    /// <summary>
    /// Show special text (Koi or Dragon) for a duration
    /// </summary>
    private System.Collections.IEnumerator ShowSpecialText(string text, float duration)
    {
        // Use rotation text field for special messages
        TextManager.Instance.SetText(TextType.rotation, text);
        TextManager.Instance.SetTextFieldActive(TextType.rotation, true);

        yield return new WaitForSeconds(duration);

        TextManager.Instance.SetTextFieldActive(TextType.rotation, false);
    }
}
