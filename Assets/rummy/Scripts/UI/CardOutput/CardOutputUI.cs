using UnityEngine;

namespace rummy.UI.CardOutput
{
    
    [RequireComponent(typeof(ScrollView))]
    public abstract class CardOutputUI : MonoBehaviour
    {        
        protected AIPlayer player;
        protected ScrollView outputView;

        private void Start()
        {
            outputView = GetComponent<ScrollView>();
            player = GetComponentInParent<AIPlayer>();

            SetupPlayerSub();
        }

        protected abstract void SetupPlayerSub();
    }

}