using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
// Assicurati di avere il modello ScuolaProvenienza qui

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data
{
    internal class ScuolaDiProvenienzaRepository
    {
        private string connectionString = "Data Source=studenti.db";

        // Creazione tabella ScuolaProvenienza
        public void CreaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS ScuolaProvenienza (
                CodiceScuola TEXT,
                NomeScuola TEXT,
                ComuneScuola TEXT,
                CodiceFiscaleStudente TEXT,
                FOREIGN KEY (CodiceFiscaleStudente) REFERENCES Studenti(CodiceFiscale)
            );
            ";

            command.ExecuteNonQuery();
        }

        /* 
        // Inserimento lista di scuole (da attivare quando hai la lista)
        public void SalvaScuole(List<ScuolaProvenienza> scuole)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            foreach (var s in scuole)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                INSERT INTO ScuolaProvenienza
                (CodiceScuola, Denominazione, CodiceFiscaleStudente)
                VALUES
                (@codice, @denom, @cf)
                ";
                
                command.Parameters.AddWithValue("@codice", s.CodiceScuola);
                command.Parameters.AddWithValue("@denom", s.Denominazione);
                command.Parameters.AddWithValue("@cf", s.CodiceFiscaleStudente);

                command.ExecuteNonQuery();
            }
        }
        */
    }
}