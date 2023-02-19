using System.Linq;
using System.Collections.Generic;
using rummy.Utility;

namespace rummy.Cards
{

    public class CardCombo
    {
        public List<Set> Sets { get; private set; }
        public List<Run> Runs { get; private set; }

        public void AddSet(Set set) => Sets.Add(set);
        public void AddRun(Run run) => Runs.Add(run);

        public int CardCount => Sets.Sum(s => s.Count) + Runs.Sum(r => r.Count);
        public int MeldCount => Sets.Count() + Runs.Count();
        public int Value => Sets.Sum(s => s.Value) + Runs.Sum(r => r.Value);

        public CardCombo() : this(new List<Set>(), new List<Run>()) { }
        public CardCombo(CardCombo other) : this(other.Sets, other.Runs) { }
        public CardCombo(List<Set> sets, List<Run> runs)
        {
            Sets = new List<Set>();
            foreach (var set in sets)
                Sets.Add(new Set(set.Cards));
            Runs = new List<Run>();
            foreach (var run in runs)
                Runs.Add(new Run(run.Cards));
        }

        /// <summary>
        /// Sort the combo's sets and runs by their value, in descending order
        /// </summary>
        public void Sort()
        {
            Sets = Sets.OrderByDescending(set => set.Value).ToList();
            Runs = Runs.OrderByDescending(run => run.Value).ToList();
        }

        public List<Card> GetCards()
        {
            var cards = new List<Card>();
            foreach (var set in Sets)
                cards.AddRange(set.Cards);
            foreach (var run in Runs)
                cards.AddRange(run.Cards);
            return cards;
        }

        /// <summary>
        /// Return whether this card combo intersects the other (whether the sets
        /// and runs of the two combos have at least one card in common)
        /// </summary>
        public bool Intersects(CardCombo other) => GetCards().Intersects(other.GetCards());

        public override string ToString()
        {
            string output = "";
            Sets.ForEach(s => output += s + ", ");
            Runs.ForEach(r => output += r + ", ");
            return output.TrimEnd().TrimEnd(',');
        }

        ///<summary>
        /// Return whether the 'other' CardCombo looks the same as this one, meaning that it has the same runs and sets.
        /// ONLY CHECKING the suit and rank, not whether the cards are actually identical Objects.
        ///</summary>
        public bool LooksEqual(CardCombo other)
        {
            if (other.MeldCount != MeldCount)
                return false;
            if (other.CardCount != CardCount)
                return false;
            if (other.Sets.Count != Sets.Count || other.Runs.Count != Runs.Count)
                return false;

            var orderedSets1 = Sets.OrderBy(set => set.Value);
            var orderedSets2 = other.Sets.OrderBy(set => set.Value);
            for (int i = 0; i < orderedSets1.Count(); i++)
            {
                if (!orderedSets1.ElementAt(i).LooksEqual(orderedSets2.ElementAt(i)))
                    return false;
            }

            var orderedRuns1 = Runs.OrderBy(run => run.Value);
            var orderedRuns2 = other.Runs.OrderBy(run => run.Value);
            for (int i = 0; i < orderedRuns1.Count(); i++)
            {
                if (!orderedRuns1.ElementAt(i).LooksEqual(orderedRuns2.ElementAt(i)))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Return whether adding any of the passed jokers will increase the CardCombo's value to or above <see cref="GameMaster.MinimumLaySum"/>.
        /// If successful, the jokers are added to the CardCombo.
        /// </summary>
        public bool TryAddJoker(List<Card> jokers)
        {
            var availableJokers = new List<Card>(jokers);

            for (int i = 0; i < Sets.Count; i++)
            {
                if (Sets[i].Cards.Count == 4)
                    continue;

                for (int j = availableJokers.Count - 1; j >= 0; j--)
                {
                    Card joker = availableJokers[j];
                    if (!Sets[i].CanFit(joker, out _))
                        continue;
                    var cards = Sets[i].Cards;
                    cards.Add(joker);
                    Sets[i] = new Set(cards);

                    if (Value >= Tb.I.GameMaster.MinimumLaySum)
                        return true;

                    availableJokers.RemoveAt(j);
                    break;
                }
            }

            for (int i = 0; i < Runs.Count; i++)
            {
                for (int j = availableJokers.Count - 1; j >= 0; j--)
                {
                    Card joker = availableJokers[j];
                    if (!Runs[i].CanFit(joker, out _, out _))
                        continue;
                    (int, int) jokerVal = Runs[i].JokerValue();
                    if (jokerVal.Item1 == 0 && jokerVal.Item2 == 0)
                        continue;

                    var cards = Runs[i].Cards;
                    if (jokerVal.Item2 > 0) // Try higher position first (=higher value)
                        cards.Add(joker);
                    else if (jokerVal.Item1 > 0)
                        cards.Insert(0, joker);
                    Runs[i] = new Run(cards);

                    if (Value >= Tb.I.GameMaster.MinimumLaySum)
                        return true;

                    availableJokers.RemoveAt(j);
                    break;
                }
            }
            return false;
        }
    }

}