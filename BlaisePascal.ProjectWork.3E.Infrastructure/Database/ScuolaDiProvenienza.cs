using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database
{
    internal class ScuolaDiProvenienza
    {
        

        public static void CreaTabellaScuola()
        {
        string connectionString = "Data Source=studenti.db";

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var command = connection.CreateCommand();

            command.CommandText =
            @"
        CREATE TABLE IF NOT EXISTS ScuolaProvenienza (
            CodiceScuola TEXT,
            Denominazione TEXT,
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
