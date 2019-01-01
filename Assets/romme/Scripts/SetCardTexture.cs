using UnityEngine;

namespace romme
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Card))]
    public class SetCardTexture : MonoBehaviour
    {
        private Card card;
        private MeshRenderer meshRend;

        private void Start()
        {
            card = GetComponent<Card>();
            meshRend = GetComponent<MeshRenderer>();
        }

        private void Update()
        {
            Texture texture;
            if (card.Number == Card.CardNumber.JOKER)
            {
                texture = Resources.Load<Texture>("cards/JOKER_" + card.Color);
            }
            else {
                string cardType = card.GetCardTypeString();
                texture = Resources.Load<Texture>("cards/" + cardType);
            }
            meshRend.sharedMaterial.SetTexture("_MainTex", texture);
        }
    }
}