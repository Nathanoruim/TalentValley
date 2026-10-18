namespace TalentValley.Api.DTOs;

public sealed record DemoStateResponse(bool Habilitado, bool Preparado);

public sealed record DemoChangeResponse(int AlunosCriados, int RecrutadoresCriados);
