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

        /// <summary>
        /// The type of this cardspot. NONE is for empty card spots as well as player hand card spots
        /// </summary>
        public SpotType Type;

        public List<Card> GetCards()
        {
            if (Type == SpotType.RUN && Run != null)
                return Run.Cards;
            else if (Type == SpotType.SET && Set != null)
                return Set.Cards;
            else if (Type == SpotType.NONE)
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
            switch (Type)
            {
                case SpotType.RUN:
                    return (Run != null ? Run.Value : 0);
                case SpotType.SET:
                    return (Set != null ? Set.Value : 0);
                default:
                    int value = 0;
                    foreach(var card in TypeNoneCards)
                    {
                        if(card.Rank == Card.CardRank.JOKER)
                            value += 20;
                        else
                            value += card.Value();
                    }
                    return value;
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
            
            if(Type == SpotType.RUN && tmpList.Count > 0)
            {
                int idx = tmpList.Count; //By default, add the new card at the end
                var highestRank = GetCards()[tmpList.Count - 1].Rank;
                //1st priority: add ace after king. If the highest rank card in the run is not a king,
                //manually add the ace at the beginning, in front of the TWO
                if(card.Rank == Card.CardRank.ACE && highestRank != Card.CardRank.KING)
                {
                    idx = 0;
                }
                //If the first item in the run is an ACE, the new card can only be added at the end
                else if(tmpList[0].Rank == Card.CardRank.ACE)
                {
                    idx = tmpList.Count;
                }
                else //Any other case, the card will be sorted by rank
                {
                    for(int i = 0; i < tmpList.Count; i++)
                    {
                        if(tmpList[i].Rank > card.Rank)
                        {
                            idx = i;
                            break;
                        }
                    }
                }
                tmpList.Insert(idx, card);
            }
            else
                tmpList.Add(card);
            SetCards(new List<Card>(tmpList));
        }

        public void RemoveCard(Card card) => SetCards(GetCards().Where(c => c != card).ToList());

        private void Update()
        {
            if (GetCards().Count == 0)
                return;

            float deltaAngle = cardsAngleSpread / GetCards().Count;

            for (int i = 0; i < GetCards().Count; i++)
            {
                float x = cardRadius * Mathf.Cos((startAngle + i * deltaAngle) * Mathf.PI / 180f);
                float z = cardRadius * Mathf.Sin((startAngle + i * deltaAngle) * Mathf.PI / 180f);
                GetCards()[i].transform.position = transform.position + new Vector3(x, -0.1f * i, z);
            }
        }

        public bool CanFit(Card additionalCard)
        {
            switch (Type)
            {
                case SpotType.NONE: return false;
                case SpotType.SET:
                    if (additionalCard.Rank != Set.Rank)
                        return false;
                    return GetCards().All(c => c.Suit != additionalCard.Suit);
                default: //SpotType.RUN:
                    if (additionalCard.Suit != Run.Suit)
                        return false;
                    var highestRank = GetCards()[GetCards().Count - 1].Rank;
                    var lowestRank = GetCards()[0].Rank;

                    return (additionalCard.Rank == highestRank + 1 && highestRank != Card.CardRank.ACE) || 
                            (additionalCard.Rank == lowestRank - 1 && lowestRank != Card.CardRank.ACE) || 
                            (additionalCard.Rank == Card.CardRank.ACE && lowestRank == Card.CardRank.TWO);
            }
        }

        public void ResetSpot()
        {
            while(GetCards().Count > 0)
            {
                Card c = GetCards()[0];
                GetCards().RemoveAt(0);
                Destroy(c.gameObject);
            }
            Type = SpotType.NONE;
            Run = null;
            Set = null;
            TypeNoneCards = new List<Card>();
        }
    }

}