using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using rummy.Utility;

namespace rummy.UI.CardOutput
{

    public class ScrollView : MonoBehaviour
    {
        public RectTransform ScrollViewContent;
        public GameObject EntryPrefab;

        public int CharsPerLine = 36;
        protected List<GameObject> spawnedTaskPanels = new();

        private void Start()
        {
            UpdateViewSize();
        }

        public void PrintMessage(Message message)
        {
            string[] messageparts = message.text.Split("\n".ToCharArray());
            foreach (string msg in messageparts)
            {
                string m = msg;
                while (m.Length > CharsPerLine)
                {
                    CreateMessageObj(m.Substring(0, CharsPerLine), message.color);
                    m = m.Substring(CharsPerLine);
                }
                CreateMessageObj(m, message.color);
            }

            UpdateViewSize();
        }

        public void ClearMessages()
        {
            spawnedTaskPanels.ClearAndDestroy();
            UpdateViewSize();
        }

        private void CreateMessageObj(string message, Color color)
        {
            GameObject messageObj = Instantiate(EntryPrefab);
            messageObj.transform.SetParent(ScrollViewContent.transform, false);
            Text text = messageObj.GetComponent<Text>();
            text.text = message;
            text.color = color;

            spawnedTaskPanels.Add(messageObj);
        }

        private void UpdateViewSize()
        {
            ScrollViewContent.sizeDelta = new Vector2(ScrollViewContent.sizeDelta.x, 15 * spawnedTaskPanels.Count + 10);
        }

        public class Message
        {
            public string text;
            public Color color;

            public Message() : this("", Color.black) { }
            public Message(string message) : this(message, Color.black) { }

            public Message(string message, Color color)
            {
                text = message;
                this.color = color;
            }
        }
    }

}