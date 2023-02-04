using System.Linq;
using System.Collections.Generic;
using rummy.Cards;

namespace rummy.Utility
{

    public static class CardUtil
    {
        /// <summary>
        /// Return all combos which don't have any cards in common, sorted descending by value
        /// </summary>
        public static List<CardCombo> GetAllUniqueCombos(List<Card> HandCards, List<Card> LaidDownCards)
        {
            var allCombos = GetAllPossibleCombos(HandCards, LaidDownCards, false);
            var uniqueCombos = new List<CardCombo>();

            foreach (var combo in allCombos)
            {
                var isUnique = true;
                foreach (var uCombo in uniqueCombos)
                {
                    if (combo.Intersects(uCombo))
                    {
                        isUnique = false;
                        break;
                    }
                }
                if (isUnique)
                    uniqueCombos.Add(combo);
            }

            return uniqueCombos;
        }

        /// <summary>
        /// Return a sorted (descending, by value) list of all possible combos which
        /// could be laid down, with sets and runs as well as joker combinations extracted from the given 'HandCards'.
        /// <param name="allowLayingAll"> Whether combos are allowed which would require the player to lay down all cards from his hand ('HandCards').
        /// This is usually not useful, unless hypothetical hands are examined, where one card was removed before. </param>
        /// </summary>
        public static List<CardCombo> GetAllPossibleCombos(List<Card> HandCards, List<Card> LaidDownCards, bool allowLayingAll)
        {
            //var jokerCards = HandCards.Where(c => !c.IsJoker());
            var nonJokerCards = HandCards.Where(c => !c.IsJoker());

            var sets = new List<Set>();
            var cardsByRank = nonJokerCards.GetCardsByRank().ToList();
            foreach (var rank in cardsByRank)
                sets.AddRange(GetPossibleSets(rank.Value, new List<Card>()));

            var runs = new List<Run>();
            var cardsBySuit = nonJokerCards.GetCardsBySuit();
            foreach (var entry in cardsBySuit)
            {
                var newRuns = GetPossibleRuns(entry.Value, new List<Card>()).Select(r => new Run(r));
                runs.AddRange(newRuns);
            }

            var jokerSets = GetPossibleJokerSets(HandCards, LaidDownCards);
            sets.AddRange(jokerSets);

            var jokerRuns = GetPossibleJokerRuns(HandCards, LaidDownCards);
            runs.AddRange(jokerRuns);

            var allCombos = new List<CardCombo>();
            allCombos.AddRange(GetPossibleSetAndRunCombos(sets, runs, new CardCombo()));
            allCombos.AddRange(GetPossibleRunCombos(runs, new CardCombo()));
            allCombos = allCombos.Where(combo => combo.MeldCount > 0)
                                 .OrderByDescending(combo => combo.Value)
                                 .ThenBy(combo => combo.MeldCount)
                                 .ToList();
            if (allowLayingAll)
                return allCombos;
            return allCombos.Where(c => c.CardCount < HandCards.Count).ToList();
        }

        /// <summary>
        /// Return all possible sets (consisting of 3 or 4 cards)
        /// which can be combined from 'availableCards' and the current set 'currentSet' 
        /// </summary>
        private static List<Set> GetPossibleSets(List<Card> availableCards, List<Card> currentSet)
        {
            var possibleSets = new List<Set>();
            for (int i = 0; i < availableCards.Count; i++)
            {
                Card card = availableCards[i];
                var newSet = new List<Card>(currentSet) { card };

                if (Set.IsValidSet(newSet))
                    possibleSets.Add(new Set(newSet));

                if (newSet.Count == 4)
                    continue;

                var otherCards = availableCards.Skip(i + 1).Where(c => c.Suit != card.Suit).ToList();
                if (otherCards.Count > 0)
                    possibleSets.AddRange(GetPossibleSets(otherCards, newSet));
            }
            return possibleSets;
        }

        /// <summary>
        /// Return all possible runs of 'minLength' <= length <= 'maxLength'
        /// which can be created from in 'availableCards' and the current run 'currentRun'
        /// </summary>
        /// <param name="availableCards">A list of cards with the same suit</param>
        private static List<List<Card>> GetPossibleRuns(List<Card> availableCards, List<Card> currentRun, int minLength = 3, int maxLength = 14)
        {
            var possibleRuns = new List<List<Card>>();
            var nextRank = Card.CardRank.TWO;
            if (currentRun.Count > 0)
            {
                Card.CardRank lastRank = currentRun[^1].Rank;
                if (lastRank != Card.CardRank.ACE)
                    nextRank = lastRank + 1;
            }

            foreach (Card card in availableCards)
            {
                // Only start runs when there's (minLength-1) more higher CardRanks or if it's an ACE
                if (currentRun.Count == 0 && card.Rank != Card.CardRank.ACE && (int)card.Rank + (minLength - 1) > (int)Card.CardRank.ACE)
                    continue;

                if (currentRun.Count > 0 && card.Rank != nextRank)
                    continue;

                var newRun = new List<Card>(currentRun) { card };
                if (newRun.Count >= minLength && newRun.Count <= maxLength)
                    possibleRuns.Add(newRun);

                if (newRun.Count == maxLength)
                    continue;

                var currentRank = card.Rank;
                if (currentRun.Count == 0 && currentRank == Card.CardRank.ACE)
                    currentRank = (Card.CardRank)1;

                var higherCards = availableCards.Where(c => c.Rank > currentRank).ToList();
                possibleRuns.AddRange(GetPossibleRuns(higherCards, newRun, minLength, maxLength));
            }
            return possibleRuns;
        }

        /// <summary>
        /// Return all possible combinations of sets and runs which can be found in the given lists of sets and runs
        /// </summary>
        private static List<CardCombo> GetPossibleSetAndRunCombos(List<Set> sets, List<Run> runs, CardCombo currentCombo)
        {
            var possibleCombos = new List<CardCombo>();
            for (int i = 0; i < sets.Count; i++)
            {
                var combo = new CardCombo(currentCombo);
                combo.AddSet(sets[i]);

                // This fixed set alone is also a possibility
                possibleCombos.Add(combo);

                // Get all runs which are possible with the current set
                List<Run> possibleRuns = runs.Where(run => !run.Intersects(sets[i])).ToList();
                possibleCombos.AddRange(GetPossibleRunCombos(possibleRuns, combo));

                // Only sets which don't intersect the current one are possible combinations
                List<Set> otherSets = sets.Skip(i + 1).Where(set => !set.Intersects(sets[i])).ToList();
                if (otherSets.Count > 0)
                    possibleCombos.AddRange(GetPossibleSetAndRunCombos(otherSets, possibleRuns, combo));
            }
            return possibleCombos;
        }

        /// <summary>
        /// Return all possible combinations of runs which can be found in the given list of runs
        /// </summary>
        private static List<CardCombo> GetPossibleRunCombos(List<Run> runs, CardCombo currentRunCombo)
        {
            var possibleRunCombos = new List<CardCombo>();
            for (int i = 0; i < runs.Count; i++)
            {
                var currentCombo = new CardCombo(currentRunCombo);
                currentCombo.AddRun(runs[i]);

                // This fixed run alone is also a possibility
                possibleRunCombos.Add(currentCombo);

                List<Run> otherRuns = runs.Skip(i + 1).Where(run => !run.Intersects(runs[i])).ToList();
                if (otherRuns.Count > 0)
                    possibleRunCombos.AddRange(GetPossibleRunCombos(otherRuns, currentCombo));
            }
            return possibleRunCombos;
        }

        /// <summary>
        /// Look for duos which could form complete 3-card-runs using a joker and
        /// return all possible combinations using the available joker cards
        /// </summary>
        private static List<Run> GetPossibleJokerRuns(List<Card> PlayerCards, List<Card> LaidDownCards)
        {
            var jokerCards = PlayerCards.Where(c => c.IsJoker());
            if (!jokerCards.Any())
                return new List<Run>();

            var thoughts = new List<string>();
            List<Duo> duoRuns = GetAllDuoRuns(PlayerCards, LaidDownCards, ref thoughts);
            if (duoRuns.Count == 0)
                return new List<Run>();

            var possibleJokerRuns = new List<Run>();

            foreach (var duo in duoRuns)
            {
                var runColor = duo.A.Color;
                var matchingJokers = jokerCards.Where(j => j.Color == runColor);
                foreach (var joker in matchingJokers)
                {
                    if (duo.B.Rank - duo.A.Rank == 1 || (duo.A.Rank == Card.CardRank.ACE && duo.B.Rank == Card.CardRank.TWO))
                    {
                        if (duo.B.Rank != Card.CardRank.ACE)
                            possibleJokerRuns.Add(new Run(duo.A, duo.B, joker));
                        if (duo.A.Rank != Card.CardRank.ACE)
                            possibleJokerRuns.Add(new Run(joker, duo.A, duo.B));
                    }
                    else
                        possibleJokerRuns.Add(new Run(duo.A, joker, duo.B));
                }
            }
            return possibleJokerRuns;
        }

        /// <summary>
        /// Return all runs which only consist of two cards and could theoretically be completed by waiting for a third
        /// </summary>
        /// <param name="PlayerCards">The cards on the player's hand</param>
        /// <param name="LaidDownCards">The cards which have already been laid down by the players. Used to check whether a run is theoretically possible</param>
        /// <returns></returns>
        public static List<Duo> GetAllDuoRuns(List<Card> PlayerCards, List<Card> LaidDownCards, ref List<string> thoughts)
        {
            var duoRuns = new List<Duo>();
            var cardsBySuit = PlayerCards.Where(c => !c.IsJoker()).GetCardsBySuit();

            foreach (var entry in cardsBySuit)
            {
                var newDuos = GetPossibleRuns(entry.Value, new List<Card>(), 2, 2).Select(r => new Duo(r[0], r[1]));
                duoRuns.AddRange(newDuos);
            }

            // Don't keep duo runs if all possible run combinations were laid down already
            var tmpRuns = new List<Duo>(duoRuns);
            foreach (var duoRun in tmpRuns)
            {
                var lowerRank = Card.CardRank.JOKER;
                bool allLowerLaid = true;
                if (duoRun.A.Rank == Card.CardRank.TWO)
                    lowerRank = Card.CardRank.ACE;
                else if (duoRun.A.Rank < Card.CardRank.ACE)
                    lowerRank = duoRun.A.Rank - 1;
                if (lowerRank != Card.CardRank.JOKER)
                {
                    var lowerCount = LaidDownCards.Count(c => c.Rank == lowerRank && c.Suit == duoRun.A.Suit);
                    allLowerLaid = lowerCount == Tb.I.CardStack.CardDeckCount;
                }

                var higherRank = Card.CardRank.JOKER;
                bool allHigherLaid = true;
                if (duoRun.B.Rank < Card.CardRank.ACE)
                    higherRank = duoRun.B.Rank + 1;
                if (higherRank != Card.CardRank.JOKER)
                {
                    var higherCount = LaidDownCards.Count(c => c.Rank == higherRank && c.Suit == duoRun.A.Suit);
                    allHigherLaid = higherCount == Tb.I.CardStack.CardDeckCount;
                }

                if (allHigherLaid && allLowerLaid)
                {
                    string cards = "";
                    if (lowerRank != Card.CardRank.JOKER)
                        cards += Card.RankLetters[lowerRank] + Card.SuitSymbols[duoRun.A.Suit] + ",";
                    if (higherRank != Card.CardRank.JOKER)
                        cards += Card.RankLetters[higherRank] + Card.SuitSymbols[duoRun.A.Suit];
                    thoughts.Add("Don't keep " + duoRun.A + duoRun.B + ": " + cards.TrimEnd(',') + " already laid down twice");
                    duoRuns.Remove(duoRun);
                }
            }

            // Find duos with the card in the middle missing (looping through all cards keeps c1<c2)
            foreach (var entry in cardsBySuit)
            {
                foreach (Card c1 in entry.Value)
                {
                    foreach (Card c2 in entry.Value)
                    {
                        if (c2.Rank - c1.Rank != 2 &&
                            (c1.Rank != Card.CardRank.ACE || c2.Rank != Card.CardRank.THREE))
                            continue;

                        var middleRank = c2.Rank - 1;
                        if (LaidDownCards.Count(c => c.Rank == middleRank && c.Suit == entry.Key) < Tb.I.CardStack.CardDeckCount)
                            duoRuns.Add(new Duo(c1, c2));
                        else
                            thoughts.Add("Don't keep " + c1 + c2 + ": " + Card.RankLetters[middleRank] +
                                Card.SuitSymbols[c1.Suit] + " already laid down twice");
                    }
                }
            }
            return duoRuns;
        }

        /// <summary>
        /// Return all possible sets which can be created using joker cards
        /// </summary>
        private static List<Set> GetPossibleJokerSets(List<Card> PlayerCards, List<Card> LaidDownCards)
        {
            var jokerCards = PlayerCards.Where(c => c.IsJoker());
            if (!jokerCards.Any())
                return new List<Set>();

            var thoughts = new List<string>();
            var duoSets = GetAllDuoSets(PlayerCards, LaidDownCards, ref thoughts);
            if (duoSets.Count == 0)
                return new List<Set>();

            var possibleJokerSets = new List<Set>();

            // First, create trios out of duos where both cards have the same color
            // for each trio, save all possible combinations with available jokers
            for (int i = 0; i < 2; i++)
            {
                Card.CardColor color = (Card.CardColor)i;
                var sameColorDuos = duoSets.Where(duo => duo.A.Color == color && duo.B.Color == color);

                foreach (var duo in sameColorDuos)
                {
                    // Find all available jokers with the opposite color
                    var possibleJokers = jokerCards.Where(j => j.Color != color);
                    foreach (var joker in possibleJokers)
                    {
                        var newSet = new Set(duo.A, duo.B, joker);
                        possibleJokerSets.Add(newSet);
                    }
                }
            }

            // Now finish all red-black duos with all possible joker cards
            var mixedDuos = duoSets.Where(duo => duo.A.Color != duo.B.Color);
            foreach (var duo in mixedDuos)
            {
                foreach (var jokerCard in jokerCards)
                {
                    var newSet = new Set(duo.A, duo.B, jokerCard);
                    possibleJokerSets.Add(newSet);
                }
            }
            return possibleJokerSets;
        }

        /// <summary>
        /// Return all possible duos (sets of two cards with different suits) from 'PlayerHandCards'
        /// </summary>
        /// <param name="PlayerHandCards">The cards in the player's hand</param>
        /// <param name="LaidDownCards">Cards which have already been laid down by players. Used to check if it
        /// is unnecessary to keep a certain duo, because the needed cards have already been laid down</param>
        /// <returns></returns>
        public static List<Duo> GetAllDuoSets(List<Card> PlayerHandCards, List<Card> LaidDownCards, ref List<string> thoughts)
        {
            var allDuos = new List<Duo>();
            var cardsByRank = PlayerHandCards.GetCardsByRank()
                                         .Where(entry => entry.Key != Card.CardRank.JOKER && entry.Value.Count >= 2);

            foreach (var kvp in cardsByRank)
            {
                var rank = kvp.Key;
                var cards = kvp.Value;

                var newDuos = new List<Duo>();
                for (int i = 0; i < cards.Count; i++)
                {
                    Card c1 = cards[i];
                    for (int j = i + 1; j < cards.Count; j++)
                    {
                        Card c2 = cards[j];
                        if (c1.Suit == c2.Suit)
                            continue;
                        var otherSuits = Card.GetOtherTwo(c1.Suit, c2.Suit);
                        int count = LaidDownCards.Count(c => c.Rank == rank && otherSuits.Contains(c.Suit));
                        // Duo can be finished if the necessary cards haven't all been laid down yet
                        if (count < 2 * Tb.I.CardStack.CardDeckCount)
                            newDuos.Add(new Duo(c1, c2));
                        else
                        {
                            thoughts.Add("Don't keep " + c1 + c2 + ": " +
                                Card.RankLetters[rank] + Card.SuitSymbols[otherSuits[0]] + "," +
                                Card.RankLetters[rank] + Card.SuitSymbols[otherSuits[1]] +
                                " already laid down twice each");
                        }
                    }
                }
                allDuos.AddRange(newDuos);
            }
            return allDuos;
        }

        /// <summary>
        /// Return the rank of the joker card, which is at index 'jokerIndex' in the list of cards 'cards'.
        /// If the rank cannot be figured out or card at the given jokerIndex is not a joker, a <see cref="RummyException"/> is thrown
        /// </summary>
        /// <param name="cards">The list of cards which contains the joker</param>
        /// <param name="jokerIndex">The joker card's index in the 'cards' list</param>
        /// <returns>The rank of the joker card</returns>
        public static Card.CardRank GetJokerRank(List<Card> cards, int jokerIndex)
        {
            if (!cards[jokerIndex].IsJoker())
                throw new RummyException("The card at the given jokerIndex is not a joker!");

            int nonJokerIdx = cards.GetFirstCardIndex(jokerIndex + 1, true);
            if (nonJokerIdx != -1)
                return cards[nonJokerIdx].Rank - (nonJokerIdx - jokerIndex);
            nonJokerIdx = cards.GetFirstCardIndex(jokerIndex - 1, false);
            if (nonJokerIdx != -1)
            {
                if (nonJokerIdx == 0 && cards[nonJokerIdx].Rank == Card.CardRank.ACE)
                    return (Card.CardRank)(jokerIndex + 1);
                return cards[nonJokerIdx].Rank + (jokerIndex - nonJokerIdx);
            }
            throw new RummyException("Rank of joker card could not be figured out!");
        }
    }

}