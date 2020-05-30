using System.Collections.Generic;
using UnityEngine;

namespace rummy.Cards
{

    [RequireComponent(typeof(Card))]
    public class SetCardTexture : MonoBehaviour
    {
        public static IDictionary<string, Texture> GetCardTextures()
        {
            if (_cardTex.Count == 0)
            {
                for (int rank = 1; rank <= Card.CardRankCount; rank++)
                {
                    for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                    {
                        string fileString = (Card.CardRank)rank + "_" + (Card.CardSuit)suit;
                        Texture texture = Resources.Load<Texture>("cards/" + fileString);
                        if (texture == null)
                            Debug.LogError("Missing texture " + fileString);
                        _cardTex.Add(fileString, texture);
                    }
                }
            }
            return _cardTex;
        }
        private static readonly IDictionary<string, Texture> _cardTex = new Dictionary<string, Texture>();

        public Material Material;
        private readonly Material[] localMaterials = new Material[1];

        private Card GetCard()
        {
            if (_c == null)
                _c = GetComponent<Card>();
            return _c;
        }
        private Card _c = null;

        private MeshRenderer GetMeshRenderer()
        {
            if (_mr == null)
                _mr = GetComponent<MeshRenderer>();
            return _mr;
        }
        private MeshRenderer _mr;

        public void UpdateTexture()
        {
            string fileString = GetCard().GetFileString();
            localMaterials[0] = new Material(Material) { mainTexture = GetCardTextures()[fileString] };
            GetMeshRenderer().materials = localMaterials;
        }
    }

}