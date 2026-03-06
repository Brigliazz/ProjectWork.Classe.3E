using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database
{
    internal class Alunno
    {
        public static void CreaDatabaseAlunno()
        {
            string connectionString = "Data Source=studenti.db";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();

                command.CommandText =
                @"
            CREATE TABLE IF NOT EXISTS Studenti (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT,
                Cognome TEXT,
                Maschio BOOLEAN,
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
        }

    }
}
