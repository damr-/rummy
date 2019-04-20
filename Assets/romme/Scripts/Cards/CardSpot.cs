using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace romme.Cards
{
    public class CardSpot : MonoBehaviour
    {
        public float startAngle;
        public float cardRadius = 2f;
        public float cardsAngleSpread = 180f;

        public enum SpotType 
        {
            NONE,
            RUN,
            SET
        }
        public SpotType Type;

        public List<Card> GetCards()
        {
            if (Type == SpotType.RUN && Run != null)
                return Run.Cards;
            else if (Type == SpotType.SET && Set != null)
                return Set.Cards;
            else if(Type == SpotType.NONE)
                return TypeNoneCards;
            return new List<Card>();
        }

        public void SetCards(List<Card> cards)
        {
            if (Type == SpotType.RUN)
                Run = new Run(cards);
            else if (Type == SpotType.SET)
                Set = new Set(cards);
            else
            {
                TypeNoneCards = new List<Card>(cards);
            }
        }

        public int GetValue()
        {
            switch(Type)
            {
                case SpotType.RUN:
                    return (Run != null ? Run.Value : 0);
                case SpotType.SET:
                    return (Set != null ? Set.Value : 0);
                default:
                    return 0;
            }
        }

        /// <summary>
        /// The stored cards of the CardSpot if its SpotType is NONE
        /// </summary>
        private List<Card> TypeNoneCards = new List<Card>();
        private Run Run;
        private Set Set;
        public bool HasCards => GetCards().Count > 0;

        public void AddCard(Card card)
        {                
            var tmpList = new List<Card>(GetCards());
            tmpList.Add(card);
            SetCards(new List<Card>(tmpList));
        }

        public void RemoveCard(Card card) => SetCards(GetCards().Where(c => c != card).ToList());

        private void Update()
        {
            if(GetCards().Count == 0)
                return;

            float deltaAngle = cardsAngleSpread / GetCards().Count;

            for (int i = 0; i < GetCards().Count; i++)
            {
                float x = cardRadius * Mathf.Cos((startAngle + i * deltaAngle) * Mathf.PI / 180f);
                float z = cardRadius * Mathf.Sin((startAngle + i * deltaAngle) * Mathf.PI / 180f);
                GetCards()[i].transform.position = transform.position + new Vector3(x, -0.1f * i, z);
            }
        }
    }

}