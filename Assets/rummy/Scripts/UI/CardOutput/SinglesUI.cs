using System.Collections.Generic;
using rummy.Cards;

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
            if (!AlwaysUpdate && !gameObject.activeInHierarchy)
                return;

            outputView.ClearMessages();
            if (Singles.Count == 0)
                return;

            outputView.PrintMessage(new ScrollView.Message(Singles.Count + " possibilit" + (Singles.Count == 1 ? "y:" : "ies:")));
            foreach(var single in Singles)
            {
                int cardValue;
                if(single.Card.IsJoker())
                    cardValue = 0;
                else
                    cardValue = single.Card.Value;
                string jokerSuffix = single.Joker != null ? " (SWAP)" : "";
                string msg = $"{single} -> {single.CardSpot} @ {single.Spot} ({cardValue}){jokerSuffix}";
                outputView.PrintMessage(new ScrollView.Message(msg));
            }
        }
    }

}