using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using romme.Cards;

namespace romme.Utility
{
    public static class Extensions
    {
        public static int Seed;

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static Stack<T> Shuffle<T>(this Stack<T> stack)
        {
            Random.InitState(Seed);
            return new Stack<T>(stack.OrderBy(x => Random.Range(0, int.MaxValue)));
        }

        public static IDictionary<Card.CardNumber, List<Card>> GetCardsByNumber(this List<Card> PlayerCards)
        {
            var cards = new Dictionary<Card.CardNumber, List<Card>>();
            for (int i = 0; i < PlayerCards.Count; i++)
            {
                Card card = PlayerCards[i];
                if (cards.ContainsKey(card.Number))
                    cards[card.Number].Add(card);
                else
                    cards.Add(card.Number, new List<Card> { card });
            }
            return cards;
        }

        public static IDictionary<Card.CardNumber, List<Card>> GetUniqueCardsByNumber(this List<Card> PlayerCards)
        {
            IDictionary<Card.CardNumber, List<Card>> uniqueCardsByNumber = new Dictionary<Card.CardNumber, List<Card>>();

            var CardsByNumber = PlayerCards.GetCardsByNumber();
            foreach (KeyValuePair<Card.CardNumber, List<Card>> entry in CardsByNumber)
            {
                var uniqueCards = entry.Value.GetUniqueCards();
                uniqueCardsByNumber.Add(entry.Key, uniqueCards);
            }

            return uniqueCardsByNumber;
        }

        public static List<Card> GetUniqueCards(this List<Card> Cards)
        {
            List<Card> uniqueCards = new List<Card>();
            foreach (Card card in Cards)
            {
                if (!uniqueCards.Any(c => c.Symbol == card.Symbol))
                    uniqueCards.Add(card);
            }
            return uniqueCards;
        }

        public static List<KeyValuePair<Card.CardNumber, List<Card>>> GetLayCardsSameNumber(this List<Card> PlayerCards)
        {
            return PlayerCards.GetUniqueCardsByNumber().Where(entry => entry.Value.Count >= 3).ToList();
        }

        public static Card CheckHigherNeighbor(Card card, List<Card> PlayerCards)
        {
            foreach(Card otherCard in PlayerCards)
            {
                if(otherCard.Symbol != card.Symbol)
                    continue;

                if((otherCard.Number == card.Number + 1) || otherCard.Number == Card.CardNumber.ACE && card.Number == Card.CardNumber.TWO)
                    return otherCard;
            }
            return null;
        }

        //public static bool Identical(this List<Card> series, List<Card> otherSeries)
        //{
        //    if(series.Count != otherSeries.Count)
        //        return false;

        //    foreach(Card c1 in series)
        //    {
        //        foreach(Card c2 in otherSeries)
        //        {
        //            if(c1.gameObject != c2.gameObject)
        //                return false;
        //        }
        //    }
        //    return true;
        //}

        public static List<List<Card>> GetLayCardsSeries(this List<Card> PlayerCards)
        {
            var LayCardsSeries = new List<List<Card>>();

            var possibleSeries = new List<List<Card>>();

            foreach(Card card in PlayerCards)
            {
                Card neighbor = CheckHigherNeighbor(card, PlayerCards);
                List<Card> series = new List<Card> { card };

                while(neighbor != null)
                {
                    series.Add(neighbor);
                    neighbor = CheckHigherNeighbor(neighbor, PlayerCards);
                }

                if(series.Count >= 3)
                    possibleSeries.Add(series);
            }

            var filteredSeries = new List<List<Card>>();

            return LayCardsSeries;


            //var LayCardsSeries = new List<List<Card>>();

            //List<List<Card>> possibleSeries = new List<List<Card>>();

            //foreach(Card c1 in PlayerCards)
            //{
            //    if(c1.Number == Card.CardNumber.JOKER)
            //        continue;

            //    var newSeries = new List<Card>() { c1 };

            //    foreach(Card c2 in PlayerCards)
            //    {
            //        if(c2 == c1)
            //            continue;
            //        if(c2.Number == Card.CardNumber.JOKER || c2.Symbol != c1.Symbol)
            //            continue;

            //        if(Mathf.Abs(c2.Number - c1.Number) == 1
            //            || (c2.Number == Card.CardNumber.ACE && c1.Number == Card.CardNumber.TWO)
            //            || (c2.Number == Card.CardNumber.TWO && c1.Number == Card.CardNumber.ACE))
            //        {
            //            if(newSeries.All(entry => entry.Number != c2.Number))
            //                newSeries.Add(c2);
            //        }
            //    }

            //    newSeries.OrderBy(c => c.Number);
            //    string output = "";
            //    foreach(var card in newSeries)
            //        output += card.Number + ", ";
            //    Debug.Log(output);

            //    if(newSeries.Count >= 3)
            //    {
            //        if(possibleSeries.Any(series => {
            //            for(int i = 0; i < series.Count; i++)
            //            {
            //                if(series[i] != newSeries[i])
            //                    return true;
            //            }
            //            return false;
            //        }))
            //        {
            //            possibleSeries.Add(newSeries);
            //        }
            //    }
            //}

            //foreach(var series in possibleSeries)
            //{
            //    string output = "Possible series: ";
            //    foreach(Card c in series)
            //        output += c.Number + ", ";
            //    Debug.Log(output);
            //}

            //return possibleSeries;
        }
    }

}