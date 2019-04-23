using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using romme.Cards;
using UniRx;

namespace romme.UI
{

    [RequireComponent(typeof(ScrollView))]
    public class CardCombosUI : MonoBehaviour
    {
        public Player Player;
        private ScrollView outputView;

        private void Start()
        {
            outputView = GetComponent<ScrollView>();

            if (Player == null)
                throw new MissingReferenceException(gameObject.name + " missing Player reference in 'CardCombosUI.cs'.");
            Player.PossibleCardCombosChanged.Subscribe(UpdateCombos);
        }

        private void UpdateCombos(List<LaydownCards> cardCombos)
        {
            outputView.ClearMessages();
            if(cardCombos.Count == 0)
                return;

            outputView.PrintMessage(new ScrollView.Message("Possibilities:"));
            foreach (LaydownCards possibility in cardCombos)
            {
                if (possibility.Count == 0)
                    continue;

                string msg = "";
                if (possibility.Sets.Count > 0)
                {
                    foreach (Set set in possibility.Sets)
                        msg += set + ", ";
                }
                if (possibility.Runs.Count > 0)
                {
                    foreach (Run run in possibility.Runs)
                        msg += run + ", ";
                    msg = msg.TrimEnd().TrimEnd(',');
                }
                msg += " (" + possibility.Value + ")";
                outputView.PrintMessage(new ScrollView.Message(msg));
            }
        }
    }

}