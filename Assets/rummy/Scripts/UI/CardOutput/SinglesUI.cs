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
            if (Singles.Count > 0)
            {
                outputView.PrintMessage($"{Singles.Count} possibilit{(Singles.Count == 1 ? "y:" : "ies:")}");
                Singles.ForEach(single => outputView.PrintMessage(GetSingleOutput(single)));
            }
        }

        public static string GetSingleOutput(Single single)
        {
            string output = $"{single.Card.ToRichString()} -> {single.CardSpot}";
            if (single.Joker != null)
                output += $" (JKR)";
            else if (single.Spot > -1)
                output += $" @{single.Spot}";
            return output;
        }
    }

}