using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TalentValley.Api.Authorization;
using TalentValley.Api.Data;

namespace TalentValley.Api.Tests;

public sealed class AlunoProfileObjetivosTests : IDisposable
{
    private readonly ApiFactory factory = new();

    public void Dispose() => factory.Dispose();

    private async Task<(HttpClient Client, Guid Id)> StudentAsync(string email = "aluno@example.test")
    {
        var id = await factory.CreateUserAsync(email);
        var client = factory.Client();
        await ApiFactory.LoginAsync(client, email);
        await ApiFactory.SetCsrfAsync(client);
        return (client, id);
    }

    private static async Task<JsonElement> ObjetivosAsync(HttpClient client)
    {
        var me = await client.GetFromJsonAsync<JsonElement>("/api/alunos/me");
        return me.GetProperty("objetivos");
    }

    [Fact]
    public async Task Profile_without_objetivos_returns_nulls()
    {
        var (client, _) = await StudentAsync();
        using var _client = client;

        var objetivos = await ObjetivosAsync(client);

        Assert.Equal(JsonValueKind.Null, objetivos.GetProperty("cargoDesejado").ValueKind);
        Assert.Equal(JsonValueKind.Null, objetivos.GetProperty("objetivoProfissional").ValueKind);
    }

    [Fact]
    public async Task UpdateObjetivos_saves_and_returns_both_fields()
    {
        var (client, _) = await StudentAsync();
        using var _client = client;

        var response = await client.PutAsJsonAsync("/api/alunos/me/objetivos", new
        {
            cargoDesejado = "  Desenvolvedor Full Stack ",
            objetivoProfissional = "Quero trabalhar com projetos reais de tecnologia."
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var objetivos = await ObjetivosAsync(client);
        Assert.Equal("Desenvolvedor Full Stack", objetivos.GetProperty("cargoDesejado").GetString());
        Assert.Equal("Quero trabalhar com projetos reais de tecnologia.",
            objetivos.GetProperty("objetivoProfissional").GetString());
    }

    [Fact]
    public async Task UpdateObjetivos_accepts_maximum_lengths()
    {
        var (client, _) = await StudentAsync();
        using var _client = client;

        var response = await client.PutAsJsonAsync("/api/alunos/me/objetivos", new
        {
            cargoDesejado = new string('a', 100),
            objetivoProfissional = new string('b', 500)
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateObjetivos_rejects_cargo_over_100()
    {
        var (client, _) = await StudentAsync();
        using var _client = client;

        var response = await client.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = new string('a', 101), objetivoProfissional = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateObjetivos_rejects_objetivo_over_500()
    {
        var (client, _) = await StudentAsync();
        using var _client = client;

        var response = await client.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = (string?)null, objetivoProfissional = new string('b', 501) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Desenvolvedor 😊")]
    [InlineData("Cargo\u0007com controle")]
    public async Task UpdateObjetivos_rejects_emoji_and_control_characters(string text)
    {
        var (client, _) = await StudentAsync();
        using var _client = client;

        var cargo = await client.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = text, objetivoProfissional = (string?)null });
        var objetivo = await client.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = (string?)null, objetivoProfissional = text });

        Assert.Equal(HttpStatusCode.BadRequest, cargo.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, objetivo.StatusCode);
    }

    [Fact]
    public async Task UpdateObjetivos_blank_values_become_null()
    {
        var (client, id) = await StudentAsync();
        using var _client = client;
        await client.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = "Analista", objetivoProfissional = "Crescer na área." });

        var response = await client.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = "   ", objetivoProfissional = "" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await factory.InScopeAsync(async provider =>
        {
            var aluno = await provider.GetRequiredService<AppDbContext>().Alunos.SingleAsync(x => x.UserId == id);
            Assert.Null(aluno.CargoDesejado);
            Assert.Null(aluno.ObjetivoProfissional);
        });
    }

    [Fact]
    public async Task UpdateObjetivos_allows_each_field_independently_and_does_not_touch_sobre()
    {
        var (client, _) = await StudentAsync();
        using var _client = client;
        await client.PutAsJsonAsync("/api/alunos/me/sobre", new { bio = "Minha apresentação." });

        await client.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = "Engenheiro de Automação", objetivoProfissional = (string?)null });
        var onlyCargo = await ObjetivosAsync(client);
        await client.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = (string?)null, objetivoProfissional = "Especializar em software." });
        var me = await client.GetFromJsonAsync<JsonElement>("/api/alunos/me");

        Assert.Equal("Engenheiro de Automação", onlyCargo.GetProperty("cargoDesejado").GetString());
        Assert.Equal(JsonValueKind.Null, onlyCargo.GetProperty("objetivoProfissional").ValueKind);
        Assert.Equal(JsonValueKind.Null, me.GetProperty("objetivos").GetProperty("cargoDesejado").ValueKind);
        Assert.Equal("Especializar em software.",
            me.GetProperty("objetivos").GetProperty("objetivoProfissional").GetString());
        Assert.Equal("Minha apresentação.", me.GetProperty("sobre").GetProperty("bio").GetString());
    }

    [Fact]
    public async Task UpdateObjetivos_requires_an_active_student()
    {
        using var anonymous = factory.Client();
        await ApiFactory.SetCsrfAsync(anonymous);
        var recruiterEmail = "recrutador@example.test";
        await factory.CreateUserAsync(recruiterEmail, AppRoles.Recruiter);
        using var recruiter = factory.Client();
        await ApiFactory.LoginAsync(recruiter, recruiterEmail);
        await ApiFactory.SetCsrfAsync(recruiter);
        var adminEmail = "admin@example.test";
        await factory.CreateUserAsync(adminEmail, AppRoles.Admin);
        using var admin = factory.Client();
        await ApiFactory.LoginAsync(admin, adminEmail);
        await ApiFactory.SetCsrfAsync(admin);
        var body = new { cargoDesejado = "Analista", objetivoProfissional = (string?)null };

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync("/api/alunos/me/objetivos", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await recruiter.PutAsJsonAsync("/api/alunos/me/objetivos", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync("/api/alunos/me/objetivos", body)).StatusCode);
    }

    [Fact]
    public async Task UpdateObjetivos_is_denied_after_the_student_is_blocked()
    {
        var (client, id) = await StudentAsync();
        using var _client = client;
        await factory.InScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            (await db.Alunos.SingleAsync(x => x.UserId == id)).Ativo = false;
            await db.SaveChangesAsync();
        });

        var response = await client.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = "Analista", objetivoProfissional = (string?)null });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateObjetivos_without_csrf_token_is_rejected()
    {
        var email = "aluno@example.test";
        await factory.CreateUserAsync(email);
        using var client = factory.Client();
        await ApiFactory.LoginAsync(client, email);

        var response = await client.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = "Analista", objetivoProfissional = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateObjetivos_changes_atualizado_em_only_when_a_value_changes()
    {
        var (client, id) = await StudentAsync();
        using var _client = client;
        async Task<DateTimeOffset> UpdatedAtAsync()
        {
            DateTimeOffset value = default;
            await factory.InScopeAsync(async provider =>
                value = (await provider.GetRequiredService<AppDbContext>().Alunos.AsNoTracking()
                    .SingleAsync(x => x.UserId == id)).AtualizadoEm);
            return value;
        }
        var original = await UpdatedAtAsync();

        await client.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = "Analista", objetivoProfissional = (string?)null });
        var afterChange = await UpdatedAtAsync();
        await client.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = "Analista", objetivoProfissional = (string?)null });
        var afterRepeat = await UpdatedAtAsync();

        Assert.True(afterChange > original);
        Assert.Equal(afterChange, afterRepeat);
    }

    [Fact]
    public async Task Inactive_student_is_not_visible_to_recruiters_with_objetivos()
    {
        var (student, id) = await StudentAsync();
        using var _student = student;
        await student.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = "Analista", objetivoProfissional = (string?)null });
        var slug = (await student.GetFromJsonAsync<JsonElement>("/api/alunos/me")).GetProperty("slug").GetString();
        await factory.InScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            (await db.Alunos.SingleAsync(x => x.UserId == id)).Ativo = false;
            await db.SaveChangesAsync();
        });
        await factory.CreateUserAsync("recrutador@example.test", AppRoles.Recruiter);
        using var recruiter = factory.Client();
        await ApiFactory.LoginAsync(recruiter, "recrutador@example.test");

        var response = await recruiter.GetAsync($"/api/talentos/{slug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Recruiter_and_admin_can_read_objetivos_read_only()
    {
        var (student, id) = await StudentAsync();
        using var _student = student;
        await student.PutAsJsonAsync("/api/alunos/me/objetivos",
            new { cargoDesejado = "Analista de Cibersegurança", objetivoProfissional = "Atuar com defesa." });
        var slug = (await student.GetFromJsonAsync<JsonElement>("/api/alunos/me")).GetProperty("slug").GetString();
        await factory.CreateUserAsync("recrutador@example.test", AppRoles.Recruiter);
        using var recruiter = factory.Client();
        await ApiFactory.LoginAsync(recruiter, "recrutador@example.test");
        await factory.CreateUserAsync("admin@example.test", AppRoles.Admin);
        using var admin = factory.Client();
        await ApiFactory.LoginAsync(admin, "admin@example.test");

        var fromRecruiter = await recruiter.GetFromJsonAsync<JsonElement>($"/api/talentos/{slug}");
        var fromAdmin = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/alunos/{id}");

        foreach (var profile in new[] { fromRecruiter, fromAdmin })
        {
            var objetivos = profile.GetProperty("objetivos");
            Assert.Equal("Analista de Cibersegurança", objetivos.GetProperty("cargoDesejado").GetString());
            Assert.Equal("Atuar com defesa.", objetivos.GetProperty("objetivoProfissional").GetString());
        }
    }

    [Fact]
    public async Task Recruiter_sees_null_objetivos_when_the_student_did_not_inform_them()
    {
        var (student, _) = await StudentAsync();
        using var _student = student;
        var slug = (await student.GetFromJsonAsync<JsonElement>("/api/alunos/me")).GetProperty("slug").GetString();
        await factory.CreateUserAsync("recrutador@example.test", AppRoles.Recruiter);
        using var recruiter = factory.Client();
        await ApiFactory.LoginAsync(recruiter, "recrutador@example.test");

        var profile = await recruiter.GetFromJsonAsync<JsonElement>($"/api/talentos/{slug}");

        var objetivos = profile.GetProperty("objetivos");
        Assert.Equal(JsonValueKind.Null, objetivos.GetProperty("cargoDesejado").ValueKind);
        Assert.Equal(JsonValueKind.Null, objetivos.GetProperty("objetivoProfissional").ValueKind);
    }
}
