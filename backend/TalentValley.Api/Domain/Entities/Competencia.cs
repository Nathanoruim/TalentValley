using TalentValley.Api.Domain.Enums;

namespace TalentValley.Api.Domain.Entities;

public class Competencia
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string NomeBusca { get; set; } = string.Empty;
    public CategoriaCompetencia Categoria { get; set; }
    public ICollection<AlunoCompetencia> Alunos { get; set; } = [];
    public ICollection<ProjetoCompetencia> Projetos { get; set; } = [];
}
