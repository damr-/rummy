using System.Linq;
using System.Collections.Generic;

namespace rummy.Cards
{

    public class CardCombo
    {
        public List<Set> Sets { get; private set; }
        public List<Run> Runs { get; private set; }

        public void AddSet(Set set) => Sets.Add(set);
        public void AddRun(Run run) => Runs.Add(run);

        public int CardCount => Sets.Sum(s => s.Count) + Runs.Sum(r => r.Count);
        public int PackCount => Sets.Count() + Runs.Count();
        public int Value => Sets.Sum(s => s.Value) + Runs.Sum(r => r.Value);

        public CardCombo() : this(new List<Set>(), new List<Run>()) { }
        public CardCombo(CardCombo other) : this(other.Sets, other.Runs) { }
        public CardCombo(List<Set> sets, List<Run> runs)
        {
            Sets = new List<Set>(sets);
            Runs = new List<Run>(runs);
        }

        /// <summary>
        /// Sorts the combo's sets and runs by their value, in descending order
        /// </summary>
        public void Sort()
        {
            Sets = Sets.OrderByDescending(set => set.Value).ToList();
            Runs = Runs.OrderByDescending(run => run.Value).ToList();
        }

        public override string ToString()
        {
            string output = "";
            Sets.ForEach(s => output += s + ", ");
            Runs.ForEach(r => output += r + ", ");
            return output.TrimEnd().TrimEnd(',');
        }

        ///<summary>
        /// Returns whether the 'other' CardCombo looks the same as this one, meaning that it has the same runs and sets.
        /// ONLY CHECKING the suit and rank, not whether the cards are actually identical Objects.
        ///</summary>
        public bool LooksEqual(CardCombo other)
        {
            if (other.PackCount != PackCount)
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
    }

}