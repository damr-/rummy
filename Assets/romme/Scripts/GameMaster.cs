using System.Collections.Generic;
using UnityEngine;
using romme.Utility;
using UniRx;
using System;
using System.Linq;

namespace romme
{

    public class GameMaster : MonoBehaviour
    {
        public float GameSpeed = 1.0f;
        public int MinimumValueForLay = 40;
        public bool AnimateCardMovement = true;
        public float CardMoveSpeed = 50f;

        public int RoundCount { get; private set; }

        public Transform CardStack;

        public GameObject CardPrefab;
        public List<Player> Players = new List<Player>();
        public Stack<Card> cardStack;

        private bool isCardBeingDealt, isDealing, isPlayerPlaying;
        private int curDealCardIdx, curPlayerIdx;

        private IDisposable cardMoveSubscription = Disposable.Empty;
        private IDisposable playerPlaySubscription = Disposable.Empty;

        public static readonly int cardsPerPlayer = 13;

        private void Start()
        {
            Time.timeScale = GameSpeed;
            RoundCount = 1;

            cardStack = CreateCardStack();
            cardStack = cardStack.Shuffle();

            foreach(Card c in cardStack)
                c.SetCardVisible(false);

            GameObject[] playersObjets = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject po in playersObjets)
            {
                Player player = po.GetComponent<Player>();
                if (player != null)
                    Players.Add(player);
            }
            if (Players.Count == 0)
                Debug.LogError("Could not find any object with tag 'Player'");

            Players = Players.OrderBy(p => p.PlayerNumber).ToList();

            isDealing = true;
            isCardBeingDealt = false;
            curDealCardIdx = 0;
            curPlayerIdx = 0;
        }

        private void Update()
        {
            if (isCardBeingDealt || isPlayerPlaying)
                return;

            if(RoundCount > 0)
                Debug.Log("Dealing card to " + Players[curPlayerIdx].name);

            Card card = cardStack.Pop();
            cardMoveSubscription = card.MoveFinished.Subscribe(CardServeFinished);
            card.MoveCard(Players[curPlayerIdx].transform.position, AnimateCardMovement);
            isCardBeingDealt = true;
        }

        private void CardServeFinished(Card card)
        {
            cardMoveSubscription.Dispose();
            isCardBeingDealt = false;
            Players[curPlayerIdx].AddCard(card);

            if (isDealing)
            {
                if (curPlayerIdx == Players.Count - 1)
                {
                    curPlayerIdx = 0;
                    curDealCardIdx++;

                    if (curDealCardIdx == cardsPerPlayer)
                    {
                        isDealing = false;
                        curPlayerIdx = 0;
                    }
                }
                else
                    curPlayerIdx++;
            }
            else //we're playing
            {
                isPlayerPlaying = true;
                CardMoveSpeed = 5; //TODO REMOVE
                Player curPlayer = Players[curPlayerIdx];
                playerPlaySubscription = curPlayer.PlayerFinished.Subscribe(PlayerFinished);
                curPlayer.BeginTurn();
            }
        }

        private void PlayerFinished(Player player)
        {
            playerPlaySubscription.Dispose();
            isPlayerPlaying = false;

            curPlayerIdx++;
            if(curPlayerIdx == Players.Count)
            {
                curPlayerIdx = 0;
                RoundCount++;
            }
        }

        private Stack<Card> CreateCardStack()
        {
            Stack<Card> cards = new Stack<Card>();

            //Spawn two decks of cards
            for (int i = 0; i < 2; i++)
            {
                //Spawn normal cards for each symbol
                for (int symbol = 1; symbol <= Card.CardSymbolsCount; symbol++)
                {
                    for (int number = 1; number <= Card.CardNumbersCount; number++)
                    {
                        //If symbol joker, only spawn if symbol is 1 or 3, 
                        //so we only have one red and one black joker
                        if ((Card.CardNumber)number == Card.CardNumber.JOKER && symbol % 2 != 1)
                            continue;

                        GameObject CardGO = Instantiate(CardPrefab, CardStack.position, Quaternion.identity);
                        Card card = CardGO.GetComponent<Card>();
                        card.Number = (Card.CardNumber)number;
                        card.Symbol = (Card.CardSymbol)symbol;
                        cards.Push(card);
                    }
                }
            }
            return cards;
        }
    }
}