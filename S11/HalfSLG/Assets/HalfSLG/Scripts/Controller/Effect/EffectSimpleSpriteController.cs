using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class EffectSimpleSpriteController 
        : EffectController
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        
        public Color Color
        {
            get { return spriteRenderer.color; }
            set { spriteRenderer.color = value; }
        }

        public override int SortingOrder
        {
            get
            {
                return spriteRenderer.sortingOrder;
            }

            set
            {
                spriteRenderer.sortingOrder = value;
            }
        }

        public override int SortingLayer
        {
            get
            {
                return spriteRenderer.sortingLayerID;
            }

            set
            {
                spriteRenderer.sortingLayerID = value;
            }
        }
    }
}