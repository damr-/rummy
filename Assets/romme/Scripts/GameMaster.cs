using System.Collections.Generic;
using UnityEngine;
using romme.Utility;

namespace romme
{

    public class GameMaster : MonoBehaviour
    {
        public GameObject CardPrefab;
        public Transform StackPosition;

        private void Start()
        {
            List<Card> cards = SpawnDeck();
            cards.Shuffle();
        }

        private List<Card> SpawnDeck()
        {
            List<Card> cards = new List<Card>();

            //Spawn two decks of cards
            for (int i = 0; i < 2; i++)
            {
                //Spawn normal cards
                for (int symbol = 1; symbol <= Card.CardSymbolsCount; symbol++)
                {
                    for (int number = 1; number <= Card.CardNumbersCount; number++)
                    {
                        GameObject CardGO = Instantiate(CardPrefab, StackPosition.position, Quaternion.identity);
                        Card card = CardGO.GetComponent<Card>();
                        card.Symbol = (Card.CardSymbol)symbol;
                        card.Number = (Card.CardNumber)number;
                        cards.Add(card);
                    }
                }
                //Spawn 2 joker cards
                for(int j = 0; j < 2; j++)
                {
                    GameObject CardGO = Instantiate(CardPrefab, StackPosition.position, Quaternion.identity);
                    Card card = CardGO.GetComponent<Card>();
                    card.Number = Card.CardNumber.JOKER;
                    card.Symbol = (Card.CardSymbol)(j+1); //j=0: Karo (red), j=1: Pik (black)
                    cards.Add(card);
                }
            }
            return cards;
        }

    }

}