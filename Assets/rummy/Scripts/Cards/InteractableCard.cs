using UnityEngine;

namespace rummy.Cards
{
    [RequireComponent(typeof(Card))]
    [RequireComponent(typeof(Collider2D))]
    public class InteractableCard : MonoBehaviour
    {
        private Card _card = null;
        private Card Card
        {
            get
            {
                if (_card == null)
                    _card = GetComponent<Card>();
                return _card;
            }
        }
        private Collider2D _coll = null;
        private Collider2D Coll
        {
            get
            {
                if (_coll == null)
                    _coll = GetComponent<Collider2D>();
                return _coll;
            }
        }

        private void Start()
        {
            Card.InteractabilityChanged.AddListener(InteractabilityChanged);
        }

        private void InteractabilityChanged(bool interactable)
        {
            Coll.enabled = interactable;
        }
    }

}