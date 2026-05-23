using UnityEngine;

namespace JuegoDeCartas.Articulos
{
    [CreateAssetMenu(fileName = "NuevoArticulo", menuName = "Tienda/Articulo")]
    public class ArticuloData : ScriptableObject
    {
        public string nombre;
        public Rareza rareza;
        public Sprite imagen;
        [TextArea] public string descripcion;
        public TipoEfectoArticulo tipoEfecto;
        public int cantidad;
    }

    public enum Rareza
    {
        Comun,
        Raro,
        Epico
    }

    public enum TipoEfectoArticulo
    {
        CurarVida,
        DarArmadura,
        AumentarRobo,
        AumentarVidaMax,
        AumentarManaMax,
        AumentarMano,
        ArmaduraPorTurno,
        RegeneracionVida,
        AgregarCartaAleatoria,
        AgregarCartaEleccion,
        MejorarCarta,
        DuplicarCarta,
        DuplicarCartaMejoras,
        ReducirCoste
    }
}
