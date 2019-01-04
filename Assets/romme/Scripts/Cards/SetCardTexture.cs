using UnityEngine;

namespace romme.Cards
{
    [ExecuteAlways]
    [RequireComponent(typeof(Card))]
    public class SetCardTexture : MonoBehaviour
    {
        private Card card;
        private MeshRenderer meshRend;

        private Card.CardNumber currentNumber;
        private Card.CardSymbol currentSymbol;
        private Card.CardColor currentColor;

        private void Start()
        {
            card = GetComponent<Card>();
            meshRend = GetComponent<MeshRenderer>();
        }

        private void Update()
        {
            if (currentNumber == card.Number && currentSymbol == card.Symbol)
                return;

            currentNumber = card.Number;
            currentSymbol = card.Symbol;
            string cardType = card.GetCardTypeString();
            Texture texture = Resources.Load<Texture>("cards/" + cardType);

            if (texture == null)
                Debug.Log(card.GetCardTypeString() + " has no texture.");

            meshRend.sharedMaterial = new Material(meshRend.material) { mainTexture = texture };
        }
    }
}