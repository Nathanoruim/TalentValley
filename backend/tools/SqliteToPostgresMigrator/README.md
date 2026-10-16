# SqliteToPostgresMigrator

Ferramenta de uso único para a virada da Railway (SQLite + volume) para o Neon (PostgreSQL) + Cloudflare R2.
Fica fora de `backend/TalentValley.slnx` (não entra no CI nem no Sonar) e será removida depois do cutover validado.

O que ela faz:

- copia todas as tabelas de dados do SQLite para o Postgres, numa única transação (qualquer falha desfaz tudo);
- converte tipos (GUID, booleanos, datas com fuso para UTC), preserva os ids dos catálogos e acerta as sequências;
- copia `fotos/`, `curriculos/` e `certificados/` do volume para o bucket R2 com as mesmas chaves;
- imprime só contagens e nomes de tabelas/colunas, nunca valores de campos (LGPD).
- **não** copia as chaves antigas do DataProtection nem o histórico de migrations.

## Antes de rodar

1. Pegue uma cópia **consistente** do SQLite da Railway. O banco usa WAL: copiar só o arquivo `.db` pode perder dados.
   Copie `.db`, `.db-wal` e `.db-shm` juntos, ou gere o arquivo com `sqlite3 origem.db "VACUUM INTO 'copia.db'"`.
   Mantenha a cópia e a pasta `storage` **fora do repositório** e apague-as depois do cutover.
2. Crie o destino vazio aplicando as migrations (não suba a API, para não rodar seed):
   `dotnet ef database update --project backend/TalentValley.Api` com `ConnectionStrings__DefaultConnection` apontando para o Postgres.
3. Defina as variáveis de ambiente (nunca em arquivo versionado):
   `TARGET_POSTGRES` (fora do localhost exige `SSL Mode=VerifyFull`), `R2_ACCOUNT_ID`, `R2_BUCKET_NAME`, `R2_ACCESS_KEY_ID`, `R2_SECRET_ACCESS_KEY`.

## Como rodar

```
dotnet run --project backend/tools/SqliteToPostgresMigrator -- --sqlite <copia.db> --storage-dir <pasta storage>
```

Opções: `--skip-database` (só arquivos), `--skip-files` (só banco; dispensa `--storage-dir` e as variáveis R2).

Códigos de saída: `0` tudo copiado e conferido; `1` abortado (o banco não fica pela metade); `2` copiado, mas há avisos
(arquivos sem referência no banco, referências sem arquivo, nomes inválidos) que o maintainer deve avaliar.

A ferramenta aborta, sem copiar nada, quando: o destino tem dados, os schemas diferem, o SQLite passa de ~400 MB
(limite do Neon Free), ou há valores que o destino não aceita (nulos em colunas obrigatórias, textos acima do tamanho da coluna).
Reenviar arquivos é seguro: o que já está no bucket com o mesmo tamanho é pulado.

## Testes

```
TALENT_VALLEY_TEST_POSTGRES="Host=localhost;Username=postgres;Password=<senha>" dotnet test backend/tools/SqliteToPostgresMigrator.Tests
```

Usam um SQLite sintético com o formato de armazenamento do EF Core e um Postgres descartável; nenhum dado real.
