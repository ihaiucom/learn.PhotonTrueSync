using UnityEngine;
using UnityEngine.UI;
using TrueSync;
using System.Collections.Generic;

/**
* @brief Manages paddle's behavior.
**/
public class PaddleController : TrueSyncBehaviour {

    /**
    * @brief Key to set/get paddle's movement from {@link TrueSyncInput}.
    **/
    private const byte INPUT_KEY_HORIZONTAL = 1;

    /**
    * @brief Margin to safe calculate margins.
    **/
    private const float MARGIN = 0.1f;

    /**
    * @brief Max allowed movement in X axis.
    **/
    public int maxX = 4;

    /**
    * @brief Movement speed in X axis.
    **/
    [AddTracking]
    public int speedX = 1;

    /**
    * @brief Prefab for a counter UI Text that be place on the top of a paddle.
    **/
    public GameObject paddleCountPrefab;

    /**
    * @brief Fast access to counter's UI Text.
    **/
    private Text paddleCountText;

    /**
    * @brief Score points.
    **/
    [AddTracking]
    private int score = 0;

    /**
    * @brief Keep a static reference to the paddles where the key is its position on screen (true for top side and false to bottom side).
    **/
    public static Dictionary<bool, PaddleController> paddlesBySide = new Dictionary<bool, PaddleController>();

    /**
    * @brief Initial setup when game is started.
    **/
    public override void OnSyncedStart() {
        paddleCountText = (Instantiate (paddleCountPrefab) as GameObject).GetComponent<Text>();
		paddleCountText.transform.SetParent(GameObject.Find ("Canvas").transform, false);

        // If paddle's owner is the first player then place it on top side
        if (owner.Id == 1) {
            Material redMatter = Resources.Load("RedMatter", typeof(Material)) as Material;
            GetComponent<MeshRenderer>().material = redMatter;

            tsRigidBody2D.position = new TSVector2(0, 8);
            // Adds this PaddleController in the {@link #paddlesBySide} dictionary with key being true (top size)
            paddlesBySide[true] = this;
        } else {
            tsRigidBody2D.position = new TSVector2(0, -8);
            // Adds this PaddleController in the {@link #paddlesBySide} dictionary with key being false (bottom size)
            paddlesBySide[false] = this;
        }
	}

    /**
    * @brief Updates counter position on screen to track paddle's position.
    **/
    void Update () {
        if (paddleCountText != null) {
            paddleCountText.transform.position = Camera.main.WorldToScreenPoint(transform.position);
        }
	}

    /**
    * @brief Sets player inputs.
    **/
    public override void OnSyncedInput() {		
		int input = 0;

		if (Input.GetKey(KeyCode.LeftArrow)) {
			input = -1;
		} else if (Input.GetKey(KeyCode.RightArrow)) {
			input = 1;
		}

        TrueSyncInput.SetInt(INPUT_KEY_HORIZONTAL, input);
	}

    /**
    * @brief Updates paddle's position.
    **/
    public override void OnSyncedUpdate() {
		int directionInput = TrueSyncInput.GetInt (INPUT_KEY_HORIZONTAL);

        TSVector2 currentPosition = tsRigidBody2D.position;

		if (directionInput < 0) {
            currentPosition.x -= speedX;
		} else if (directionInput > 0) {
            currentPosition.x += speedX;
        }

        if (FP.Abs(currentPosition.x) <= (maxX + MARGIN)) {
            tsRigidBody2D.position = currentPosition;
        }
    }

    /**
    * @brief Increment score points and updates counter's UI Text.
    **/
    public void Score() {
		this.score++;
		this.paddleCountText.text = this.score + "";
	}

}