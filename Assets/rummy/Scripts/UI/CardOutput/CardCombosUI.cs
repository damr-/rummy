using System.Linq;
using System.Collections.Generic;
using rummy.Cards;
using UnityEngine;
using rummy.Utility;

namespace rummy.UI.CardOutput
{

    public class CardCombosUI : CardOutputUI
    {
        private Color notEnoughPointsColor = new Color(25/255f, 25/255f, 25/255f, 0.5f);

        protected override void SetupPlayerSub()
        {
            player.PossibleCardCombosChanged.AddListener(UpdateCombos);
        }

        private void UpdateCombos(List<CardCombo> cardCombos)
        {
            if (!gameObject.activeInHierarchy)
                return;

            outputView.ClearMessages();
            if (cardCombos.Count == 0)
                return;

            // Do not display duplicate possibilities
            List<CardCombo> uniqueCombos = new List<CardCombo>();
            foreach (CardCombo combo in cardCombos)
            {
                if (uniqueCombos.All(c => !c.LooksEqual(combo)))
                    uniqueCombos.Add(combo);
            }

            string poss = " possibilit" + (uniqueCombos.Count == 1 ? "y" : "ies");
            string var = " variant" + (cardCombos.Count == 1 ? "" : "s");
            string header = uniqueCombos.Count + poss + " [" + cardCombos.Count + var + "]:";
            outputView.PrintMessage(new ScrollView.Message(header));

            for (int i = 0; i < uniqueCombos.Count; i++)
            {
                CardCombo cardCombo = uniqueCombos[i];
                if (cardCombo.MeldCount == 0)
                    continue;

                string msg = "";
                if (cardCombo.Sets.Count > 0)
                {
                    foreach (Set set in cardCombo.Sets)
                        msg += set + ", ";
                }
                if (cardCombo.Runs.Count > 0)
                {
                    foreach (Run run in cardCombo.Runs)
                        msg += run + ", ";
                }
                msg = msg.TrimEnd().TrimEnd(',') + " (" + cardCombo.Value + ")";

                Color msgColor = Color.black;
                if(!player.HasLaidDown)
                    msgColor = cardCombo.Value < Tb.I.GameMaster.MinimumLaySum ? notEnoughPointsColor : Color.black;
                outputView.PrintMessage(new ScrollView.Message(msg, msgColor));
            }
        }
    }

}