using BlaisePascal.ProjectWork._3E.Application.ImportModels;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace BlaisePascal.ProjectWork._3E.Infrastructure.Database.Data
{
    public static class AlunnoRepository
    {
        private static string connectionString = "Data Source=studenti.db";

        public static void CreaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
            CREATE TABLE IF NOT EXISTS Studenti (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT,
                Cognome TEXT,
                Sesso BOOLEAN,
                DataDiNascita TEXT,
                DataArrivoInItalia TEXT,
                CodiceFiscale TEXT UNIQUE,
                Cittadinanza TEXT,
                ComuneResidenza TEXT,
                Disabilita BOOLEAN,
                Dsa BOOLEAN,
                DisabilitaAssistenzaBase BOOLEAN,
                Indirizzo TEXT,
                VotoEsameTerzaMedia INTEGER,
                FaReligione BOOLEAN
            );
            ";
            command.ExecuteNonQuery();
        }
        public static void SvuotaTabella()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Studenti;";
            command.ExecuteNonQuery();
        }
        public static void SalvaStudenti(List<StudenteImportDto> alunni)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            foreach (var s in alunni)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                INSERT INTO Studenti
                (Nome, Cognome, Sesso, CodiceFiscale, Cittadinanza, ComuneResidenza, Disabilita, Dsa, Indirizzo, VotoEsameTerzaMedia, FaReligione, DataArrivoInItalia, DataDiNascita, DisabilitaAssistenzaBase)
                VALUES
                (@nome, @cognome, @sesso, @cf, @cittadinanza, @comune, @disabilita, @dsa, @indirizzo, @voto, @religione, @arrivo, @nascita, @assistenza)
                ";

                command.Parameters.AddWithValue("@nome", s.Nome ?? string.Empty);
                command.Parameters.AddWithValue("@cognome", s.Cognome ?? string.Empty);
                command.Parameters.AddWithValue("@sesso", s.Sesso);
                command.Parameters.AddWithValue("@cf", s.CodiceFiscale ?? string.Empty);
                command.Parameters.AddWithValue("@cittadinanza", s.Cittadinanza ?? string.Empty);
                command.Parameters.AddWithValue("@comune", s.ComuneResidenza ?? string.Empty);
                command.Parameters.AddWithValue("@disabilita", s.Disabilita);
                command.Parameters.AddWithValue("@dsa", s.Dsa);
                command.Parameters.AddWithValue("@indirizzo", s.Indirizzo ?? string.Empty);
                command.Parameters.AddWithValue("@voto", int.TryParse(s.VotoEsameTerzaMedia, out int voto) ? voto : 0);
                command.Parameters.AddWithValue("@religione", s.FaReligione);
                command.Parameters.AddWithValue("@arrivo", s.DataArrivoInItalia ?? string.Empty);
                command.Parameters.AddWithValue("@nascita", s.DataDiNascita ?? string.Empty);
                command.Parameters.AddWithValue("@assistenza", s.DisabilitaAssistenzaBase);

                command.ExecuteNonQuery();
            }
        }
    }
}