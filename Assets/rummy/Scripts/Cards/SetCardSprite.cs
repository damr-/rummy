using System.Collections.Generic;
using UnityEngine;

namespace rummy.Cards
{

    [RequireComponent(typeof(Card))]
    public class SetCardSprite : MonoBehaviour
    {
        private static Sprite BackSprite
        {
            get
            {
                if (_backSprite == null)
                    _backSprite = Resources.Load<Sprite>("cards/card_back");
                return _backSprite;
            }
        }
        private static Sprite _backSprite = null;

        private static IDictionary<string, Sprite> GetCardTextures()
        {
            if (_cardTex.Count == 0)
            {
                for (int rank = 1; rank <= Card.CardRankCount; rank++)
                {
                    for (int suit = 1; suit <= Card.CardSuitCount; suit++)
                    {
                        string fileString = (Card.CardRank)rank + "_" + (Card.CardSuit)suit;
                        Sprite texture = Resources.Load<Sprite>("cards/" + fileString);
                        if (texture == null)
                            Debug.LogError("Missing texture " + fileString);
                        _cardTex.Add(fileString, texture);
                    }
                }
            }
            return _cardTex;
        }
        private static readonly IDictionary<string, Sprite> _cardTex = new Dictionary<string, Sprite>();

        private Card Card
        {
            get
            {
                if (_c == null)
                {
                    _c = GetComponent<Card>();
                    _c.VisibilityChanged.AddListener(UpdateVisibility);
                    _c.HasBeenTurned.AddListener(CardTurned);
                    _c.SentToBackground.AddListener(SentToBackground);
                }
                return _c;
            }
        }
        private Card _c = null;

        private SpriteRenderer SpriteRenderer
        {
            get
            {
                if (_sr == null)
                    _sr = GetComponent<SpriteRenderer>();
                return _sr;
            }
        }
        private SpriteRenderer _sr;

        public void UpdateTexture()
        {
            string fileString = GetFileString(Card);
            SpriteRenderer.sprite = GetCardTextures()[fileString];
        }

        private void UpdateVisibility(bool visible)
        {
            SpriteRenderer.enabled = visible;
        }

        private void CardTurned(bool turned)
        {
            if (turned)
                SpriteRenderer.sprite = BackSprite;
            else
                UpdateTexture();
        }

        private void SentToBackground(bool background)
        {
            SpriteRenderer.sortingOrder = background ? -1 : 0;
        }

        private string GetFileString(Card card)
        {
            return card.Rank + "_" + card.Suit;
        }
    }

}