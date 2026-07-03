# Changelog

Le novità di ogni versione di Apri P7M. Le release complete, con installer e
pacchetti, sono nella [pagina release](https://github.com/luigiplacidi/ApriP7M/releases).

## 1.0.5 — 3 luglio 2026

- La pagina PayPal per le donazioni si apre in italiano
- La Cronologia si spiega da sola: vive finché l'app è aperta, si svuota alla
  chiusura, niente resta salvato su disco
- Testi senza gergo tecnico in tutta l'app: via "upload", "cloud", "log",
  "default" — ora dicono cosa succede in parole semplici

## 1.0.4 — 2 luglio 2026

- La reinstallazione sopra una versione esistente ora aggiorna senza bloccarsi,
  anche se nella cartella restano residui di una versione precedente
- Scegliendo una cartella non vuota, il setup propone la sottocartella
  dedicata `Apri P7M` invece di rifiutarla
- Niente più avviso di Windows ("È possibile che il programma non sia stato
  disinstallato correttamente") alla disinstallazione
- "Controlla aggiornamenti" mostra sempre l'esito: aggiornamento disponibile,
  già aggiornato, oppure spiega che con l'installer standalone le nuove
  versioni si scaricano dal sito
- Bandiera italiana visibile nella pagina Informazioni (Windows non mostra le
  emoji delle bandiere)
- Testi privacy allineati: "nessuna telemetria automatica"

## 1.0.3 — 2 luglio 2026

- Pubblicazione sul [Microsoft Store](https://apps.microsoft.com/store/detail/9PN10B89Z7LR)
- Il pulsante "Lascia una recensione" apre la scheda dello Store
  (prima falliva in silenzio)
- Il controllo aggiornamenti non va più in errore con l'installer standalone
- Nuovi test automatici delle promesse di prodotto: nessuna API di rete nel
  Core, estrazione P7M fedele byte per byte, PDF di cortesia, pulizia dei
  file temporanei, diagnostica disattivata di default

## 1.0.2 — 1 luglio 2026

- Firma "Fatto orgogliosamente in Italia"
- Correzioni ai link dell'installer pubblico

## 1.0.1 — 1 luglio 2026

- Corretta la disinstallazione della versione standalone

## 1.0.0 — 1 luglio 2026

- Prima release pubblica: apre `.p7m`, `.pdf.p7m`, `.xml`, `.xml.p7m` e `.zip`,
  estrae il documento originale e genera il PDF di cortesia delle fatture.
  Tutto in locale, senza upload.
