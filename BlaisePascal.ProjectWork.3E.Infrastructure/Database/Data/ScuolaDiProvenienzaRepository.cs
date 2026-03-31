using BlaisePascal.ProjectWork._3E.Application.ImportModels;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data
{
    public static class ScuolaDiProvenienzaRepository
    {
        private static string connectionString = "Data Source=studenti.db";

        public static void CreaTabella()
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
        public static ScuolaProvImportDto LeggiScuola(int id)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            SELECT * FROM ScuolaProvenienza WHERE Id = @id
            ";
            command.Parameters.AddWithValue("@id", id);

            var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new ScuolaProvImportDto
                {
                    
                    CodiceScuola = reader.GetString(1),
                    DenominazioneScuola = reader.GetString(2),
                    ComuneScuola = reader.GetString(3),
                    CodiceFiscaleStudente = reader.GetString(4)
                };
            }
            return null;
        }
        public static void SvuotaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM ScuolaProvenienza;";
            command.ExecuteNonQuery();
        }

        public static void SalvaScuole(List<ScuolaProvImportDto> scuole)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            foreach (var s in scuole)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                INSERT INTO ScuolaProvenienza
                (CodiceScuola, NomeScuola, ComuneScuola, CodiceFiscaleStudente)
                VALUES
                (@codice, @denom, @comune, @cf)
                ";

                command.Parameters.AddWithValue("@codice", s.CodiceScuola ?? string.Empty);
                command.Parameters.AddWithValue("@denom", s.DenominazioneScuola ?? string.Empty);
                command.Parameters.AddWithValue("@comune", s.ComuneScuola ?? string.Empty);
                command.Parameters.AddWithValue("@cf", s.CodiceFiscaleStudente ?? string.Empty);

                command.ExecuteNonQuery();
            }
        }
    }
}