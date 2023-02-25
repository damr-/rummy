using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using rummy.Cards;
using rummy.Utility;

namespace rummy.UI.CardOutput
{

    public class CardCombosUI : CardOutputUI
    {
        public static readonly string grey = ColorUtility.ToHtmlStringRGBA(new(25 / 255f, 25 / 255f, 25 / 255f, 0.5f));
        private static float alpha = 255 / 255f;
        private static float dark = 200 / 255f;
        public static readonly List<Color> highlightColors = new()
        {
            new( dark,    0, dark, alpha),
            new(    0, dark, dark, alpha),
            new( dark, 0.5f,    0, alpha),
            new(    0, dark,    0, alpha),
            new(    0,    0, dark, alpha)
        };

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
            outputView.PrintMessage($"{uniqueCombos.Count} {poss} [{cardCombos.Count} {var}]:");
            foreach (CardCombo cardCombo in uniqueCombos)
            {
                if (cardCombo.MeldCount > 0)
                {
                    bool greyedOut = !player.HasLaidDown && cardCombo.Value < Tb.I.GameMaster.MinimumLaySum;
                    string msg = GetComboOutput(cardCombo, greyedOut);
                    outputView.PrintMessage(msg);
                }
            }
        }

        public static string GetComboOutput(CardCombo combo, bool greyedOut, bool addBrackets = false)
        {
            string output = "";
            int counter = 0;
            foreach (var set in combo.Sets)
            {
                string setOutput = set.ToString(greyedOut ? $"#{grey}" : "");
                if (addBrackets)
                {
                    string bracketColor = greyedOut ? grey : ColorUtility.ToHtmlStringRGBA(highlightColors[counter++]);
                    setOutput = $"<color=#{bracketColor}>[</color>{setOutput}<color=#{bracketColor}>]</color>";
                }
                output += $"{setOutput} ";
            }
            foreach (var run in combo.Runs)
            {
                string runOutput = run.ToString(greyedOut ? $"#{grey}" : "");
                if (addBrackets)
                {
                    string bracketColor = greyedOut ? grey : ColorUtility.ToHtmlStringRGBA(highlightColors[counter++]);
                    runOutput = $"<color=#{bracketColor}>[</color>{runOutput}<color=#{bracketColor}>]</color>";
                }
                output += $"{runOutput} ";
            }
            output += $"<color=#{(greyedOut ? grey : "000")}>({combo.Value})</color>";
            return output;
        }
    }

}