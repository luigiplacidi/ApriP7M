<?xml version="1.0" encoding="UTF-8"?>
<!--
  Foglio di stile minimo per la copia leggibile (HTML) della FatturaPA.
  Usato per l'anteprima in WebView2. Confronta gli elementi per local-name()
  così da gestire fatture con o senza prefisso di namespace.
  Questo HTML è una copia di cortesia: il documento fiscale resta l'XML originale.
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="html" encoding="UTF-8" indent="yes"/>

  <xsl:template match="/">
    <html lang="it">
      <head>
        <meta charset="UTF-8"/>
        <meta name="viewport" content="width=device-width, initial-scale=1"/>
        <title>Fattura elettronica — copia di cortesia</title>
        <style>
          :root { color-scheme: light dark; }
          body { font-family: 'Segoe UI Variable', 'Segoe UI', system-ui, sans-serif;
                 margin: 24px; line-height: 1.45; }
          h1 { font-size: 1.3rem; margin: 0 0 4px; }
          .sub { color: #666; margin: 0 0 16px; }
          .parties { display: flex; gap: 16px; flex-wrap: wrap; }
          .party { flex: 1 1 280px; border: 1px solid #ccc; border-radius: 8px; padding: 12px; }
          .party h2 { font-size: .75rem; text-transform: uppercase; color: #666; margin: 0 0 6px; }
          table { width: 100%; border-collapse: collapse; margin-top: 12px; font-size: .9rem; }
          th, td { text-align: left; padding: 6px 8px; border-bottom: 1px solid #e0e0e0; }
          th { background: #f3f3f3; }
          .total { text-align: right; font-weight: 700; margin-top: 12px; }
          .notice { margin-top: 24px; font-size: .8rem; color: #666; font-style: italic;
                    border-top: 1px solid #ddd; padding-top: 8px; }
        </style>
      </head>
      <body>
        <xsl:for-each select="//*[local-name()='FatturaElettronicaBody']">
          <xsl:variable name="gen" select=".//*[local-name()='DatiGeneraliDocumento']"/>
          <h1>Fattura elettronica — copia di cortesia</h1>
          <p class="sub">
            Documento
            <strong>
              <xsl:value-of select="$gen/*[local-name()='TipoDocumento']"/>
              n. <xsl:value-of select="$gen/*[local-name()='Numero']"/>
            </strong>
            del <xsl:value-of select="$gen/*[local-name()='Data']"/>
          </p>

          <div class="parties">
            <div class="party">
              <h2>Cedente / Prestatore</h2>
              <xsl:apply-templates select="//*[local-name()='CedentePrestatore']" mode="party"/>
            </div>
            <div class="party">
              <h2>Cessionario / Committente</h2>
              <xsl:apply-templates select="//*[local-name()='CessionarioCommittente']" mode="party"/>
            </div>
          </div>

          <table>
            <thead>
              <tr><th>#</th><th>Descrizione</th><th>Q.tà</th><th>Prezzo</th><th>IVA %</th><th>Totale</th></tr>
            </thead>
            <tbody>
              <xsl:for-each select=".//*[local-name()='DettaglioLinee']">
                <tr>
                  <td><xsl:value-of select="*[local-name()='NumeroLinea']"/></td>
                  <td><xsl:value-of select="*[local-name()='Descrizione']"/></td>
                  <td><xsl:value-of select="*[local-name()='Quantita']"/></td>
                  <td><xsl:value-of select="*[local-name()='PrezzoUnitario']"/></td>
                  <td><xsl:value-of select="*[local-name()='AliquotaIVA']"/></td>
                  <td><xsl:value-of select="*[local-name()='PrezzoTotale']"/></td>
                </tr>
              </xsl:for-each>
            </tbody>
          </table>

          <p class="total">
            Totale documento:
            <xsl:value-of select="$gen/*[local-name()='ImportoTotaleDocumento']"/>
            <xsl:text> </xsl:text>
            <xsl:value-of select="$gen/*[local-name()='Divisa']"/>
          </p>
        </xsl:for-each>

        <p class="notice">
          Il PDF/HTML generato è una copia leggibile di cortesia.
          Il documento fiscalmente rilevante resta il file XML originale.
        </p>
      </body>
    </html>
  </xsl:template>

  <xsl:template match="*" mode="party">
    <div>
      <strong>
        <xsl:choose>
          <xsl:when test=".//*[local-name()='Denominazione']">
            <xsl:value-of select=".//*[local-name()='Denominazione']"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select=".//*[local-name()='Nome']"/>
            <xsl:text> </xsl:text>
            <xsl:value-of select=".//*[local-name()='Cognome']"/>
          </xsl:otherwise>
        </xsl:choose>
      </strong>
    </div>
    <div>
      P.IVA
      <xsl:value-of select=".//*[local-name()='IdFiscaleIVA']/*[local-name()='IdPaese']"/>
      <xsl:value-of select=".//*[local-name()='IdFiscaleIVA']/*[local-name()='IdCodice']"/>
    </div>
    <div>
      <xsl:value-of select=".//*[local-name()='Sede']/*[local-name()='Indirizzo']"/>,
      <xsl:value-of select=".//*[local-name()='Sede']/*[local-name()='CAP']"/>
      <xsl:value-of select=".//*[local-name()='Sede']/*[local-name()='Comune']"/>
    </div>
  </xsl:template>

</xsl:stylesheet>
