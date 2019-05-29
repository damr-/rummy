using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using romme.Cards;

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
            var combinations = new List<CardCombo>() { new CardCombo() };
            CardUtil.GetPossibleSetAndRunCombos(combinations, sets, runs);

            if (combinations.Last().PackCount > 0)
                combinations.Add(new CardCombo());
            //Check the possible runs when no sets are fixed
            CardUtil.GetPossibleRunCombos(combinations, runs, new CardCombo());

            combinations = combinations.Where(ldc => ldc.PackCount > 0).ToList();

            //Don't allow combinations which cannot be reduced further but would require the player to lay down all cards
            //for example don't allow laying down 3 sets of 3, when having 9 cards in hand in total
            //Havin 8 cards in hand, 2 sets of 4 is a valid combination since single cards can be kept on hand
            var validCombos = new List<CardCombo>();
            foreach (var c in combinations)
            {
                if (c.CardCount < handCardCount ||
                    (c.Sets.Any(set => set.Count > 3) || c.Runs.Any(run => run.Count > 3)))
                    validCombos.Add(c);
            }
            return validCombos;
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
        private static void GetPossibleSetAndRunCombos(List<CardCombo> fixedCardsList, /*CardCombo currentCombo ,*/ List<Set> sets, List<Run> runs)
        {
            //Iterate through all possible combinations of sets
            for (int i = 0; i < sets.Count; i++)
            {
                var oldEntry = new CardCombo(fixedCardsList.Last()); //Store the list of sets until now

                var fixedSet = sets[i]; //Assume we lay down the current set
                fixedCardsList.Last().AddSet(fixedSet); //The fixed list of set has to include the new one

                //Get all possible runs with the fixed set(s)
                List<Run> possibleRuns = runs.Where(run => !run.Intersects(fixedSet)).ToList();
                GetPossibleRunCombos(fixedCardsList, possibleRuns, new CardCombo());

                if (sets.Count > 1 && i < sets.Count - 1) //All the other sets with higher index are to be checked 
                {
                    //Only sets which don't intersect the fixed one are possible combinations
                    List<Set> otherSets = sets.GetRange(i + 1, sets.Count - i - 1).Where(set => !set.Intersects(fixedSet)).ToList();

                    if (otherSets.Count > 0)
                    {
                        //This fixed set alone is also a possibility
                        var newEntry = new CardCombo(fixedCardsList.Last());
                        fixedCardsList.Add(newEntry);

                        GetPossibleSetAndRunCombos(fixedCardsList, otherSets, possibleRuns);
                    }
                    else
                    {
                        fixedCardsList.Add(oldEntry);
                    }
                }
                else //Last set of the list has been reached
                {
                    oldEntry.RemoveLastSet(); //Go back one more layer...
                    fixedCardsList.Add(oldEntry); //...this permutation is done. Add new entry for the next one.
                }
            }
        }

        /// <summary>
        /// Returns all possible sets which can be laid down. Different sets do NOT contain one or more of the same card
        /// </summary>
        public static List<Set> GetPossibleSets(List<Card> PlayerCards)
        {
            List<Set> possibleSets = new List<Set>();
            List<KeyValuePair<Card.CardRank, List<Card>>> cardsByRank = new List<KeyValuePair<Card.CardRank, List<Card>>>();

            var cardPool = new List<Card>(PlayerCards);
            //The search for card sets has to be done multiple times since GetUniqueCardsByRank() could overlook a set
            //(e.g.: player has 6 J's, 2 cards each share the same suit, that's 2 full sets, but the function discards one)
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
        public static List<Run> GetPossibleRuns(this List<Card> PlayerCards)
        {
            var possibleRuns = new List<Run>();

            foreach (Card card in PlayerCards)
            {
                //KING cannot start a run
                if (card.Rank == Card.CardRank.KING)
                    continue;
                GetRuns(PlayerCards, possibleRuns, new List<Card>() { card }, true);
            }

            return possibleRuns;
        }

        private static void GetRuns(List<Card> PlayerCards, List<Run> runs, List<Card> currentRun, bool firstInRun)
        {
            Card card = currentRun.Last();
            List<Card> higherCards = GetCardOneRankHigher(PlayerCards, card, firstInRun);

            if (firstInRun)
                firstInRun = false;

            if (higherCards.Count == 0)
                return;

            foreach (Card c in higherCards)
            {
                List<Card> run = new List<Card>(currentRun);
                run.Add(c);

                if (run.Count > 2)
                    runs.Add(new Run(run));
                GetRuns(PlayerCards, runs, run, firstInRun);
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

            //A set can only consist of cards with the same rank
            if (cards.Any(c => c.Rank != cards[0].Rank))
                return false;

            var usedSuits = new List<Card.CardSuit>();
            for (int i = 0; i < cards.Count; i++)
            {
                var suit = cards[i].Suit;
                if (usedSuits.Contains(suit))
                    return false;
                usedSuits.Add(suit);
            }
            return true;
        }
    }

}