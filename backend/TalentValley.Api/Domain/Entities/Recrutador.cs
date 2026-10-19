using TalentValley.Api.Domain.Enums;

namespace TalentValley.Api.Domain.Entities;

public class Recrutador
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string Empresa { get; set; } = string.Empty;
    public string EmpresaBusca { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public StatusRecrutador Status { get; set; }
    // LGPD consent copied from the approved registration request.
    public DateTimeOffset? ConsentimentoEm { get; set; }
    public string? VersaoTermos { get; set; }
    // Optional newsletter consent: current choice and when it was last changed.
    public bool NewsletterAceita { get; set; }
    public DateTimeOffset? NewsletterAtualizadaEm { get; set; }
}
