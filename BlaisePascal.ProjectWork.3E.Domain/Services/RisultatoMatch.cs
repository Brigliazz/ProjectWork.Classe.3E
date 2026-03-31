using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BlaisePascal.ProjectWork._3E.Domain.Services.Categorie;

namespace BlaisePascal.ProjectWork._3E.Domain.Services
{
    // RisultatoMatch
    //
    // Rappresenta il risultato del tentativo di matching per UNA preferenza
    // espressa da uno studente richiedente.
    public sealed class RisultatoMatch
    {
        // Lo studente che ha espresso la preferenza
        public Studente Richiedente { get; init; } = null!;

        // Il testo originale grezzo dal campo "Scelta compagno/a"
        public string TestoOriginale { get; init; } = string.Empty;

        // Il segmento normalizzato usato per il matching (dopo pulizia)
        public string SegmentoNormalizzato { get; init; } = string.Empty;

        // Il candidato trovato nel pool (null se NessunMatch)
        public Studente? CandidatoTrovato { get; init; }

        // Score FuzzySharp (0–100)
        public int Score { get; init; }

        // Categoria risultante
        public CategoriaMatch Categoria { get; init; }

        // Messaggio descrittivo utile per il log / UI di revisione
        public string Messaggio { get; init; } = string.Empty;
    }
}
