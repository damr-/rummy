using System.Collections.Generic;
using UniRx;
using System;
using UnityEngine;
using romme.Utility;

namespace romme
{

    public class Player : MonoBehaviour
    {
        public static int CurrentPlayerIndex = 1;

        public float startAngle;
        public float cardRadius = 5f;
        public float cardsAngleSpread = 180f;
        public bool IsLocalPlayer = false;
        private bool isPlayingCards, isCardBeingLaidDown, isDiscardingCard, hasLaidDownBefore, isThinking;
        private float thinkStartTime, thinkDuration = 2f;

        public Transform PlayerCardSpots;
        private List<List<Card>> layDownCards = new List<List<Card>>();
        private int currentLayDownCardsIdx = 0, currentCardIdx = 0;
        private CardSpot currentCardSpot;

        public IObservable<Player> PlayerFinished { get { return playerFinishedSubject; } }
        private readonly ISubject<Player> playerFinishedSubject = new Subject<Player>();

        [SerializeField]
        private List<Card> PlayerCards = new List<Card>();

        public int CardCount { get { return PlayerCards.Count; } }

        public int PlayerNumber { get; private set; }

        private void Start()
        {
            PlayerNumber = CurrentPlayerIndex++;
        }

        public void AddCard(Card card)
        {
            PlayerCards.Add(card);
            if (IsLocalPlayer)
                card.SetCardVisible(true);
        }

        private void Update()
        {
            float deltaAngle = cardsAngleSpread / PlayerCards.Count;

            for (int i = 0; i < PlayerCards.Count; i++)
            {
                float x = cardRadius * Mathf.Cos((startAngle + i * deltaAngle) * Mathf.PI / 180f);
                float z = cardRadius * Mathf.Sin((startAngle + i * deltaAngle) * Mathf.PI / 180f);
                PlayerCards[i].transform.position = transform.position + new Vector3(x, -0.1f * i, z);
            }

            if(isThinking && Time.time - thinkStartTime > thinkDuration)
            {
                isThinking = false;
                Play();
            }

            if (isPlayingCards)
            {
                if (isCardBeingLaidDown || isDiscardingCard)
                    return;

                if (currentCardSpot == null)
                    currentCardSpot = GetFirstEmptyCardSpot();

                Card card = layDownCards[currentLayDownCardsIdx][currentCardIdx];
                PlayerCards.Remove(card);
                Debug.Log("Laying down " + card.GetCardTypeString());
                card.MoveFinished.Subscribe(CardLayDownMoveFinished);
                card.MoveCard(currentCardSpot.transform.position, Tb.I.GameMaster.AnimateCardMovement);
                isCardBeingLaidDown = true;
            }
        }

        public void BeginTurn()
        {
            isThinking = true;
            thinkStartTime = Time.time;
        }

        public void Play()
        {
            //TODO 
            // - check cards on hand for number series to lay down
            // - check if single cards fit with already lying cards at card spots

            //TODO it's not that simple...
            //If it's the first round, just discard a random card and finish turn
            if (Tb.I.GameMaster.RoundCount == 1)
            {
                Debug.Log("First round, just discard a card.");
                DiscardUnusableCard();
                return;
            }

            //Gather all cards with the same number
            IDictionary<Card.CardNumber, List<Card>> SameNumberCards = new Dictionary<Card.CardNumber, List<Card>>();
            for (int i = 0; i < PlayerCards.Count; i++)
            {
                Card card = PlayerCards[i];
                if (SameNumberCards.ContainsKey(card.Number))
                    SameNumberCards[card.Number].Add(card);
                else
                    SameNumberCards.Add(card.Number, new List<Card> { card });
            }


            IDictionary<Card.CardNumber, List<Card>> ActualSameNumberCards = new Dictionary<Card.CardNumber, List<Card>>();
            foreach (KeyValuePair<Card.CardNumber, List<Card>> number in SameNumberCards)
            {
                if (number.Value.Count < 3)
                    continue;

                //The actual unique cards with the same number
                List<Card> actualCards = new List<Card>();

                List<Card.CardSymbol> usedSymbols = new List<Card.CardSymbol>();

                foreach (Card c in number.Value)
                {
                    if (!usedSymbols.Contains(c.Symbol))
                    {
                        usedSymbols.Add(c.Symbol);
                        actualCards.Add(c);
                    }
                }
                if (actualCards.Count >= 3)
                    ActualSameNumberCards.Add(number.Key, actualCards);
            }

            isPlayingCards = true;
            isCardBeingLaidDown = false;
            bool canLayDown = true;

            //If never laid down before, check if enough points can be accumulated by laying the actual same number cards down
            if (!hasLaidDownBefore)
            {
                int sum = 0;
                foreach (KeyValuePair<Card.CardNumber, List<Card>> number in ActualSameNumberCards)
                    sum += Card.CardValues[number.Key] * number.Value.Count;
                canLayDown = sum >= Tb.I.GameMaster.MinimumValueForLay;
            }

            if (canLayDown)
            {
                foreach (KeyValuePair<Card.CardNumber, List<Card>> number in ActualSameNumberCards)
                {
                    Debug.Log("Laying down " + number.Value.Count + " " + number.Key);
                    layDownCards.Add(number.Value);
                }
                currentLayDownCardsIdx = 0;
                currentCardIdx = 0;

                if(!hasLaidDownBefore)
                    hasLaidDownBefore = true;
            }
            else //nothing to do, time to discard a card
            {
                DiscardUnusableCard();
            }
        }

        private void CardLayDownMoveFinished(Card card)
        {
            isCardBeingLaidDown = false;
            currentCardSpot.Cards.Add(card);

            if (currentCardIdx == layDownCards[currentLayDownCardsIdx].Count - 1)
            {
                currentCardIdx = 0;
                currentLayDownCardsIdx++;

                //Find a new spot for the new set of cards
                currentCardSpot = null;

                //If all laydown series have been laid down, proceed with turn
                if (currentLayDownCardsIdx == layDownCards.Count)
                {
                    currentLayDownCardsIdx = 0;
                    //we're done laying cards down, time to discard a card
                    DiscardUnusableCard();
                }
            }
            else
                currentCardIdx++;
        }

        private void DiscardUnusableCard()
        {
            Card card = PlayerCards[UnityEngine.Random.Range(0, PlayerCards.Count - 1)];
            PlayerCards.Remove(card);
            card.MoveFinished.Subscribe(DiscardCardFinished);
            card.MoveCard(Tb.I.DiscardStack.transform.position + Vector3.up * 0.001f * Tb.I.DiscardStack.Cards.Count, 
                            Tb.I.GameMaster.AnimateCardMovement);
            isDiscardingCard = true;
        }

        private void DiscardCardFinished(Card card)
        {
            Tb.I.DiscardStack.Cards.Add(card);
            isDiscardingCard = false;
            FinishTurn();
        }

        private void FinishTurn()
        {
            isPlayingCards = false;
            layDownCards.Clear();
            currentLayDownCardsIdx = 0;
            playerFinishedSubject.OnNext(this);
        }

        public CardSpot GetFirstEmptyCardSpot()
        {
            foreach(Transform child in PlayerCardSpots)
            {
                CardSpot spot = child.GetComponent<CardSpot>();
                if (!spot.HasCards)
                    return spot;
            }
            return null;
        }

    }

}