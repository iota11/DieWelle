using UnityEngine;
public enum LevelState { Playing, GameOver }

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public LevelState currentState = LevelState.Playing;
    //public GameObject loseText;
    public SurfingController player;
    public CameraFollower cameraFollower;

    public void Awake() {
        Instance = this;
    }
    public void OnPlayerDied() {
        currentState = LevelState.GameOver;
        //loseText.SetActive(true);
        cameraFollower.Unfollow();
    }

    public void OnReplay() {
        currentState = LevelState.Playing;
        //loseText.SetActive(false);
        player.ResetPlayer();
    }
}
