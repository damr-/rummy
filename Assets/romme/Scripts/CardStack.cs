using System.Collections.Generic;
using romme.Utility;
using UnityEngine;

namespace romme
{

    public class CardStack : MonoBehaviour
    {
        public GameObject CardPrefab;

        public Stack<Card> Cards = new Stack<Card>();
        private bool stackCreated;

        public void CreateCardStack()
        {
            if(stackCreated)
            {
                Debug.LogWarning("Something tried to create a stack although there already is one!");
                return;
            }

            //Spawn two decks of cards
            for (int i = 0; i < 2; i++)
            {
                //Spawn normal cards for each symbol
                for (int symbol = 1; symbol <= Card.CardSymbolsCount; symbol++)
                {
                    for (int number = 1; number <= Card.CardNumbersCount; number++)
                    {
                        //If number is joker, only spawn if symbol is 1 or 3, 
                        //so we only have one red and one black joker
                        if ((Card.CardNumber)number == Card.CardNumber.JOKER && symbol % 2 != 1)
                            continue;

                        GameObject CardGO = Instantiate(CardPrefab, transform.position, Quaternion.identity);
                        Card card = CardGO.GetComponent<Card>();
                        card.Number = (Card.CardNumber)number;
                        card.Symbol = (Card.CardSymbol)symbol;
                        Cards.Push(card);
                    }
                }
            }

            foreach (Card c in Cards)
                c.SetVisible(false);

            stackCreated = true;
        }

        public void ShuffleCardStack()
        {
            Cards = Cards.Shuffle();
        }

        public Card DrawCard()
        {
            return Cards.Pop();
        }
    }

}