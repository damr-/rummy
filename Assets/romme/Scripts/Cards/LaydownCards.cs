using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace romme.Cards
{

    public class LaydownCards
    {
        public List<Set> Sets { get; private set; }
        public List<Run> Runs { get; private set; }

        public int Count => Sets.Count() + Runs.Count();
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

        public LaydownCards() : this(new List<Set>(), new List<Run>()) { }
        public LaydownCards(LaydownCards other) : this(other.Sets, other.Runs) { }
        public LaydownCards(List<Set> sets, List<Run> runs)
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

        public void RemoveLastSet()
        {
            if (Sets.Count > 0)
                Sets.RemoveAt(Sets.Count - 1);
        }

        public void RemoveLastRun()
        {
            if (Runs.Count > 0)
                Runs.RemoveAt(Runs.Count - 1);
        }
    }

}