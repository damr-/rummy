using UnityEngine;
using UnityEngine.UI;

namespace rummy.UI
{
    [RequireComponent(typeof(Slider))]
    public class UpdateSliderValue : MonoBehaviour
    {
        private Slider slider;

        private GameMaster GameMaster;

        private void Start()
        {
            slider = GetComponent<Slider>();
            GameMaster = GetComponentInParent<GameMaster>();
        }

        private void Update()
        {
            if (isActiveAndEnabled)
                slider.value = GameMaster.CurrentCardMoveSpeed;
        }

    }
}