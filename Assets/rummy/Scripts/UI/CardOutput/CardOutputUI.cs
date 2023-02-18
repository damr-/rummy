using UnityEngine;

namespace rummy.UI.CardOutput
{
    
    [RequireComponent(typeof(ScrollView))]
    public abstract class CardOutputUI : MonoBehaviour
    {
        public bool AlwaysUpdate = false;

        public int selectedLine = -1;

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