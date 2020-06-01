using System.Collections.Generic;
using rummy.Cards;
using UnityEngine;

namespace rummy
{

    public class CardSpotsNode : RadialLayout<CardSpot>
    {
        public GameObject CardSpotPrefab;

        public float degreesPerSpot = 30;

        public float spotStartAngle;
        public float spotRadius = 3f;
        public float spotAngleSpread = 180f;

        public override List<CardSpot> Objects { get; protected set; } = new List<CardSpot>();

        public CardSpot AddCardSpot()
        {
            GameObject cardSpotGO = Instantiate(CardSpotPrefab, transform.position, Quaternion.identity, transform);
            cardSpotGO.name = transform.parent.name + "-CardSpot" + (Objects.Count + 1);
            CardSpot cardSpot = cardSpotGO.GetComponent<CardSpot>();
            cardSpot.startAngle = spotStartAngle;
            cardSpot.radius = spotRadius;
            cardSpot.angleSpread = spotAngleSpread;

            Objects.Add(cardSpot);
            angleSpread = Objects.Count * degreesPerSpot;
            UpdatePositions();
            Objects.ForEach(spot => spot.UpdatePositions());
            return cardSpot;
        }

        public List<Card> ResetNode()
        {
            var cards = new List<Card>();
            foreach (var cardSpot in Objects)
                cards.AddRange(cardSpot.ResetSpot());

            while (Objects.Count > 0)
            {
                CardSpot obj = Objects[0];
                Objects.RemoveAt(0);
                Destroy(obj.gameObject);
            }
            return cards;
        }

        protected override void InitValues()
        {
            yOffset = -1;
        }

    }

}