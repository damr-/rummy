using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using romme.Cards;
using UniRx;

namespace romme.UI
{
    [RequireComponent(typeof(ScrollView))]
    public abstract class CardOutputUI : MonoBehaviour
    {        
        protected Player player;
        protected ScrollView outputView;

        private void Start()
        {
            outputView = GetComponent<ScrollView>();
            player = GetComponentInParent<Player>();

            SetupPlayerSub();
        }

        protected abstract void SetupPlayerSub();
    }

}