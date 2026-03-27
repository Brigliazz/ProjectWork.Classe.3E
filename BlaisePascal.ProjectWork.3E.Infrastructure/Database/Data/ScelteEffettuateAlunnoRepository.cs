using BlaisePascal.ProjectWork._3E.Application.ImportModels;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data
{
    internal class ScelteEffettuateAlunnoRepository
    {
        private string connectionString = "Data Source=studenti.db";

        public void CreaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Scelte (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                IndirizzoScelto TEXT,
                CodiceFiscaleStudente TEXT,
                FOREIGN KEY (CodiceFiscaleStudente) REFERENCES Studenti(CodiceFiscale)
            );
            ";

            command.ExecuteNonQuery();
        }

        /* 
        // 2️⃣ Inserimento lista di scelte (da attivare quando hai la lista)
        public void SalvaScelte(List<Scelta> scelte)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            foreach (var sc in scelte)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                INSERT INTO Scelte
                (IndirizzoScelto, CodiceFiscaleStudente)
                VALUES
                (@indirizzo, @cf)
                ";
                
                command.Parameters.AddWithValue("@indirizzo", sc.IndirizzoScelto);
                command.Parameters.AddWithValue("@cf", sc.CodiceFiscaleStudente);

                command.ExecuteNonQuery();
            }
        }
    }
}