using System;
using System.Globalization;
using System.Collections.Generic;
using romme.Utility;
using UniRx;
using UnityEngine;

namespace romme.Cards
{

    public class Card : MonoBehaviour
    {
        #region defs
        public enum CardColor
        {
            BLACK = 0,
            RED = 1
        }

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

        public enum CardSuit
        {
            HEART = 1,  //HERZ
            TILE = 2,   //KARO
            PIKE = 3,   //PIK
            CLOVERS = 4 //KREUZ
        }

        public static readonly IDictionary<CardRank, int> CardValues = new Dictionary<CardRank, int>
        {
            { CardRank.JOKER, 0 },
            { CardRank.TWO, 2 },
            { CardRank.THREE, 3 },
            { CardRank.FOUR, 4 },
            { CardRank.FIVE, 5 },
            { CardRank.SIX, 6 },
            { CardRank.SEVEN, 7 },
            { CardRank.EIGHT, 8 },
            { CardRank.NINE, 9 },
            { CardRank.TEN, 10 },
            { CardRank.JACK, 10 },
            { CardRank.QUEEN, 10 },
            { CardRank.KING, 10 },
            { CardRank.ACE, 10 }
        };

        /// <summary>
        /// The number of different card ranks in the game
        /// </summary>
        public static readonly int CardRankCount = 14;

        /// <summary>
        /// The number of different card suits in the game
        /// </summary>
        public static readonly int CardSuitCount = 4;

        public static string GetSuitSymbol(Card card)
        {
            if(card.IsJoker())
            {
                return card.IsBlack() ? "b" : "r";
            }
            
            switch(card.Suit)
            {
                case CardSuit.CLOVERS: return "♣"; 
                case CardSuit.HEART: return "♥"; 
                case CardSuit.PIKE: return "♠"; 
                default: return "♦"; //TILE
            }
        }

        public static string GetRankLetter(CardRank rank)
        {
            switch(rank)
            {
                case CardRank.TWO: return "2"; 
                case CardRank.THREE: return "3"; 
                case CardRank.FOUR: return "4"; 
                case CardRank.FIVE: return "5"; 
                case CardRank.SIX: return "6"; 
                case CardRank.SEVEN: return "7"; 
                case CardRank.EIGHT: return "8"; 
                case CardRank.NINE: return "9"; 
                case CardRank.TEN: return "10"; 
                case CardRank.JACK: return "J"; 
                case CardRank.QUEEN: return "Q"; 
                case CardRank.KING: return "K"; 
                case CardRank.ACE: return "A"; 
                default: return "?"; //JOKER
            }
        }
        #endregion

        public CardRank Rank = CardRank.TWO;
        public CardSuit Suit = CardSuit.HEART;
        public CardColor Color
        {
            get
            {
                if (Suit == CardSuit.HEART || Suit == CardSuit.TILE)
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
                    //Most of the time, joker values are calculated in-situ since it depends on the context
                    //Here, Joker on hand is worth 0 so it will always be discarded last
                    return 0;
                }
                return CardValues[Rank];
            }
        }

        public bool IsBlack() => Color == Card.CardColor.BLACK;
        public bool IsRed() => Color == Card.CardColor.RED;
        public bool IsJoker() => Rank == Card.CardRank.JOKER;

        private bool isCardMoving;
        private Vector3 targetPos;
        public IObservable<Card> MoveFinished { get { return moveFinishedSubject; } }
        private readonly ISubject<Card> moveFinishedSubject = new Subject<Card>();

        public string GetFileString() => Rank + "_" + Suit;
        public override string ToString() => GetRankLetter(Rank) + GetSuitSymbol(this);

        public void SetVisible(bool visible)
        {
            transform.rotation = Quaternion.identity;
            transform.rotation *= Quaternion.LookRotation(Vector3.forward, Vector3.up * (visible ? 1 : -1));
        }

        public void MoveCard(Vector3 targetPosition, bool animateMovement)
        {
            isCardMoving = true;
            targetPos = targetPosition;
            if (!animateMovement)
                FinishMove();
        }

        private void FinishMove()
        {
            transform.position = targetPos;
            isCardMoving = false;
            moveFinishedSubject.OnNext(this);
        }

        private void Update()
        {
            if (!isCardMoving)
                return;
            if (Vector3.Distance(transform.position, targetPos) <= 2 * Time.deltaTime * Tb.I.GameMaster.CardMoveSpeed)
                FinishMove();
            else
                transform.Translate((targetPos - transform.position).normalized * Time.deltaTime * Tb.I.GameMaster.CardMoveSpeed, Space.World);
        }

    }
}