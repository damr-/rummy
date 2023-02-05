using System.Collections.Generic;
using rummy.Utility;

namespace rummy.Cards
{

    public class HandCardSpot : RadialLayout<Card>
    {
        public override List<Card> Objects { get; protected set; } = new List<Card>();
        public bool HasCards => Objects.Count > 0;

        protected override void InitValues()
        {
            zIncrement = 0.01f;
        }

        public virtual void AddCard(Card card)
        {
            AddCard(card, Objects.Count);
        }

        protected void AddCard(Card card, int index)
        {
            if (Objects.Contains(card))
                throw new RummyException($"CardSpot {gameObject} already contains card {card}");
            Objects.Insert(index, card);
            UpdatePositions();
        }

        public void RemoveCard(Card card)
        {
            if (!Objects.Contains(card))
                throw new RummyException($"CardSpot {gameObject} doesn't contain card {card}");
            Objects.Remove(card);
            UpdatePositions();
        }

        public virtual void ResetSpot()
        {
            Objects.ClearAndDestroy();
        }
    }

}