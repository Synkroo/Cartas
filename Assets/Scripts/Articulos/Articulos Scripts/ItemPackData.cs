using System;
using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Progression;

namespace JuegoDeCartas.Articulos
{
    [Serializable]
    public class PackRarityWeight
    {
        public Rareza rarity;
        [Min(0f)] public float weight = 1f;
    }

    [CreateAssetMenu(fileName = "NuevoSobre", menuName = "Tienda/Sobre")]
    public class ItemPackData : StableContentData
    {
        public string packName;
        [TextArea] public string description;
        public Sprite image;
        public Rareza displayRarity;
        [Min(0f)] public float shopAppearanceWeight = 1f;
        [Min(0)] public int price = 200;
        [Min(1)] public int choiceCount = 3;
        [Tooltip("Vacio permite cualquier articulo.")]
        public List<TipoEfectoArticulo> allowedEffects = new List<TipoEfectoArticulo>();
        public List<PackRarityWeight> rarityWeights = new List<PackRarityWeight>();

        public bool Allows(ArticuloData item)
        {
            return item != null &&
                   (allowedEffects == null ||
                    allowedEffects.Count == 0 ||
                    allowedEffects.Contains(item.tipoEfecto));
        }

        void OnValidate()
        {
            EnsureContentId();
            shopAppearanceWeight = Mathf.Max(0f, shopAppearanceWeight);
            price = Mathf.Max(0, price);
            choiceCount = Mathf.Max(1, choiceCount);
        }
    }
}
