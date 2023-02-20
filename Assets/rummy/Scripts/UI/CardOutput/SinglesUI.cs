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

            outputView.PrintMessage($"{Singles.Count} possibilit{(Singles.Count == 1 ? "y:" : "ies:")}");
            foreach(var single in Singles)
            {
                int cardValue;
                if(single.Card.IsJoker())
                    cardValue = 0;
                else
                    cardValue = single.Card.Value;
                string jokerSuffix = single.Joker != null ? " (JKR)" : "";
                string spotOutput = single.Spot > -1 ? $" @{single.Spot}" : ""; 
                string msg = $"{single.Card.ToRichString()} -> {single.CardSpot}{spotOutput} ({cardValue}){jokerSuffix}";
                outputView.PrintMessage(msg);
            }
        }
    }

}