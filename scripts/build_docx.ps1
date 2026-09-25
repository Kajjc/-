# Word-версия сопроводительной документации: HTML (scripts/docx_html.py) -> DOCX.
# Нужен установленный Microsoft Word (используется через COM). Пути — абсолютные:
#   powershell -ExecutionPolicy Bypass -File scriptsuild_docx.ps1 -Html C:\pathuild\documentation.html -Out C:\path\docsrena-peregovorov-documentation.docx
# Шаг 1 сохраняет HTML в DOCX со встроенными картинками. Шаг 2 открывает DOCX как обычный
# документ и уже в нём подгоняет таблицы под ширину A4 (в режиме HTML Word подгоняет их под
# «окно» веб-разметки, и широкие таблицы вылезают за поля), вставляет оглавление и номера страниц.
param([string]$Html, [string]$Out, [string]$Pdf = "")
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    # --- step 1: HTML -> DOCX, embed linked pictures
    $doc = $word.Documents.Open($Html, $false, $true)
    $linked = 0
    foreach ($s in @($doc.InlineShapes)) {
        try {
            if ($s.LinkFormat -ne $null) {
                $s.LinkFormat.SavePictureWithDocument = $true
                $s.LinkFormat.BreakLink()
                $linked++
            }
        } catch {}
    }
    $doc.SaveAs2($Out, 16)
    $doc.Close($false)

    # --- step 2: reopen as a normal document, lay out for A4
    $doc = $word.Documents.Open($Out, $false, $false)
    $doc.ActiveWindow.View.Type = 3
    $ps = $doc.PageSetup
    $ps.PaperSize = 7
    $ps.Orientation = 0
    $ps.TopMargin = 56.7
    $ps.BottomMargin = 56.7
    $ps.LeftMargin = 62.4
    $ps.RightMargin = 51.0

    $tables = 0
    foreach ($t in @($doc.Tables)) {
        $tables++
        $t.Rows.AllowBreakAcrossPages = $true
        if ($t.Columns.Count -ge 6) { $t.Range.Font.Size = 7.5 }
        $t.AllowAutoFit = $true
        $t.PreferredWidthType = 2
        $t.PreferredWidth = 100
        $t.AutoFitBehavior(1)
        $t.AutoFitBehavior(2)
    }

    # TOC in place of the [[TOC]] marker: sections and appendices (heading level 2)
    $rng = $doc.Content
    $found = $rng.Find.Execute("[[TOC]]")
    if ($found) {
        $rng.Text = ""
        [void]$doc.TablesOfContents.Add($rng, $true, 2, 2)
    }
    $footer = $doc.Sections.Item(1).Footers.Item(1)
    [void]$footer.PageNumbers.Add(1, $true)
    $doc.Repaginate()
    if ($found) { $doc.TablesOfContents.Item(1).Update() }

    $doc.Save()
    if ($Pdf -ne "") { $doc.SaveAs2($Pdf, 17) }
    $pages = $doc.ComputeStatistics(2)
    $doc.Close($false)
    "saved; embedded pictures: $linked; tables: $tables; pages: $pages; toc: $found"
} finally {
    $word.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
}
