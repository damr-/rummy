using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using rummy.Cards;
using rummy.Utility;
using rummy.UI;

namespace rummy
{

    public class HumanPlayer : Player
    {
        private Card hoveredCard = null;
        private Card hoveredSingleJoker = null;
        private CardSpot hoveredSingleCardSpot = null;

        public UnityEvent<int> ComboSelectionChanged = new();
        /// <summary>Index of the highlit combo when a card is hovered</summary>
        private int selectedVariant = 1;
        private int possibleCombosCount = 0;
        private int possibleSinglesCount = 0;

        private bool isDiscarding = false;

        /// <summary>
        /// All possible single cards to lay down
        /// </summary>
        private List<Single> allPossibleSingles = new();

        public void EndTurn()
        {
            if (State == PlayerState.PLAYING)
                State = PlayerState.DISCARDING;
        }

        private Card GetClickedCard()
        {
            Card clickedCard = null;
            if (Input.GetMouseButtonDown(0))
            {
                var p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(p, Vector2.zero);
                if (hit.collider != null)
                    clickedCard = hit.transform.GetComponent<Card>();
            }
            return clickedCard;
        }

        private bool GetComboToLay(Card clickedCard)
        {
            var combo = GetSelectedComboVariant(clickedCard);

            if (combo != null && (HasLaidDown || combo.Value >= Tb.I.GameMaster.MinimumLaySum))
            {
                combo.GetCards().ForEach(c => c.SetInteractable(false, null));
                HasLaidDown = true;

                laydownCardCombo = combo;
                layStage = laydownCardCombo.Sets.Count == 0 ? LayStage.RUNS : LayStage.SETS;
                return true;
            }
            return false;
        }

        private bool GetSingleToLay(Card clickedCard)
        {
            var single = GetSelectedSingleVariant(clickedCard);

            if (single != null)
            {
                clickedCard.SetInteractable(false, null);

                laydownSingles = new() { single };
                layStage = LayStage.SINGLES;
                return true;
            }
            return false;
        }

        protected override void Update()
        {
            if (State == PlayerState.PLAYING)
            {
                if (!Tb.I.GameMaster.LayingAllowed() ||
                    ((allPossibleCardCombos.Count == 0 || (!HasLaidDown && allPossibleCardCombos[0].Value < Tb.I.GameMaster.MinimumLaySum))
                    && (allPossibleSingles.Count == 0 || !HasLaidDown)))
                {
                    State = PlayerState.DISCARDING;
                }
                else
                {
                    Card clickedCard = GetClickedCard();
                    if (clickedCard != null)
                    {
                        bool clickedCombo = GetComboToLay(clickedCard);
                        bool clickedSingle = false;
                        if (!clickedCombo && HasLaidDown)
                            clickedSingle = GetSingleToLay(clickedCard);
                        if (clickedCombo || clickedSingle)
                        {
                            RemoveAllHighlights(true);
                            State = PlayerState.LAYING;
                            isCardBeingLaidDown = false;
                            currentMeldIdx = 0;
                            currentCardIdx = 0;
                            currentCardSpot = null;
                        }
                    }
                }
            }

            if (State == PlayerState.DISCARDING && !isDiscarding)
            {
                Card clickedCard = GetClickedCard();
                if (clickedCard != null)
                {
                    isDiscarding = true;
                    RemoveAllHighlights(true);
                    clickedCard.SetInteractable(false, null);

                    DiscardCard(clickedCard);
                    UpdatePossibilities(false);
                }
            }

            if (hoveredCard != null)
            {
                int prevVariant = selectedVariant;
                if (Input.GetKeyDown(KeyCode.UpArrow))
                    selectedVariant -= 1;
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                    selectedVariant += 1;
                selectedVariant = Mathf.Max(0, Mathf.Min(possibleCombosCount + possibleSinglesCount - 1, selectedVariant));

                if (selectedVariant != prevVariant)
                {
                    RemoveAllHighlights(false);
                    if (selectedVariant < possibleCombosCount)
                        HighlightSelectedCombo();
                    else
                        HighlightSelectedSingleVariant();
                }
            }
            base.Update();
        }

        protected override Card GetCardToDraw()
        {
            return Tb.I.CardStack.DrawCard();
        }

        protected override void DrawCardMoveFinished(Card card, bool isServingCard)
        {
            base.DrawCardMoveFinished(card, isServingCard);
            card.SetInteractable(true, HandCardHovered);
            if (!isServingCard)
                UpdatePossibilities(true);
        }

        private void HandCardHovered(Card card, bool hovered)
        {
            if (hovered)
            {
                hoveredCard = card;

                possibleCombosCount = 0;
                foreach (var combo in allPossibleCardCombos)
                {
                    if (combo.GetCards().Contains(hoveredCard))
                        possibleCombosCount += 1;
                }
                possibleSinglesCount = 0;
                foreach (var single in allPossibleSingles)
                {
                    if (single.Card == hoveredCard)
                        possibleSinglesCount += 1;
                }

                selectedVariant = 0;
                if (selectedVariant < possibleCombosCount)
                    HighlightSelectedCombo();
                else
                    HighlightSelectedSingleVariant();
            }
            else
            {
                RemoveAllHighlights(true);
            }
        }

        private void RemoveAllHighlights(bool unsetHoveredCard)
        {
            if (unsetHoveredCard)
                hoveredCard = null;
            foreach (var c in HandCardSpot.Objects)
                c.GetComponent<HoverInfo>().SetHovered(false);
            if (hoveredSingleJoker != null)
            {
                hoveredSingleJoker.GetComponent<HoverInfo>().SetHovered(false);
                hoveredSingleJoker = null;
            }
            else if(hoveredSingleCardSpot != null)
            {
                hoveredSingleCardSpot.Objects.ForEach(c => c.GetComponent<HoverInfo>().SetHovered(false));
                hoveredSingleCardSpot = null;
            }
        }

        private void HighlightSelectedCombo()
        {
            var combo = GetSelectedComboVariant(hoveredCard);
            if (combo != null)
                combo.GetCards().ForEach(c => c.GetComponent<HoverInfo>().SetHovered(true));
        }

        private CardCombo GetSelectedComboVariant(Card focussedCard)
        {
            int counter = 0;
            foreach (var combo in allPossibleCardCombos)
            {
                var comboCards = combo.GetCards();
                if (comboCards.Contains(focussedCard))
                {
                    if (counter == selectedVariant)
                        return combo;
                    counter += 1;
                }
            }
            return null;
        }

        private void HighlightSelectedSingleVariant()
        {
            var single = GetSelectedSingleVariant(hoveredCard);
            if (single != null)
            {
                single.Card.GetComponent<HoverInfo>().SetHovered(true);
                hoveredSingleJoker = single.Joker;
                if (hoveredSingleJoker != null)
                    hoveredSingleJoker.GetComponent<HoverInfo>().SetHovered(true);
                else
                {
                    hoveredSingleCardSpot = single.CardSpot;
                    hoveredSingleCardSpot.Objects.ForEach(c => c.GetComponent<HoverInfo>().SetHovered(true));
                }
            }
        }

        private Single GetSelectedSingleVariant(Card focussedCard)
        {
            int counter = 0;
            foreach (var single in allPossibleSingles)
            {
                if (single.Card == focussedCard)
                {
                    if (counter == selectedVariant - possibleCombosCount)
                        return single;
                    counter += 1;
                }
            }
            return null;
        }

        protected override void LayingCardsDone()
        {
            UpdatePossibilities(true);
        }

        protected override void ReturnJokerMoveFinished(Card joker)
        {
            base.ReturnJokerMoveFinished(joker);
            joker.SetInteractable(true, HandCardHovered);
            UpdatePossibilities(true);
        }

        private void UpdatePossibilities(bool setToPlaying)
        {
            allPossibleCardCombos = UpdatePossibleCombos();
            allPossibleSingles = UpdatePossibleSingles(new CardCombo(), true);
            if(setToPlaying)
                State = PlayerState.PLAYING;
        }

        protected override void DiscardCardMoveFinished(Card card)
        {
            base.DiscardCardMoveFinished(card);
            isDiscarding = false;
        }
    }

}