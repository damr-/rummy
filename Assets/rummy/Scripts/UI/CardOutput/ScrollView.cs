using System.Collections.Generic;
using UnityEngine;
using rummy.Utility;
using TMPro;

namespace rummy.UI.CardOutput
{

    public class ScrollView : MonoBehaviour
    {
        public RectTransform ScrollViewContent;
        public GameObject EntryPrefab;

        [SerializeField]
        protected float lineHeight = 15;
        protected List<GameObject> spawnedTaskPanels = new();

        private void Start()
        {
            UpdateViewSize();
        }

        public void PrintMessage(string message)
        {
            CreateMessageObj(message);
            UpdateViewSize();
        }

        public void ClearMessages()
        {
            spawnedTaskPanels.ClearAndDestroy();
            UpdateViewSize();
        }

        private void CreateMessageObj(string message)
        {
            GameObject messageObj = Instantiate(EntryPrefab);
            messageObj.transform.SetParent(ScrollViewContent.transform, false);
            messageObj.GetComponent<TextMeshProUGUI>().text = message;

            spawnedTaskPanels.Add(messageObj);
        }

        private void UpdateViewSize()
        {
            ScrollViewContent.sizeDelta = new Vector2(ScrollViewContent.sizeDelta.x, 10 + spawnedTaskPanels.Count * lineHeight);
        }
    }

}