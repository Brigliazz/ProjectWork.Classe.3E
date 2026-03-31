using BlaisePascal.ProjectWork._3E.Application.ImportModels;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data
{
    public static class ScelteEffettuateAlunnoRepository
    {
        private static string connectionString = "Data Source=studenti.db";

        public static void CreaTabella()
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

        public static SceltaImportDto LeggiScelta(int id)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            SELECT * FROM Scelte WHERE Id = @id
            ";
            command.Parameters.AddWithValue("@id", id);

            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new SceltaImportDto
                {
                    Id = reader.GetInt32(0),
                    IndirizzoScelto = reader.GetString(1),
                    CodiceFiscaleStudente = reader.GetString(2)
                };
            }
            return null;
        }   

        public static void SvuotaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Scelte;";
            command.ExecuteNonQuery();
        }
        public static void SalvaScelte(List<SceltaImportDto> scelte)
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

                command.Parameters.AddWithValue("@indirizzo", sc.IndirizzoScelto ?? string.Empty);
                command.Parameters.AddWithValue("@cf", sc.CodiceFiscaleStudente ?? string.Empty);

                command.ExecuteNonQuery();
            }
        }
    }
}