using BlaisePascal.ProjectWork._3E.Application.ImportModels;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data
{
    public static class GenitoriRepository
    {
        private static string connectionString = "Data Source=studenti.db";

        public static void CreaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Genitori (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT,
                Cognome TEXT,
                NumeroTelefono TEXT,
                Email TEXT,
                CodiceFiscaleStudente TEXT,
                FOREIGN KEY (CodiceFiscaleStudente) REFERENCES Studenti(CodiceFiscale)
            );
            ";

            command.ExecuteNonQuery();
        }

        public static GenitoreImportDTO LeggiGenitore(int id)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            SELECT * FROM Genitori WHERE Id = @id
            ";
            command.Parameters.AddWithValue("@id", id);

            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new GenitoreImportDTO
                {
                    Id = reader.GetInt32(0),
                    Nome = reader.GetString(1),
                    Cognome = reader.GetString(2),
                    Numero = reader.GetString(3),
                    Mail = reader.GetString(4),
                    CodiceFiscale = reader.GetString(5)
                };
            }
            return null;
        }

        public static void SvuotaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Genitori;";
            command.ExecuteNonQuery();
        }

        public static void SalvaGenitori(List<GenitoreImportDTO> genitori)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            foreach (var g in genitori)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                INSERT INTO Genitori
                (Nome, Cognome, NumeroTelefono, Email, CodiceFiscaleStudente)
                VALUES
                (@nome, @cognome, @telefono, @email, @cf)
                ";

                command.Parameters.AddWithValue("@nome", g.Nome ?? string.Empty);
                command.Parameters.AddWithValue("@cognome", g.Cognome ?? string.Empty);
                command.Parameters.AddWithValue("@telefono", g.Numero ?? string.Empty);
                command.Parameters.AddWithValue("@email", g.Mail ?? string.Empty);
                command.Parameters.AddWithValue("@cf", g.CodiceFiscale ?? string.Empty);

                command.ExecuteNonQuery();
            }
        }
    }
}