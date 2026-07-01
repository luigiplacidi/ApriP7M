# Note su licenze di terze parti

Apri P7M (codice proprio) è distribuito sotto licenza [Source Available](LICENSE).
Utilizza le seguenti librerie di terze parti:

| Libreria | Licenza | Uso |
|---|---|---|
| BouncyCastle.Cryptography | MIT | Parsing CMS/PKCS#7/CAdES (estrazione contenuto P7M) |
| QuestPDF | Community License | Generazione del PDF di cortesia dalle fatture |
| CommunityToolkit.Mvvm | MIT | Pattern MVVM nella UI |
| Microsoft.WindowsAppSDK / WinUI 3 | MIT | Interfaccia nativa Windows |
| Microsoft.Web.WebView2 | BSD-3-Clause | Anteprima locale PDF/HTML |

## QuestPDF — nota sulla licenza

QuestPDF usa la **Community License**. È **gratuita**, anche per uso commerciale,
per: privati, no-profit, istituti accademici e aziende con **fatturato annuo
inferiore a 1.000.000 USD**. L'idoneità è su auto-certificazione in buona fede.

Apri P7M — app gratuita, source available, di un privato — **rientra pienamente** nella
Community License. Nessun costo e nessun rischio in questa configurazione.

Se in futuro il progetto superasse la soglia (1M USD/anno), ci sono **90 giorni**
per acquistare una licenza Professional/Enterprise. In alternativa, è disponibile
a costo zero un fallback senza dipendenze esterne: generare il PDF stampando in
locale l'HTML prodotto dall'XSLT, via
`CoreWebView2.PrintToPdfAsync()` (WebView2, già incluso).

Termini aggiornati: <https://www.questpdf.com/license/community.html>.
