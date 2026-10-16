using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TalentValley.Api.Data;
using TalentValley.Api.Services;

namespace TalentValley.Api.Tests;

public sealed class AdminDatabaseTests
{
    [Fact]
    public async Task Database_unique_email_constraint_protects_writes_that_bypass_Identity_validation()
    {
        using var factory = new ApiFactory();
        var first = await factory.CreateUserAsync("one@example.test");
        var second = await factory.CreateUserAsync("two@example.test");
        await factory.InScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(x => x.Id == second);
            user.NormalizedEmail = (await db.Users.SingleAsync(x => x.Id == first)).NormalizedEmail;
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            var postgres = Assert.IsType<PostgresException>(error.InnerException);
            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
            Assert.Equal("EmailIndex", postgres.ConstraintName);
        });
    }

    [Theory]
    [InlineData("João Silva", "joao-silva")]
    [InlineData("  Érica---da / Conceição  ", "erica-da-conceicao")]
    [InlineData("---", "aluno")]
    public void Slug_handles_accents_and_separators(string input, string expected) =>
        Assert.Equal(expected, SlugService.BaseSlug(input));

    [Fact]
    public async Task Long_slug_collisions_reserve_suffix_space()
    {
        using var factory = new ApiFactory();
        var id = await factory.CreateUserAsync("person@example.test");
        await factory.InScopeAsync(async provider =>
        {
            var db = provider.GetRequiredService<AppDbContext>();
            var name = new string('a', 200);
            (await db.Alunos.SingleAsync(x => x.UserId == id)).Slug = SlugService.BaseSlug(name);
            await db.SaveChangesAsync();
            var slug = await provider.GetRequiredService<SlugService>().GenerateAsync(name);
            Assert.Equal(new string('a', 178) + "-2", slug);
            Assert.Equal(180, slug.Length);
        });
    }
}
