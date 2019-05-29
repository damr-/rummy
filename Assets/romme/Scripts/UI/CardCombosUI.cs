using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using romme.Cards;
using UniRx;

namespace romme.UI
{

    public class CardCombosUI : CardOutputUI
    {
        protected override void SetupPlayerSub()
        {
            player.PossibleCardCombosChanged.Subscribe(UpdateCombos);
        }

        private void UpdateCombos(List<CardCombo> cardCombos)
        {
            outputView.ClearMessages();
            if (cardCombos.Count == 0)
                return;
            
            //Do not display duplicate possibilities
            List<CardCombo> uniqueCombos = new List<CardCombo>();
            foreach(CardCombo combo in cardCombos)
            {
                 if(uniqueCombos.All(c => !c.LooksEqual(combo)))
                    uniqueCombos.Add(combo);
            }
            uniqueCombos = uniqueCombos.OrderByDescending(c => c.Value).ToList();

            string poss = " possibilit" + (uniqueCombos.Count == 1 ? "y" : "ies");
            string var = " variant" + (cardCombos.Count == 1 ? "" : "s");
            string header = uniqueCombos.Count + poss + " (" + cardCombos.Count + var + "):";
            outputView.PrintMessage(new ScrollView.Message(header));

            for (int i = 0; i < uniqueCombos.Count; i++)
            {
                CardCombo cardCombo = uniqueCombos[i];
                if (cardCombo.PackCount == 0)
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
                outputView.PrintMessage(new ScrollView.Message(msg));
            }
        }
    }

}