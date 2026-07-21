import { useState } from 'react'

import { downloadSample } from '@/api/client'
import { Card } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'

// The design's format-hint tabs show illustrative column/element names that don't match
// data-contract.md's locked schema (e.g. "invoice_no"/"direction=sales|purchase" and an
// XML "invoiceMain"/"lineVatRate" structure that doesn't exist in the real contract).
// The skill wins on conflict (CLAUDE.md hard rule) — this shows the real, correct
// structure from data-contract.md sections 1 and 2, not the mockup's placeholder text.
const CSV_EXAMPLE = `InvoiceNumber,IssueDate,PartnerName,PartnerTaxNumber,Direction,NetAmount,VatCode,VatAmount,GrossAmount,Currency
INV-2026-001,2026-06-03,Kovács Kft.,12345678-2-41,OUT,100000,27,27000,127000,HUF`

const XML_EXAMPLE = `<InvoiceData xmlns="http://schemas.nav.gov.hu/OSA/3.0/data">
  <invoiceNumber>INV-2026-001</invoiceNumber>
  <invoiceIssueDate>2026-06-03</invoiceIssueDate>
  <invoiceDirection>OUTBOUND</invoiceDirection>
  <invoiceLines>
    <line>
      <lineNetAmount>100000</lineNetAmount>
      <lineVatData>
        <vatPercentage>0.27</vatPercentage>
        <lineVatAmount>27000</lineVatAmount>
      </lineVatData>
      <lineGrossAmount>127000</lineGrossAmount>
    </line>
  </invoiceLines>
</InvoiceData>`

export function FormatHintTabs() {
  const [downloading, setDownloading] = useState<string | null>(null)

  async function handleDownload(name: 'clean.csv' | 'nav.xml') {
    setDownloading(name)
    try {
      await downloadSample(name)
    } finally {
      setDownloading(null)
    }
  }

  return (
    <Card className="mt-[34px] overflow-hidden">
      <Tabs defaultValue="csv">
        <TabsList>
          <TabsTrigger value="csv">Expected CSV structure</TabsTrigger>
          <TabsTrigger value="xml">NAV 3.0 XML structure</TabsTrigger>
        </TabsList>

        <div className="p-5">
          <TabsContent value="csv">
            <pre className="tabular-nums-mono overflow-x-auto rounded-[10px] border border-divider bg-muted p-[14px] text-[12.5px] leading-[1.7] text-foreground/90">
              {CSV_EXAMPLE}
            </pre>
            <p className="mt-3 text-[12.5px] leading-relaxed text-muted-foreground">
              <b className="text-foreground">Direction</b> = OUT | IN ·{' '}
              <b className="text-foreground">VatCode</b> = 27, 18, 5, 0, AAM, TAM, EUFAD, FAD · amounts in HUF,
              invariant decimal (e.g. 27000.50) · dates as yyyy-MM-dd.
            </p>
          </TabsContent>

          <TabsContent value="xml">
            <pre className="tabular-nums-mono overflow-x-auto rounded-[10px] border border-divider bg-muted p-[14px] text-[12.5px] leading-[1.7] text-foreground/90">
              {XML_EXAMPLE}
            </pre>
            <p className="mt-3 text-[12.5px] leading-relaxed text-muted-foreground">
              <b className="text-foreground">invoiceDirection</b>: OUTBOUND → OUT, INBOUND → IN (optional, defaults
              to OUTBOUND) · exempt/reverse-charge lines use{' '}
              <span className="text-foreground">{'<vatExemption case="AAM|TAM|EUFAD|FAD"/>'}</span> instead of
              vatPercentage.
            </p>
          </TabsContent>

          <div className="mt-4 flex gap-[18px] border-t border-divider pt-4">
            <button
              type="button"
              onClick={() => handleDownload('clean.csv')}
              disabled={downloading !== null}
              className="text-sm text-primary hover:underline disabled:opacity-50"
            >
              {downloading === 'clean.csv' ? 'Downloading…' : '↓ Download sample .csv'}
            </button>
            <button
              type="button"
              onClick={() => handleDownload('nav.xml')}
              disabled={downloading !== null}
              className="text-sm text-primary hover:underline disabled:opacity-50"
            >
              {downloading === 'nav.xml' ? 'Downloading…' : '↓ Download sample NAV 3.0 .xml'}
            </button>
          </div>
        </div>
      </Tabs>
    </Card>
  )
}
