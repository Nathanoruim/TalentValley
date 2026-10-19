namespace TalentValley.Api.Domain.Enums;

public enum TipoCompetencia
{
    TECNICA,
    COMPORTAMENTAL
}

// The declaration order is the order the categories appear in the catalog lists.
// The type (technical/behavioral) and the area are derived from the category, so there is a single column.
public enum CategoriaCompetencia
{
    // Tecnologia
    Linguagens,
    BackendApis,
    Frontend,
    BancoDeDados,
    NuvemDevOps,
    TestesQualidade,
    DadosIa,
    Mobile,
    Arquitetura,
    Seguranca,
    Ferramentas,
    PraticasEngenharia,
    TecnologiaAplicada,

    // Gestão de projetos e processos
    MetodologiasAgeis,
    GestaoProjetos,
    ProcessosQualidade,

    // Inovação e empreendedorismo
    Inovacao,
    Empreendedorismo,

    // Negócios
    AdministracaoFinancas,
    VendasAtendimento,
    PessoasRh,
    LogisticaOperacoes,

    // Marketing, design e comunicação
    MarketingDigital,
    Design,
    Comunicacao,

    // Comportamentais (soft skills)
    ComunicacaoRelacionamento,
    LiderancaColaboracao,
    PensamentoCriatividade,
    PosturaAutogestao
}

public static class CategoriaCompetenciaExtensions
{
    public static TipoCompetencia Tipo(this CategoriaCompetencia categoria) =>
        categoria >= CategoriaCompetencia.ComunicacaoRelacionamento
            ? TipoCompetencia.COMPORTAMENTAL
            : TipoCompetencia.TECNICA;

    public static string Area(this CategoriaCompetencia categoria) => categoria switch
    {
        <= CategoriaCompetencia.TecnologiaAplicada => "Tecnologia",
        <= CategoriaCompetencia.ProcessosQualidade => "Gestão de projetos e processos",
        <= CategoriaCompetencia.Empreendedorismo => "Inovação e empreendedorismo",
        <= CategoriaCompetencia.LogisticaOperacoes => "Negócios",
        <= CategoriaCompetencia.Comunicacao => "Marketing, design e comunicação",
        _ => "Comportamentais"
    };

    public static string Rotulo(this CategoriaCompetencia categoria) => categoria switch
    {
        CategoriaCompetencia.Linguagens => "Linguagens",
        CategoriaCompetencia.BackendApis => "Backend e APIs",
        CategoriaCompetencia.Frontend => "Frontend",
        CategoriaCompetencia.BancoDeDados => "Banco de dados",
        CategoriaCompetencia.NuvemDevOps => "Nuvem e DevOps",
        CategoriaCompetencia.TestesQualidade => "Testes e qualidade",
        CategoriaCompetencia.DadosIa => "Dados e IA",
        CategoriaCompetencia.Mobile => "Mobile",
        CategoriaCompetencia.Arquitetura => "Arquitetura",
        CategoriaCompetencia.Seguranca => "Segurança",
        CategoriaCompetencia.Ferramentas => "Ferramentas",
        CategoriaCompetencia.PraticasEngenharia => "Práticas de engenharia",
        CategoriaCompetencia.TecnologiaAplicada => "Tecnologia aplicada",
        CategoriaCompetencia.MetodologiasAgeis => "Metodologias ágeis",
        CategoriaCompetencia.GestaoProjetos => "Gestão de projetos",
        CategoriaCompetencia.ProcessosQualidade => "Processos e qualidade",
        CategoriaCompetencia.Inovacao => "Inovação",
        CategoriaCompetencia.Empreendedorismo => "Empreendedorismo",
        CategoriaCompetencia.AdministracaoFinancas => "Administração e finanças",
        CategoriaCompetencia.VendasAtendimento => "Vendas e atendimento",
        CategoriaCompetencia.PessoasRh => "Pessoas e RH",
        CategoriaCompetencia.LogisticaOperacoes => "Logística e operações",
        CategoriaCompetencia.MarketingDigital => "Marketing digital",
        CategoriaCompetencia.Design => "Design",
        CategoriaCompetencia.Comunicacao => "Comunicação",
        CategoriaCompetencia.ComunicacaoRelacionamento => "Comunicação e relacionamento",
        CategoriaCompetencia.LiderancaColaboracao => "Liderança e colaboração",
        CategoriaCompetencia.PensamentoCriatividade => "Pensamento e criatividade",
        CategoriaCompetencia.PosturaAutogestao => "Postura e autogestão",
        _ => categoria.ToString()
    };

    // Heading used to group the options in the catalog lists, e.g. "Tecnologia · Backend e APIs".
    public static string Grupo(this CategoriaCompetencia categoria) =>
        categoria.Tipo() == TipoCompetencia.COMPORTAMENTAL
            ? categoria.Rotulo()
            : $"{categoria.Area()} · {categoria.Rotulo()}";
}
