using System.Collections.Generic;
using UniRx;
using Single = romme.Cards.Single;

namespace romme.UI
{

    public class SinglesUI : CardOutputUI
    {
        protected override void SetupPlayerSub()
        {
            player.PossibleSinglesChanged.Subscribe(UpdateSingles);
        }

        private void UpdateSingles(List<Single> Singles)
        {
            outputView.ClearMessages();
            if (Singles.Count == 0)
                return;

            outputView.PrintMessage(new ScrollView.Message(Singles.Count + " possibilit" + (Singles.Count == 1 ? "y:" : "ies:")));
            foreach(var c in Singles)
            {
                int cardValue = 0;
                if(c.Card.IsJoker())
                {
                    //TODO: Find out the joker value
                    cardValue = 0;
                }
                else
                {
                    cardValue = c.Card.Value;
                }
                string msg = "" + c.Card + ": " + c.CardSpot.gameObject.name + " (" + cardValue + ")" + (c.Joker != null ? " ⇅" : "");
                outputView.PrintMessage(new ScrollView.Message(msg));
            }
        }
    }

}