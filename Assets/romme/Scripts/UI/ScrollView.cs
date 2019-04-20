using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace romme.UI
{

    public class ScrollView : MonoBehaviour
    {
        public Transform ScrollViewContent;
        public GameObject EntryPrefab;

        public int CharsPerLine = 36;
        protected List<GameObject> spawnedTaskPanels = new List<GameObject>();

        public void PrintMessage(Message message)
        {
            string[] messageparts = message.text.Split("\n".ToCharArray());
            foreach(string msg in messageparts)
            {
                string m = msg;
                while(m.Length > CharsPerLine)
                {
                    CreateMessageObj(m.Substring(0, CharsPerLine), message.color);
                    m = m.Substring(CharsPerLine);
                }
                CreateMessageObj(m, message.color);
            }
        }

        public void ClearMessages()
        {
            while(spawnedTaskPanels.Count > 0)
            {
                var panel = spawnedTaskPanels[0];
                spawnedTaskPanels.RemoveAt(0);
                Destroy(panel);
            }
        }

        protected void CreateMessageObj(string message, Color color)
        {
            GameObject messageObj = Instantiate(EntryPrefab);
            messageObj.transform.SetParent(ScrollViewContent.transform, false);
            Text text = messageObj.GetComponent<Text>();
            text.text = message;
            text.color = color;

            spawnedTaskPanels.Add(messageObj);
        }

        public class Message
        {
            public string text;
            public Color color;

            public Message() : this("", Color.black){}
            public Message(string message): this(message, Color.black){}

            public Message(string message, Color color)
            {
                text = message;
                this.color = color;
            }
        }
    }

}