using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentValley.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecrutadorConsentAndNewsletter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NewsletterAceita",
                table: "SolicitacoesCadastro",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NewsletterAtualizadaEm",
                table: "SolicitacoesCadastro",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConsentimentoEm",
                table: "Recrutadores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NewsletterAceita",
                table: "Recrutadores",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NewsletterAtualizadaEm",
                table: "Recrutadores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersaoTermos",
                table: "Recrutadores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NewsletterAceita",
                table: "Alunos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NewsletterAtualizadaEm",
                table: "Alunos",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewsletterAceita",
                table: "SolicitacoesCadastro");

            migrationBuilder.DropColumn(
                name: "NewsletterAtualizadaEm",
                table: "SolicitacoesCadastro");

            migrationBuilder.DropColumn(
                name: "ConsentimentoEm",
                table: "Recrutadores");

            migrationBuilder.DropColumn(
                name: "NewsletterAceita",
                table: "Recrutadores");

            migrationBuilder.DropColumn(
                name: "NewsletterAtualizadaEm",
                table: "Recrutadores");

            migrationBuilder.DropColumn(
                name: "VersaoTermos",
                table: "Recrutadores");

            migrationBuilder.DropColumn(
                name: "NewsletterAceita",
                table: "Alunos");

            migrationBuilder.DropColumn(
                name: "NewsletterAtualizadaEm",
                table: "Alunos");
        }
    }
}
