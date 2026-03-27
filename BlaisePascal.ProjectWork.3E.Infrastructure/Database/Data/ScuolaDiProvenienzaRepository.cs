using BlaisePascal.ProjectWork._3E.Application.ImportModels;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data
{
    internal class ScuolaDiProvenienzaRepository
    {
        private string connectionString = "Data Source=studenti.db";

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

        public void SalvaScuole(List<ScuolaProvImportDto> scuole)
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
                command.Parameters.AddWithValue("@cf", s.CodiceFiscale ?? string.Empty);

                command.ExecuteNonQuery();
            }
        }
    }
}