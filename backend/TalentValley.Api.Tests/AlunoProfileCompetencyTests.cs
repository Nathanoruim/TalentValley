using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TalentValley.Api.Authorization;
using TalentValley.Api.Data;
using TalentValley.Api.Domain.Entities;
using TalentValley.Api.Domain.Enums;
using TalentValley.Api.DTOs;
using TalentValley.Api.Services;

namespace TalentValley.Api.Tests;

public sealed class AlunoProfileCompetencyTests : IDisposable
{
    private readonly ApiFactory factory = new();

    private async Task<HttpClient> StudentClientAsync(string email = "aluno@example.test")
    {
        await factory.CreateUserAsync(email);
        var client = factory.Client();
        await ApiFactory.LoginAsync(client, email);
        await ApiFactory.SetCsrfAsync(client);
        return client;
    }

    [Fact]
    public async Task Competencias_catalog_returned()
    {
        using var client = await StudentClientAsync();
        var items = await client.GetFromJsonAsync<List<CatalogoCompetenciaResponse>>("/api/competencias", ApiFactory.JsonOptions);
        Assert.NotNull(items);
        Assert.Equal(CatalogSeedService.CompetenciaNames.Length, items!.Count);
    }

    [Fact]
    public async Task Competencias_catalog_seed_is_complete_idempotent_and_preserves_existing_records()
    {
        await factory.InScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var seed = provider.GetRequiredService<CatalogSeedService>();
            var existing = new Competencia
            {
                Nome = "Existing competency", NomeBusca = NameNormalizer.Normalize("Existing competency")
            };
            db.Competencias.Add(existing);
            await db.SaveChangesAsync();
            var existingId = existing.Id;

            await seed.InitializeAsync();
            var countAfterFirstRun = await db.Competencias.CountAsync();
            await seed.InitializeAsync();

            Assert.Equal(countAfterFirstRun, await db.Competencias.CountAsync());
            Assert.Equal(existingId, (await db.Competencias.SingleAsync(x => x.Nome == existing.Nome)).Id);
            var configured = await db.Competencias.Select(x => x.NomeBusca).ToListAsync();
            Assert.All(CatalogSeedService.CompetenciaNames,
                name => Assert.Contains(NameNormalizer.Normalize(name), configured));
        });
    }

    [Fact]
    public void Configured_competency_names_have_unique_normalized_search_keys()
    {
        var normalized = CatalogSeedService.CompetenciaNames.Select(NameNormalizer.Normalize).ToList();

        Assert.Equal(normalized.Count, normalized.Distinct().Count());
    }

    [Fact]
    public async Task Competencias_catalog_contains_representative_categories()
    {
        using var client = await StudentClientAsync();
        var items = (await client.GetFromJsonAsync<List<CatalogoCompetenciaResponse>>("/api/competencias", ApiFactory.JsonOptions))!;
        var names = items.Select(x => x.Nome).ToHashSet();

        var required = new[]
        {
            "C#", "ASP.NET Core", "React", "PostgreSQL", "Docker", "GitHub Actions", "xUnit",
            "Machine Learning", "Flutter", "Clean Architecture", "OWASP", "Figma"
        };
        Assert.All(required, name => Assert.Contains(name, names));
    }

    [Fact]
    public async Task Competencias_catalog_is_grouped_by_category_in_a_stable_order()
    {
        using var client = await StudentClientAsync();
        var items = (await client.GetFromJsonAsync<List<CatalogoCompetenciaResponse>>("/api/competencias", ApiFactory.JsonOptions))!;

        Assert.Equal("Tecnologia · Linguagens", items.Single(x => x.Nome == "C#").Categoria);
        Assert.Equal("Tecnologia · Frontend", items.Single(x => x.Nome == "React").Categoria);
        Assert.Equal("Gestão de projetos e processos · Metodologias ágeis", items.Single(x => x.Nome == "Scrum").Categoria);
        Assert.Equal("Marketing, design e comunicação · Design", items.Single(x => x.Nome == "Figma").Categoria);
        Assert.Equal("Liderança e colaboração", items.Single(x => x.Nome == "Liderança").Categoria);
        Assert.Equal(TipoCompetencia.TECNICA, items.Single(x => x.Nome == "React").Tipo);
        Assert.Equal(TipoCompetencia.COMPORTAMENTAL, items.Single(x => x.Nome == "Liderança").Tipo);

        var expectedOrder = Enum.GetValues<CategoriaCompetencia>().Select(x => x.Grupo()).ToList();
        var categoriesInResponse = items.Select(x => x.Categoria).Distinct().ToList();
        Assert.Equal(expectedOrder, categoriesInResponse);
        Assert.Equal(categoriesInResponse.Count, items.Select(x => x.Categoria).Aggregate(
            new List<string>(), (list, categoria) => { if (list.LastOrDefault() != categoria) list.Add(categoria); return list; }).Count);
    }

    [Fact]
    public async Task Competencias_catalog_has_technical_and_behavioral_items_in_every_category()
    {
        using var client = await StudentClientAsync();
        var items = (await client.GetFromJsonAsync<List<CatalogoCompetenciaResponse>>("/api/competencias", ApiFactory.JsonOptions))!;

        Assert.Contains(items, x => x.Tipo == TipoCompetencia.TECNICA);
        Assert.Contains(items, x => x.Tipo == TipoCompetencia.COMPORTAMENTAL);
        Assert.Equal(Enum.GetValues<CategoriaCompetencia>().Length, items.Select(x => x.Categoria).Distinct().Count());
    }

    [Fact]
    public async Task UpdateCompetencias_accepts_up_to_the_behavioral_limit_and_rejects_more()
    {
        using var client = await StudentClientAsync();
        var catalog = (await client.GetFromJsonAsync<List<CatalogoCompetenciaResponse>>("/api/competencias", ApiFactory.JsonOptions))!;
        var behavioral = catalog.Where(x => x.Tipo == TipoCompetencia.COMPORTAMENTAL).Select(x => x.Id).ToList();
        var technical = catalog.First(x => x.Tipo == TipoCompetencia.TECNICA).Id;

        var accepted = await client.PutAsJsonAsync("/api/alunos/me/competencias",
            new { competenciaIds = behavioral.Take(AlunoService.MaxCompetenciasComportamentais).Append(technical) });
        var rejected = await client.PutAsJsonAsync("/api/alunos/me/competencias",
            new { competenciaIds = behavioral.Take(AlunoService.MaxCompetenciasComportamentais + 1) });

        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
    }

    [Fact]
    public async Task Profile_returns_the_type_of_each_competency_so_hard_and_soft_skills_can_be_split()
    {
        using var client = await StudentClientAsync();
        var catalog = (await client.GetFromJsonAsync<List<CatalogoCompetenciaResponse>>("/api/competencias", ApiFactory.JsonOptions))!;
        var technical = catalog.First(x => x.Tipo == TipoCompetencia.TECNICA);
        var behavioral = catalog.First(x => x.Tipo == TipoCompetencia.COMPORTAMENTAL);

        var saved = await client.PutAsJsonAsync("/api/alunos/me/competencias",
            new { competenciaIds = new[] { technical.Id, behavioral.Id } });
        var profile = (await client.GetFromJsonAsync<MeResponse>("/api/alunos/me", ApiFactory.JsonOptions))!;

        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        Assert.Equal(TipoCompetencia.TECNICA, profile.Competencias.Single(x => x.Id == technical.Id).Tipo);
        Assert.Equal(TipoCompetencia.COMPORTAMENTAL, profile.Competencias.Single(x => x.Id == behavioral.Id).Tipo);
    }

    [Fact]
    public async Task Competencias_catalog_does_not_contain_removed_generic_items()
    {
        using var client = await StudentClientAsync();
        var names = (await client.GetFromJsonAsync<List<CatalogoCompetenciaResponse>>("/api/competencias", ApiFactory.JsonOptions))!
            .Select(x => x.Nome).ToHashSet();

        var removed = new[]
        {
            "REST", "Version Control", "Validation", "Logging", "System Integration", "REST Client",
            "Search", "Sorting", "Filtering", "Pagination", "HTTP", "JSON", "Data Integrity"
        };
        Assert.All(removed, name => Assert.DoesNotContain(name, names));
    }

    [Fact]
    public async Task Competencias_seed_corrects_the_category_of_existing_items()
    {
        await factory.InScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var react = await db.Competencias.SingleAsync(x => x.NomeBusca == "react");
            react.Categoria = CategoriaCompetencia.Mobile;
            await db.SaveChangesAsync();

            await provider.GetRequiredService<CatalogSeedService>().InitializeAsync();

            Assert.Equal(CategoriaCompetencia.Frontend, (await db.Competencias.AsNoTracking().SingleAsync(x => x.NomeBusca == "react")).Categoria);
        });
    }

    [Fact]
    public async Task Competencias_search_works()
    {
        using var client = await StudentClientAsync();
        var items = await client.GetFromJsonAsync<List<CatalogoCompetenciaResponse>>("/api/competencias?search=react", ApiFactory.JsonOptions);
        Assert.NotNull(items);
        Assert.Contains(items!, item => item.Nome == "React");
    }

    [Fact]
    public async Task Competencias_search_is_case_insensitive()
    {
        using var client = await StudentClientAsync();
        var items = await client.GetFromJsonAsync<List<CatalogoCompetenciaResponse>>("/api/competencias?search=ReAcT", ApiFactory.JsonOptions);

        Assert.Contains(items!, item => item.Nome == "React");
    }

    [Fact]
    public async Task Competencias_catalog_requires_authentication()
    {
        using var client = factory.Client();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/competencias")).StatusCode);
    }

    [Fact]
    public async Task Active_recruiter_can_read_and_search_competency_catalog()
    {
        await factory.CreateUserAsync("recruiter@example.test", AppRoles.Recruiter);
        using var client = factory.Client();
        await ApiFactory.LoginAsync(client, "recruiter@example.test");

        var catalogResponse = await client.GetAsync("/api/competencias");
        Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);
        var items = await catalogResponse.Content.ReadFromJsonAsync<List<CatalogoCompetenciaResponse>>(ApiFactory.JsonOptions);
        Assert.NotNull(items);
        Assert.Equal(CatalogSeedService.CompetenciaNames.Length, items!.Count);

        var searchResponse = await client.GetAsync("/api/competencias?search=react");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var matches = await searchResponse.Content.ReadFromJsonAsync<List<CatalogoCompetenciaResponse>>(ApiFactory.JsonOptions);
        Assert.Contains(matches!, item => item.Nome == "React");
    }

    [Theory]
    [InlineData(AppRoles.Student)]
    [InlineData(AppRoles.Recruiter)]
    public async Task Blocked_account_cannot_read_competency_catalog(string role)
    {
        const string email = "blocked@example.test";
        var id = await factory.CreateUserAsync(email, role);
        using var client = factory.Client();
        await ApiFactory.LoginAsync(client, email);

        await factory.InScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            if (role == AppRoles.Student) (await db.Alunos.SingleAsync(x => x.UserId == id)).Ativo = false;
            else (await db.Recrutadores.SingleAsync(x => x.UserId == id)).Status = StatusRecrutador.BLOQUEADO;
            await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/competencias")).StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_read_competency_catalog()
    {
        await factory.CreateUserAsync("admin@example.test", AppRoles.Admin);
        using var client = factory.Client();
        await ApiFactory.LoginAsync(client, "admin@example.test");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/competencias")).StatusCode);
    }

    [Fact]
    public async Task UpdateCompetencias_full_replacement_works()
    {
        using var client = await StudentClientAsync();
        var catalog = await client.GetFromJsonAsync<List<CatalogoCompetenciaResponse>>("/api/competencias", ApiFactory.JsonOptions);
        var ids = catalog!.Take(3).Select(x => x.Id).ToList();
        var response = await client.PutAsJsonAsync("/api/alunos/me/competencias", new { competenciaIds = ids });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var profile = await client.GetFromJsonAsync<MeResponse>("/api/alunos/me", ApiFactory.JsonOptions);
        Assert.Equal(ids.OrderBy(x => x), profile!.Competencias.Select(x => x.Id).OrderBy(x => x));
    }

    [Fact]
    public async Task UpdateCompetencias_unknown_id_rejected()
    {
        using var client = await StudentClientAsync();
        var response = await client.PutAsJsonAsync("/api/alunos/me/competencias", new { competenciaIds = new[] { 99999 } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCompetencias_duplicates_normalized()
    {
        using var client = await StudentClientAsync();
        var catalog = await client.GetFromJsonAsync<List<CatalogoCompetenciaResponse>>("/api/competencias", ApiFactory.JsonOptions);
        var id = catalog!.First().Id;
        var response = await client.PutAsJsonAsync("/api/alunos/me/competencias", new { competenciaIds = new[] { id, id, id } });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var profile = await client.GetFromJsonAsync<MeResponse>("/api/alunos/me", ApiFactory.JsonOptions);
        Assert.Single(profile!.Competencias);
    }

    [Fact]
    public async Task UpdateCompetencias_idempotent_same_state_no_duplicates()
    {
        using var client = await StudentClientAsync();
        var catalog = await client.GetFromJsonAsync<List<CatalogoCompetenciaResponse>>("/api/competencias", ApiFactory.JsonOptions);
        var ids = catalog!.Take(2).Select(x => x.Id).ToList();
        await client.PutAsJsonAsync("/api/alunos/me/competencias", new { competenciaIds = ids });
        await client.PutAsJsonAsync("/api/alunos/me/competencias", new { competenciaIds = ids });

        var profile = await client.GetFromJsonAsync<MeResponse>("/api/alunos/me", ApiFactory.JsonOptions);
        Assert.Equal(2, profile!.Competencias.Count);
    }

    public void Dispose() => factory.Dispose();
}
