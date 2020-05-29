using System.Collections.Generic;
using rummy.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace rummy.Cards
{

    public class Card : MonoBehaviour
    {
        #region defs
        public enum CardColor
        {
            BLACK = 0,
            RED = 1
        }

        /// <summary>
        /// The number of different card ranks in the game
        /// </summary>
        public static readonly int CardRankCount = 14;

        public enum CardRank
        {
            JOKER = 1,
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

        /// <summary>
        /// The number of different card suits in the game
        /// </summary>
        public static readonly int CardSuitCount = 4;

        public enum CardSuit
        {
            HEARTS = 1,
            DIAMONDS = 2,
            SPADES = 3,
            CLUBS = 4
        }

        public static readonly IDictionary<CardRank, int> CardValues = new Dictionary<CardRank, int>
        {
            { CardRank.JOKER,   0 },
            { CardRank.TWO,     2 },
            { CardRank.THREE,   3 },
            { CardRank.FOUR,    4 },
            { CardRank.FIVE,    5 },
            { CardRank.SIX,     6 },
            { CardRank.SEVEN,   7 },
            { CardRank.EIGHT,   8 },
            { CardRank.NINE,    9 },
            { CardRank.TEN,     10 },
            { CardRank.JACK,    10 },
            { CardRank.QUEEN,   10 },
            { CardRank.KING,    10 },
            { CardRank.ACE,     10 }
        };

        public static readonly IDictionary<CardRank, string> RankLetters = new Dictionary<CardRank, string>
        {
            { CardRank.TWO,     "2" },
            { CardRank.THREE,   "3" },
            { CardRank.FOUR,    "4" },
            { CardRank.FIVE,    "5" },
            { CardRank.SIX,     "6" },
            { CardRank.SEVEN,   "7" },
            { CardRank.EIGHT,   "8" },
            { CardRank.NINE,    "9" },
            { CardRank.TEN,     "10" },
            { CardRank.JACK,    "J" },
            { CardRank.QUEEN,   "Q" },
            { CardRank.KING,    "K" },
            { CardRank.ACE,     "A" },
            { CardRank.JOKER,   "?"}
        };

        public static readonly IDictionary<CardSuit, char> SuitSymbols = new Dictionary<CardSuit, char>
        {
#if UNITY_WEBGL
            { CardSuit.HEARTS,   'h' },
            { CardSuit.DIAMONDS, 'd' },
            { CardSuit.CLUBS,    'c' },
            { CardSuit.SPADES,   's' }
#else
            { CardSuit.HEARTS,  '♥' },
            { CardSuit.DIAMOND, '♦' },
            { CardSuit.CLUBS,   '♣' },
            { CardSuit.SPADES,  '♠' }
#endif
        };

        private static char GetSuitSymbol(Card card)
        {
            if (card.IsJoker())
                return card.IsBlack() ? 'b' : 'r';
            return SuitSymbols[card.Suit];
        }

        public static List<CardSuit> GetOtherTwo(CardSuit s1, CardSuit s2)
        {
            if (s1 == s2)
            {
                Tb.I.GameMaster.LogMsg("GetOtherTwo got the same CardSuit twice", LogType.Error);
                return new List<CardSuit>();
            }
            var suits = new List<CardSuit>() { CardSuit.HEARTS, CardSuit.DIAMONDS, CardSuit.SPADES, CardSuit.CLUBS };
            suits.Remove(s1);
            suits.Remove(s2);
            return suits;
        }
        #endregion

        public CardRank Rank = CardRank.TWO;
        public CardSuit Suit = CardSuit.HEARTS;
        public CardColor Color
        {
            get
            {
                if (Suit == CardSuit.HEARTS || Suit == CardSuit.DIAMONDS)
                    return CardColor.RED;
                return CardColor.BLACK;
            }
        }

        public int Value
        {
            get
            {
                if (Rank == CardRank.JOKER)
                {
                    //Most of the time, joker values are calculated directly since it depends on the context
                    //Here, joker on hand is worth 0 so it will always be discarded last
                    return 0;
                }
                return CardValues[Rank];
            }
        }

        public bool IsBlack() => Color == CardColor.BLACK;
        public bool IsRed() => Color == CardColor.RED;
        public bool IsJoker() => Rank == CardRank.JOKER;

        private bool isMoving;
        private Vector3 targetPos;

        public class Event_MoveFinished : UnityEvent<Card> { }
        public Event_MoveFinished MoveFinished = new Event_MoveFinished();

        public string GetFileString() => Rank + "_" + Suit;
        public override string ToString() => RankLetters[Rank] + GetSuitSymbol(this);

        public void SetVisible(bool visible)
        {
            transform.rotation = Quaternion.identity;
            transform.rotation *= Quaternion.LookRotation(Vector3.forward, Vector3.up * (visible ? 1 : -1));
        }

        public void MoveCard(Vector3 targetPosition, bool animateMovement)
        {
            isMoving = true;
            targetPos = targetPosition;
            if (!animateMovement)
                FinishMove();
        }

        private void FinishMove()
        {
            transform.position = targetPos;
            isMoving = false;
            MoveFinished.Invoke(this);
        }

        private void Update()
        {
            if (!isMoving)
                return;
            if (Vector3.Distance(transform.position, targetPos) <= 2 * Time.deltaTime * Tb.I.GameMaster.CardMoveSpeed)
                FinishMove();
            else
                transform.Translate((targetPos - transform.position).normalized * Time.deltaTime * Tb.I.GameMaster.CardMoveSpeed, Space.World);
        }

    }
}