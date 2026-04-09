using System;
using System.Collections.Generic;
using System.Linq;
using BlaisePascal.ProjectWork._3E.Application.ImportModels;
using BlaisePascal.ProjectWork._3E.Domain.Aggregates.Studente;
using BlaisePascal.ProjectWork._3E.Domain.Enums;
namespace BlaisePascal.ProjectWork._3E.Application.Services
{
    /// <summary>
    /// Responsabile della traduzione dei DTO di importazione nelle entità di dominio.
    /// Non contiene logica di business: si limita a costruire valori coerenti per il dominio.
    /// </summary>
    public static class StudenteMapper
    {
        // Codice ISTAT per Italia (deve combaciare con la costante in Cittadinanza.cs)
        private const int CodiceItaliaISTAT = 200;
        // Codice generico per stranieri quando il codice ISTAT non è disponibile
        private const int CodiceStranieroGenerico = 100;

        /// <summary>
        /// Converte una lista di StudenteImportDto in entità di dominio Studente.
        /// Arricchisce ogni studente con preferenza compagno e codice scuola di provenienza.
        /// </summary>
        public static List<Studente> MappaStudenti(
            List<StudenteImportDto> alunniDto,
            List<PreferenzaCompagnoImportDto> preferenzeDto,
            List<ScuolaProvImportDto> scuoleDto)
        {
            // Indice CF→preferenza per ricerca O(1)
            var prefByCf = preferenzeDto
                .Where(p => !string.IsNullOrWhiteSpace(p.CodiceFiscaleStudente))
                .ToDictionary(p => p.CodiceFiscaleStudente!, p => p.NomeStudenteScelto ?? string.Empty,
                              StringComparer.OrdinalIgnoreCase);

            // Indice CF→codice scuola per ricerca O(1)
            var scuolaByCf = scuoleDto
                .Where(s => !string.IsNullOrWhiteSpace(s.CodiceFiscaleStudente))
                .ToDictionary(s => s.CodiceFiscaleStudente!, s => s.CodiceScuola ?? "SCONOSCIUTA",
                              StringComparer.OrdinalIgnoreCase);

            var studenti = new List<Studente>(alunniDto.Count);

            foreach (var dto in alunniDto)
            {
                try
                {
                    studenti.Add(MappaSingolo(dto, prefByCf, scuolaByCf));
                }
                catch (Exception ex)
                {
                    // Log e prosegui: uno studente malformato non blocca l'intera distribuzione
                    Console.WriteLine($"[StudenteMapper] WARN — Studente CF={dto.CodiceFiscale} scartato: {ex.Message}");
                }
            }

            return studenti;
        }

        // ──────────────────────────────────────────────────────────────
        // Mapping di un singolo studente
        // ──────────────────────────────────────────────────────────────

        private static Studente MappaSingolo(
            StudenteImportDto dto,
            Dictionary<string, string> prefByCf,
            Dictionary<string, string> scuolaByCf)
        {
            var sesso       = dto.Sesso ? Sesso.Femmina : Sesso.Maschio;
            var cittadinanza = MappaCittadinanza(dto.Cittadinanza);
            var dataNascita  = ParseData(dto.DataDiNascita);
            var dataArrivo   = ParseDataNullable(dto.DataArrivoInItalia);

            // Invariante di dominio: disabilità implica assistenza base
            bool disabilitaAssBase = dto.DisabilitaAssistenzaBase || dto.Disabilita;
            var profiloBES = ProfiloBES.Crea(dto.Disabilita, dto.Dsa, disabilitaAssBase);

            // Scelta compagno: presente solo se il CF ha una preferenza registrata
            SceltaCompagno? sceltaCompagno = null;
            if (!string.IsNullOrWhiteSpace(dto.CodiceFiscale) &&
                prefByCf.TryGetValue(dto.CodiceFiscale, out var nomeScelto) &&
                !string.IsNullOrWhiteSpace(nomeScelto))
            {
                sceltaCompagno = SceltaCompagno.Crea(nomeScelto);
            }

            // Codice scuola di provenienza (fallback al campo Indirizzo del DTO se mancante)
            string codiceScuola = string.Empty;
            if (!string.IsNullOrWhiteSpace(dto.CodiceFiscale) &&
                scuolaByCf.TryGetValue(dto.CodiceFiscale, out var codice))
            {
                codiceScuola = codice;
            }
            if (string.IsNullOrWhiteSpace(codiceScuola))
                codiceScuola = string.IsNullOrWhiteSpace(dto.Indirizzo) ? "SCONOSCIUTA" : dto.Indirizzo;

            // Voto esame: 0 viene trattato come assente
            int? votoEsame = dto.VotoEsameTerzaMedia > 0 ? dto.VotoEsameTerzaMedia : null;

            return Studente.Crea(
                nome:                    dto.Nome     ?? string.Empty,
                cognome:                 dto.Cognome  ?? string.Empty,
                sesso:                   sesso,
                codiceFiscale:           string.IsNullOrWhiteSpace(dto.CodiceFiscale) ? Guid.NewGuid().ToString("N") : dto.CodiceFiscale,
                dataNascita:             dataNascita,
                dataArrivoItalia:        dataArrivo,
                cittadinanza:            cittadinanza,
                codiceScuolaProvenienza: codiceScuola,
                profiloBES:              profiloBES,
                faReligione:             dto.FaReligione,
                votoEsame:               votoEsame,
                sceltaCompagno:          sceltaCompagno
            );
        }

        // ──────────────────────────────────────────────────────────────
        // Helper: conversione Cittadinanza
        // ──────────────────────────────────────────────────────────────

        private static Cittadinanza MappaCittadinanza(string? cittadinanzaDto)
        {
            if (string.IsNullOrWhiteSpace(cittadinanzaDto))
                return Cittadinanza.Italiana;

            // Se è già un numero ISTAT valido, usarlo direttamente
            if (int.TryParse(cittadinanzaDto.Trim(), out int codice) && codice > 0)
                return Cittadinanza.Crea(codice);

            // Confronto testuale: varianti di "Italia"
            if (cittadinanzaDto.Trim().Equals("ITALIA", StringComparison.OrdinalIgnoreCase) ||
                cittadinanzaDto.Trim().Equals("ITALIANA", StringComparison.OrdinalIgnoreCase) ||
                cittadinanzaDto.Trim().Equals("IT", StringComparison.OrdinalIgnoreCase))
                return Cittadinanza.Italiana;

            // Qualsiasi altro valore → straniero generico
            return Cittadinanza.Crea(CodiceStranieroGenerico);
        }

        // ──────────────────────────────────────────────────────────────
        // Helper: parsing date
        // ──────────────────────────────────────────────────────────────

        private static DateOnly ParseData(string? testo)
        {
            if (string.IsNullOrWhiteSpace(testo))
                return DateOnly.MinValue;

            // Formati comuni nei file Excel italiani
            string[] formati = { "dd/MM/yyyy", "yyyy-MM-dd", "d/M/yyyy", "MM/dd/yyyy", "yyyyMMdd" };
            foreach (var fmt in formati)
            {
                if (DateOnly.TryParseExact(testo.Trim(), fmt,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var data))
                    return data;
            }

            // Fallback: proviamo il parsing generico
            if (DateTime.TryParse(testo.Trim(), out var dt))
                return DateOnly.FromDateTime(dt);

            Console.WriteLine($"[StudenteMapper] WARN — Data non parsabile: '{testo}', uso DateOnly.MinValue");
            return DateOnly.MinValue;
        }

        private static DateOnly? ParseDataNullable(string? testo)
        {
            if (string.IsNullOrWhiteSpace(testo))
                return null;

            var data = ParseData(testo);
            return data == DateOnly.MinValue ? null : data;
        }
    }
}
