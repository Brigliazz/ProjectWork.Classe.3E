using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
// servità mettere lo using

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data
{
    internal class StudenteRepository
    {
        private string connectionString = "Data Source=studenti.db";

        //Crea tabella
        public void CreaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            /*
             public string Nome { get; set; }ok
        public string Cognome { get; set; }ok
        public bool Sesso { get; set; }ok
        public string CodiceFiscale { get; set; }ok
        public string Cittadinanza { get; set; }ok
        public string ComuneResidenza { get; set; }ok
        public bool Disabilita { get; set; }
        public bool Dsa { get; set; }
        public string Indirizzo { get; set; }
            1. DATI STUDENTE (Anagrafica e Profilo)
- Nome   ok
- Cognome  ok
- Sesso (es. Maschio/Femmina)  ok
- Codice Fiscale (Univoco) ok
- Data di Nascita ok
- Data Arrivo in Italia (Opzionale) ok
- Cittadinanza (Codice ISTAT, es. 200 = Italia) ok
- Comune di Residenza ok
- Indirizzo ok
- Disabilità (Sì/No) ok
- DSA (Sì/No) ok
- Disabilità Assistenza Base (Sì/No) ok
- Fa Religione (Sì/No) ok
- Voto Esame Terza Media ok
            */
            var command = connection.CreateCommand();
            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Studenti (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT,
                Cognome TEXT,
                Maschio BOOLEAN,
                DataDiNascita DATE,
                DataDiArrivoInItalia DATE,
                Cittadinaza TEXT,
                FaReligione BOOLEAN,
                VotoEsame INT,
                CodiceFiscale TEXT UNIQUE,
                Cittadinanza TEXT,
                ComuneResidenza TEXT,
                Disabilita BOOLEAN,
                Dsa BOOLEAN,
                DisabilitaAssistenzaBase BOOLEAN,
                Indirizzo TEXT
            );
            ";
            command.ExecuteNonQuery();
        }

        // Inserimento lista studenti
        /*    public void SalvaStudenti(List<Studente> studenti)
            {
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                foreach (var s in studenti)
                {
                    var command = connection.CreateCommand();
                    command.CommandText =
                    @"
                    INSERT INTO Studenti
                    (Nome, Cognome, Maschio, CodiceFiscale, Cittadinanza, ComuneResidenza, Disabilita, Dsa, Indirizzo)
                    VALUES
                    (@nome, @cognome, @maschio, @cf, @cittadinanza, @comune, @disabilita, @dsa, @indirizzo)
                    ";
                    command.Parameters.AddWithValue("@nome", s.Nome);
                    command.Parameters.AddWithValue("@cognome", s.Cognome);
                    command.Parameters.AddWithValue("@maschio", s.Maschio);
                    command.Parameters.AddWithValue("@cf", s.CodiceFiscale);
                    command.Parameters.AddWithValue("@cittadinanza", s.Cittadinanza);
                    command.Parameters.AddWithValue("@comune", s.ComuneResidenza);
                    command.Parameters.AddWithValue("@disabilita", s.Disabilita);
                    command.Parameters.AddWithValue("@dsa", s.Dsa);
                    command.Parameters.AddWithValue("@indirizzo", s.Indirizzo);
                    command.ExecuteNonQuery();
                }
           } */
    }
}