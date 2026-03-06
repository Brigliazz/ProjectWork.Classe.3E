using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlaisePascal.ProjectWork._3E.Domain.Objects.Studente
{
    sealed record BESprofile
    {
        public bool HasDisabilita { get; }
        public bool HasDSA { get; }
        public bool HasDisabilitaAssBase { get; }

        // Proprietà derivate — logica di dominio incapsulata qui
        public bool HasQualsiasiBES => HasDisabilita || HasDSA || HasDisabilitaAssBase;
        public bool IsVincolanteP1 => HasDisabilita; // impatta max studenti e unicità per classe

        // Costruttore privato — nessuno può istanziare direttamente
        private BESprofile(bool hasDisabilita, bool hasDSA, bool hasDisabilitaAssBase)
        {
            HasDisabilita = hasDisabilita;
            HasDSA = hasDSA;
            HasDisabilitaAssBase = hasDisabilitaAssBase;
        }

        // Factory method — unico punto di creazione valida
       /* public static BESprofile Crea(bool hasDisabilita, bool hasDSA, bool hasDisabilitaAssBase)
        {
            // Invariante di dominio: disabilità implica sempre BES base
            if (hasDisabilita && !hasDisabilitaAssBase)
                throw new DomainException("Uno studente con disabilità deve avere DisabilitaAssBase = true.");

            return new BESprofile(hasDisabilita, hasDSA, hasDisabilitaAssBase);
        }*/

        // Istanza vuota — evita null nel dominio
        public static BESprofile Nessuno => Crea(false, false, false);
    }
}
