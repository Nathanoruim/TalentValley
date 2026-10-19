using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentValley.Api.Data.Migrations
{
    /// <summary>
    /// LGPD: removes people's names from audit entries about records that no longer exist. Data-only (no schema change).
    /// The texts mirror AuditoriaService.NamelessDescription.
    /// </summary>
    public partial class RemoveNamesFromErasedPersonAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Old deletion entries: keep only the ID.
            migrationBuilder.Sql("""
                UPDATE "Auditorias" SET "Descricao" = 'Aluno ' || "EntidadeId" || ' excluído.'
                WHERE "Acao" = 'ALUNO_EXCLUIDO';
                UPDATE "Auditorias" SET "Descricao" = 'Recrutador ' || "EntidadeId" || ' excluído.'
                WHERE "Acao" = 'RECRUTADOR_EXCLUIDO';
                """);

            // 2. Other entries about a person who was deleted (found through the deletion entry's EntidadeId).
            migrationBuilder.Sql("""
                UPDATE "Auditorias" SET "Descricao" = CASE "Acao"
                    WHEN 'ALUNO_CRIADO' THEN 'Aluno criado.'
                    WHEN 'ALUNO_BLOQUEADO' THEN 'Aluno bloqueado.'
                    WHEN 'ALUNO_REATIVADO' THEN 'Aluno reativado.'
                    WHEN 'RECRUTADOR_CRIADO' THEN 'Recrutador criado.'
                    WHEN 'RECRUTADOR_BLOQUEADO' THEN 'Recrutador bloqueado.'
                    WHEN 'RECRUTADOR_REATIVADO' THEN 'Recrutador reativado.'
                    END
                WHERE "Acao" IN ('ALUNO_CRIADO', 'ALUNO_BLOQUEADO', 'ALUNO_REATIVADO',
                                 'RECRUTADOR_CRIADO', 'RECRUTADOR_BLOQUEADO', 'RECRUTADOR_REATIVADO')
                  AND "EntidadeTipo" IN ('ALUNO', 'RECRUTADOR')
                  AND "EntidadeId" IN (SELECT "EntidadeId" FROM "Auditorias"
                                       WHERE "Acao" IN ('ALUNO_EXCLUIDO', 'ALUNO_EXCLUIDO_PROPRIO',
                                                        'RECRUTADOR_EXCLUIDO', 'RECRUTADOR_EXCLUIDO_PROPRIO'));
                """);

            // 3. Entries about registration requests and RPV formations that no longer exist.
            migrationBuilder.Sql("""
                UPDATE "Auditorias" SET "Descricao" = CASE "Acao"
                    WHEN 'SOLICITACAO_CADASTRO_APROVADA' THEN 'Solicitação de cadastro aprovada.'
                    WHEN 'SOLICITACAO_CADASTRO_REJEITADA' THEN 'Solicitação de cadastro rejeitada.'
                    END
                WHERE "Acao" IN ('SOLICITACAO_CADASTRO_APROVADA', 'SOLICITACAO_CADASTRO_REJEITADA')
                  AND "EntidadeTipo" = 'SOLICITACAO_CADASTRO'
                  AND NOT EXISTS (SELECT 1 FROM "SolicitacoesCadastro" s WHERE s."Id"::text = "Auditorias"."EntidadeId");
                UPDATE "Auditorias" SET "Descricao" = CASE "Acao"
                    WHEN 'FORMACAO_RPV_APROVADA' THEN 'Formação RPV aprovada.'
                    WHEN 'FORMACAO_RPV_REJEITADA' THEN 'Formação RPV rejeitada.'
                    WHEN 'FORMACAO_RPV_VALIDACAO_REMOVIDA' THEN 'Validação de formação RPV removida.'
                    END
                WHERE "Acao" IN ('FORMACAO_RPV_APROVADA', 'FORMACAO_RPV_REJEITADA', 'FORMACAO_RPV_VALIDACAO_REMOVIDA')
                  AND "EntidadeTipo" = 'FORMACAO'
                  AND NOT EXISTS (SELECT 1 FROM "Formacoes" f WHERE f."Id"::text = "Auditorias"."EntidadeId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible on purpose: the erased names must not come back.
        }
    }
}
