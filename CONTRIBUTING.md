# Contribuire a Apri P7M

Grazie per l'interesse! Apri P7M è un piccolo progetto gratuito con codice sorgente pubblico.
Ci lavoro quando posso, ma ci lavoro davvero 🙂

## Principi non negoziabili

Prima di proporre una modifica, tieni presente i vincoli del progetto. Una PR che
li viola non potrà essere accettata:

- **Nessun upload di file.** Niente cloud, niente invio di documenti online.
- **Niente telemetria continua.** La diagnostica è opt-in, manuale e minimizzata.
- **Niente verifica legale della firma.** Apri P7M *estrae* il contenuto, non
  certifica la validità della firma digitale.
- **Niente nei log che identifichi un documento** (contenuto, nome file completo,
  percorso).
- **Niente file di test reali nel repository** (vedi sezione test corpus).

## Come iniziare

1. Fai un fork e crea un branch dedicato (`feat/...`, `fix/...`).
2. La logica va in `ApriP7M.Core` (testabile, senza UI). La UI in `ApriP7M.App`.
3. Aggiungi test in `tests/ApriP7M.Core.Tests` per ogni comportamento nuovo.
4. Esegui `dotnet build` e `dotnet test`.

> La build completa dell'app richiede Windows. `ApriP7M.Core` si compila e si testa
> su qualsiasi OS con .NET 10.

## File di test

Non includere nelle PR documenti reali, fatture, contratti, file P7M, XML o PDF
contenenti dati personali. Per riprodurre un problema usa file sintetici,
anonimizzati o esempi pubblici linkati nella descrizione della issue.

## Stile

- Segui `.editorconfig`.
- Nomi e commenti coerenti con il codice circostante (italiano per copy utente,
  inglese per identificatori del codice va bene).
- Messaggi di commit chiari.

## Segnalazioni

- Bug → usa il template "Bug report".
- Idee → usa il template "Feature request".
