using System.Collections.Generic;

namespace JuegoDeCartas.Articulos
{
    public class ItemPackOffer
    {
        public ItemPackData Definition { get; }
        public List<ArticuloData> Contents { get; }
        public bool Claimed { get; set; }
        public bool Reserved { get; set; }

        public ItemPackOffer(ItemPackData definition, List<ArticuloData> contents)
        {
            Definition = definition;
            Contents = contents ?? new List<ArticuloData>();
        }
    }
}
