using System.Collections.Generic;
using UnityEngine;
using JuegoDeCartas.Cards;

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

        [Header("Card Pool")]
        [Tooltip("Pool opcional usado por articulos que ofrecen cartas externas.")]
        public List<CardData> cardPool = new List<CardData>();

        void OnValidate()
        {
            cantidad = Mathf.Max(0, cantidad);
        }
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
        ReducirCoste,
        DescartarCarta,
        DespertarEpifania,
        AgregarCartaOtraClase
    }
}
