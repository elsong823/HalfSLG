using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ELGame
{
    public class EffectController 
        : BaseBehaviour, IELPoolObject
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public Color Color
        {
            get
            {
                if (spriteRenderer != null)
                    return spriteRenderer.color;

                return Color.white;
            }
            set
            {
                if (spriteRenderer != null)
                    spriteRenderer.color = value;
            }
        }

        public int SortingLayer
        {
            get
            {
                if (spriteRenderer != null)
                    return spriteRenderer.sortingLayerID;
                return -1;
            }
            set
            {
                if (spriteRenderer != null)
                    spriteRenderer.sortingLayerID = value;
            }
        }

        public int SortingOrder
        {
            get
            {
                if (spriteRenderer != null)
                    return spriteRenderer.sortingOrder;

                return -1;
            }
            set
            {
                if (spriteRenderer != null)
                    spriteRenderer.sortingOrder = value;
            }
        }

        public bool PoolObjActive
        {
            get
            {
                return gameObject.activeSelf;
            }
            set
            {
                gameObject.SetActive(value);
            }
        }
    }
}