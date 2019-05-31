using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using romme.Cards;
using System;

namespace romme.Utility
{

    public static class CardUtil
    {
        // public static CardCombo GetHighestValueCombination(List<CardCombo> combinations)
        // {
        //     CardCombo bestValueCombination = new CardCombo();
        //     int curVal = 0, highestVal = 0;
        //     foreach (CardCombo possibility in combinations)
        //     {
        //         if (possibility.PackCount == 0)
        //             continue;

        //         curVal = possibility.Value;
        //         if (curVal > highestVal)
        //         {
        //             highestVal = curVal;
        //             bestValueCombination = new CardCombo(possibility);
        //         }
        //     }
        //     return bestValueCombination;
        // }

        /// <summary>
        /// Get all possible card combinations which could be created from the given sets and runs
        /// 'handCardCount' is the number of cards on the player's hand
        /// </summary>
        public static List<CardCombo> GetAllPossibleCombos(List<Set> sets, List<Run> runs, int handCardCount)
        {
            var combos = new List<CardCombo>();
            CardUtil.GetPossibleSetAndRunCombos(combos, sets, runs, new CardCombo());
            CardUtil.GetPossibleRunCombos(combos, runs, new CardCombo());
            combos = combos.Where(ldc => ldc.PackCount > 0).ToList();

            //Don't allow combinations which cannot be reduced further but would require the player to lay down all cards
            //for example don't allow laying down 3 sets of 3, when having 9 cards in hand in total
            //Having 8 cards in hand, 2 sets of 4 is a valid combination since single cards can be kept on hand
            var possibleCombos = new List<CardCombo>();
            foreach (var combo in combos)
            {
                if (combo.CardCount < handCardCount ||
                    (combo.Sets.Any(set => set.Count > 3) || combo.Runs.Any(run => run.Count > 3)))
                    possibleCombos.Add(combo);
            }
            return possibleCombos;
        }

        private static void GetPossibleRunCombos(List<CardCombo> possibleRunCombos, List<Run> runs, CardCombo currentRunCombo)
        {
            for (int i = 0; i < runs.Count; i++)
            {
                CardCombo cc = new CardCombo(currentRunCombo);
                cc.AddRun(runs[i]);

                //This fixed run alone is also a possibility
                possibleRunCombos.Add(cc);

                List<Run> otherRuns = runs.GetRange(i + 1, runs.Count - (i + 1)).Where(run => !run.Intersects(runs[i])).ToList();
                if (otherRuns.Count > 0)
                    GetPossibleRunCombos(possibleRunCombos, otherRuns, cc);
            }
        }

        /// <summary>
        /// Calculates all possible combinations of card packs that could be laid down.
        /// Stores the result in the passed List<LaydownCards> 'combinations'
        /// </summary>
        private static void GetPossibleSetAndRunCombos(List<CardCombo> possibleCombos, List<Set> sets, List<Run> runs, CardCombo currentCombo)
        {
            for (int i = 0; i < sets.Count; i++)
            {
                CardCombo cc = new CardCombo(currentCombo);
                cc.AddSet(sets[i]);

                //This fixed set alone is also a possibility
                possibleCombos.Add(cc);

                //Get all runs which are possible with the current set fixed
                List<Run> possibleRuns = runs.Where(run => !run.Intersects(sets[i])).ToList();
                GetPossibleRunCombos(possibleCombos, possibleRuns, cc);

                //Only sets which don't intersect the current one are possible combinations
                List<Set> otherSets = sets.GetRange(i + 1, sets.Count - i - 1).Where(set => !set.Intersects(sets[i])).ToList();
                if (otherSets.Count > 0)
                    GetPossibleSetAndRunCombos(possibleCombos, otherSets, possibleRuns, cc);
            }
        }

        /// <summary>
        /// Returns all possible sets which consist of 3 or 4 cards.
        /// </summary>
        public static List<Set> GetPossibleSets(List<Card> PlayerCards)
        {
            List<Set> possibleSets = new List<Set>();

            var cardsByRank = PlayerCards.GetCardsByRank().Where(rank => rank.Key != Card.CardRank.JOKER).ToList();

            foreach (var rank in cardsByRank)
                GetPossibleSets(possibleSets, rank.Value, new List<Card>());

            return possibleSets;
        }

        private static void GetPossibleSets(List<Set> possibleSets, List<Card> availableCards, List<Card> currentSet)
        {
            for (int i = 0; i < availableCards.Count; i++)
            {
                Card card = availableCards[i];
                var newSet = new List<Card>(currentSet);
                newSet.Add(card);

                if (newSet.Count > 4)
                    continue;

                if (IsValidSet(newSet))
                {
                    if (newSet.Count == 3 || newSet.Count == 4)
                        possibleSets.Add(new Set(newSet));
                }

                List<Card> otherCards = availableCards.
                        GetRange(i + 1, availableCards.Count - (i + 1)).
                        Where(c => c.Suit != card.Suit).
                        ToList();
                GetPossibleSets(possibleSets, otherCards, newSet);
            }
        }

        /// <summary>
        /// Returns all possible runs that could be laid down. 
        /// Different runs CAN contain one or more of the same card since runs can be of any length > 2
        /// <summary>
        public static List<Run> GetPossibleRuns(List<Card> PlayerCards)
        {
            var possibleRuns = new List<Run>();
            GetPossibleRuns(possibleRuns, PlayerCards, PlayerCards, new List<Card>());
            return possibleRuns;
        }

        private static void GetPossibleRuns(List<Run> possibleRuns, List<Card> PlayerCards, List<Card> availableCards, List<Card> currentRun)
        {
            foreach (Card card in availableCards)
            {
                //KING cannot start a run
                if (currentRun.Count == 0 && card.Rank == Card.CardRank.KING)
                    continue;

                var newRun = new List<Card>(currentRun);
                newRun.Add(card);

                if (newRun.Count >= 3)
                    possibleRuns.Add(new Run(newRun));

                List<Card> higherCards = GetCardOneRankHigher(PlayerCards, newRun.Last(), newRun.Count == 1);
                GetPossibleRuns(possibleRuns, PlayerCards, higherCards, newRun);
            }
        }

        /// <summary>
        /// Returns all cards in PlayerCards which have the same suit and are one rank higher than 'card'.
        /// 'firstInRun': whether 'card' is the first card in a run. Used to determine whether ACE can connect to TWO or to KING
        /// </summary>
        /// <returns> The card or null if none was found </returns>
        public static List<Card> GetCardOneRankHigher(List<Card> PlayerCards, Card card, bool firstInRun)
        {
            List<Card> foundCards = new List<Card>();
            foreach (Card otherCard in PlayerCards)
            {
                if (otherCard == card || otherCard.Suit != card.Suit || foundCards.Contains(otherCard))
                    continue;
                //Allow going from ACE to TWO but only if ACE is the first card in the run
                if (firstInRun && card.Rank == Card.CardRank.ACE && otherCard.Rank == Card.CardRank.TWO)
                    foundCards.Add(otherCard);
                else if (otherCard.Rank == card.Rank + 1)
                    foundCards.Add(otherCard);
            }
            return foundCards;
        }

        /// <summary>
        /// Returns all possible sets which can be created using joker cards
        /// excluding cards used in  'possibleSets' and 'possibleRuns'
        /// </summary>
        public static List<Set> GetPossibleJokerSets(List<Card> PlayerCards, List<Set> possibleSets, List<Run> possibleRuns)
        {
            var duoSetsByRank = GetAllDuosByRank(PlayerCards);

            //Stop here if no duos were found
            if (duoSetsByRank.Count == 0)
                return new List<Set>();

            List<Card[]> duos = new List<Card[]>();
            foreach (var entry in duoSetsByRank)
                duos.AddRange(entry.Value);

            var possibleJokerSets = new List<Set>();
            List<Card> jokerCards = PlayerCards.Where(c => c.IsJoker()).ToList();

            // First, create trios out of duos where both cards have the same color
            // for each trio, save all posslbe combinations with available jokers
            for (int i = 0; i < 2; i++)
            {
                Card.CardColor color = (Card.CardColor)i;
                var sameColorDuos = duos.Where(duo => duo.All(c => c.Color == color));

                foreach (var duo in sameColorDuos)
                {
                    //Find all available jokers with the opposite color
                    var possibleJokers = jokerCards.Where(j => j.Color != color);
                    foreach (var joker in possibleJokers)
                    {
                        var newSet = new Set(duo[0], duo[1], joker);
                        possibleJokerSets.Add(newSet);
                    }
                }
            }

            // Now finish all red-black duos with all possible joker cards
            var mixedDuos = duos.Where(duo => duo[0].Color != duo[1].Color);
            foreach (var duo in mixedDuos)
            {
                foreach (var jokerCard in jokerCards)
                {
                    var newSet = new Set(duo[0], duo[1], jokerCard);
                    possibleJokerSets.Add(newSet);
                }
            }

            return possibleJokerSets;
        }

        /// <summary>
        /// Returns all possible duos (sets of two cards with different suits) from 'PlayerCards', sorted by rank
        /// </summary>
        private static List<KeyValuePair<Card.CardRank, List<Card[]>>> GetAllDuosByRank(List<Card> PlayerCards)
        {
            var allDuos = new List<KeyValuePair<Card.CardRank, List<Card[]>>>();

            var cardsByRank =
                    PlayerCards.GetUniqueCardsByRank()
                        .Where(entry => entry.Key != Card.CardRank.JOKER && entry.Value.Count >= 2).ToList();


            foreach (var entry in cardsByRank)
            {
                List<Card[]> currentDuos = new List<Card[]>();
                foreach (Card c1 in entry.Value)
                {
                    for (int j = entry.Value.IndexOf(c1); j < entry.Value.Count; j++)
                    {
                        Card c2 = entry.Value[j];
                        if (c1.Suit != c2.Suit)
                            currentDuos.Add(new Card[] { c1, c2 });
                    }
                }
                allDuos.Add(new KeyValuePair<Card.CardRank, List<Card[]>>(entry.Key, currentDuos));
            }
            return allDuos;
        }

        private static void asd(List<List<Card>> list, List<Card> other, List<Card> current)
        {
            for (int i = 0; i < other.Count; i++)
            {
                Card card = other[i];
                var newCurrent = new List<Card>(current);
                newCurrent.Add(card);

                if (newCurrent.Count == 1)
                {
                    var newOther = other.GetRange(i + 1, other.Count - (i + 1)).Where(c => c.Suit != card.Suit).ToList();
                    asd(list, newOther, newCurrent);
                }
                else if (newCurrent.Count == 2)
                {
                    list.Add(newCurrent);
                }
            }
        }

        public static bool IsValidRun(List<Card> cards)
        {
            if (cards.Count == 0)
                return false;

            //A run can only consist of cards with the same suit (or joker with the matching color)
            Card representiveCard = cards.GetFirstCard();
            foreach (var card in cards)
            {
                if (!card.IsJoker())
                {
                    if (card.Suit != representiveCard.Suit)
                        return false;
                }
                else
                {
                    if (card.Color != representiveCard.Color)
                        return false;
                }
            }

            for (int i = 0; i < cards.Count - 1; i++)
            {
                //Ace can be the start of a run
                if (i == 0 && cards[i].Rank == Card.CardRank.ACE)
                {
                    //Run is only valid if next card is a TWO or a JOKER
                    if (cards[i + 1].Rank != Card.CardRank.TWO && !cards[i + 1].IsJoker())
                        return false;
                }
                //otherwise, rank has to increase by one
                else if (cards[i + 1].Rank != cards[i].Rank + 1 && !cards[i].IsJoker() && !cards[i + 1].IsJoker())
                    return false;
            }
            return true;
        }

        public static bool IsValidSet(List<Card> cards)
        {
            if (cards.Count < 3 || cards.Count > 5)
                return false;

            if (cards.Count == 5)
                return false;

            //A set can only consist of cards with the same rank and/or a joker
            if (cards.Any(c => !c.IsJoker() && c.Rank != cards.GetFirstCard().Rank))
                return false;

            var usedSuits = new List<Card.CardSuit>();
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].IsJoker())
                    continue; //Skip checking joker for now

                var suit = cards[i].Suit;
                if (usedSuits.Contains(suit))
                    return false;
                usedSuits.Add(suit);
            }

            //Check joker now if necessary
            Card joker = cards.Where(c => c.IsJoker()).FirstOrDefault();
            if (joker != null)
            {
                if (joker.IsBlack()
                    && usedSuits.Contains(Card.CardSuit.CLOVERS)
                    && usedSuits.Contains(Card.CardSuit.PIKE))
                    return false;
                if (joker.IsRed()
                    && usedSuits.Contains(Card.CardSuit.HEART)
                    && usedSuits.Contains(Card.CardSuit.TILE))
                    return false;
            }
            return true;
        }
    }

}