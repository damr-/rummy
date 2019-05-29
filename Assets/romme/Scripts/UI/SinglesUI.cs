using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using romme.Cards;
using UniRx;

namespace romme.UI
{

    public class SinglesUI : CardOutputUI
    {
        protected override void SetupPlayerSub()
        {
            player.PossibleSinglesChanged.Subscribe(UpdateSingles);
        }

        private void UpdateSingles(List<KeyValuePair<Card, CardSpot>> Singles)
        {
            outputView.ClearMessages();
            if (Singles.Count == 0)
                return;

            outputView.PrintMessage(new ScrollView.Message(Singles.Count + " possibilit" + (Singles.Count == 1 ? "y:" : "ies:")));
            foreach(var c in Singles)
            {
                string msg = "" + c.Key + ": " + c.Value.gameObject.name + " (" + c.Key.Value + ")";
                outputView.PrintMessage(new ScrollView.Message(msg));
            }
        }
    }

}