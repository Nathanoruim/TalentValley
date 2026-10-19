using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentValley.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetenciaCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Competencias",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Ferramentas");
            // Temporary value for existing rows: the catalog seed sets the real category on startup.

            // Renames keep the same Id, so students, projects and recruiter filters are not affected.
            migrationBuilder.Sql("""
                UPDATE "Competencias" SET "Nome" = 'C (linguagem)', "NomeBusca" = 'c (linguagem)' WHERE "NomeBusca" = 'c';
                UPDATE "Competencias" SET "Nome" = 'Go (Golang)', "NomeBusca" = 'go (golang)' WHERE "NomeBusca" = 'go';
                UPDATE "Competencias" SET "Nome" = 'Microsoft Excel', "NomeBusca" = 'microsoft excel' WHERE "NomeBusca" = 'excel';
                UPDATE "Competencias" SET "Nome" = 'Índices de banco de dados', "NomeBusca" = 'indices de banco de dados' WHERE "NomeBusca" = 'indexes';
                UPDATE "Competencias" SET "Nome" = 'Transações de banco de dados', "NomeBusca" = 'transacoes de banco de dados' WHERE "NomeBusca" = 'transactions';
                """);

            // Merges: whoever had the old item gets the item that stays, then the old item is removed.
            foreach (var (source, target) in new[]
            {
                ("rest", "rest apis"),
                ("version control", "git"),
                ("validation", "input validation"),
                ("logging", "observability"),
                ("system integration", "api integration"),
                ("rest client", "postman")
            })
            {
                migrationBuilder.Sql($"""
                    INSERT INTO "AlunoCompetencias" ("AlunoId", "CompetenciaId")
                    SELECT ac."AlunoId", t."Id" FROM "AlunoCompetencias" ac
                    INNER JOIN "Competencias" s ON s."Id" = ac."CompetenciaId" AND s."NomeBusca" = '{source}'
                    INNER JOIN "Competencias" t ON t."NomeBusca" = '{target}'
                    ON CONFLICT DO NOTHING;
                    INSERT INTO "ProjetoCompetencias" ("ProjetoId", "CompetenciaId")
                    SELECT pc."ProjetoId", t."Id" FROM "ProjetoCompetencias" pc
                    INNER JOIN "Competencias" s ON s."Id" = pc."CompetenciaId" AND s."NomeBusca" = '{source}'
                    INNER JOIN "Competencias" t ON t."NomeBusca" = '{target}'
                    ON CONFLICT DO NOTHING;
                    """);
            }

            // Removed items (the six merged ones above plus seven too generic to filter by).
            migrationBuilder.Sql("""
                DELETE FROM "AlunoCompetencias" WHERE "CompetenciaId" IN (SELECT "Id" FROM "Competencias" WHERE "NomeBusca" IN
                    ('rest', 'version control', 'validation', 'logging', 'system integration', 'rest client',
                     'search', 'sorting', 'filtering', 'pagination', 'http', 'json', 'data integrity'));
                DELETE FROM "ProjetoCompetencias" WHERE "CompetenciaId" IN (SELECT "Id" FROM "Competencias" WHERE "NomeBusca" IN
                    ('rest', 'version control', 'validation', 'logging', 'system integration', 'rest client',
                     'search', 'sorting', 'filtering', 'pagination', 'http', 'json', 'data integrity'));
                DELETE FROM "Competencias" WHERE "NomeBusca" IN
                    ('rest', 'version control', 'validation', 'logging', 'system integration', 'rest client',
                     'search', 'sorting', 'filtering', 'pagination', 'http', 'json', 'data integrity');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Competencias");
            // The renames, merges and removals of Up are not reverted.
        }
    }
}
