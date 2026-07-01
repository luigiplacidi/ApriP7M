<p align="center">
  <img src="assets/github-banner.png" alt="Apri P7M" width="100%">
</p>

<h1 align="center">Apri P7M</h1>

<p align="center">
  <strong>Apri, visualizza e converti. Tutto resta sul tuo PC.</strong>
</p>

<p align="center">
  App gratuita per Windows che apre, mostra e converte file <code>.p7m</code>, XML e fatture elettroniche in PDF leggibili — <strong>senza caricare nulla online</strong>.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Microsoft%20Store-in%20arrivo-blue?logo=microsoft" alt="Microsoft Store in arrivo">
  <a href="https://aprip7m.it"><img src="https://img.shields.io/badge/sito-aprip7m.it-2563eb" alt="Sito Apri P7M"></a>
  <img src="https://img.shields.io/badge/licenza-source%20available-green" alt="Source available">
  <img src="https://img.shields.io/badge/.NET-10%20LTS-512BD4?logo=dotnet" alt=".NET 10">
  <img src="https://img.shields.io/badge/Windows-11-0078D6?logo=windows" alt="Windows 11">
</p>

---

## I tuoi documenti restano sul tuo PC

Apri P7M nasce da un problema reale: aprire i file `.p7m` ricevuti via PEC senza
dover installare software complicati, legarsi a un provider di firma digitale o —
peggio — caricare documenti sensibili su siti online.

Apri P7M fa una cosa sola, e la fa bene: **apre il contenitore firmato, estrae il
documento originale e lo rende leggibile**. Tutto in locale, offline, senza
registrazione.

> **I tuoi file restano sul tuo computer. Apri P7M non carica nulla online.**

## Download

La versione consigliata sarà quella del **Microsoft Store** appena la scheda sarà
pubblica: installazione più semplice, aggiornamenti gestiti da Windows e pacchetto
firmato dallo Store.

- **Sito ufficiale:** <https://aprip7m.it>
- **Installer standalone (.exe):**
  <https://github.com/luigiplacidi/ApriP7M/releases/latest/download/Apri.P7M.Setup.x64.exe>
- **Tutte le release:** <https://github.com/luigiplacidi/ApriP7M/releases>
- **Microsoft Store:** in attesa di pubblicazione

L'installer standalone è una procedura guidata Windows: permette di scegliere la
cartella di installazione, creare il collegamento sul desktop, associare le
estensioni `.p7m` e `.xml`, e avviare l'app al termine. Gli archivi `.zip`
restano apribili dall'app, ma non vengono associati per non sostituire il gestore
ZIP di Windows.

Quando viene pubblicato un nuovo tag `v*`, la pipeline GitHub Release genera e
carica automaticamente gli artefatti ufficiali nella pagina release del progetto.

## Cosa fa

- 📂 Apre file `.p7m`, `.pdf.p7m`, `.xml.p7m`, `.xml` e `.zip`
- 📄 Estrae il documento originale contenuto nel `.p7m`
- 🧾 Converte le fatture elettroniche XML in un **PDF leggibile di cortesia**
- 🗜️ Apre archivi `.zip` con più XML/P7M e relativi allegati
- 💾 Salva il documento originale estratto e il PDF generato
- 👁️ Mostra l'anteprima del risultato
- ⚙️ Permette di gestire dalle impostazioni le associazioni file di Windows
- 🔌 Funziona **completamente offline**

> ℹ️ **Sulle fatture:** il PDF generato è una **copia leggibile di cortesia**.
> Il documento fiscalmente rilevante resta **il file XML originale**.

## Cosa NON fa (per scelta)

Apri P7M **non** firma documenti, **non** verifica la validità legale della firma,
**non** sostituisce i software ufficiali di verifica, **non** invia PEC, **non**
gestisce la conservazione sostitutiva, **non** usa il cloud, **non** richiede login,
**non** ha telemetria continua, **non** installa servizi in background e **non** parte
con Windows.

Apri P7M apre ed estrae. Per la verifica legale della firma, usa gli strumenti
ufficiali.

## Privacy

La privacy è il cuore del progetto:

- ❌ Nessun upload dei file
- ❌ Nessuna telemetria nella v1
- ❌ Nessun tracciamento dei documenti
- ✅ Funzionamento offline
- ✅ File temporanei gestiti e ripuliti
- ✅ Log locali minimizzati — **mai il contenuto dei documenti**

È prevista una funzione **"Diagnostica anonima"**, disattivata di default,
volontaria, manuale e con anteprima di ciò che verrebbe condiviso. Dettagli in
[PRIVACY.md](PRIVACY.md).

## Stack tecnico

| Componente | Tecnologia |
|---|---|
| Runtime | .NET 10 (LTS) |
| UI | WinUI 3 + Windows App SDK 2.2 (Fluent Design, tema chiaro/scuro) |
| CMS / PKCS#7 / CAdES | BouncyCastle (estrazione contenuto, **non** validazione firma) |
| Fattura XML → PDF | XSLT → HTML + QuestPDF |
| Anteprima | WebView2 (solo viewer locale, nessuna rete) |
| Distribuzione | Microsoft Store (MSIX) + installer standalone |

## Struttura del repository

```
src/
  ApriP7M.App/      WinUI 3 — interfaccia, ViewModel, View
  ApriP7M.Core/     Logica pura, senza UI (estrazione P7M, parsing fattura, render)
  ApriP7M.Store/    Integrazione Microsoft Store (aggiornamenti, recensioni)
tests/
  ApriP7M.Core.Tests/
```

## Build

> ⚠️ La build dell'app richiede **Windows 11** con .NET 10 SDK e il workload
> Windows App SDK. La libreria `ApriP7M.Core` è cross-platform e testabile ovunque.

```powershell
dotnet restore
dotnet build -c Release
dotnet test tests/ApriP7M.Core.Tests
```

## Aggiornamenti

Quando la versione **Microsoft Store** sarà pubblica, gli aggiornamenti passeranno
dal canale normale di Windows. Se usi l'installer standalone, scarica le nuove
versioni solo dal sito ufficiale o dalle release GitHub del progetto.

## Contribuire

Le contribuzioni sono benvenute — vedi [CONTRIBUTING.md](CONTRIBUTING.md).
Per segnalazioni di sicurezza, [SECURITY.md](SECURITY.md).

## Licenza

Il codice è **source available**: puoi leggerlo, studiarlo, compilarlo per test
personali, proporre modifiche e segnalare problemi. Non è una licenza open source
OSI e non autorizza la redistribuzione commerciale o la pubblicazione di versioni
modificate senza permesso scritto. La vendita dell'app, degli installer, dei
pacchetti o di versioni derivate è vietata senza autorizzazione scritta. Vedi
[LICENSE](LICENSE).

---

<p align="center"><em>Creato per mia moglie. Condiviso con chiunque abbia litigato almeno una volta con un file .p7m.</em></p>
