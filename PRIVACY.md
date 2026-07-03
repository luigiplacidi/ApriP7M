# Privacy — Apri P7M

**I tuoi documenti restano sul tuo PC. Apri P7M non carica nulla online.**

Questo documento descrive come Apri P7M tratta i tuoi dati. In breve: non li tratta
fuori dal tuo computer.

## Principi

- **Niente caricato online.** Nessun file (P7M, PDF, XML, ZIP, allegati) viene mai inviato
  online. Tutto avviene sul PC dell'utente.
- **Funzionamento offline.** L'app non richiede connessione internet per le sue
  funzioni principali.
- **Nessuna registrazione.** Nessun account, nessun login.
- **Nessuna telemetria automatica.** Nessun tracciamento automatico dell'uso.
- **Nessun servizio in background.** L'app non si avvia con Windows e non installa
  servizi.
- **Nessun privilegio amministrativo** richiesto, salvo dove strettamente necessario.

## File temporanei

Durante l'estrazione e la conversione, l'app può creare file temporanei (es. il
documento estratto da un `.p7m`). Questi file:

- restano sul tuo computer;
- sono salvati in una cartella temporanea dedicata dell'app;
- vengono **ripuliti** al termine dell'operazione o alla chiusura.

## Log locali

L'app può scrivere note tecniche (log) **locali e ridotte al minimo** per facilitare la
risoluzione dei problemi. Queste note:

- **non contengono mai** il contenuto dei documenti;
- **non contengono** i nomi dei file né le cartelle;
- registrano solo informazioni tecniche (codice errore, modulo, fase).

## Diagnostica anonima (opt-in)

Apri P7M offre una funzione opzionale e volontaria di diagnostica anonima.

Regole:

- **Spenta finché non la attivi tu.**
- Nessuna telemetria continua, nessun invio automatico.
- La diagnostica è generata solo dopo un errore e solo dopo una tua azione esplicita.
- Prima della condivisione, l'app mostra esattamente cosa verrebbe condiviso.

### Cosa NON viene mai incluso

- Documenti, PDF, XML, P7M, ZIP o allegati
- Contenuto dei file
- Nomi file completi
- Percorsi locali
- Dati fiscali o personali
- Identificativi persistenti

### Cosa può essere incluso

Solo poche informazioni tecniche essenziali:

- Versione dell'app
- Versione di Windows
- Tipo di file generico, es. "P7M"
- Dimensione del file in fascia, es. "1-5MB"
- Codice errore
- Fase dell'errore
- Modulo coinvolto
- Timestamp arrotondato all'ora
- Lingua dell'app

## Aggiornamenti

Gli aggiornamenti sono gestiti dal **Microsoft Store**. Il controllo aggiornamenti
non invia documenti, nomi file o dati personali.

## Domande

Per dubbi sulla privacy, apri una issue sul repository o usa la pagina contatti del
sito.
