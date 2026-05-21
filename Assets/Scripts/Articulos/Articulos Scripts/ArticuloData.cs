using UnityEngine;

namespace JuegoDeCartas.Articulos
{
    [CreateAssetMenu(fileName = "NuevoArticulo", menuName = "Tienda/Articulo")]
    public class ArticuloData : ScriptableObject
    {
        public string nombre;
        public int precio;
        public Rareza rareza;
        public Sprite imagen;
        [TextArea] public string descripcion;
        [TextArea] public string efecto;
    }

    public enum Rareza
    {
        Comun,
        PocoComun,
        Raro,
        Epico,
        Legendario
    }
}
