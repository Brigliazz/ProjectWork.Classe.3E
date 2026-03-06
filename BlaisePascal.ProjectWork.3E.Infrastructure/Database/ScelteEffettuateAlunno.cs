using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database
{
    internal class ScelteEffettuateAlunno
    {
        public static void CreaTabellaScelte()
        {
            string connectionString = "Data Source=studenti.db";

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                var command = connection.CreateCommand();

                command.CommandText =
                @"
        CREATE TABLE IF NOT EXISTS Scelte (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            IndirizzoScelto TEXT,
            CodiceFiscaleStudente TEXT,

            FOREIGN KEY (CodiceFiscaleStudente)
            REFERENCES Studenti(CodiceFiscale)
        );
        ";

                command.ExecuteNonQuery();
            }
        }
    }
}
