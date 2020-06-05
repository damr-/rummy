using System.Collections.Generic;
using rummy.Utility;
using UnityEngine;

namespace rummy.Cards
{

    public class HandCardSpot : RadialLayout<Card>
    {
        public override List<Card> Objects { get; protected set; } = new List<Card>();
        public bool HasCards => Objects.Count > 0;

        [Tooltip("The factor by which cards will be scaled when added to the spot. When removed, the scaling is undone")]
        public float CardScale = 1.0f;

        protected override void InitValues()
        {
            yIncrement = -0.01f;
        }

        public virtual void AddCard(Card card)
        {
            if (Objects.Contains(card))
                throw new RummyException("CardSpot " + gameObject.name + " already contains " + card);
            AddCard(card, Objects.Count);
        }

        protected void AddCard(Card card, int index)
        {
            Objects.Insert(index, card);
            card.transform.SetParent(transform, true);
            card.transform.localScale = card.transform.localScale * CardScale;
            UpdatePositions();
        }

        public void RemoveCard(Card card)
        {
            Objects.Remove(card);
            card.transform.localScale = card.transform.localScale / CardScale;
            card.transform.SetParent(null, true);
            UpdatePositions();
        }

        public virtual List<Card> ResetSpot()
        {
            var cards = new List<Card>(Objects);
            while (Objects.Count > 0)
                RemoveCard(Objects[0]);
            return cards;
        }
    }

}