using RepyPharma.Models;
using RepyPharma.Services.Interfaces;


public class ReportHtmlService
{
    public string GenerateReplacementHtml(ReplenishmentReport report)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append(@"
        <!DOCTYPE html>
        <html lang='pt-BR'>
        <head>
            <meta charset='UTF-8'>
            <title>Reposição da Farmácia Central</title>
            <style>
                * { box-sizing: border-box; margin: 0; padding: 0; }

                body {
                    font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif;
                    font-size: 12px;
                    color: #111;
                    padding: 2cm;
                    background: white;
                }

                h1 {
                    font-size: 18px;
                    font-weight: 600;
                    margin-bottom: 1.5rem;
                    color: #111;
                }

                .section {
                    margin-bottom: 1.5rem;
                    page-break-inside: avoid;
                    margin-top: 2rem;
                }

                .section-title {
                    font-size: 15px;
                    font-weight: 600;
                    margin-bottom: 0.5rem;
                    margin-top: 2rem;
                    padding-bottom: 4px;
                    border-bottom: 1.5px solid #333;
                    color: #111;
                }

                .section-empty {
                    font-size: 12px;
                    color: #999;
                    padding: 4px 0;
                }

                table {
                    width: 100%;
                    border-collapse: collapse;
                    font-size: 11px;
                }

                thead th {
                    text-align: left;
                    padding: 5px 8px;
                    font-weight: 600;
                    font-size: 11px;
                    color: #333;
                    border-bottom: 1px solid #ccc;
                    background: #f5f5f5;
                }

                tbody td {
                    padding: 5px 8px;
                    border-bottom: 0.5px solid #e0e0e0;
                    vertical-align: top;
                }

                tbody tr:last-child td { border-bottom: none; }

                .lot-row {
                    display: grid;
                    grid-template-columns: 140px 70px 80px 80px;
                    gap: 0 8px;
                    align-items: center;
                    margin-bottom: 2px;
                }

                .lot-recommended { color: #1a56db; }
                .chip-indicated {
                    font-size: 10px;
                    background: #e8f0fe;
                    color: #1a56db;
                    padding: 1px 20px;
                    border-radius: 99px;
                    white-space: nowrap;
                    width: fit-content;
                }

                .conflict-warning {
                    font-size: 10px;
                    color: #b45309;
                    margin-top: 3px;
                }

                .row-conflict td {
                    background-color: #fef9c3 !important;
                }

                .no-stock { color: #dc2626; }

                @media print {
                    @page { size: portrait; margin: 1cm; margin-top: 2cm; }
                    body { padding: 1cm; }
                }
            </style>
        </head>
        <body>
        <div>
            <h1>Reposição da Farmácia Central</h1>
            <div style='margin-bottom: 1rem; color:#555'>
                <div><strong>Data do relatório:</strong> {GetReportGenerationTime()}</div>
                <div><strong>Total de itens:</strong> {GetTotalItemsCount(report)} </div>
            </div>
            <div style='margin-bottom: 1rem; padding: 10px; background: #fef3c7; color: #b45309; border-left: 4px solid #fbbf24'>
                ⚠️ Itens destacados em amarelo indicam que o lote recomendado para reposição é diferente do lote atualmente na farmácia.
                Sempre verifique a disponibilidade real dos itens e a compatibilidade dos lotes antes de realizar a reposição.
            </div>
            <div style='margin-bottom: 1rem; padding: 10px; background: #d1fae5; color: #065f46; border-left: 4px solid #34d399'>
                ℹ️ O relatório é dividido em etapas de reposição: Fracionamento, CAF e Almoxarifado. Itens que precisam ser repostos, mas não estão no fracionamento, aparecerão nas etapas seguintes. 
                Verifique a compatibilidade desses itens antes de realizar a reposição.
            </div>
            <div style='margin-bottom: 1.5rem; padding: 10px; background: #e0f2fe; color: #0369a1; border-left: 4px solid #60a5fa'>
                ✅ Itens com o lote recomendado destacado em azul indicam que o lote sugerido para reposição é o mesmo que está atualmente na farmácia, facilitando a reposição.
            </div>
            

        
        ");

        AppendSection(sb, "1ª Etapa — Fracionamento", report.FromFractionation, "1059");
        AppendSection(sb, "2ª Etapa — CAF", report.FromCafOnly, "999");
        AppendSection(sb, "3ª Etapa — Almoxarifado", report.FromStockOnly, "996");

        sb.Append("</div></body></html>");

        return sb.ToString();

    }
    private DateTime GetReportGenerationTime()
    {
        return DateTime.Now;
    }

    private int GetTotalItemsCount(ReplenishmentReport report)
    {
        return report.FromFractionation.Count
            + report.FromCafOnly.Count
            + report.FromStockOnly.Count
            + report.NoSourceAvailable.Count;
    }
    private void AppendSection(
        System.Text.StringBuilder sb,
        string title,
        List<ReplenishmentItem> items,
        string locationId)
    {
        sb.Append($@"
            <div class='section'>
                <div class='section-title'>{title} — {items.Count} itens</div>
        ");

        if (!items.Any())
        {
            sb.Append("<p class='section-empty'>Nenhum item nesta etapa.</p>");
        }
        else
        {
            sb.Append(@"
                <table>
                    <thead>
                        <tr>
                            <th style='width:10wv'>Código</th>
                            <th style='width:30wv'>Nome</th>
                            <th style='width:60wv'>Lotes Disponíveis</th>
                        </tr>
                    </thead>
                    <tbody>
            ");

            foreach (var item in items)
            {
                var rowClass = item.HasLotConflict ? "row-conflict" : "";

                sb.Append($"<tr class='{rowClass}'>");
                sb.Append($"<td>{item.Code}</td>");
                sb.Append($"<td>{item.Name}</td>");
                sb.Append("<td>");

                if (!item.AvailableBatches.Any())
                {
                    sb.Append("<span class='no-stock'>Sem estoque disponível</span>");
                }
                else
                {
                    foreach (var batch in item.AvailableBatches)
                    {
                        var isRecommended = batch.Batch == item.RecommendedBatch?.Batch;
                        var lotClass = isRecommended ? "lot-row lot-recommended" : "lot-row";
                        var validity = batch.Validity.HasValue
                            ? batch.Validity.Value.ToString("MM/yyyy")
                            : "—";

                        foreach (var location in batch.Locations
                            .Where(l => l.LocationId == locationId && l.Quantity > 0))
                        {
                            var chip = isRecommended
                                ? "<span class='chip-indicated'>indicado</span>"
                                : "<span></span>";

                            sb.Append($@"
                                <div class='{lotClass}'>
                                    <span>{batch.Batch}</span>
                                    <span style='color:#666'>{validity}</span>
                                    <span>{location.Quantity} un.</span>
                                    {chip}
                                </div>
                            ");
                        }
                    }

                    if (item.HasLotConflict)
                    {
                        sb.Append("<div class='conflict-warning'>⚠️ Lote diferente do que está na farmácia</div>");
                    }
                }

                sb.Append("</td></tr>");
            }

            sb.Append("</tbody></table>");
        }

        sb.Append("</div>");
    }
}