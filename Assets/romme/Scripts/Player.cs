using System.Collections.Generic;
using UniRx;
using System;
using System.Linq;
using UnityEngine;
using romme.Cards;
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

        [SerializeField]
        private float waitDuration = 2f;
        private float waitStartTime;

        private IDisposable cardMoveSubscription = Disposable.Empty;

        public enum PlayerState
        {
            IDLE = 0,
            DRAWING = 1,
            WAITING = 2,
            PLAYING = 3,
            LAYING = 4,
            DISCARDING = 5
        }
        public PlayerState playerState = PlayerState.IDLE;

        private List<Card> PlayerCards = new List<Card>();
        public int PlayerCardCount { get { return PlayerCards.Count; } }

        public Transform PlayerCardSpotsParent;
        public List<CardSpot> GetPlayerCardSpots()
        {
            if (playerCardSpots.Count == 0)
                playerCardSpots = PlayerCardSpotsParent.GetComponentsInChildren<CardSpot>().ToList();
            return playerCardSpots;
        }
        private List<CardSpot> playerCardSpots = new List<CardSpot>();
        private CardSpot currentCardSpot;

        private List<List<Card>> layDownCards = new List<List<Card>>();
        private int currentLayDownCardsIdx = 0, currentCardIdx = 0;
        private bool isCardBeingLaidDown, hasLaidDownBefore;

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
                playerState = PlayerState.IDLE;
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

            List<KeyValuePair<Card.CardNumber, List<Card>>> LayCardsSameNumber = PlayerCards.GetLayCardsSameNumber();
            //IDictionary<Card.CardNumber, List<Card>> LayCardsSerues = PlayerCards.GetLayCardsSeries(); TODO

            //TODO
            //Check if player has jokers which can fill up LayCardsSameNumber
            List<Card> jokerCards = PlayerCards.Where(c => c.Number == Card.CardNumber.JOKER).ToList();
            var LayCardsWithJoker = new List<KeyValuePair<Card.CardNumber, List<Card>>>();
            int jokerCount = jokerCards.Count;

            if (jokerCount > 0)
            {
                var possibleWithJoker = GetPossibleSameNumberWithJoker(jokerCards, LayCardsSameNumber);

                if(possibleWithJoker.Count > 0)
                {
                    List<Card> usedJokerCards = new List<Card>();
                    do
                    {
                        var entry = possibleWithJoker[0];
                        possibleWithJoker.RemoveAt(0);

                        var jokerCard = PlayerCards.FirstOrDefault(c => c.Number == Card.CardNumber.JOKER &&
                                                                    !usedJokerCards.Contains(c) &&
                                                                    c.Color == entry.Key);

                        if (jokerCard == null)
                            continue;

                        usedJokerCards.Add(jokerCard);
                        entry.Value.Value.Add(jokerCard);
                        LayCardsWithJoker.Add(entry.Value);

                    } while (possibleWithJoker.Count > 0 && usedJokerCards.Count < jokerCount);
                }
            }

            var layCards = new List<KeyValuePair<Card.CardNumber, List<Card>>>();
            layCards.AddRange(LayCardsSameNumber);
            layCards.AddRange(LayCardsWithJoker);

            //TODO later layCards has to be a list of list of cards since for example
            // a series of cards cannot be categorized by Card.CardNumber
            //caculating individual joker values will be necessary for calculating sum
            if (layCards.Count > 0)
            {
                int sum = 0;
                if (!hasLaidDownBefore)
                {
                    foreach (KeyValuePair<Card.CardNumber, List<Card>> entry in layCards)
                    {
                        int cardValue = Card.CardValues[entry.Key];
                        sum += cardValue * entry.Value.Count;

                        foreach(Card card in entry.Value)
                        {
                            if (card.Number == Card.CardNumber.JOKER)
                                card.JokerCardValue = cardValue;
                        }
                    }
                }

                if (hasLaidDownBefore || sum >= Tb.I.GameMaster.RequLaySum)
                {
                    layDownCards.Clear();
                    foreach (KeyValuePair<Card.CardNumber, List<Card>> entry in layCards)
                    {
                        Debug.Log("Laying down " + entry.Value.Count + " " + entry.Key);
                        layDownCards.Add(entry.Value);
                    }
                    currentLayDownCardsIdx = 0;
                    currentCardIdx = 0;

                    if (!hasLaidDownBefore)
                        hasLaidDownBefore = true;

                    isCardBeingLaidDown = false;
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

        private List<KeyValuePair<Card.CardColor, KeyValuePair<Card.CardNumber, List<Card>>>>
                                            GetPossibleSameNumberWithJoker(List<Card> jokerCards, 
                                                                           List<KeyValuePair<Card.CardNumber, List<Card>>> layCardsSameNumber)
        {
            int jokerCount = jokerCards.Count;

            var possibleCardsUnfiltered = PlayerCards.GetCardsByNumber().Where(entry => entry.Key != Card.CardNumber.JOKER &&
                                                                                entry.Value.Count >= 2); //So that 1 Joker can finish a triplet

            var possibleCards = new List<KeyValuePair<Card.CardNumber, List<Card>>>();
            foreach(var entry in possibleCardsUnfiltered)
            {
                //Get all cards which will be laid down anway which have the same CardNumber as the currently investigated cards
                var layCardsWithSameCardNumber = layCardsSameNumber.Where(e => e.Key == entry.Key);

                List<Card> eligibleCards = new List<Card>();

                if (!layCardsWithSameCardNumber.Any())
                {
                    eligibleCards = entry.Value;
                }
                else
                {
                    foreach (var sameCardNumberPair in layCardsWithSameCardNumber)
                    {
                        foreach (Card card in entry.Value)
                        {
                            //If card is not gonna be laid down, it is eligible for being used with one or more jokers
                            if (!sameCardNumberPair.Value.Contains(card))
                                eligibleCards.Add(card);
                        }
                    }
                }

                var uniqueEligibleCards = eligibleCards.GetUniqueCards();
                if (uniqueEligibleCards.Count == 2)
                    possibleCards.Add(new KeyValuePair<Card.CardNumber, List<Card>>(entry.Key, uniqueEligibleCards));
            }

            if (!possibleCards.Any()) //return empty dictionary which won't do anything
                return new List<KeyValuePair<Card.CardColor, KeyValuePair<Card.CardNumber, List<Card>>>>();

            var coloredPossibleCards = new List<KeyValuePair<Card.CardColor, KeyValuePair<Card.CardNumber, List<Card>>>>();

            //Select possible card combinations by required joker color
            for (int i = 0; i < 2; i++)
            {
                Card.CardColor curColor = (Card.CardColor)i;
                bool hasJokerOfColor = jokerCards.Any(jc => jc.Color == curColor);
                foreach (var entry in possibleCards)
                {
                    if (hasJokerOfColor && entry.Value.Count(c => c.Color == curColor) != 2)
                        coloredPossibleCards.Add(new KeyValuePair<Card.CardColor, KeyValuePair<Card.CardNumber, List<Card>>>(curColor, entry));
                }
            }

            coloredPossibleCards = coloredPossibleCards.OrderByDescending(entry => (int)entry.Value.Key).ToList();

            //return list, ordered descending by CardNumber
            return coloredPossibleCards;
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
            //Card card = PlayerCards[UnityEngine.Random.Range(0, PlayerCards.Count - 1)];

            var result = PlayerCards.GetCardsByNumber().FirstOrDefault(entry => entry.Key != Card.CardNumber.JOKER && entry.Value.Count == 1);
            Card card = null;
            try {
                card = result.Value[0];
            }
            catch (NullReferenceException e) {
                card = PlayerCards[UnityEngine.Random.Range(0, PlayerCards.Count - 1)];
            }

            PlayerCards.Remove(card);
            card.MoveFinished.Subscribe(DiscardCardMoveFinished);
            card.MoveCard(Tb.I.DiscardStack.GetNextCardPos(), Tb.I.GameMaster.AnimateCardMovement);
        }

        private void DiscardCardMoveFinished(Card card)
        {
            Tb.I.DiscardStack.AddCard(card);
            turnFinishedSubject.OnNext(this);
            playerState = PlayerState.IDLE;
        }

        public CardSpot GetFirstEmptyCardSpot()
        {
            foreach (CardSpot spot in GetPlayerCardSpots())
            {
                if (!spot.HasCards)
                    return spot;
            }
            return null;
        }

        public int GetLaidCardsSum()
        {
            int sum = 0;
            foreach (CardSpot spot in GetPlayerCardSpots())
            {
                foreach (Card card in spot.Cards)
                {
                    int cardValue = Card.CardValues[card.Number];
                    if (card.Number == Card.CardNumber.JOKER)
                        cardValue = card.JokerCardValue;
                    sum += cardValue;
                }
            }
            return sum;
        }

    }

}