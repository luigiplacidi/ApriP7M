# Security Policy — Apri P7M

## Ambito

Apri P7M elabora file potenzialmente non fidati (`.p7m`, `.xml`, `.zip` ricevuti via
PEC). La sicurezza del parsing è una priorità: un file malformato non deve mai poter
compromettere il sistema dell'utente.

Aree sensibili:

- Parsing CMS/PKCS#7/CAdES (BouncyCastle)
- Estrazione archivi ZIP (rischio *zip slip* / path traversal)
- Trasformazione XSLT delle fatture (rischio XXE / entità esterne)
- Anteprima in WebView2 (deve restare isolata, senza accesso di rete)

## Segnalare una vulnerabilità

**Non aprire una issue pubblica** per vulnerabilità di sicurezza.

Scrivi in privato a: **security@aprip7m.it** (o usa "Private vulnerability reporting"
di GitHub, se attivo).

Includi:

- descrizione del problema e impatto;
- passi per riprodurlo;
- versione di Apri P7M e di Windows.

> ⚠️ Non allegare documenti reali contenenti dati personali. Se serve un campione,
> usa un file sintetico o anonimizzato.

## Tempi

Cerco di rispondere entro pochi giorni. Trattandosi di un progetto gestito nel tempo
libero, la tempistica di fix dipende dalla gravità.

## Buone pratiche adottate

- Estrazione ZIP con validazione dei percorsi (no path traversal).
- Parser XML con risoluzione delle entità esterne disabilitata (no XXE).
- WebView2 configurata senza navigazione esterna né accesso di rete.
- File temporanei ripuliti; nessun contenuto documento nei log.
