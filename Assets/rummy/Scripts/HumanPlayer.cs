using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using rummy.Cards;
using rummy.Utility;
using rummy.UI;
using rummy.UI.CardOutput;

namespace rummy
{

    public class HumanPlayer : Player
    {
        public TextMeshProUGUI selectedCardDisplay;
        private string defaultSelectedCardDisplayText = "";

        public UnityEvent<int> ComboSelectionChanged = new();

        /// <summary>All possible combos of melds to lay down</summary>
        private List<CardCombo> allPossibleCardCombos = new();
        /// <summary>All possible single cards to lay down</summary>
        private List<Single> allPossibleSingles = new();

        private bool isDiscarding = false;

        /// <summary>Index of the highlit combo when a card is hovered</summary>
        private int selectedVariant = 1;
        private int possibleCombosCount = 0;
        private int possibleSinglesCount = 0;

        private Card hoveredCard = null;
        private Card hoveredSinglePartner = null;
        private CardSpot hoveredSingleCardSpot = null;

        private void Start()
        {
            defaultSelectedCardDisplayText = selectedCardDisplay.text;
        }

        public void SwitchStage()
        {
            if (State == PlayerState.PLAYING)
                State = PlayerState.DISCARDING;
            else if (State == PlayerState.DISCARDING && !isDiscarding)
                State = PlayerState.PLAYING;
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

        private CardCombo GetComboToLay(Card clickedCard)
        {
            var combo = GetSelectedComboVariant(clickedCard);

            if (combo != null)
            {
                if (HasLaidDown || combo.Value >= Tb.I.GameMaster.MinimumLaySum)
                {
                    combo.GetCards().ForEach(c => c.SetInteractable(false, null));
                    HasLaidDown = true;

                    laydownCardCombo = combo;
                    layStage = laydownCardCombo.Sets.Count == 0 ? LayStage.RUNS : LayStage.SETS;
                }
                else
                    combo = null;
            }
            return combo;
        }

        private Single GetSingleToLay(Card clickedCard)
        {
            var single = GetSelectedSingleVariant(clickedCard);

            if (single != null)
            {
                clickedCard.SetInteractable(false, null);

                laydownSingles = new() { single };
                layStage = LayStage.SINGLES;
            }
            return single;
        }

        protected override void Update()
        {
            if (State == PlayerState.PLAYING)
            {
                if (!Tb.I.GameMaster.LayingAllowed() || HandCardCount == 1 ||
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
                        CardCombo combo = GetComboToLay(clickedCard);
                        Single single = null;
                        if (combo == null && HasLaidDown)
                            single = GetSingleToLay(clickedCard);
                        if (combo != null || single != null)
                        {
                            List<Card> ignoredCards = (single != null) ? new() { single.Card } : combo.GetCards();
                            UpdatePossibilities(false, ignoredCards);
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
                    clickedCard.SetInteractable(false, null);
                    RemoveAllHighlights(true);

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
            if (hoveredSinglePartner != null)
            {
                hoveredSinglePartner.GetComponent<HoverInfo>().SetHovered(false);
                hoveredSinglePartner = null;
            }
            if (hoveredSingleCardSpot != null)
            {
                hoveredSingleCardSpot.Objects.ForEach(c => c.GetComponent<HoverInfo>().SetHovered(false));
                hoveredSingleCardSpot = null;
            }
            selectedCardDisplay.text = defaultSelectedCardDisplayText;
        }

        private void HighlightSelectedCombo()
        {
            var combo = GetSelectedComboVariant(hoveredCard);
            if (combo != null)
            {
                int counter = 0;
                foreach (Set set in combo.Sets)
                {
                    foreach (var card in set.Cards)
                        card.GetComponent<HoverInfo>().SetHovered(true, CardCombosUI.highlightColors[counter]);
                    counter++;
                }
                foreach (Run run in combo.Runs)
                {
                    foreach (var card in run.Cards)
                        card.GetComponent<HoverInfo>().SetHovered(true, CardCombosUI.highlightColors[counter]);
                    counter++;
                }

                bool greyedOut = !HasLaidDown && combo.Value < Tb.I.GameMaster.MinimumLaySum;
                string outputText = CardCombosUI.GetComboOutput(combo, greyedOut, true);
                selectedCardDisplay.text = $"<b>{outputText}</b>";
            }
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
            Single single = GetSelectedSingleVariant(hoveredCard);
            if (single != null)
            {
                single.Card.GetComponent<HoverInfo>().SetHovered(true);
                if (single.Joker != null)
                {
                    hoveredSinglePartner = single.Joker;
                }
                else if (single.Spot > -1)
                {
                    if (single.Spot == single.CardSpot.Objects.Count)
                        hoveredSinglePartner = single.CardSpot.Objects[^1];
                    else
                        hoveredSinglePartner = single.CardSpot.Objects[0];
                }

                if (hoveredSinglePartner != null)
                {
                    hoveredSinglePartner.GetComponent<HoverInfo>().SetHovered(true);
                }
                else
                {
                    hoveredSingleCardSpot = single.CardSpot;
                    hoveredSingleCardSpot.Objects.ForEach(c => c.GetComponent<HoverInfo>().SetHovered(true));
                }

                selectedCardDisplay.text = $"<b>{SinglesUI.GetSingleOutput(single)}</b>";
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

        private void UpdatePossibilities(bool setToPlaying, List<Card> ignoredCards = null)
        {
            allPossibleCardCombos = UpdatePossibleCombos(ignoredCards);

            List<Card> cardPool = HandCardSpot.Objects;
            if (ignoredCards != null)
                cardPool = cardPool.Except(ignoredCards).ToList();
            allPossibleSingles = PlayerUtil.GetAllSingleLaydownCards(cardPool);
            PossibleSinglesChanged.Invoke(allPossibleSingles);

            if (setToPlaying)
                State = PlayerState.PLAYING;
        }

        protected override void DiscardCardMoveFinished(Card card)
        {
            base.DiscardCardMoveFinished(card);
            isDiscarding = false;
        }
    }

}