using System.Collections.Generic;
using Single = rummy.Cards.Single;

namespace rummy.UI.CardOutput
{

    public class SinglesUI : CardOutputUI
    {
        protected override void SetupPlayerSub()
        {
            player.PossibleSinglesChanged.AddListener(UpdateSingles);
        }

        private void UpdateSingles(List<Single> Singles)
        {
            if (!gameObject.activeInHierarchy)
                return;

            outputView.ClearMessages();
            if (Singles.Count == 0)
                return;

            outputView.PrintMessage(new ScrollView.Message(Singles.Count + " possibilit" + (Singles.Count == 1 ? "y:" : "ies:")));
            foreach(var c in Singles)
            {
                int cardValue;
                if(c.Card.IsJoker())
                {
                    //TODO: Find out the joker value (not really necessary though?)
                    cardValue = 0;
                }
                else
                {
                    cardValue = c.Card.Value;
                }
                string msg = "" + c.Card + ": " + c.CardSpot.gameObject.name + " (" + cardValue + ")" + (c.Joker != null ? " (SWAP)" : "");
                outputView.PrintMessage(new ScrollView.Message(msg));
            }
        }
    }

}