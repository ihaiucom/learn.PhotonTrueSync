using UnityEngine;
using UnityEngine.UI;
using TrueSync;

/**
* @brief Handles how game should end.
**/
public class GameEndHandler : TrueSyncBehaviour {

    /**
    * @brief Necessary score to win the game.
    **/
    public int winScore;

    /**
    * @brief UI Text to show result status (win/lose).
    **/
    public Text resultText;

    /**
    * @brief UI Panel containing result information.
    **/
    public GameObject resultScreen;

    /**
    * @brief Initial setup.
    **/
    void Start() {
        resultScreen.SetActive(false);
    }

    /**
    * @brief Called when a goal is scored.
    **/
    void GoalScored(GoalBehavior goal) {
        if (goal.score >= winScore) {            
            gameObject.SetActive(false);

            if (localOwner.Id == goal.owner.Id) {
                HandleWin();
            } else {
                HandleLoss();
            }
        }
    }

    /**
    * @brief Called when local player reaches {@link #winScore}.
    **/
    void HandleWin() {
        resultText.text = "YOU WIN";
        ShowMessageScreen();
    }

    /**
    * @brief Called when enemy player reaches {@link #winScore}.
    **/
    void HandleLoss() {
        resultText.text = "YOU LOSE";
        ShowMessageScreen();
    }

    /**
    * @brief Enables result screen panel.
    **/
    void ShowMessageScreen() {
        resultScreen.SetActive(true);
    }

}