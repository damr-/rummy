using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using romme.Cards;

namespace romme.Utility
{

    public static class CardUtil
    {
        public static LaydownCards GetHighestValueCombination(List<LaydownCards> combinations)
        {
            LaydownCards bestValueCombination = new LaydownCards();
            int curVal = 0, highestVal = 0;
            foreach (LaydownCards possibility in combinations)
            {
                if (possibility.PackCount == 0)
                    continue;

                curVal = possibility.Value;
                if (curVal > highestVal)
                {
                    highestVal = curVal;
                    bestValueCombination = new LaydownCards(possibility);
                }
            }
            return bestValueCombination;
        }

        public static void GetPossibleRunCombinations(List<LaydownCards> fixedCardsList, List<Run> runs)
        {
            for (int i = 0; i < runs.Count; i++)
            {
                var oldEntry = new LaydownCards(fixedCardsList.Last()); //Store the list of runs until now

                var fixedRun = runs[i];
                fixedCardsList.Last().AddRun(fixedRun);    //Add the newly fixed run            

                if (runs.Count > 1 && i < runs.Count - 1) //Only check for other runs if this is not the last one
                {
                    //All the other runs with higher index and wich do not intersect with the fixedRun are to be checked
                    List<Run> otherRuns = runs.GetRange(i + 1, runs.Count - i - 1).Where(run => !run.Intersects(fixedRun)).ToList();

                    if (otherRuns.Count > 0)
                    {
                        //This fixed run alone is also a possibility
                        var newEntry = new LaydownCards(fixedCardsList.Last());
                        fixedCardsList.Add(newEntry);

                        GetPossibleRunCombinations(fixedCardsList, otherRuns);
                    }
                    else //There are no possible runs that can be laid down with the fixed one
                    {
                        //revert to the previous fixed one and try the other runs in the list of runs
                        fixedCardsList.Add(oldEntry);
                    }
                }
                else
                {
                    oldEntry.RemoveLastRun();   //The last element of the current list of runs has been reached
                                                //therefore we need to go back one layer more to prepare he next perms
                    fixedCardsList.Add(oldEntry); //Add the entry for the next possible perm
                }
            }
        }

        /// <summary>
        /// Calculates all possible combinations of card packs that could be laid down.
        /// Stores the result in the passed List<LaydownCards> combinations
        /// </summary>
        //FIXME: Is this not basically the same as GetPossibleRunCombinations? maybe make <T> functions
        public static void GetPossibleCombinations(List<LaydownCards> fixedCardsList, List<Set> sets, List<Run> runs)
        {
            //Iterate through all possible combinations of sets
            for (int i = 0; i < sets.Count; i++)
            {
                var oldEntry = new LaydownCards(fixedCardsList.Last()); //Store the list of sets until now

                var fixedSet = sets[i]; //Assume we lay down the current set
                fixedCardsList.Last().AddSet(fixedSet); //The fixed list of set has to include the new one

                //Get all possible runs with the fixed set(s)
                List<Run> possibleRuns = runs.Where(run => !run.Intersects(fixedSet)).ToList();
                GetPossibleRunCombinations(fixedCardsList, possibleRuns);

                if (sets.Count > 1 && i < sets.Count - 1) //All the other sets with higher index are to be checked 
                {
                    //Only sets which don't intersect the fixed one are possible combinations
                    List<Set> otherSets = sets.GetRange(i + 1, sets.Count - i - 1).Where(set => !set.Cards.Intersects(fixedSet.Cards)).ToList();

                    if (otherSets.Count > 0)
                    {
                        //This fixed set alone is also a possibility
                        var newEntry = new LaydownCards(fixedCardsList.Last());
                        fixedCardsList.Add(newEntry);

                        GetPossibleCombinations(fixedCardsList, otherSets, possibleRuns);
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
        /// Returns the first card in PlayerCards which has the same suit and is one rank higher.
        /// 'firstInRun': whether 'card' is the first card in a run. Used to determine whether ACE can connect to TWO or to KING
        /// </summary>
        /// <returns> The card or null if none was found </returns>
        public static Card GetCardOneRankHigher(List<Card> PlayerCards, Card card, bool firstInRun)
        {
            foreach (Card otherCard in PlayerCards)
            {
                if (otherCard == card || otherCard.Suit != card.Suit)
                    continue;
                //Allow going from ACE to TWO but only if ACE is the first card in the run
                if (firstInRun && card.Rank == Card.CardRank.ACE && otherCard.Rank == Card.CardRank.TWO)
                    return otherCard;
                else if (otherCard.Rank == card.Rank + 1)
                    return otherCard;
            }
            return null;
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
        /// Different runs CAN contain one or more of the same card since runs can be of any length > 2s
        /// <summary>
        public static List<Run> GetPossibleRuns(this List<Card> PlayerCards)
        {
            var runs = new List<Run>();

            foreach (Card card in PlayerCards)
            {
                //KING cannot start a run
                if (card.Rank == Card.CardRank.KING)
                    continue;

                List<Card> run = new List<Card>();
                Card nextCard = card;
                bool firstInRun = true;

                do
                {
                    run.Add(nextCard);

                    if (run.Count >= 3)
                    {
                        Run newRun = new Run(run);
                        runs.Add(newRun);
                    }
                    nextCard = GetCardOneRankHigher(PlayerCards, nextCard, firstInRun);
                    if (firstInRun)
                        firstInRun = false;
                } while (nextCard != null);
            }
            return runs;
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