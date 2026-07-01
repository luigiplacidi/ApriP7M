# Privacy — Apri P7M

**I tuoi documenti restano sul tuo PC. Apri P7M non carica nulla online.**

Questo documento descrive come Apri P7M tratta i tuoi dati. In breve: non li tratta
fuori dal tuo computer.

## Principi

- **Nessun upload.** Nessun file (P7M, PDF, XML, ZIP, allegati) viene mai inviato
  online. Tutta l'elaborazione avviene in locale.
- **Funzionamento offline.** L'app non richiede connessione internet per le sue
  funzioni principali.
- **Nessuna registrazione.** Nessun account, nessun login.
- **Nessuna telemetria nella v1.** Nessun tracciamento automatico dell'uso.
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

L'app può scrivere log diagnostici **locali e minimizzati** per facilitare la
risoluzione dei problemi. I log:

- **non contengono mai** il contenuto dei documenti;
- **non contengono** nomi file completi né percorsi locali;
- registrano solo informazioni tecniche (codice errore, modulo, fase).

## Diagnostica anonima (opt-in)

Apri P7M offre una funzione opzionale e volontaria di diagnostica anonima.

Regole:

- **Disattivata di default.**
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

Solo metadati tecnici minimizzati:

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
