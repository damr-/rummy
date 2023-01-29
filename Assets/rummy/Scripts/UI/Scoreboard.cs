using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace rummy.UI
{
    public class Scoreboard : MonoBehaviour
    {
        public RectTransform ScrollViewContent;
        public GameObject LinePrefab;
        public GameObject EntryPrefab;

        protected List<GameObject> lines = new();

        private void Start()
        {
            UpdateViewSize();
        }

        public void AddPlayerNamesLine(List<Player> players)
        {
            AddLine(players, true);
        }

        public void AddScoreLine(List<Player> players)
        {
            AddLine(players, false);
        }

        private void AddLine(List<Player> players, bool addNames)
        {
            GameObject lineObj = Instantiate(LinePrefab, ScrollViewContent.transform);

            foreach (var p in players)
            {
                GameObject scoreObj = Instantiate(EntryPrefab, lineObj.transform);
                string content;
                if (addNames)
                    content = p.PlayerName;
                else
                {
                    string score = p.GetHandCardsSum().ToString();
                    if (score == "0")
                        score = "-";
                    content = score;
                }
                scoreObj.GetComponentInChildren<TextMeshProUGUI>().text = content;
            }

            lines.Add(lineObj);
            UpdateViewSize();
        }

        private void UpdateViewSize()
        {
            ScrollViewContent.sizeDelta = new Vector2(ScrollViewContent.sizeDelta.x, 25 * lines.Count + 10);
        }
    }

}