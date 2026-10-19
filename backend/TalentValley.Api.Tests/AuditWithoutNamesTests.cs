using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using TalentValley.Api.Authorization;
using TalentValley.Api.Data;
using TalentValley.Api.Domain.Entities;
using TalentValley.Api.Domain.Enums;
using TalentValley.Api.Services;

namespace TalentValley.Api.Tests;

// LGPD: audit entries must not keep the name of a person who was erased.
public sealed class AuditWithoutNamesTests : IDisposable
{
    private const string PreviousMigration = "20261019213636_AddRecrutadorConsentAndNewsletter";
    private const string RequestType = AuditoriaService.RegistrationRequestEntityType;
    private const string FormationType = AuditoriaService.FormationEntityType;
    private readonly ApiFactory factory = new();

    [Fact]
    public async Task Admin_deleting_a_student_removes_the_name_from_every_audit_entry_about_that_student()
    {
        var adminId = await factory.CreateUserAsync("admin@example.test", AppRoles.Admin);
        var studentId = await factory.CreateUserAsync("student@example.test");
        var otherId = await factory.CreateUserAsync("other@example.test");
        var requestId = Guid.NewGuid();
        var formationId = Guid.NewGuid();
        var otherRequestId = Guid.NewGuid();
        await factory.InScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            db.Formacoes.Add(new Formacao { Id = formationId, AlunoId = studentId, Nome = "Curso", NomeBusca = "curso",
                Instituicao = "RPV", EhRioPombaValley = true, StatusValidacaoRpv = StatusValidacaoRpv.VERIFICADO });
            db.SolicitacoesCadastro.AddRange(
                Request(requestId, "Maria Álvares", "student@example.test"),
                Request(otherRequestId, "Joana Souza", "other@example.test"));
            db.Auditorias.AddRange(
                Entry(adminId, AcaoAuditoria.ALUNO_CRIADO, AppRoles.Student, studentId, "Aluno Maria Álvares criado."),
                Entry(adminId, AcaoAuditoria.SOLICITACAO_CADASTRO_APROVADA, RequestType,
                    requestId, "Solicitação de cadastro de Maria Álvares aprovada."),
                Entry(adminId, AcaoAuditoria.FORMACAO_RPV_APROVADA, FormationType, formationId,
                    "Formação RPV Curso do aluno Maria Álvares aprovada."),
                Entry(adminId, AcaoAuditoria.ALUNO_CRIADO, AppRoles.Student, otherId, "Aluno Joana Souza criado."),
                Entry(adminId, AcaoAuditoria.SOLICITACAO_CADASTRO_APROVADA, RequestType,
                    otherRequestId, "Solicitação de cadastro de Joana Souza aprovada."));
            await db.SaveChangesAsync();
        });
        using var admin = factory.Client();
        await ApiFactory.LoginAsync(admin, "admin@example.test");
        await ApiFactory.SetCsrfAsync(admin);

        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.PostAsync($"/api/admin/alunos/{studentId}/bloquear", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/alunos/{studentId}")).StatusCode);

        await factory.InScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var all = await db.Auditorias.AsNoTracking().ToListAsync();
            var ofStudent = all.Where(x => x.EntidadeId == studentId.ToString() ||
                                           x.EntidadeId == requestId.ToString() ||
                                           x.EntidadeId == formationId.ToString()).ToList();
            Assert.Equal(5, ofStudent.Count); // criado, solicitação, formação, bloqueado, excluído
            Assert.All(ofStudent, x => Assert.DoesNotContain("Maria", x.Descricao));
            Assert.Equal($"Aluno {studentId} excluído.",
                ofStudent.Single(x => x.Acao == AcaoAuditoria.ALUNO_EXCLUIDO).Descricao);
            Assert.Equal("Formação RPV aprovada.",
                ofStudent.Single(x => x.Acao == AcaoAuditoria.FORMACAO_RPV_APROVADA).Descricao);
            // Another person's entries are untouched.
            Assert.Equal("Aluno Joana Souza criado.", all.Single(x => x.EntidadeId == otherId.ToString()).Descricao);
            Assert.Equal("Solicitação de cadastro de Joana Souza aprovada.",
                all.Single(x => x.EntidadeId == otherRequestId.ToString()).Descricao);
        });
    }

    [Fact]
    public async Task Admin_deleting_a_recruiter_removes_the_name_from_every_audit_entry_about_that_recruiter()
    {
        var adminId = await factory.CreateUserAsync("admin@example.test", AppRoles.Admin);
        var recruiterId = await factory.CreateUserAsync("recruiter@example.test", AppRoles.Recruiter);
        var requestId = Guid.NewGuid();
        await factory.InScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            db.SolicitacoesCadastro.Add(Request(requestId, "Carlos Lima", "recruiter@example.test"));
            db.Auditorias.AddRange(
                Entry(adminId, AcaoAuditoria.RECRUTADOR_CRIADO, AppRoles.Recruiter, recruiterId,
                    "Recrutador Carlos Lima criado por aprovação de solicitação."),
                Entry(adminId, AcaoAuditoria.SOLICITACAO_CADASTRO_APROVADA, RequestType,
                    requestId, "Solicitação de cadastro de Carlos Lima aprovada."));
            await db.SaveChangesAsync();
        });
        using var admin = factory.Client();
        await ApiFactory.LoginAsync(admin, "admin@example.test");
        await ApiFactory.SetCsrfAsync(admin);

        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.PostAsync($"/api/admin/recrutadores/{recruiterId}/bloquear", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.DeleteAsync($"/api/admin/recrutadores/{recruiterId}")).StatusCode);

        await factory.InScopeAsync(async provider =>
        {
            var all = await provider.GetRequiredService<AppDbContext>().Auditorias.AsNoTracking().ToListAsync();
            Assert.Equal(4, all.Count); // criado, solicitação, bloqueado, excluído
            Assert.All(all, x => Assert.DoesNotContain("Carlos", x.Descricao));
            Assert.Equal("Recrutador criado.", all.Single(x => x.Acao == AcaoAuditoria.RECRUTADOR_CRIADO).Descricao);
        });
    }

    [Fact]
    public async Task Student_deleting_a_formation_removes_the_name_from_that_formations_audit_entries_only()
    {
        var adminId = await factory.CreateUserAsync("admin@example.test", AppRoles.Admin);
        var studentId = await factory.CreateUserAsync("student@example.test");
        var deletedId = Guid.NewGuid();
        var keptId = Guid.NewGuid();
        await factory.InScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            db.Formacoes.AddRange(
                new Formacao { Id = deletedId, AlunoId = studentId, Nome = "Curso A", NomeBusca = "curso a", Instituicao = "RPV" },
                new Formacao { Id = keptId, AlunoId = studentId, Nome = "Curso B", NomeBusca = "curso b", Instituicao = "RPV" });
            db.Auditorias.AddRange(
                Entry(adminId, AcaoAuditoria.FORMACAO_RPV_APROVADA, FormationType, deletedId,
                    "Formação RPV Curso A do aluno Maria Álvares aprovada."),
                Entry(adminId, AcaoAuditoria.FORMACAO_RPV_APROVADA, FormationType, keptId,
                    "Formação RPV Curso B do aluno Maria Álvares aprovada."));
            await db.SaveChangesAsync();
        });
        using var student = factory.Client();
        await ApiFactory.LoginAsync(student, "student@example.test");
        await ApiFactory.SetCsrfAsync(student);

        Assert.Equal(HttpStatusCode.NoContent, (await student.DeleteAsync($"/api/alunos/me/formacoes/{deletedId}")).StatusCode);

        await factory.InScopeAsync(async provider =>
        {
            var all = await provider.GetRequiredService<AppDbContext>().Auditorias.AsNoTracking().ToListAsync();
            Assert.Equal("Formação RPV aprovada.", all.Single(x => x.EntidadeId == deletedId.ToString()).Descricao);
            // The student is still alive and the other formation exists: its entry keeps the name.
            Assert.Equal("Formação RPV Curso B do aluno Maria Álvares aprovada.",
                all.Single(x => x.EntidadeId == keptId.ToString()).Descricao);
        });
    }

    [Fact]
    public async Task Retention_cleanup_of_rejected_requests_removes_the_name_from_their_audit_entries()
    {
        var adminId = await factory.CreateUserAsync("admin@example.test", AppRoles.Admin);
        var oldId = Guid.NewGuid();
        var recentId = Guid.NewGuid();
        await factory.InScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            db.SolicitacoesCadastro.AddRange(Rejected(oldId, "old@example.test", 31), Rejected(recentId, "recent@example.test", 29));
            db.Auditorias.AddRange(
                Entry(adminId, AcaoAuditoria.SOLICITACAO_CADASTRO_REJEITADA, RequestType, oldId,
                    "Solicitação de cadastro de Carlos Souza rejeitada."),
                Entry(adminId, AcaoAuditoria.SOLICITACAO_CADASTRO_REJEITADA, RequestType, recentId,
                    "Solicitação de cadastro de Carlos Souza rejeitada."));
            await db.SaveChangesAsync();
        });
        using var admin = factory.Client();
        await ApiFactory.LoginAsync(admin, "admin@example.test");

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/solicitacoes-cadastro")).StatusCode);

        await factory.InScopeAsync(async provider =>
        {
            var all = await provider.GetRequiredService<AppDbContext>().Auditorias.AsNoTracking().ToListAsync();
            Assert.Equal("Solicitação de cadastro rejeitada.", all.Single(x => x.EntidadeId == oldId.ToString()).Descricao);
            Assert.Equal("Solicitação de cadastro de Carlos Souza rejeitada.",
                all.Single(x => x.EntidadeId == recentId.ToString()).Descricao);
        });
    }

    [Fact]
    public async Task Migration_removes_names_from_old_entries_of_erased_records_and_keeps_the_rest()
    {
        var connection = PostgresTestDatabase.CreateDatabase();
        try
        {
            var erasedId = Guid.NewGuid();
            var livingId = Guid.NewGuid();
            var selfErasedRecruiterId = Guid.NewGuid();
            var orphanRequestId = Guid.NewGuid();
            var livingRequestId = Guid.NewGuid();
            var orphanFormationId = Guid.NewGuid();
            await using (var db = new AppDbContext(PostgresTestDatabase.Options(connection)))
            {
                // Go back to the state before this migration, then write rows the way old versions did.
                await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
                db.SolicitacoesCadastro.Add(Request(livingRequestId, "Rita Alves", "rita@example.test"));
                db.Auditorias.AddRange(
                    Entry(null, AcaoAuditoria.ALUNO_CRIADO, AppRoles.Student, erasedId, "Aluno Paulo Reis criado."),
                    Entry(null, AcaoAuditoria.ALUNO_BLOQUEADO, AppRoles.Student, erasedId, "Aluno Paulo Reis bloqueado."),
                    Entry(null, AcaoAuditoria.ALUNO_EXCLUIDO, AppRoles.Student, erasedId, "Aluno Paulo Reis excluído."),
                    Entry(null, AcaoAuditoria.RECRUTADOR_EXCLUIDO, AppRoles.Recruiter, Guid.NewGuid(),
                        "Recrutador Paulo Reis excluído."),
                    Entry(null, AcaoAuditoria.RECRUTADOR_CRIADO, AppRoles.Recruiter, selfErasedRecruiterId, "Recrutador Paulo Reis criado."),
                    Entry(null, AcaoAuditoria.RECRUTADOR_BLOQUEADO, AppRoles.Recruiter, selfErasedRecruiterId, "Recrutador Paulo Reis bloqueado."),
                    Entry(null, AcaoAuditoria.RECRUTADOR_EXCLUIDO_PROPRIO, AppRoles.Recruiter, selfErasedRecruiterId, "Recrutador excluiu a própria conta."),
                    Entry(null, AcaoAuditoria.FORMACAO_RPV_VALIDACAO_REMOVIDA, FormationType, orphanFormationId, "Validação da formação RPV Curso do aluno Paulo Reis removida."),
                    Entry(null, AcaoAuditoria.ALUNO_CRIADO, AppRoles.Student, livingId, "Aluno Rita Alves criado."),
                    Entry(null, AcaoAuditoria.SOLICITACAO_CADASTRO_REJEITADA, RequestType,
                        orphanRequestId, "Solicitação de cadastro de Paulo Reis rejeitada."),
                    Entry(null, AcaoAuditoria.SOLICITACAO_CADASTRO_APROVADA, RequestType,
                        livingRequestId, "Solicitação de cadastro de Rita Alves aprovada."),
                    Entry(null, AcaoAuditoria.FORMACAO_RPV_REJEITADA, FormationType, orphanFormationId,
                        "Formação RPV Curso do aluno Paulo Reis rejeitada."));
                await db.SaveChangesAsync();
                await db.Database.MigrateAsync();
            }

            await using var verify = new AppDbContext(PostgresTestDatabase.Options(connection));
            var all = await verify.Auditorias.AsNoTracking().ToListAsync();
            Assert.DoesNotContain(all, x => x.Descricao.Contains("Paulo"));
            Assert.Equal("Aluno criado.",
                all.Single(x => x.Acao == AcaoAuditoria.ALUNO_CRIADO && x.EntidadeId == erasedId.ToString()).Descricao);
            Assert.Equal($"Aluno {erasedId} excluído.", all.Single(x => x.Acao == AcaoAuditoria.ALUNO_EXCLUIDO).Descricao);
            Assert.Equal("Solicitação de cadastro rejeitada.",
                all.Single(x => x.Acao == AcaoAuditoria.SOLICITACAO_CADASTRO_REJEITADA).Descricao);
            Assert.Equal("Recrutador criado.", all.Single(x => x.Acao == AcaoAuditoria.RECRUTADOR_CRIADO).Descricao);
            Assert.Equal("Recrutador bloqueado.", all.Single(x => x.Acao == AcaoAuditoria.RECRUTADOR_BLOQUEADO).Descricao);
            Assert.Equal("Validação de formação RPV removida.",
                all.Single(x => x.Acao == AcaoAuditoria.FORMACAO_RPV_VALIDACAO_REMOVIDA).Descricao);
            Assert.Equal("Formação RPV rejeitada.",
                all.Single(x => x.Acao == AcaoAuditoria.FORMACAO_RPV_REJEITADA).Descricao);
            // Records that still exist keep their entries.
            Assert.Equal("Aluno Rita Alves criado.", all.Single(x => x.EntidadeId == livingId.ToString()).Descricao);
            Assert.Equal("Solicitação de cadastro de Rita Alves aprovada.",
                all.Single(x => x.EntidadeId == livingRequestId.ToString()).Descricao);
        }
        finally
        {
            PostgresTestDatabase.DropDatabase(connection);
        }
    }

    private static SolicitacaoCadastro Rejected(Guid id, string email, int analyzedDaysAgo)
    {
        var request = Request(id, "Carlos Souza", email);
        request.Status = StatusSolicitacaoCadastro.REJEITADA;
        request.AnalisadoEm = DateTimeOffset.UtcNow.AddDays(-analyzedDaysAgo);
        return request;
    }

    private static SolicitacaoCadastro Request(Guid id, string name, string email) => new()
    {
        Id = id, Tipo = TipoSolicitacaoCadastro.ALUNO, NomeCompleto = name, Email = email,
        EmailNormalizado = email.ToUpperInvariant()
    };

    private static Auditoria Entry(Guid? adminId, AcaoAuditoria action, string type, Guid entityId, string description) => new()
    {
        Id = Guid.NewGuid(), AdminUserId = adminId, AdminEmailSnapshot = "admin@example.test", Acao = action,
        EntidadeTipo = type, EntidadeId = entityId.ToString(), Descricao = description,
        CriadoEm = DateTimeOffset.UtcNow
    };

    public void Dispose() => factory.Dispose();
}
