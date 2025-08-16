using System;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using CompanyApp.Web.Models;
using QuestPDF.Helpers;

namespace CompanyApp.Web.Pdf
{
    public class InvoiceDocument : IDocument
    {
        private readonly Order _order;
        private readonly decimal _total;
        private readonly decimal _paid;
        private readonly decimal _due;

        public InvoiceDocument(Order order)
        {
            _order = order;
            _total = order.Items.Sum(i => i.UnitPrice * i.Quantity);
            _paid  = order.Payments?.Sum(p => p.Amount) ?? 0m;
            _due   = Math.Max(0, _total - _paid);
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(ts => ts.FontSize(11));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignRight().Text(txt =>
                {
                    txt.Span("Стр. ").FontSize(9);
                    txt.CurrentPageNumber().FontSize(9);
                    txt.Span(" из ").FontSize(9);
                    txt.TotalPages().FontSize(9);
                });
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text($"Накладная № {_order.InvoiceNumber ?? "-"}")
                    .FontSize(18).SemiBold();

                var dt = _order.InvoiceDate?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "-";
                col.Item().Text($"Дата: {dt}");

                col.Item().Text($"Покупатель: {_order.Customer?.FullName ?? "—"}");
                if (!string.IsNullOrWhiteSpace(_order.Customer?.Phone))
                    col.Item().Text($"Тел: {_order.Customer!.Phone}");

                col.Item().PaddingTop(8).LineHorizontal(0.5f);
            });
        }

        void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(28);  // #
                        c.RelativeColumn(6);   // товар
                        c.RelativeColumn(2);   // цена
                        c.RelativeColumn(2);   // кол-во
                        c.RelativeColumn(2);   // сумма
                    });

                    // Шапка
                    table.Header(h =>
                    {
                        h.Cell().Text("#").SemiBold();
                        h.Cell().Text("Товар").SemiBold();
                        h.Cell().AlignRight().Text("Цена").SemiBold();
                        h.Cell().AlignRight().Text("Кол-во").SemiBold();
                        h.Cell().AlignRight().Text("Сумма").SemiBold();
                    });

                    // Строки
                    var i = 1;
                    foreach (var it in _order.Items)
                    {
                        table.Cell().Text(i.ToString());
                        table.Cell().Text(it.Product?.Name ?? $"Товар {it.ProductId}");
                        table.Cell().AlignRight().Text(it.UnitPrice.ToString("C"));
                        table.Cell().AlignRight().Text(it.Quantity.ToString());
                        table.Cell().AlignRight().Text((it.UnitPrice * it.Quantity).ToString("C"));
                        i++;
                    }
                });

                // Итого
                col.Item().PaddingTop(8).AlignRight().Column(sum =>
                {
                    sum.Item().Text($"Итого: {_total.ToString("C")}").SemiBold();
                    sum.Item().Text($"Оплачено: {_paid.ToString("C")}");
                    sum.Item().Text($"К оплате: {_due.ToString("C")}").SemiBold();
                });
            });
        }
    }
}
