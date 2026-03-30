using Microsoft.Data.Sqlite;
using System;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data
{
    public static class NumeroClassiRepository
    {
        private static string connectionString = "Data Source=studenti.db";

        public static void CreaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS NumeroClassi (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Automazione INTEGER,
                Informatica INTEGER,
                Biotecnologie INTEGER
            );
            ";
            command.ExecuteNonQuery();
        }

        public static void SvuotaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM NumeroClassi;";
            command.ExecuteNonQuery();
        }

        public static void SalvaNumeroClassi(int automazione, int informatica, int biotecnologie)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            INSERT INTO NumeroClassi
            (Automazione, Informatica, Biotecnologie)
            VALUES
            (@automazione, @informatica, @biotecnologie)
            ";

            command.Parameters.AddWithValue("@automazione", automazione);
            command.Parameters.AddWithValue("@informatica", informatica);
            command.Parameters.AddWithValue("@biotecnologie", biotecnologie);

            command.ExecuteNonQuery();
        }
    }
}
