using UnityEngine;
using UnityEngine.UI;
using TrueSync;

/**
* @brief Manages what should happen when a goal is scored.
**/
public class GoalBehavior : TrueSyncBehaviour {

    /**
    * @brief Called when a goal is scored.
    **/
    [AddTracking]
    public int score;

    /**
    * @brief UI Text to show game score.
    **/
    public Text scoreText;

    /**
    * @brief An instance of {@link GameEndHandler}.
    **/
    public GameEndHandler gameEndHandler;

    /**
    * @brief AudioSource for sound effects.
    **/
    AudioSource audioSource;

    /**
    * @brief Initial setup.
    **/
    void Start() {
        audioSource = GetComponent<AudioSource>();
        scoreText.text = "0";
    }

    /**
    * @brief When a ball hits this goal the score is updated and a 'GoalScored' is triggered.
    **/
    public void OnSyncedTriggerEnter(TSCollision2D otherBody) {
        if (otherBody.gameObject.tag == "ball") {
            score++;
            UpdateScore();
            otherBody.gameObject.SendMessage("GoalScored");
            gameEndHandler.gameObject.SendMessage("GoalScored", this);
        }
    }

    /**
    * @brief Updates UI Text score and plays a sound effect.
    **/
    void UpdateScore() {
        audioSource.Play();   
        scoreText.text = score.ToString();
    }

}