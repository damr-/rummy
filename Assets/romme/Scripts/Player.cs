using System.Collections.Generic;
using UniRx;
using System;
using UnityEngine;
using romme.Utility;

namespace romme
{

    public class Player : MonoBehaviour
    {
        public int PlayerNumber;
        public float startAngle;
        public float cardRadius = 5f;
        public float cardsAngleSpread = 180f;
        public bool ShowCards = false;
        private bool isCardBeingLaidDown, hasLaidDownBefore;
        private float waitStartTime;
        private readonly float waitDuration = 2f;

        private IDisposable cardMoveSubscription = Disposable.Empty;

        public enum PlayerState
        {
            NONE = 0,
            DRAWING = 1,
            WAITING = 2,
            PLAYING = 3,
            LAYING = 4,
            DISCARDING = 5
        }
        public PlayerState playerState = PlayerState.NONE;

        private List<Card> PlayerCards = new List<Card>();
        public int PlayerCardCount { get { return PlayerCards.Count; } }

        public List<CardSpot> PlayerCardSpots;
        private List<List<Card>> layDownCards = new List<List<Card>>();
        private int currentLayDownCardsIdx = 0, currentCardIdx = 0;
        private CardSpot currentCardSpot;

        public IObservable<Player> TurnFinished { get { return turnFinishedSubject; } }
        private readonly ISubject<Player> turnFinishedSubject = new Subject<Player>();

        public void AddCard(Card card)
        {
            PlayerCards.Add(card);
            if (ShowCards)
                card.SetVisible(true);
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

            if (playerState == PlayerState.WAITING && Time.time - waitStartTime > waitDuration)
            {
                playerState = PlayerState.PLAYING;
                Play();
            }

            if (playerState == PlayerState.LAYING)
            {
                if (isCardBeingLaidDown)
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
            DrawCard(false);
        }

        public void DrawCard(bool isServingCard)
        {
            playerState = PlayerState.DRAWING;

            //TODO check if we can (!isServingCard and point limit) and want to draw from discard stack
            //    if (fromDiscardStack)
            //        return Tb.I.DiscardStack.DrawCard();
            Card card = Tb.I.CardStack.DrawCard();

            cardMoveSubscription = card.MoveFinished.Subscribe(c => DrawCardFinished(c, isServingCard));
            card.MoveCard(transform.position, Tb.I.GameMaster.AnimateCardMovement);
        }

        private void DrawCardFinished(Card card, bool isServingCard)
        {
            cardMoveSubscription.Dispose();
            AddCard(card);
            if (isServingCard)
                playerState = PlayerState.NONE;
            else 
            { 
                playerState = PlayerState.WAITING;
                waitStartTime = Time.time;
            }
        }

        private void Play()
        {
            //TODO 
            // - check cards on hand for number series to lay down
            // - check if single cards fit with already lying cards at card spots

            //If it's the first round, just discard a card and finish turn
            if (Tb.I.GameMaster.RoundCount == 1)
            {
                DiscardUnusableCard();
                return;
            }

            IDictionary<Card.CardNumber, List<Card>> LayCardsSameNumber = PlayerCards.GetLayCardsSameNumber();
            //IDictionary<Card.CardNumber, List<Card>> LayCardsSerues = PlayerCards.GetLayCardsSeries(); TODO

            isCardBeingLaidDown = false;

            if (LayCardsSameNumber.Count > 0)
            {
                int sum = 0;
                if (!hasLaidDownBefore)
                {
                    foreach (KeyValuePair<Card.CardNumber, List<Card>> number in LayCardsSameNumber)
                        sum += Card.CardValues[number.Key] * number.Value.Count;
                }

                if (hasLaidDownBefore || sum >= Tb.I.GameMaster.MinimumValueForLay)
                {
                    layDownCards.Clear();
                    foreach (KeyValuePair<Card.CardNumber, List<Card>> number in LayCardsSameNumber)
                    {
                        Debug.Log("Laying down " + number.Value.Count + " " + number.Key);
                        layDownCards.Add(number.Value);
                    }
                    currentLayDownCardsIdx = 0;
                    currentCardIdx = 0;

                    if (!hasLaidDownBefore)
                        hasLaidDownBefore = true;

                    playerState = PlayerState.LAYING;
                }
                else
                {
                    DiscardUnusableCard();
                }
            }
            else
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
                //Find a new spot for the next set of cards
                currentCardSpot = null;

                if (currentLayDownCardsIdx == layDownCards.Count)
                {
                    currentLayDownCardsIdx = 0;
                    DiscardUnusableCard();
                }
            }
            else
                currentCardIdx++;
        }

        private void DiscardUnusableCard()
        {
            playerState = PlayerState.DISCARDING;

            //TODO
            //Card card = ChooseMostUselessCard();
            Card card = PlayerCards[UnityEngine.Random.Range(0, PlayerCards.Count - 1)];

            PlayerCards.Remove(card);
            card.MoveFinished.Subscribe(DiscardCardMoveFinished);
            card.MoveCard(Tb.I.DiscardStack.GetNextCardPos(), Tb.I.GameMaster.AnimateCardMovement);
        }

        private void DiscardCardMoveFinished(Card card)
        {
            Tb.I.DiscardStack.AddCard(card);
            turnFinishedSubject.OnNext(this);
            playerState = PlayerState.NONE;
        }

        public CardSpot GetFirstEmptyCardSpot()
        {
            foreach(CardSpot spot in PlayerCardSpots)
            {
                if (!spot.HasCards)
                    return spot;
            }
            return null;
        }

    }

}