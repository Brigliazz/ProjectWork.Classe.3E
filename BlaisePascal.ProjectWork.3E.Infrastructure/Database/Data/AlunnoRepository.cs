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