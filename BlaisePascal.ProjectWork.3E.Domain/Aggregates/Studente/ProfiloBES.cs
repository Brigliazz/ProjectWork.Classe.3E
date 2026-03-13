using BlaisePascal.ProjectWork._3E.Domain.Exceptions;

namespace BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente
{
    public sealed record ProfiloBES
    {
        public bool HasDisabilita { get; }
        public bool HasDSA { get; }
        public bool HasDisabilitaAssBase { get; }

        // Proprietà derivate — logica di dominio incapsulata qui
        public bool HasQualsiasiBES => HasDisabilita || HasDSA || HasDisabilitaAssBase;
        public bool IsVincolanteP1 => HasDisabilita; // impatta max studenti e unicità per classe

        // Costruttore privato — nessuno può istanziare direttamente
        private ProfiloBES(bool hasDisabilita, bool hasDSA, bool hasDisabilitaAssBase)
        {
            HasDisabilita = hasDisabilita;
            HasDSA = hasDSA;
            HasDisabilitaAssBase = hasDisabilitaAssBase;
        }

        // Factory method — unico punto di creazione valida
        public static ProfiloBES Crea(bool hasDisabilita, bool hasDSA, bool hasDisabilitaAssBase)
        {
            // Invariante di dominio: disabilità implica sempre BES base
            if (hasDisabilita && !hasDisabilitaAssBase)
                throw new DomainException("Uno studente con disabilità deve avere DisabilitaAssBase = true.");

            return new ProfiloBES(hasDisabilita, hasDSA, hasDisabilitaAssBase);
        }

        // Istanza vuota — evita null nel dominio
        public static ProfiloBES Nessuno => Crea(false, false, false);
    }
}
