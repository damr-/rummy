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
        /// Returns all possible sets which consist of 3 cards.
        /// All possible permutations of complete quadruples will also be returned
        /// </summary>
        public static List<Set> GetPossibleSets(List<Card> PlayerCards)
        {
            List<Set> possibleSets = new List<Set>();
            List<KeyValuePair<Card.CardRank, List<Card>>> cardsByRank = new List<KeyValuePair<Card.CardRank, List<Card>>>();

            var cardPool = new List<Card>(PlayerCards);
            //The search for card sets has to be done multiple times since GetUniqueCardsByRank() could overlook a set
            //(e.g.: player has 6 J's, 2 cards each share the same suit, that's 2 full sets, but the function would discard one)
            do
            {
                //Remove all cards of previously found sets from the card pool
                foreach (var set in possibleSets)
                {
                    foreach (var card in set.Cards)
                        cardPool.Remove(card);
                }
                //Get all possible trios in the cardpool, sorted by rank
                cardsByRank = cardPool.GetUniqueCardsByRank().Where(rank => rank.Key != Card.CardRank.JOKER && rank.Value.Count == 3).ToList();
                foreach (var rank in cardsByRank)
                {
                    Set newSet = new Set(rank.Value);
                    possibleSets.Add(newSet);
                }

                //Get all possible trio perms from all quadruples in the cardpool, sorted by rank
                cardsByRank = cardPool.GetUniqueCardsByRank().Where(rank => rank.Key != Card.CardRank.JOKER && rank.Value.Count == 4).ToList();
                foreach (var rank in cardsByRank)
                {
                    //Add the quadruple
                    Set newSet = new Set(rank.Value);
                    possibleSets.Add(newSet);
                    //Add all trio permutations
                    newSet = new Set(rank.Value.GetRange(0, 3));
                    possibleSets.Add(newSet);
                    newSet = new Set(rank.Value.GetRange(1, 3));
                    possibleSets.Add(newSet);
                    newSet = new Set(rank.Value[0], rank.Value[2], rank.Value[3]);
                    possibleSets.Add(newSet);
                    newSet = new Set(rank.Value[0], rank.Value[1], rank.Value[3]);
                    possibleSets.Add(newSet);
                }
            } while (cardsByRank.Count > 0);

            return possibleSets;
        }
        
        /// <summary>
        /// Returns all possible runs that could be laid down. 
        /// Different runs CAN contain one or more of the same card since runs can be of any length > 2
        /// <summary>
        public static List<Run> GetPossibleRuns(List<Card> PlayerCards, int minLength = 3)
        {
            var possibleRuns = new List<Run>();
            GetPossibleRuns(possibleRuns, PlayerCards, PlayerCards, new List<Card>(), minLength);
            return possibleRuns;
        }

        private static void GetPossibleRuns(List<Run> possibleRuns, List<Card> PlayerCards, List<Card> availableCards, List<Card> currentRun, int minLength)
        {
            foreach (Card card in availableCards)
            {
                //KING cannot start a run
                if (currentRun.Count == 0 && card.Rank == Card.CardRank.KING)
                    continue;

                var newRun = new List<Card>(currentRun);
                newRun.Add(card);

                if (newRun.Count >= minLength)
                    possibleRuns.Add(new Run(newRun));

                List<Card> higherCards = GetCardOneRankHigher(PlayerCards, newRun.Last(), newRun.Count == 1);
                GetPossibleRuns(possibleRuns, PlayerCards, higherCards, newRun, minLength);
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
        /// Returns all possible sets which can be created using joker cards, excluding cards used in 
        /// 'possibleSets' and 'possibleRuns'
        /// </summary>
        public static List<Set> GetPossibleJokerSets(List<Card> PlayerCards, List<Set> possibleSets, List<Run> possibleRuns)
        {
            var possibleJokerSets = new List<Set>();

            var duoSets = PlayerCards.GetCardsByRank()
                    .Where(entry => entry.Key != Card.CardRank.JOKER && entry.Value.Count == 2);
            List<List<Card>> possibleDuos = new List<List<Card>>();
            foreach (var duo in duoSets)
            {
                if (possibleSets.All(set => !set.Cards.Intersects(duo.Value)))
                    possibleDuos.Add(duo.Value);
            }

            List<Card> jokerCards = PlayerCards.Where(c => c.Rank == Card.CardRank.JOKER).ToList();
            int jokerCount = jokerCards.Count;
            List<Card> usedJokers = new List<Card>();

            foreach (var duo in possibleDuos)
            {
                var possibleJokers = jokerCards.Except(usedJokers);
                if (duo.All(c => c.Color == Card.CardColor.BLACK))
                    possibleJokers = possibleJokers.Where(c => c.Color == Card.CardColor.RED);
                else if (duo.All(c => c.Color == Card.CardColor.RED))
                    possibleJokers = possibleJokers.Where(c => c.Color == Card.CardColor.BLACK);

                if (possibleJokers.Count() > 0)
                {
                    var newSet = new Set(duo[0], duo[1], possibleJokers.First());
                    possibleJokerSets.Add(newSet);
                    usedJokers.Add(possibleJokers.First());
                }
            }

            return possibleJokerSets;
        }

        public static bool IsValidRun(List<Card> cards)
        {
            if (cards.Count == 0)
                return false;

            //A run can only consist of cards with the same suit
            Card.CardSuit runSuit = (cards[0].Rank != Card.CardRank.JOKER) ? cards[0].Suit : cards[1].Suit;
            if (cards.Any(c => c.Suit != runSuit))
                return false;

            for (int i = 0; i < cards.Count - 1; i++)
            {
                //Ace can be the start of a run
                if (i == 0 && cards[i].Rank == Card.CardRank.ACE)
                {
                    //Run is only valid if next card is a TWO or a JOKER
                    if (cards[i + 1].Rank != Card.CardRank.TWO && cards[i + 1].Rank != Card.CardRank.JOKER)
                        return false;
                }//otherwise, rank has to increase by one
                else if (cards[i + 1].Rank != cards[i].Rank + 1 && cards[i + 1].Rank != Card.CardRank.JOKER)
                    return false;
            }
            return true;
        }

        public static bool IsValidSet(List<Card> cards)
        {
            if (cards.Count == 0)
                return false;

            Card.CardRank setRank = (cards[0].Rank != Card.CardRank.JOKER ? cards[0].Rank : cards[1].Rank);

            //A set can only consist of cards with the same rank
            if (cards.Any(c => c.Rank != setRank && c.Rank != Card.CardRank.JOKER))
                return false;

            var usedSuits = new List<Card.CardSuit>();
            for (int i = 0; i < cards.Count; i++)
            {
                if(cards[i].Rank == Card.CardRank.JOKER)
                    continue; //Skip checking joker for now

                var suit = cards[i].Suit;
                if (usedSuits.Contains(suit))
                    return false;
                usedSuits.Add(suit);
            }

            //Check joker now if necessary
            Card joker = cards.Where(c => c.Rank == Card.CardRank.JOKER).FirstOrDefault();
            if(joker != null)
            {
                if(joker.Color == Card.CardColor.BLACK 
                    && usedSuits.Contains(Card.CardSuit.CLOVERS)
                    && usedSuits.Contains(Card.CardSuit.PIKE))
                    return false;
                if(joker.Color == Card.CardColor.RED 
                    && usedSuits.Contains(Card.CardSuit.HEART)
                    && usedSuits.Contains(Card.CardSuit.TILE))
                    return false;
            }
            return true;
        }
    }

}