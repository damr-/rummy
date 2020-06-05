using System.Linq;
using System.Collections.Generic;
using rummy.Cards;

namespace rummy.Utility
{

    public static class CardUtil
    {
        /// <summary>
        /// Returns all combos which don't have any cards in common, sorted descending by value
        /// </summary>
        public static List<CardCombo> GetAllUniqueCombos(List<Card> HandCards, List<Card> LaidDownCards)
        {
            var allCombos = GetAllPossibleCombos(HandCards, LaidDownCards, false);
            var uniqueCombos = new List<CardCombo>();

            foreach(var combo in allCombos)
            {
                var isUnique = true;
                foreach(var uCombo in uniqueCombos)
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
        /// Returns a sorted (descending, by value) list of all possible combos which could be laid down, with sets and runs as well as joker
        /// combinations extracted from the given 'HandCards'.
        /// <param name="allowLayingAll"> Whether combos are allowed which would require the player to lay down all cards from his hand ('HandCards').
        /// This is usually not useful, unless hypothetical hands are examined, where one card was removed before. </param>
        /// </summary>
        public static List<CardCombo> GetAllPossibleCombos(List<Card> HandCards, List<Card> LaidDownCards, bool allowLayingAll)
        {
            var sets = GetPossibleSets(HandCards);
            var runs = GetPossibleRuns(HandCards);

            var jokerSets = GetPossibleJokerSets(HandCards, LaidDownCards);
            sets.AddRange(jokerSets);

            var jokerRuns = GetPossibleJokerRuns(HandCards, LaidDownCards);
            runs.AddRange(jokerRuns);

            var allCombos = new List<CardCombo>();
            GetPossibleSetAndRunCombos(allCombos, sets, runs, new CardCombo());
            GetPossibleRunCombos(allCombos, runs, new CardCombo());
            allCombos = allCombos.Where(combo => combo.PackCount > 0)
                                 .OrderByDescending(combo => combo.Value)
                                 .ToList();
            if (allowLayingAll)
                return allCombos;
            return allCombos.Where(c => c.CardCount < HandCards.Count).ToList();
        }

        private static void GetPossibleRunCombos(List<CardCombo> possibleRunCombos, List<Run> runs, CardCombo currentRunCombo)
        {
            for (int i = 0; i < runs.Count; i++)
            {
                var currentCombo = new CardCombo(currentRunCombo);
                currentCombo.AddRun(runs[i]);

                // This fixed run alone is also a possibility
                possibleRunCombos.Add(currentCombo);

                List<Run> otherRuns = runs.GetRange(i + 1, runs.Count - (i + 1)).Where(run => !run.Intersects(runs[i])).ToList();
                if (otherRuns.Count > 0)
                    GetPossibleRunCombos(possibleRunCombos, otherRuns, currentCombo);
            }
        }

        /// <summary>
        /// Calculates all possible combinations of card packs that could be laid down.
        /// Stores the result in the passed List of LaydownCards 'possibleCombos'
        /// <param name="possibleCombos"> The list which will contain all possible combinations in the end</param>
        /// </summary>
        private static void GetPossibleSetAndRunCombos(List<CardCombo> possibleCombos, List<Set> sets, List<Run> runs, CardCombo currentCombo)
        {
            for (int i = 0; i < sets.Count; i++)
            {
                var combo = new CardCombo(currentCombo);
                combo.AddSet(sets[i]);

                // This fixed set alone is also a possibility
                possibleCombos.Add(combo);

                // Get all runs which are possible with the current set
                List<Run> possibleRuns = runs.Where(run => !run.Intersects(sets[i])).ToList();
                GetPossibleRunCombos(possibleCombos, possibleRuns, combo);

                // Only sets which don't intersect the current one are possible combinations
                List<Set> otherSets = sets.GetRange(i + 1, sets.Count - i - 1).Where(set => !set.Intersects(sets[i])).ToList();
                if (otherSets.Count > 0)
                    GetPossibleSetAndRunCombos(possibleCombos, otherSets, possibleRuns, combo);
            }
        }

        /// <summary>
        /// Returns all possible sets found in 'PlayerCards' which consist of 3 or 4 cards
        /// </summary>
        public static List<Set> GetPossibleSets(List<Card> PlayerCards)
        {
            var possibleSets = new List<Set>();
            var cardsByRank = PlayerCards.GetCardsByRank().Where(rank => rank.Key != Card.CardRank.JOKER);
            foreach (var rank in cardsByRank)
                GetPossibleSets(possibleSets, rank.Value, new List<Card>());
            return possibleSets;
        }

        private static void GetPossibleSets(List<Set> possibleSets, List<Card> availableCards, List<Card> currentSet)
        {
            for (int i = 0; i < availableCards.Count; i++)
            {
                Card card = availableCards[i];
                var newSet = new List<Card>(currentSet) { card };

                if (newSet.Count > 4)
                    continue;

                if (Set.IsValidSet(newSet) && (newSet.Count == 3 || newSet.Count == 4))
                    possibleSets.Add(new Set(newSet));

                List<Card> otherCards = availableCards.
                        GetRange(i + 1, availableCards.Count - (i + 1)).
                        Where(c => c.Suit != card.Suit).
                        ToList();
                GetPossibleSets(possibleSets, otherCards, newSet);
            }
        }

        /// <summary>
        /// Returns all possible runs of length >= 3 which can be found in 'PlayerCards'
        /// </summary>
        public static List<Run> GetPossibleRuns(List<Card> PlayerCards)
        {
            var possibleRuns = new List<List<Card>>();
            var cardsBySuit = PlayerCards.Where(c => !c.IsJoker())
                                          .ToList()
                                          .GetCardsBySuit();

            foreach (var entry in cardsBySuit)
                GetPossibleRuns(possibleRuns, entry.Value, entry.Value, new List<Card>());
            return possibleRuns.Select(r => new Run(r)).ToList();
        }

        private static void GetPossibleRuns(List<List<Card>> possibleRuns, List<Card> sameSuitCards, List<Card> availableCards, List<Card> currentRun, int minLength = 3, int maxLength = 14)
        {
            foreach (Card card in availableCards)
            {
                // A card cannot start a run if there's less than minLength higher ranks, unless it's an ACE
                if (currentRun.Count == 0 && card.Rank != Card.CardRank.ACE && (int)card.Rank + (minLength - 1) > (int)Card.CardRank.ACE)
                    continue;

                var newRun = new List<Card>(currentRun) { card };
                if (newRun.Count >= minLength && newRun.Count <= maxLength)
                    possibleRuns.Add(newRun);

                List<Card> higherCards = GetOneRankHigherCards(sameSuitCards, newRun[newRun.Count - 1], newRun.Count == 1);
                GetPossibleRuns(possibleRuns, sameSuitCards, higherCards, newRun, minLength, maxLength);
            }
        }

        /// <summary>
        /// Returns all cards in 'cards' which are one rank higher than 'card'
        /// </summary>
        /// <param name="firstInRun">Whether 'card' is the first card in a run</param>
        /// <returns> The cards or an empty list if none were found </returns>
        private static List<Card> GetOneRankHigherCards(List<Card> cards, Card card, bool firstInRun)
        {
            List<Card> foundCards = new List<Card>();
            foreach (Card otherCard in cards)
            {
                if (otherCard == card || foundCards.Contains(otherCard))
                    continue;
                // Allow going from ACE to TWO but only if ACE is the first card in the run
                if (firstInRun && card.Rank == Card.CardRank.ACE && otherCard.Rank == Card.CardRank.TWO)
                    foundCards.Add(otherCard);
                else if (otherCard.Rank == card.Rank + 1)
                    foundCards.Add(otherCard);
            }
            return foundCards;
        }

        /// <summary>
        /// Looks for duos which could form complete 3-card-runs using a joker and
        /// returns all possible combinations using the available joker cards
        /// </summary>
        public static List<Run> GetPossibleJokerRuns(List<Card> PlayerCards, List<Card> LaidDownCards)
        {
            var jokerCards = PlayerCards.Where(c => c.IsJoker());
            if (!jokerCards.Any())
                return new List<Run>();

            var thoughts = new List<string>();
            var duoRuns = GetAllDuoRuns(PlayerCards, LaidDownCards, ref thoughts);
            if (!duoRuns.Any())
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
        /// Returns all runs which only consist of two cards and could theoretically be completed by waiting for a third.
        /// </summary>
        /// <param name="PlayerCards">The cards on the player's hand</param>
        /// <param name="LaidDownCards">The cards which have already been laid down by the players. Used to check whether a run is theoretically possible</param>
        /// <returns></returns>
        public static List<Duo> GetAllDuoRuns(List<Card> PlayerCards, List<Card> LaidDownCards, ref List<string> thoughts)
        {
            var duoRunList = new List<List<Card>>();
            var playerCardsNoJokers = PlayerCards.Where(c => !c.IsJoker()).ToList();
            var cardsBySuit = playerCardsNoJokers.GetCardsBySuit();
            foreach (var entry in cardsBySuit)
                GetPossibleRuns(duoRunList, entry.Value, entry.Value, new List<Card>(), 2, 2);

            var duoRuns = new List<Duo>();
            foreach (var duoRun in duoRunList)
                duoRuns.Add(new Duo(duoRun[0], duoRun[1]));

            // Don't keep duo runs if all possible run combinations were laid down already
            var tmpRuns = new List<Duo>(duoRuns);
            foreach (var duoRun in tmpRuns)
            {
                var lowerRank = Card.CardRank.JOKER;
                int lowerCount = 0;
                bool allLowerLaid = false;
                if (duoRun.A.Rank == Card.CardRank.TWO)
                    lowerRank = Card.CardRank.ACE;
                else if (duoRun.A.Rank > Card.CardRank.TWO)
                    lowerRank = duoRun.A.Rank - 1;
                if (lowerRank != Card.CardRank.JOKER)
                {
                    lowerCount = LaidDownCards.Count(c => c.Rank == lowerRank && c.Suit == duoRun.A.Suit);
                    allLowerLaid = lowerCount == 2;
                }

                var higherRank = Card.CardRank.JOKER;
                int higherCount = 0;
                bool allHigherLaid = false;
                if (duoRun.B.Rank < Card.CardRank.ACE)
                    higherRank = duoRun.B.Rank + 1;
                if (higherRank != Card.CardRank.JOKER)
                {
                    higherCount = LaidDownCards.Count(c => c.Rank == higherRank && c.Suit == duoRun.A.Suit);
                    allHigherLaid = higherCount == 2;
                }

                if ((allLowerLaid && higherRank == Card.CardRank.JOKER) ||
                    (allHigherLaid && lowerRank == Card.CardRank.JOKER) ||
                    (allHigherLaid && allLowerLaid))
                {
                    thoughts.Add("Don't keep " + duoRun.A + duoRun.B + ": all cards for runs already laid down twice");
                    duoRuns.Remove(duoRun);
                }
            }

            // Find duos with the card in the middle missing
            foreach (Card c1 in playerCardsNoJokers)
            {
                foreach (Card c2 in playerCardsNoJokers)
                {
                    if (c1 == c2 || c1.Suit != c2.Suit || c1.Rank == c2.Rank)
                        continue;

                    if ((c1.Rank == Card.CardRank.ACE && c2.Rank == Card.CardRank.THREE) ||
                        (c1.Rank == c2.Rank - 2))
                    {
                        var middleRank = c1.Rank + 1;
                        var middleSuit = c1.Suit;
                        if (LaidDownCards.Count(c => c.Rank == middleRank && c.Suit == middleSuit) < 2)
                            duoRuns.Add(new Duo(c1, c2));
                        else
                            thoughts.Add("Don't keep " + c1 + c2 + ": " + Card.RankLetters[middleRank] +
                                Card.SuitSymbols[middleSuit] + " already laid down twice");
                    }
                }
            }
            return duoRuns;
        }

        /// <summary>
        /// Returns all possible sets which can be created using joker cards.
        /// </summary>
        public static List<Set> GetPossibleJokerSets(List<Card> PlayerCards, List<Card> LaidDownCards)
        {
            var jokerCards = PlayerCards.Where(c => c.IsJoker());
            if (!jokerCards.Any())
                return new List<Set>();

            var thoughts = new List<string>();
            var duoSets = GetAllDuoSets(PlayerCards, LaidDownCards, ref thoughts);
            if (!duoSets.Any())
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
        /// Returns all possible duos (sets of two cards with different suits) from 'PlayerHandCards'
        /// </summary>
        /// <param name="PlayerHandCards">The cards in the player's hand</param>
        /// <param name="LaidDownCards">Cards which have already been laid down by players.
        /// Used to check whether it is unnecessary to keep a certain duo, because the third card has already been laid down twice</param>
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
                        if (count < 4)
                            newDuos.Add(new Duo(c1, c2));
                        else
                            thoughts.Add("Don't keep " + c1 + c2 + ": " +
                                Card.RankLetters[rank] + Card.SuitSymbols[otherSuits[0]] + "," +
                                Card.RankLetters[rank] + Card.SuitSymbols[otherSuits[1]] +
                                " already laid down twice each");
                    }
                }
                allDuos.AddRange(newDuos);
            }
            return allDuos;
        }

        /// <summary>
        /// Returns the run/set from the given list of runs/sets which has the lowest value
        /// </summary>
        /// <param name="runs">An optional list of runs to check</param>
        /// <param name="sets">An optional list of sets to check</param>
        /// <returns></returns>
        public static Pack GetLowestValue(List<Run> runs, List<Set> sets)
        {
            var minValRun = runs.OrderBy(run => run.Value).FirstOrDefault();
            var minValSet = sets.OrderBy(set => set.Value).FirstOrDefault();
            if (minValRun != null && minValSet != null)
            {
                if (minValRun.Value < minValSet.Value)
                    minValSet = null;
                else
                    minValRun = null;
            }

            if (minValRun != null)
                return minValRun;
            else if (minValSet != null)
                return minValSet;
            else
                throw new RummyException("Could not find the lowest valued set or run!" +
                    " Maybe both lists are empty?");
        }

        /// <summary>
        /// Returns the rank of the joker card, which is at index 'jokerIndex' in the list of cards 'cards'.
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