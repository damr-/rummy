using UnityEngine;
using rummy.Cards;
using rummy.Utility;

namespace rummy
{

    public class HumanPlayer : Player
    {
        private Card cardToDiscard = null;
        public void SetCardToDiscard(Card card) => cardToDiscard = card;

        private void Update()
        {
            if (State == PlayerState.PLAYING)
            {
                if (!Tb.I.GameMaster.LayingAllowed() || !HasLaidDown)
                {
                    State = PlayerState.DISCARDING;
                }
                else
                {
                    State = PlayerState.LAYING;
                    isCardBeingLaidDown = false;
                    //currentCardSpot = null;
                }
            }

            if (State == PlayerState.DISCARDING)
            {
                if (cardToDiscard == null)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        var p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        RaycastHit2D hit = Physics2D.Raycast(p, Vector2.zero);
                        if (hit.collider != null)
                            cardToDiscard = hit.transform.GetComponent<Card>();
                    }
                }
                else
                {
                    cardToDiscard.SetInteractable(false);
                    DiscardCard(cardToDiscard);
                    cardToDiscard = null;
                }
            }

            if (State == PlayerState.LAYING && !isCardBeingLaidDown)
            {
                isCardBeingLaidDown = true;

                //if (layStage == LayStage.SINGLES)
                //{
                //    currentCardSpot = singleLayDownCards[currentCardIdx].CardSpot;
                //}
                //else if (currentCardSpot == null)
                //{
                //    currentCardSpot = PlayerCardSpotsNode.AddCardSpot();
                //    currentCardSpot.Type = (layStage == LayStage.RUNS) ? CardSpot.SpotType.RUN : CardSpot.SpotType.SET;
                //}

                //Card card = layStage switch
                //{
                //    LayStage.SETS => laydownCards.Sets[currentMeldIdx].Cards[currentCardIdx],
                //    LayStage.RUNS => laydownCards.Runs[currentMeldIdx].Cards[currentCardIdx],
                //    LayStage.SINGLES => singleLayDownCards[currentCardIdx].Card,
                //    _ => throw new RummyException($"Invalid lay stage: {layStage}")
                //};
                //HandCardSpot.RemoveCard(card);
                //if (!CardsVisible)
                //    card.SetTurned(false);
                //card.MoveFinished.AddListener(LayDownCardMoveFinished);
                //card.MoveCard(currentCardSpot.transform.position, Tb.I.GameMaster.AnimateCardMovement);
            }

            if (State == PlayerState.RETURNING_JOKER && !isJokerBeingReturned)
            {
                //isJokerBeingReturned = true;
                //var joker = singleLayDownCards[currentCardIdx].Joker;
                //currentCardSpot.RemoveCard(joker);
                //joker.MoveFinished.AddListener(ReturnJokerMoveFinished);
                //joker.MoveCard(HandCardSpot.transform.position, Tb.I.GameMaster.AnimateCardMovement);
            }
        }

        protected override Card GetCardToDraw()
        {
            return Tb.I.CardStack.DrawCard();
        }

        protected override void DrawCardMoveFinished(Card card, bool isServingCard)
        {
            base.DrawCardMoveFinished(card, isServingCard);

            card.SetInteractable(true);

            if (isServingCard)
                return;

            UpdatePossibleCombos();
            UpdatePossibleSingles(new CardCombo());
            State = PlayerState.PLAYING;
        }
    }

}