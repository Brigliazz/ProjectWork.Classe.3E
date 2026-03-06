using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
 // Assicurati di avere il modello Genitore qui

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data
{
    internal class GenitoriRepository
    {
        private string connectionString = "Data Source=studenti.db";

        // 1️⃣ Creazione tabella Genitori
        public void CreaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Genitori (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT,
                NumeroTelefono TEXT,
                Email TEXT,
                CodiceFiscaleStudente TEXT,
                FOREIGN KEY (CodiceFiscaleStudente) REFERENCES Studenti(CodiceFiscale)
            );
            ";

            command.ExecuteNonQuery();
        }

        /* 
        // Inserimento lista di genitori (da attivare quando hai la lista)
        public void SalvaGenitori(List<Genitore> genitori)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            foreach (var g in genitori)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                INSERT INTO Genitori
                (Nome, NumeroTelefono, Email, CodiceFiscaleStudente)
                VALUES
                (@nome, @telefono, @email, @cf)
                ";
                
                command.Parameters.AddWithValue("@nome", g.Nome);
                command.Parameters.AddWithValue("@telefono", g.NumeroTelefono);
                command.Parameters.AddWithValue("@email", g.Email);
                command.Parameters.AddWithValue("@cf", g.CodiceFiscaleStudente);

                command.ExecuteNonQuery();
            }
        }
        */
    }
}