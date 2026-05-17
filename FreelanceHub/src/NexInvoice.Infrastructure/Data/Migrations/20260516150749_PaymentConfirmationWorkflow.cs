using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexInvoice.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PaymentConfirmationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedBy",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "Payments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedBy",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "Payments");
        }
    }
}
