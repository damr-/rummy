using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using rummy.Cards;
using rummy.Utility;

namespace rummy.UI.CardOutput
{

    public class CardCombosUI : CardOutputUI
    {
        public static readonly Color grey = new(25 / 255f, 25 / 255f, 25 / 255f, 0.5f);

        protected override void SetupPlayerSub()
        {
            player.PossibleCardCombosChanged.AddListener(UpdateCombos);
        }

        private void UpdateCombos(List<CardCombo> cardCombos)
        {
            if (!AlwaysUpdate && !gameObject.activeInHierarchy)
                return;

            outputView.ClearMessages();
            if (cardCombos.Count == 0)
                return;

            // Do not display duplicate possibilities
            List<CardCombo> uniqueCombos = new();
            foreach (CardCombo combo in cardCombos)
            {
                if (uniqueCombos.All(c => !c.LooksEqual(combo)))
                    uniqueCombos.Add(combo);
            }

            string poss = $"possibilit{(uniqueCombos.Count == 1 ? "y" : "ies")}";
            string var = $"variant{(cardCombos.Count == 1 ? "" : "s")}";
            string header = $"{uniqueCombos.Count} {poss} [{cardCombos.Count} {var}]:";
            outputView.PrintMessage(header);
            for (int i = 0; i < uniqueCombos.Count; i++)
            {
                CardCombo cardCombo = uniqueCombos[i];
                if (cardCombo.MeldCount == 0)
                    continue;
                bool greyedOut = !player.HasLaidDown && cardCombo.Value < Tb.I.GameMaster.MinimumLaySum;
                string overrideColor = greyedOut ? $"#{ColorUtility.ToHtmlStringRGBA(grey)}" : "";

                string msg = "";
                if (cardCombo.Sets.Count > 0)
                {
                    foreach (Set set in cardCombo.Sets)
                        msg += $"{set.ToString(overrideColor)}, ";
                }
                if (cardCombo.Runs.Count > 0)
                {
                    foreach (Run run in cardCombo.Runs)
                        msg += $"{run.ToString(overrideColor)}, ";
                }
                msg = $"{msg.TrimEnd().TrimEnd(',')} <color=#{ColorUtility.ToHtmlStringRGBA(greyedOut ? grey : Color.black)}>({cardCombo.Value})</color>";
                outputView.PrintMessage(msg);
            }
        }
    }

}