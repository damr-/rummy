using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace romme.Cards
{

    public class CardCombo
    {
        public List<Set> Sets { get; private set; }
        public List<Run> Runs { get; private set; }

        public int CardCount => Sets.Sum(s => s.Count) + Runs.Sum(r => r.Count);
        public int PackCount => Sets.Count() + Runs.Count();
        public int Value
        {
            get
            {
                int sum = 0;
                Sets.ForEach(s => sum += s.Value);
                Runs.ForEach(r => sum += r.Value);
                return sum;
            }
        }

        public CardCombo() : this(new List<Set>(), new List<Run>()) { }
        public CardCombo(CardCombo other) : this(other.Sets, other.Runs) { }
        public CardCombo(List<Set> sets, List<Run> runs)
        {
            Sets = new List<Set>();
            Runs = new List<Run>();

            foreach (var set in sets)
                Sets.Add(set);
            foreach (var run in runs)
                Runs.Add(run);
        }

        public void AddSet(Set set)
        {
            Sets.Add(set);
        }

        public void AddRun(Run run)
        {
            Runs.Add(run);
        }

        /// <summary>
        /// Sorts the combo's sets and runs DESCENDING according to their value
        /// </summary>
        public void Sort()
        {
            Sets = Sets.OrderByDescending(set => set.Value).ToList();
            Runs = Runs.OrderByDescending(run => run.Value).ToList();
        }

        public Set RemoveLastSet()
        {
            if (Sets.Count == 0)
                return null;
            Set removedSet = Sets[Sets.Count - 1];
            Sets.Remove(removedSet);
            return removedSet;
        }

        public Run RemoveLastRun()
        {
            if (Runs.Count == 0)
                return null;
            Run removedRun = Runs[Runs.Count - 1];
            Runs.Remove(removedRun);
            return removedRun;
        }

        public override string ToString()
        {
            string output = "";
            Sets.ForEach(s => output += s + ", ");
            Runs.ForEach(r => output += r + ", ");
            return output.TrimEnd().TrimEnd(',');
        }

        ///<summary>
        /// Returns whether the 'other' CardCombo looks the same as this one, 
        /// meaning that it has the same runs and sets.
        /// ONLY CHECKING the suit and rank, not whether the cards are actually identical Objects.
        ///</summary>
        public bool LooksEqual(CardCombo other)
        {
            if(other.PackCount != PackCount)
                return false;
            if(other.CardCount != CardCount)
                return false;
            if(other.Sets.Count != Sets.Count || other.Runs.Count != Runs.Count)
                return false;
            
            var orderedSets1 = Sets.OrderBy(set => set.Value);
            var orderedSets2 = other.Sets.OrderBy(set => set.Value);
            for (int i = 0; i < orderedSets1.Count(); i++)
            {
                if(!orderedSets1.ElementAt(i).LooksEqual(orderedSets2.ElementAt(i)))
                    return false;                
            }

            var orderedRuns1 = Runs.OrderBy(run => run.Value);
            var orderedRuns2 = other.Runs.OrderBy(run => run.Value);
            for (int i = 0; i < orderedRuns1.Count(); i++)
            {
                if(!orderedRuns1.ElementAt(i).LooksEqual(orderedRuns2.ElementAt(i)))
                    return false;                
            }
            return true;
        }
    }

}