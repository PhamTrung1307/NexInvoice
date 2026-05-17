using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Common.Settings;
using NexInvoice.Application.Features.Invoices.Pdfs;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Entities;
using NexInvoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NexInvoice.Infrastructure.Services;

internal sealed class PdfService : IPdfService
{
    private readonly AppDbContext _dbContext;
    private readonly FreelancerSettings _freelancerSettings;

    public PdfService(
        AppDbContext dbContext,
        IOptions<FreelancerSettings> freelancerOptions)
    {
        _dbContext = dbContext;
        _freelancerSettings = freelancerOptions.Value;
    }

    public async Task<PdfFileResponse> GenerateInvoicePdfAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var invoice = await _dbContext.Invoices
            .AsNoTracking()
            .Include(currentInvoice => currentInvoice.Client)
            .Include(currentInvoice => currentInvoice.Project)
            .Include(currentInvoice => currentInvoice.Items)
            .FirstOrDefaultAsync(currentInvoice => currentInvoice.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            throw new NotFoundException("Không tìm thấy hóa đơn.");
        }

        var pdfBytes = Document.Create(container => ComposeInvoice(container, invoice))
            .GeneratePdf();

        return new PdfFileResponse(
            pdfBytes,
            $"invoice-{invoice.InvoiceNumber}.pdf",
            "application/pdf");
    }

    private void ComposeInvoice(IDocumentContainer container, Invoice invoice)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(text => text.FontSize(10));

            page.Header().Element(header => ComposeHeader(header, invoice));
            page.Content().Element(content => ComposeContent(content, invoice));
            page.Footer()
                .AlignCenter()
                .Text(text =>
                {
                    text.Span("Trang ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
        });
    }

    private void ComposeHeader(IContainer container, Invoice invoice)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("HÓA ĐƠN").FontSize(22).Bold();
                column.Item().Text($"Số hóa đơn: {invoice.InvoiceNumber}").FontSize(12);
            });

            row.ConstantItem(180).AlignRight().Column(column =>
            {
                column.Item().Text("Ngày phát hành").SemiBold();
                column.Item().Text(FormatDate(invoice.IssueDate));

                if (invoice.DueDate.HasValue)
                {
                    column.Item().PaddingTop(6).Text("Ngày đến hạn").SemiBold();
                    column.Item().Text(FormatDate(invoice.DueDate.Value));
                }
            });
        });
    }

    private void ComposeContent(IContainer container, Invoice invoice)
    {
        container.PaddingTop(24).Column(column =>
        {
            column.Spacing(16);

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(section => ComposeFreelancerInfo(section));
                row.RelativeItem().Element(section => ComposeClientInfo(section, invoice));
            });

            column.Item().Text($"Dự án: {invoice.Project?.Name ?? "Không có"}").SemiBold();
            column.Item().Element(section => ComposeItemsTable(section, invoice));
            column.Item().AlignRight().Width(260).Element(section => ComposeTotals(section, invoice));
            column.Item().Element(section => ComposePaymentNote(section));
        });
    }

    private void ComposeFreelancerInfo(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Thông tin freelancer").Bold();
            column.Item().Text(_freelancerSettings.Name);
            column.Item().Text(_freelancerSettings.Email);

            if (!string.IsNullOrWhiteSpace(_freelancerSettings.PhoneNumber))
            {
                column.Item().Text(_freelancerSettings.PhoneNumber);
            }

            if (!string.IsNullOrWhiteSpace(_freelancerSettings.Address))
            {
                column.Item().Text(_freelancerSettings.Address);
            }
        });
    }

    private static void ComposeClientInfo(IContainer container, Invoice invoice)
    {
        container.Column(column =>
        {
            column.Item().Text("Thông tin khách hàng").Bold();
            column.Item().Text(invoice.Client?.Name ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(invoice.Client?.CompanyName))
            {
                column.Item().Text(invoice.Client.CompanyName);
            }

            column.Item().Text(invoice.Client?.Email ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(invoice.Client?.PhoneNumber))
            {
                column.Item().Text(invoice.Client.PhoneNumber);
            }

            if (!string.IsNullOrWhiteSpace(invoice.Client?.Address))
            {
                column.Item().Text(invoice.Client.Address);
            }
        });
    }

    private static void ComposeItemsTable(IContainer container, Invoice invoice)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Element(TableHeaderCell).Text("Mô tả");
                header.Cell().Element(TableHeaderCell).AlignRight().Text("Số lượng");
                header.Cell().Element(TableHeaderCell).AlignRight().Text("Đơn giá");
                header.Cell().Element(TableHeaderCell).AlignRight().Text("Thành tiền");
            });

            foreach (var item in invoice.Items.OrderBy(item => item.CreatedAt))
            {
                table.Cell().Element(TableBodyCell).Text(item.Description);
                table.Cell().Element(TableBodyCell).AlignRight().Text(FormatMoney(item.Quantity));
                table.Cell().Element(TableBodyCell).AlignRight().Text(FormatMoney(item.UnitPrice));
                table.Cell().Element(TableBodyCell).AlignRight().Text(FormatMoney(item.Amount));
            }
        });
    }

    private static void ComposeTotals(IContainer container, Invoice invoice)
    {
        container.Column(column =>
        {
            column.Item().Element(row => ComposeTotalRow(row, "Tạm tính", invoice.Subtotal));
            column.Item().Element(row => ComposeTotalRow(row, "Thuế", invoice.TaxAmount));
            column.Item().Element(row => ComposeTotalRow(row, "Giảm giá", invoice.DiscountAmount));
            column.Item().PaddingTop(6).BorderTop(1).BorderColor(Colors.Grey.Lighten2);
            column.Item().Element(row => ComposeTotalRow(row, "Tổng cộng", invoice.TotalAmount, true));
        });
    }

    private void ComposePaymentNote(IContainer container)
    {
        container.PaddingTop(12).Column(column =>
        {
            column.Item().Text("Ghi chú thanh toán").Bold();
            column.Item().Text(_freelancerSettings.PaymentNote);
        });
    }

    private static void ComposeTotalRow(
        IContainer container,
        string label,
        decimal amount,
        bool isTotal = false)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(label).SemiBold();
            var amountText = row.RelativeItem().AlignRight().Text(FormatMoney(amount));

            if (isTotal)
            {
                amountText.FontSize(13).Bold();
            }
        });
    }

    private static IContainer TableHeaderCell(IContainer container)
    {
        return container
            .Background(Colors.Grey.Lighten3)
            .PaddingVertical(6)
            .PaddingHorizontal(4)
            .DefaultTextStyle(text => text.SemiBold());
    }

    private static IContainer TableBodyCell(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3)
            .PaddingVertical(6)
            .PaddingHorizontal(4);
    }

    private static string FormatDate(DateOnly date)
    {
        return date.ToString("dd/MM/yyyy");
    }

    private static string FormatMoney(decimal amount)
    {
        return amount.ToString("N0");
    }
}
