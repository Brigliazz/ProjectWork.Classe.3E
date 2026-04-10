using System;

namespace BlaisePascal.ProjectWork._3E.Domain.Exceptions
{
    public class ClasseCapienzaSuperataException : DomainException
    {
        public string NomeClasse { get; }
        public int Limite { get; }

        public ClasseCapienzaSuperataException(string nomeClasse, int limite, string message)
            : base(message)
        {
            NomeClasse = nomeClasse;
            Limite = limite;
        }
    }
}
