using UnityEngine;

namespace romme.UI.CardOutput
{
    
    [RequireComponent(typeof(ScrollView))]
    public abstract class CardOutputUI : MonoBehaviour
    {        
        protected Player player;
        protected ScrollView outputView;

        private void Start()
        {
            outputView = GetComponent<ScrollView>();
            player = GetComponentInParent<Player>();

            SetupPlayerSub();
        }

        protected abstract void SetupPlayerSub();
    }

}