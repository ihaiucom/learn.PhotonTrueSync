using UnityEngine;
using UnityEngine.UI;


using System.Collections;


namespace Tutoral.PUN
{
    public class PlayerUI : MonoBehaviour
    {


        #region Public Properties


        [Tooltip("UI Text to display Player's Name")]
        public Text PlayerNameText;


        [Tooltip("UI Slider to display Player's Health")]
        public Slider PlayerHealthSlider;

        [Tooltip("Pixel offset from the player target")]
        public Vector3 ScreenOffset = new Vector3(0f, 30f, 0f);

        #endregion


        #region Private Properties

        PlayerManager _target;

        float _characterControllerHeight = 1f;
        Transform _targetTransform;
        Vector3 _targetPosition;

        #endregion


        #region MonoBehaviour Messages
        void Awake()
        {
            this.GetComponent<Transform>().SetParent(GameObject.Find("Game Manager/Canvas").GetComponent<Transform>());
        }


        void Update()
        {
            // Destroy itself if the target is null, It's a fail safe when Photon is destroying Instances of a Player over the network
            if (_target == null)
            {
                Destroy(this.gameObject);
                return;
            }

            // Reflect the Player Health
            if (PlayerHealthSlider != null)
            {
                PlayerHealthSlider.value = _target.Health;
            }
        }

        void LateUpdate()
        {
            // #Critical
            // Follow the Target GameObject on screen.
            if (_targetTransform != null)
            {
                _targetPosition = _targetTransform.position;
                _targetPosition.y += _characterControllerHeight;
                this.transform.position = Camera.main.WorldToScreenPoint(_targetPosition) + ScreenOffset;
            }
        }
        #endregion


        #region Public Methods
        public void SetTarget(PlayerManager target)
        {
            if (target == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> PlayMakerManager target for PlayerUI.SetTarget.", this);
                return;
            }
            // Cache references for efficiency
            _target = target;
            _targetTransform = target.transform;
            if (PlayerNameText != null)
            {
                PlayerNameText.text = _target.photonView.owner.NickName;
            }

        }

        #endregion


    }
}