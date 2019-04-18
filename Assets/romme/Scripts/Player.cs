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

        private readonly List<Card> PlayerCards = new List<Card>();
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

        #region CardSets
        private List<Set> possibleSets = new List<Set>();
        private List<Run> possibleRuns = new List<Run>();
        // private List<KeyValuePair<Card.CardRank, List<Card>>> possibleJokerSets = new List<KeyValuePair<Card.CardRank, List<Card>>>();
        private readonly List<List<Card>> layDownCards = new List<List<Card>>();
        private int currentLayDownCardsIdx = 0, currentCardIdx = 0;
        private bool isCardBeingLaidDown, hasLaidDown; //Whether the player already laid down cards in a preceding turn 
        #endregion

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
                Debug.Log("Laying down " + card);
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

            Card card;
            //TODO: check if we WANT to draw from discard stack
            if (false && !isServingCard && GetLaidCardsSum() >= Tb.I.GameMaster.MinimumLaySum)
                card = Tb.I.DiscardStack.DrawCard();
            else
                card = Tb.I.CardStack.DrawCard();

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
            //TODO:
            // - check if single cards fit with already lying cards at card spots

            //If it's the first round, just discard a card and finish turn
            if (Tb.I.GameMaster.RoundCount == 1)
            {
                DiscardUnusableCard();
                return;
            }

            possibleSets.Clear();
            possibleRuns.Clear();

            possibleSets = PlayerCards.GetSets();
            possibleRuns = PlayerCards.GetRuns();

            //TODO: layCards cannot(?) be a list of cards if we want to preserve the knowledge about runs and sets for cardspots


            //TODO: Get possible runs with joker cards and choose which RUN or SET to play
            /*
            var possibleJokerSetsColoured = PlayerCards.GetJokerSets(possibleSets);
            // Choose which SETS to play with joker (the ones with the highest point sum will be chosen)
            if(possibleJokerSetsColoured.Count > 0)
            {
                possibleJokerSets = new List<KeyValuePair<Card.CardRank, List<Card>>>();
                List<Card> usedJokerCards = new List<Card>();
                int jokerCount = PlayerCards.Where(c => c.Rank == Card.CardRank.JOKER).Count();

                do
                {
                    var possibleSet = possibleJokerSetsColoured[0];
                    possibleJokerSetsColoured.RemoveAt(0);

                    var jokerCard = PlayerCards.FirstOrDefault(c => c.Rank == Card.CardRank.JOKER &&
                                                                !usedJokerCards.Contains(c) &&
                                                                c.Color == possibleSet.Key);
                    if (jokerCard == null)
                        continue;

                    usedJokerCards.Add(jokerCard);
                    possibleSet.Value.Value.Add(jokerCard);
                    possibleJokerSets.Add(possibleSet.Value);
                } while (possibleJokerSetsColoured.Count > 0 && usedJokerCards.Count < jokerCount);

            }*/


            var layCards = new List<List<Card>>();

            if (layCards.Count > 0)
            {
                int sum = 0;
                if (!hasLaidDown)
                {
                    foreach (List<Card> entry in layCards)
                    {
                        foreach(Card card in entry)
                        {
                            int cardValue = Card.CardValues[card.Rank];
                            if (card.Rank == Card.CardRank.JOKER)
                            {
                                //card.JokerCardValue = 0; //TODO: fix joker card value
                                cardValue = 0;
                            }
                            sum += cardValue;
                        }
                    }
                }

                if (hasLaidDown || sum >= Tb.I.GameMaster.MinimumLaySum)
                {
                    layDownCards.Clear();
                    foreach (List<Card> entry in layCards)
                    {
                        //TODO: this is only useful for sets
                        Debug.Log("Laying down " + entry.Count + " " + entry[0].Rank);
                        layDownCards.Add(entry);
                    }
                    currentLayDownCardsIdx = 0;
                    currentCardIdx = 0;

                    if (!hasLaidDown)
                        hasLaidDown = true;

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

        private void CardLayDownMoveFinished(Card card)
        {
            isCardBeingLaidDown = false;
            currentCardSpot.Cards.Add(card);

            if (currentCardIdx == layDownCards[currentLayDownCardsIdx].Count - 1)
            {
                currentCardIdx = 0;
                currentLayDownCardsIdx++;
                currentCardSpot = null;     //Find a new spot for the next set of cards

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

            //Get all player cards except the ones used for possible runs and sets
            List<Card> filteredPlayerCards = new List<Card>();
            foreach(var c in PlayerCards)
                filteredPlayerCards.Add(c);
            foreach(var run in possibleRuns)
                filteredPlayerCards = filteredPlayerCards.Except(run.Cards).ToList();
            foreach(var set in possibleSets)
                filteredPlayerCards = filteredPlayerCards.Except(set.Cards).ToList();

            KeyValuePair<Card.CardRank, List<Card>> result =  
            filteredPlayerCards.GetCardsByRank().FirstOrDefault(entry => entry.Key != Card.CardRank.JOKER && entry.Value.Count == 1);

            Card card = null;
            try {
                card = result.Value[0];
            }
            catch (NullReferenceException e) {
                do{
                    card = filteredPlayerCards[UnityEngine.Random.Range(0, filteredPlayerCards.Count - 1)];
                }while(card.Rank == Card.CardRank.JOKER);
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
                    int cardValue = Card.CardValues[card.Rank];
                    //TODO: fix joker card value here somehow
                    sum += cardValue;
                }
            }
            return sum;
        }

    }
}