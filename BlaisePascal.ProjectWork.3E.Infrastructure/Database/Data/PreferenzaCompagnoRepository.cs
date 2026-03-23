using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

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
            CREATE TABLE IF NOT EXISTS Scelte (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                CodiceFiscaleStudenteScelto TEXT,
                CodiceFiscaleStudente TEXT,
                FOREIGN KEY (CodiceFiscaleStudente) REFERENCES Studenti(CodiceFiscale)
            );
            ";

            command.ExecuteNonQuery();
        }
    }
}
