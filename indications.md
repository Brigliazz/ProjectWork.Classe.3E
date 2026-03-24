# Funzionamento di DistribuzioneClassiService

Il servizio `DistribuzioneClassiService` si occupa di smistare gli studenti iscritti nelle classi prime disponibili, garantendo il rispetto di normative, l'equità e il bilanciamento attraverso 3 fasi principali.

## Flusso Principale (`DistribuisciAsync`)

Il metodo asincrono `DistribuisciAsync` orchestra l'intero processo:
1. Recupera gli studenti non assegnati (`GetNonAssegnatiAsync`) e le classi disponibili (`GetAllAsync`) dai rispettivi repository.
2. Crea una lista mutabile, ovvero un `pool` (riga 41), che agisce da "serbatoio": ogni volta che uno studente viene assegnato a una classe, viene rimosso da questa lista.
3. Esegue sequenzialmente tre fasi principali chiamando i rispettivi metodi privati e infine salva in modo asincrono le entità modificate sul database (`SaveChangesAsync()`).

---

## Fase 1 (F1): Scheletro delle Classi (Studenti con Disabilità)

**Metodo di riferimento:** `F1_AssegnaDisabili(List<Studente> pool, List<ClassePrima> classi)`

In questa fase viene garantita la precedenza assoluta agli studenti con disabilità certificata.

- **Logica:** Si estraggono dal pool gli studenti per i quali `ProfiloBES.HasDisabilita` è `true` (riga 69).
- **Precondizione (Hard Limit):** L'algoritmo verifica che il numero di studenti disabili non superi il numero totale di classi disponibili. In caso contrario, viene lanciata una `DomainException` per segnare l'impossibilità di proseguire (riga 74). Si assume il vincolo di *al massimo un disabile per classe*.
- **Assegnazione:** Attraverso il ciclo `foreach`, ogni studente disabile viene assegnato alla classe momentaneamente con meno studenti che non possiede ancora un disabile (`!HasStudenteConDisabilita`). Una volta assegnato, lo studente viene rimosso dal `pool` (riga 87).

---

## Fase 2 (F2): Distribuzione Ragazze

**Metodo di riferimento:** `F2_DistribuisciRagazze(List<Studente> pool, List<ClassePrima> classi)`

Questa fase assicura che le alunne siano equamente distribuite e, per evitare l'isolamento sociologico (particolarmente critico negli indirizzi tecnici o a predominanza maschile), vengano sempre inserite in piccoli gruppi o "cluster".

- **Gruppi Minimi:** La dimensione ideale del gruppo viene calcolata dinamicamente e varia da 2 a 4 ragazze per classe in base alla mole totale (`Math.Clamp`, riga 114). Se ci sono meno di 2 ragazze totali nel pool, l'assegnazione protetta non può avvenire e la/e ragazza/e viene/vengono inserita/e semplicemente nella classe momentaneamente più vuota.
- **Accorpamento per Provenienza:** Le ragazze vengono ordinate per `CodiceScuolaProvenienza` (riga 118) così che ex-compagne delle scuole medie finiscano tendenzialmente nello stesso "gruppo".
- **Gestione del Residuo:** I gruppi vengono creati da N alunne. Se al termine della spartizione l'ultimo gruppo generato è composto da *una sola ragazza* (residuo = 1), per evitare l'isolamento questa viene accorpata al penultimo gruppo creato (riga 134-138).
- **Assegnazione:** Ogni blocco indivisibile (gruppo) viene processato interamente e tutte le ragazze facenti parte del gruppo vanno alla classe con maggior `CapienzaResidua` del momento, venendo poi spuntate dal `pool`.

---

## Fase 3 (F3): Distribuzione Maschi e Bilanciamento 

**Metodo di riferimento:** `F3_DistribuisciMaschiEBilancia(...)`

Questa macro-fase si divide in: l'inserimento effettivo della maggioranza dell'utenza e infine l'applicazione dinamica di scambi per parificare attributi chiave.

### 3a. Inserimento dei maschi rimanenti (riga 165)
- I ragazzi restanti nel database (`pool`) vengono raggruppati a loro volta in base al `CodiceScuolaProvenienza`, privilegiando nella distribuzione iniziale i gruppi provenienti dalla stessa scuola media più numerosi (`OrderByDescending`, riga 168).
- Viene stimata la **Capienza Ideale**, una media matematica teorica di alunni per sezione (riga 172).
- Per ogni gruppo-scuola, il modulo riempie progressivamente le sezioni, dando sempre la precedenza alla classe con la più alta differenza tra capienza effettiva e capienza ideale (quindi la classe "con più posti liberi per arrivare in media"). Gli studenti vengono così sottratti uno a uno dal raggruppamento e dal pool (riga 196-201).

### 3b. Bilanciamento "Soft" (Algoritmo di Swapping) (riga 205)
A classi formate formalmente a livello numerico, per garantire classi eterogenee secondo direttive o necessità didattiche, il sistema attua un meccanismo di *swap* (scambio 1 a 1 per non variare la capienza). N.B. Gli unici candidati per lo scambio sono gli studenti Maschi non disabili, per scongiurare di intaccare lo scheletro dell'F1 e l'anti-isolamento femminile dell'F2. Tali scambi perdurano finché la differenza tra la classe più piena e meno piena della caratteristica non scende sotto a `1`.

In ordine preciso di chiamata:
1. **Stranieri (`BilanciaSoft_Stranieri`, riga 220):**
   - Viene prelevato uno straniero dalla classe con più stranieri per passarlo alla classe che ne ha in assoluto meno.
   - Si preleva dalla seconda classe in cambio un italiano e gli si inverte posto.
   - Vengono introdotte in questo scope controlli sulle percentuali massime tollerate (es. `30%` limite fisiologico, riga 268-281).
2. **DSA (`BilanciaSoft_Attributo` con flag DSA, riga 208):**
   - Stessa logica iterativa e prelievo da "classe più gravida" a "classe più sguarnita".
   - **Eccezione:** Nel cercare un "candidato italiano regolare", vengono esclusi esplicitamente gli studenti inseriti come stranieri nel bilanciamento antecedente. 
3. **IRC (Alunni avvalentesi dell'Insegnamento Religione Cattolica, riga 210):**
   - L'algoritmo di iterazione riparte ma esonera tutti gli studenti extracomunitari ed i BES-DSA da qualsiasi spostamento futuro.
4. **Eccellenze (Alunni con voti di fascia superiore, riga 212):**
   - Ultimissimo raffinamento: bilanciamento dei profitti alti scartando dal circolo di swap gli Stranieri, i DSA e perfino coloro inquadrati sotto IRC.

Questo incastro basato sul blocco a filtri progressivi "congela" l'ossatura sociologica senza disfare gli equilibri faticosamente raggiunti nei passaggi precedenti, consegnando le classi finali bilanciate.
