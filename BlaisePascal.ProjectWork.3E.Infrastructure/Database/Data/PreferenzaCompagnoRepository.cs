using BlaisePascal.ProjectWork._3E.Application.ImportModels;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data
{
    internal class PreferenzaCompagnoRepository
    {
        private string connectionString = "Data Source=studenti.db";

        public void CreaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS PreferenzeCompagno (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                NomeStudenteScelto TEXT,
                CodiceFiscaleStudente TEXT,
                FOREIGN KEY (CodiceFiscaleStudente) REFERENCES Studenti(CodiceFiscale)
            );
            ";

            command.ExecuteNonQuery();
        }

        public void SalvaPreferenze(List<PreferenzaCompagnoImportDto> preferenze)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            foreach (var p in preferenze)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                INSERT INTO PreferenzeCompagno
                (NomeStudenteScelto, CodiceFiscaleStudente)
                VALUES
                (@nomeScelto, @cf)
                ";

                command.Parameters.AddWithValue("@nomeScelto", p.NomeStudenteScelto ?? string.Empty);
                command.Parameters.AddWithValue("@cf", p.CodiceFiscale ?? string.Empty);

                command.ExecuteNonQuery();
            }
        }
    }
}