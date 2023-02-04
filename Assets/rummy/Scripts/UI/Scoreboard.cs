using System.Collections.Generic;
using TMPro;
using UnityEngine;
using rummy.Utility;

namespace rummy.UI
{
    public class Scoreboard : MonoBehaviour
    {
        public RectTransform ScrollViewContent;
        public GameObject LinePrefab;
        public GameObject EntryPrefab;

        private readonly List<GameObject> lines = new();
        private readonly List<int> playerTotals = new();

        private static readonly int BASE_WIDTH = 100;

        public void AddLine(List<Player> players, bool isNamesLine)
        {
            GameObject lineObj = Instantiate(LinePrefab, ScrollViewContent.transform);

            if (isNamesLine)
                AddPlayerNamesLine(players, lineObj);
            else
                AddScoresLine(players, lineObj);

            lines.Add(lineObj);
            // Update the vertical size of the scroll view
            ScrollViewContent.sizeDelta = new Vector2(ScrollViewContent.sizeDelta.x, 15 * lines.Count + 10);
        }

        private void AddPlayerNamesLine(List<Player> players, GameObject lineObj)
        {
            foreach (Player p in players)
            {
                GameObject scoreObj = Instantiate(EntryPrefab, lineObj.transform);
                var tmp = scoreObj.GetComponentInChildren<TextMeshProUGUI>();
                tmp.text = $"{p.PlayerName} (0)";
                playerTotals.Add(0);
            }

            // Adjust width to fit all players
            var rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(BASE_WIDTH * players.Count, rectTransform.sizeDelta.y);

        }

        private void AddScoresLine(List<Player> players, GameObject lineObj)
        {
            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];

                GameObject scoreObj = Instantiate(EntryPrefab, lineObj.transform);
                int score = p.GetHandCardsSum();
                string scoreText = score.ToString();
                scoreObj.GetComponentInChildren<TextMeshProUGUI>().text = scoreText == "0" ? "-" : scoreText;

                playerTotals[i] += score;
                lines[0].transform.GetChild(i).GetComponentInChildren<TextMeshProUGUI>().text = $"{p.PlayerName} ({playerTotals[i]})";
            }
        }

        public void Clear()
        {
            lines.ClearAndDestroy();
            for (int i = 0; i < playerTotals.Count; i++)
                playerTotals[i] = 0;
        }
    }

}