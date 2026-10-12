<div align="center">

# Talent Valley

**[🇺🇸 English](#english) · [🇧🇷 Português](#portugues)**

[Live demo / Demo online](https://talent-valley-dw6o.vercel.app/)

![Next.js](https://img.shields.io/badge/Next.js-000?logo=nextdotjs&logoColor=fff)
![React](https://img.shields.io/badge/React-20232A?logo=react&logoColor=61DAFB)
![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?logo=typescript&logoColor=fff)
![.NET](https://img.shields.io/badge/.NET_10-512BD4?logo=dotnet&logoColor=fff)
![SQLite](https://img.shields.io/badge/SQLite-003B57?logo=sqlite&logoColor=fff)
![Status](https://img.shields.io/badge/status-MVP-orange)

</div>

---

<a id="english"></a>

## 🇺🇸 English

> **The career platform for Rio Pomba Valley.** Trusted student profiles and structured talent discovery for authorized recruiters. · [Ir para Português](#portugues)

### Overview

Talent Valley connects the students of the Rio Pomba Valley ecosystem with recruiters. Students maintain a complete professional profile; **authorized** recruiters search, compare and shortlist talent using structured filters instead of scrolling through unstructured résumés.

The project started as a showcase for a technology fair (team of 5) and, after strong reception, is being developed into a real product.

### Key features

#### For students
- Complete professional profile: basic data, bio, contact, competencies, languages, availability and work modalities
- Education, work experience and up to **2 featured projects** (with technologies)
- Private uploads: photo (JPEG/PNG/WebP, up to 5 MB), résumé and certificates (PDF, up to 10 MB)
- **RPV verification**: Rio Pomba Valley formations can be validated by admins through a certificate review flow
- Career timeline combining education and experience

#### For recruiters
- Talent search with combinable filters: name, city/state, competencies, formation type/name/status, RPV-verified, availability and work modality
- Sorting by relevance (transparent scoring), most recent or name
- Full talent profile with protected access to photo, résumé and certificates
- Favorites list and **side-by-side comparison** of two talents (factual only: no scores or recommendations)
- Personal dashboard: profiles updated and new students since last login

#### For admins
- Dashboard with aggregate indicators (active students/recruiters, pending validations, most-used competencies)
- RPV validation queue: approve, reject or revoke, with an audit trail
- Account creation with activation by e-mail

### Tech stack

| Layer | Technologies |
|---|---|
| Frontend | Next.js, React, TypeScript, MUI (Material UI), Tailwind CSS, Framer Motion, GSAP |
| Backend | ASP.NET Core Web API (.NET 10), ASP.NET Core Identity, JWT, Entity Framework Core |
| Database | SQLite (WAL mode, foreign keys enforced) |
| E-mail | Brevo (production) |
| Hosting | Vercel (frontend) |
| CI | GitHub Actions |

### Security highlights

- JWT (HS256) delivered **only** via `HttpOnly` cookie (`SameSite=Lax`, `Secure` outside Development) and never exposed in JSON
- CSRF protection (antiforgery token in `X-XSRF-TOKEN`) on every state-changing request
- Role + **live account status** check on every protected request: blocking a user takes effect immediately, even with a valid token
- Lockout after 5 failed logins (15 min), e-mail confirmation required, generic responses to prevent account enumeration
- Host-header filtering (`AllowedHosts`)
- Uploads validated by size, extension, MIME type and file signature; files are never served statically, and storage keys/paths never leave the API
- Integration tests running the real Identity/JWT/antiforgery pipeline

### Architecture

```
talent-valley/
├── .agents/                    Repository skills
├── .github/workflows/          CI
├── frontend/                   Next.js application
└── backend/
    ├── TalentValley.Api/       ASP.NET Core Web API
    ├── TalentValley.Api.Tests/ Identity/security integration tests
    └── TalentValley.slnx       Backend solution
```

File access goes through `IFileStorage` (currently `LocalFileStorage`), so the storage provider can be swapped without touching application workflows.

### Getting started

#### Prerequisites
- Node.js 20.9+ and npm
- .NET 10 SDK

#### 1. Frontend
```bash
cd frontend
npm install
npm run dev
```
Open `http://localhost:3000`.

#### 2. Backend

**Set the JWT signing key** (Base64, at least 32 random bytes; never commit it):
```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
dotnet user-secrets set 'Jwt:SigningKey' ([Convert]::ToBase64String($bytes)) --project backend/TalentValley.Api
```

**Apply the migrations** (the API never migrates on startup):
```bash
dotnet tool restore
dotnet ef database update --project backend/TalentValley.Api
```

**Run the API:**
```bash
cd backend/TalentValley.Api
dotnet run --launch-profile http
```

> Use the `http` profile (`http://localhost:5087`) when working with the frontend on `http://localhost:3000`, so both share the same scheme with `SameSite=Lax` cookies. The `https` profile (`https://localhost:7102`) is for API tools. In Development, the OpenAPI document is at `/openapi/v1.json`.

**Optional: bootstrap an admin (Development)**
```bash
dotnet user-secrets set 'BootstrapAdmin:Email' 'admin@example.test' --project backend/TalentValley.Api
dotnet user-secrets set 'BootstrapAdmin:Name' 'Local Administrator' --project backend/TalentValley.Api
dotnet user-secrets set 'BootstrapAdmin:Password' '<strong-password>' --project backend/TalentValley.Api
```
Bootstrap is idempotent and never overwrites an existing account. In Development, activation/reset links are printed to the API console, so no e-mail provider is needed.

### Configuration (production)

| Variable | Purpose |
|---|---|
| `AllowedHosts` | Production domain (host-header validation) |
| `Jwt__SigningKey` | Base64-encoded key, ≥ 32 random bytes |
| `Jwt__Issuer` / `Jwt__Audience` | JWT issuer and audience |
| `Frontend__BaseUrl` | Production HTTPS origin (used in activation/reset links) |
| `ConnectionStrings__DefaultConnection` | Database connection string |
| `Storage__RootPath` | Path to a **persistent volume**. Ephemeral container storage will lose uploads |
| `DataProtection__KeysPath` | Persistent path so activation/reset tokens survive restarts |
| `Brevo__ApiKey`, `Brevo__SenderAddress`, `Brevo__SenderName` | Transactional e-mail (all three required) |

Missing or weak `Jwt:SigningKey` aborts startup in every environment.

### Tests and checks

```bash
dotnet restore backend/TalentValley.slnx
dotnet build backend/TalentValley.slnx --no-restore
dotnet test backend/TalentValley.slnx --no-build
dotnet ef migrations has-pending-model-changes --project backend/TalentValley.Api
```

Each test runs against its own temporary SQLite database; the developer database is never touched.

### API at a glance

| Area | Endpoints |
|---|---|
| Auth | `GET /api/auth/csrf`, `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`, `POST /api/auth/activate-account`, `POST /api/auth/forgot-password`, `POST /api/auth/reset-password` |
| Student | `GET /api/alunos/me`, `PUT /api/alunos/me/{dados-basicos,sobre,contato,competencias,idiomas,disponibilidade}`, CRUD on `/formacoes`, `/experiencias`, `/projetos`, `GET /trajetoria` |
| Recruiter | `GET /api/talentos`, `GET /api/talentos/{slug}`, `GET /api/recrutador/comparar`, `/favoritos`, `/dashboard` |
| Admin | `GET /api/admin/dashboard`, `GET /api/admin/validacoes-rpv` + approve/reject/revoke |
| Catalogs | `GET /api/competencias`, `GET /api/idiomas` |

Every state-changing request needs the `X-XSRF-TOKEN` header (fetch it from `/api/auth/csrf` with `credentials: 'include'`, and again after login/logout).

### Roadmap

- [x] Authentication, roles and account activation
- [x] Student profile (data, education, experience, projects, files)
- [x] Recruiter search, favorites and comparison
- [x] Admin dashboard and RPV validation
- [ ] Production hosting and hardening (security review)
- [ ] MVP launch

### Contributing

The project is currently developed by a closed team. Suggestions and bug reports are welcome through [Issues](https://github.com/gustavobarcelos18/TalentValley/issues).

### Authors

Built by the Talent Valley team. Maintainer: [@gustavobarcelos18](https://github.com/gustavobarcelos18).

### License

All rights reserved. Not licensed for reuse until stated otherwise.

---

<a id="portugues"></a>

## 🇧🇷 Português

> **A plataforma de carreiras do Rio Pomba Valley.** Perfis de estudantes confiáveis e busca estruturada de talentos para recrutadores autorizados. · [Go to English](#english)

### Visão geral

O Talent Valley conecta os estudantes do ecossistema Rio Pomba Valley com recrutadores. O aluno mantém um perfil profissional completo; recrutadores **autorizados** buscam, comparam e favoritam talentos com filtros estruturados, sem precisar garimpar currículos soltos.

O projeto nasceu como vitrine para uma feira de tecnologia (equipe de 5 pessoas) e, após a ótima receptividade, está sendo desenvolvido como produto real.

### Principais funcionalidades

#### Para estudantes
- Perfil profissional completo: dados básicos, sobre, contato, competências, idiomas, disponibilidade e modalidades de trabalho
- Formações, experiências e até **2 projetos em destaque** (com tecnologias)
- Uploads privados: foto (JPEG/PNG/WebP, até 5 MB), currículo e certificados (PDF, até 10 MB)
- **Verificação RPV**: formações do Rio Pomba Valley são validadas por admins via análise de certificado
- Trajetória unindo formações e experiências

#### Para recrutadores
- Busca de talentos com filtros combináveis: nome, cidade/UF, competências, tipo/nome/status da formação, RPV verificado, disponibilidade e modalidade
- Ordenação por relevância (pontuação transparente), mais recentes ou nome
- Perfil completo do talento com acesso protegido a foto, currículo e certificados
- Favoritos e **comparação lado a lado** de dois talentos (apenas fatos: sem notas nem recomendações)
- Dashboard pessoal: perfis atualizados e novos alunos desde o último acesso

#### Para admins
- Dashboard com indicadores agregados (alunos/recrutadores ativos, validações pendentes, competências mais usadas)
- Fila de validação RPV: aprovar, rejeitar ou revogar, com trilha de auditoria
- Criação de contas com ativação por e-mail

### Stack

| Camada | Tecnologias |
|---|---|
| Frontend | Next.js, React, TypeScript, MUI (Material UI), Tailwind CSS, Framer Motion, GSAP |
| Backend | ASP.NET Core Web API (.NET 10), ASP.NET Core Identity, JWT, Entity Framework Core |
| Banco de dados | SQLite (modo WAL, foreign keys ativas) |
| E-mail | Brevo (produção) |
| Hospedagem | Vercel (frontend) |
| CI | GitHub Actions |

### Destaques de segurança

- JWT (HS256) entregue **somente** via cookie `HttpOnly` (`SameSite=Lax`, `Secure` fora do Development) e nunca exposto em JSON
- Proteção CSRF (token antiforgery em `X-XSRF-TOKEN`) em toda requisição que altera estado
- Verificação de papel + **status atual da conta** a cada requisição protegida: bloquear um usuário vale na hora, mesmo com token válido
- Bloqueio após 5 logins falhos (15 min), confirmação de e-mail obrigatória, respostas genéricas contra enumeração de contas
- Filtro de Host header (`AllowedHosts`)
- Uploads validados por tamanho, extensão, MIME type e assinatura do arquivo; arquivos nunca são servidos estaticamente, e chaves/caminhos de storage não saem da API
- Testes de integração rodando o pipeline real de Identity/JWT/antiforgery

### Arquitetura

```
talent-valley/
├── .agents/                    Skills do repositório
├── .github/workflows/          CI
├── frontend/                   Aplicação Next.js
└── backend/
    ├── TalentValley.Api/       ASP.NET Core Web API
    ├── TalentValley.Api.Tests/ Testes de integração de Identity/segurança
    └── TalentValley.slnx       Solution do backend
```

O acesso a arquivos passa por `IFileStorage` (hoje `LocalFileStorage`), então dá para trocar o provedor de storage sem mexer nos fluxos da aplicação.

### Como rodar

#### Pré-requisitos
- Node.js 20.9+ e npm
- .NET 10 SDK

#### 1. Frontend
```bash
cd frontend
npm install
npm run dev
```
Acesse `http://localhost:3000`.

#### 2. Backend

**Defina a chave de assinatura do JWT** (Base64, no mínimo 32 bytes aleatórios; nunca commite):
```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
dotnet user-secrets set 'Jwt:SigningKey' ([Convert]::ToBase64String($bytes)) --project backend/TalentValley.Api
```

**Aplique as migrations** (a API nunca migra no startup):
```bash
dotnet tool restore
dotnet ef database update --project backend/TalentValley.Api
```

**Rode a API:**
```bash
cd backend/TalentValley.Api
dotnet run --launch-profile http
```

> Use o profile `http` (`http://localhost:5087`) ao trabalhar com o frontend em `http://localhost:3000`, para que ambos usem o mesmo esquema com cookies `SameSite=Lax`. O profile `https` (`https://localhost:7102`) serve para ferramentas de API. Em Development, o documento OpenAPI fica em `/openapi/v1.json`.

**Opcional: criar um admin inicial (Development)**
```bash
dotnet user-secrets set 'BootstrapAdmin:Email' 'admin@example.test' --project backend/TalentValley.Api
dotnet user-secrets set 'BootstrapAdmin:Name' 'Administrador Local' --project backend/TalentValley.Api
dotnet user-secrets set 'BootstrapAdmin:Password' '<senha-forte>' --project backend/TalentValley.Api
```
O bootstrap é idempotente e nunca sobrescreve uma conta existente. Em Development, os links de ativação/redefinição aparecem no console da API, então não precisa de provedor de e-mail.

### Configuração (produção)

| Variável | Finalidade |
|---|---|
| `AllowedHosts` | Domínio de produção (validação do Host header) |
| `Jwt__SigningKey` | Chave em Base64, ≥ 32 bytes aleatórios |
| `Jwt__Issuer` / `Jwt__Audience` | Issuer e audience do JWT |
| `Frontend__BaseUrl` | Origem HTTPS de produção (usada nos links de ativação/redefinição) |
| `ConnectionStrings__DefaultConnection` | String de conexão do banco |
| `Storage__RootPath` | Caminho de um **volume persistente**. Storage efêmero de container perde os uploads |
| `DataProtection__KeysPath` | Caminho persistente para os tokens de ativação/redefinição sobreviverem a restarts |
| `Brevo__ApiKey`, `Brevo__SenderAddress`, `Brevo__SenderName` | E-mail transacional (os três são obrigatórios) |

`Jwt:SigningKey` ausente ou fraca impede a API de iniciar em qualquer ambiente.

### Testes e verificações

```bash
dotnet restore backend/TalentValley.slnx
dotnet build backend/TalentValley.slnx --no-restore
dotnet test backend/TalentValley.slnx --no-build
dotnet ef migrations has-pending-model-changes --project backend/TalentValley.Api
```

Cada teste roda em um banco SQLite temporário próprio; o banco de desenvolvimento nunca é tocado.

### API resumida

| Área | Endpoints |
|---|---|
| Auth | `GET /api/auth/csrf`, `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`, `POST /api/auth/activate-account`, `POST /api/auth/forgot-password`, `POST /api/auth/reset-password` |
| Aluno | `GET /api/alunos/me`, `PUT /api/alunos/me/{dados-basicos,sobre,contato,competencias,idiomas,disponibilidade}`, CRUD em `/formacoes`, `/experiencias`, `/projetos`, `GET /trajetoria` |
| Recrutador | `GET /api/talentos`, `GET /api/talentos/{slug}`, `GET /api/recrutador/comparar`, `/favoritos`, `/dashboard` |
| Admin | `GET /api/admin/dashboard`, `GET /api/admin/validacoes-rpv` + aprovar/rejeitar/revogar |
| Catálogos | `GET /api/competencias`, `GET /api/idiomas` |

Toda requisição que altera estado exige o header `X-XSRF-TOKEN` (busque em `/api/auth/csrf` com `credentials: 'include'`, e de novo após login/logout).

### Roadmap

- [x] Autenticação, papéis e ativação de conta
- [x] Perfil do aluno (dados, formações, experiências, projetos, arquivos)
- [x] Busca, favoritos e comparação para recrutadores
- [x] Dashboard admin e validação RPV
- [ ] Hospedagem de produção e hardening (revisão de segurança)
- [ ] Lançamento do MVP

### Contribuindo

O projeto é desenvolvido por uma equipe fechada por enquanto. Sugestões e bugs são bem-vindos em [Issues](https://github.com/gustavobarcelos18/TalentValley/issues).

### Autores

Feito pela equipe Talent Valley. Mantenedor: [@gustavobarcelos18](https://github.com/gustavobarcelos18).

### Licença

Todos os direitos reservados. Sem licença de reuso até segunda ordem.
