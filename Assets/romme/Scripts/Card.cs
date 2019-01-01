using System.Collections.Generic;
using UnityEngine;

namespace romme
{

    public class Card : MonoBehaviour
    {
        #region defs

        public enum CardColor
        {
            BLACK = 0,
            RED = 1
        }

        public enum CardNumber
        {
            JOKER = -1,
            TWO = 2,
            THREE = 3,
            FOUR = 4,
            FIVE = 5,
            SIX = 6,
            SEVEN = 7,
            EIGHT = 8,
            NINE = 9,
            TEN = 10,
            JACK = 11,
            QUEEN = 12,
            KING = 13,
            ACE = 14
        }

        public enum CardSymbol
        {
            HEART = 1,
            KARO = 2,
            PIK = 3,
            KREUZ = 4
        }

        public static readonly IDictionary<CardNumber, int> CardValues = new Dictionary<CardNumber, int>()
        {
            { CardNumber.JOKER, -1 },
            { CardNumber.TWO, 2 },
            { CardNumber.THREE, 3 },
            { CardNumber.FOUR, 4 },
            { CardNumber.FIVE, 5 },
            { CardNumber.SIX, 6 },
            { CardNumber.SEVEN, 7 },
            { CardNumber.EIGHT, 8 },
            { CardNumber.NINE, 9 },
            { CardNumber.TEN, 10 },
            { CardNumber.JACK, 10 },
            { CardNumber.QUEEN, 10 },
            { CardNumber.KING, 10 },
            { CardNumber.ACE, 10 }
        };

        public static readonly int CardNumbersCount = 13;
        public static readonly int CardSymbolsCount = 4;

        #endregion

        public CardNumber Number = CardNumber.TWO;
        public CardSymbol Symbol = CardSymbol.HEART;
        public CardColor Color {
            get
            {
                if (Symbol == CardSymbol.HEART || Symbol == CardSymbol.KARO)
                    return CardColor.RED;
                return CardColor.BLACK;
            }
        }

        public string GetCardTypeString()
        {
            return Number + "_" + Symbol;
        }


    }
}