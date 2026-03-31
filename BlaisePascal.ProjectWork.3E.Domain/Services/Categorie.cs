using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlaisePascal.ProjectWork._3E.Domain.Services
{
    public class Categorie
    {
        // Enumerazione delle tre categorie di esito
        public enum CategoriaMatch
        {
            Certo,      // score ≥ SogliaMatchCerto   → entra automaticamente in OR-Tools
            Incerto,    // SogliaMatchIncerto ≤ score < SogliaMatchCerto → revisione umana
            NessunMatch // score < SogliaMatchIncerto  → scartato, loggato
        }

    }
}
