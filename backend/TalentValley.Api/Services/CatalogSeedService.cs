using Microsoft.EntityFrameworkCore;
using TalentValley.Api.Data;
using TalentValley.Api.Domain.Entities;
using TalentValley.Api.Domain.Enums;

namespace TalentValley.Api.Services;

public sealed class CatalogSeedService(AppDbContext database)
{
    public static readonly (string Nome, CategoriaCompetencia Categoria)[] Competencias =
    [
        .. Group(CategoriaCompetencia.Linguagens,
            "C (linguagem)", "C++", "C#", "Java", "Kotlin", "Python", "PHP", "Ruby", "Go (Golang)", "Rust",
            "JavaScript", "TypeScript", "Dart", "Swift", "SQL", "Bash", "PowerShell"),

        .. Group(CategoriaCompetencia.BackendApis,
            ".NET", "ASP.NET Core", "Entity Framework Core", "Node.js", "Express.js", "NestJS",
            "Spring Boot", "Spring Framework", "Django", "Flask", "FastAPI", "Laravel", "Ruby on Rails",
            "REST APIs", "GraphQL", "WebSockets", "gRPC", "Swagger/OpenAPI", "Webhooks", "Middleware",
            "Background Services", "API Design", "API Integration", "SignalR", "MediatR", "Hangfire", "Dapper",
            "RabbitMQ", "Apache Kafka", "File Upload"),

        .. Group(CategoriaCompetencia.Frontend,
            "HTML", "CSS", "Sass", "React", "Next.js", "Vue.js", "Nuxt.js", "Angular", "Svelte",
            "Tailwind CSS", "Bootstrap", "Material UI", "Styled Components", "Redux", "Zustand",
            "React Hook Form", "Zod", "Responsive Web Design", "Web Accessibility", "Axios", "Vite",
            "ESLint", "Prettier"),

        .. Group(CategoriaCompetencia.BancoDeDados,
            "MySQL", "PostgreSQL", "SQL Server", "Oracle Database", "SQLite", "MongoDB", "Redis", "Firebase",
            "Supabase", "DynamoDB", "Elasticsearch", "Prisma", "Knex.js", "Database Modeling",
            "Relational Databases", "NoSQL", "Database Migrations", "Query Optimization",
            "Índices de banco de dados", "Transações de banco de dados"),

        .. Group(CategoriaCompetencia.NuvemDevOps,
            "Docker", "Docker Compose", "Kubernetes", "GitHub Actions", "Azure DevOps", "AWS", "Microsoft Azure",
            "Google Cloud", "CI/CD", "Linux", "Nginx", "Terraform", "Ansible", "Jenkins", "Vercel", "Netlify",
            "Infrastructure as Code", "Reverse Proxy", "Environment Configuration", "GitLab CI/CD",
            "Azure Functions", "AWS Lambda", "Serverless", "Cloudflare", "Prometheus", "Grafana",
            "OpenTelemetry", "Observability"),

        .. Group(CategoriaCompetencia.TestesQualidade,
            "xUnit", "NUnit", "Jest", "Vitest", "React Testing Library", "Cypress", "Playwright", "Selenium",
            "Postman", "Unit Testing", "Integration Testing", "End-to-End Testing", "TDD",
            "Test Automation", "API Testing"),

        .. Group(CategoriaCompetencia.DadosIa,
            "Pandas", "NumPy", "Jupyter", "Power BI", "Microsoft Excel", "Machine Learning",
            "Artificial Intelligence", "Scikit-learn", "TensorFlow", "PyTorch", "OpenAI API", "Gemini API",
            "LangChain", "Data Analysis", "Data Visualization", "Prompt Engineering"),

        .. Group(CategoriaCompetencia.Mobile,
            "Android", "iOS", "React Native", "Flutter", ".NET MAUI"),

        .. Group(CategoriaCompetencia.Arquitetura,
            "Clean Architecture", "SOLID", "Design Patterns", "Domain-Driven Design", "MVC",
            "Microservices", "Layered Architecture", "CQRS", "Event-Driven Architecture", "Repository Pattern",
            "Software Architecture", "Dependency Injection"),

        .. Group(CategoriaCompetencia.Seguranca,
            "OWASP", "Authentication", "Authorization", "RBAC", "API Security", "Cryptography", "HTTPS/TLS",
            "Secure Coding", "CORS", "CSRF", "Input Validation", "Secrets Management", "JWT", "OAuth 2.0",
            "OpenID Connect"),

        .. Group(CategoriaCompetencia.Ferramentas,
            "Git", "GitHub", "GitLab", "Visual Studio", "Visual Studio Code", "Jira", "Trello",
            "npm", "pnpm", "Yarn"),

        .. Group(CategoriaCompetencia.PraticasEngenharia,
            "Code Review", "Refactoring", "Clean Code", "Debugging", "Performance Optimization", "Error Handling"),

        .. Group(CategoriaCompetencia.TecnologiaAplicada,
            "Arduino", "Raspberry Pi", "Internet das Coisas (IoT)", "MQTT", "Automação industrial", "CLP (PLC)",
            "ERP", "SAP", "TOTVS Protheus", "Odoo", "Geoprocessamento (GIS)", "QGIS", "Agricultura de precisão",
            "Prontuário eletrônico", "HL7/FHIR"),

        // Gestão de projetos e processos
        .. Group(CategoriaCompetencia.MetodologiasAgeis,
            "Agile", "Scrum", "Kanban", "Lean", "Scrum Master", "Product Owner", "Gestão de produtos"),

        .. Group(CategoriaCompetencia.GestaoProjetos,
            "Gestão de projetos", "PMBOK", "PMP", "Gestão de escopo", "Gestão de cronograma", "Gestão de riscos",
            "Gestão de orçamento de projetos", "Gestão de stakeholders", "Gestão de mudanças", "Gestão de portfólio",
            "Microsoft Project", "Asana", "Notion", "Planejamento estratégico"),

        .. Group(CategoriaCompetencia.ProcessosQualidade,
            "BPMN", "Mapeamento de processos", "Gestão de processos (BPM)", "Melhoria contínua", "PDCA",
            "Lean Manufacturing", "Six Sigma", "5S", "Gestão da qualidade", "ISO 9001", "OKR",
            "KPIs e indicadores", "Balanced Scorecard", "Análise de causa raiz"),

        // Inovação e empreendedorismo
        .. Group(CategoriaCompetencia.Inovacao,
            "Design Thinking", "Design Sprint", "Gestão da inovação", "Inovação aberta", "Propriedade intelectual",
            "Prototipagem", "Gamificação", "Pesquisa e desenvolvimento (P&D)", "Customer Discovery",
            "Validação de hipóteses"),

        .. Group(CategoriaCompetencia.Empreendedorismo,
            "Empreendedorismo", "Intraempreendedorismo", "Startups", "Lean Startup", "Business Model Canvas",
            "Modelagem de negócios", "Plano de negócios", "Análise de viabilidade", "Pitch", "MVP",
            "Pesquisa de mercado", "Captação de investimento"),

        // Negócios
        .. Group(CategoriaCompetencia.AdministracaoFinancas,
            "Gestão financeira", "Contabilidade", "Fluxo de caixa", "Planejamento financeiro", "Controle orçamentário",
            "Análise de custos", "Precificação", "Contas a pagar e receber", "Conciliação bancária",
            "Emissão de notas fiscais", "Rotinas fiscais", "Análise de demonstrações financeiras", "Matemática financeira"),

        .. Group(CategoriaCompetencia.VendasAtendimento,
            "Vendas consultivas", "Prospecção de clientes", "Negociação comercial", "CRM", "Atendimento ao cliente",
            "Pós-venda", "Customer Success", "Planejamento comercial", "Gestão de carteira de clientes",
            "Vendas online (e-commerce)"),

        .. Group(CategoriaCompetencia.PessoasRh,
            "Recrutamento e seleção", "Departamento pessoal", "Treinamento e desenvolvimento", "Gestão de desempenho",
            "Cultura organizacional", "Clima organizacional", "Onboarding", "Remuneração e benefícios",
            "Legislação trabalhista", "Employer branding"),

        .. Group(CategoriaCompetencia.LogisticaOperacoes,
            "Logística", "Gestão de estoque", "Cadeia de suprimentos", "Compras e suprimentos",
            "Planejamento e controle da produção (PCP)", "Gestão de armazéns", "Transporte e distribuição",
            "Gestão de frotas", "Importação e exportação"),

        // Marketing, design e comunicação
        .. Group(CategoriaCompetencia.MarketingDigital,
            "Marketing digital", "SEO", "Google Ads", "Meta Ads", "Marketing de conteúdo", "E-mail marketing",
            "Inbound Marketing", "Growth Marketing", "Google Analytics", "Mídias sociais", "Marketing de influência",
            "Planejamento de marketing"),

        .. Group(CategoriaCompetencia.Design,
            "UX Design", "UI Design", "Pesquisa com usuários", "Wireframing", "Design System", "Figma",
            "Design gráfico", "Branding", "Adobe Photoshop", "Adobe Illustrator", "Adobe InDesign", "Canva",
            "Edição de vídeo", "Adobe Premiere Pro", "Motion design", "Fotografia", "Design de produto"),

        .. Group(CategoriaCompetencia.Comunicacao,
            "Redação", "Copywriting", "Storytelling", "Produção de conteúdo", "Relações públicas",
            "Comunicação corporativa", "Assessoria de imprensa"),

        // Comportamentais (soft skills)
        .. Group(CategoriaCompetencia.ComunicacaoRelacionamento,
            "Comunicação", "Comunicação escrita", "Oratória e apresentações", "Escuta ativa", "Empatia", "Negociação",
            "Relacionamento interpessoal", "Orientação ao cliente", "Networking", "Assertividade"),

        .. Group(CategoriaCompetencia.LiderancaColaboracao,
            "Liderança", "Trabalho em equipe", "Colaboração", "Gestão de conflitos", "Mentoria", "Delegação",
            "Motivação de equipes", "Feedback construtivo", "Diversidade e inclusão"),

        .. Group(CategoriaCompetencia.PensamentoCriatividade,
            "Resolução de problemas", "Pensamento crítico", "Pensamento analítico", "Pensamento estratégico",
            "Criatividade", "Mentalidade inovadora", "Tomada de decisão", "Visão sistêmica", "Curiosidade",
            "Atenção a detalhes"),

        .. Group(CategoriaCompetencia.PosturaAutogestao,
            "Adaptabilidade", "Flexibilidade", "Resiliência", "Proatividade", "Autonomia", "Responsabilidade",
            "Comprometimento", "Ética profissional", "Organização", "Gestão do tempo", "Foco em resultados",
            "Aprendizado contínuo", "Inteligência emocional")
    ];

    public static readonly string[] CompetenciaNames = [.. Competencias.Select(x => x.Nome)];

    public static readonly string[] IdiomaNames =
    [
        "Português", "Inglês", "Espanhol", "Francês", "Alemão"
    ];

    private static IEnumerable<(string, CategoriaCompetencia)> Group(CategoriaCompetencia categoria, params string[] nomes) =>
        nomes.Select(nome => (nome, categoria));

    public async Task InitializeAsync()
    {
        await SeedCompetenciasAsync();
        await SeedIdiomasAsync();
    }

    // Adds missing competencies and keeps the category of the existing ones in sync with the catalog.
    private async Task SeedCompetenciasAsync()
    {
        var existing = await database.Competencias.ToDictionaryAsync(x => x.NomeBusca);
        foreach (var (nome, categoria) in Competencias)
        {
            var busca = NameNormalizer.Normalize(nome);
            if (existing.TryGetValue(busca, out var competencia))
                competencia.Categoria = categoria;
            else
                database.Competencias.Add(new() { Nome = nome, NomeBusca = busca, Categoria = categoria });
        }
        await database.SaveChangesAsync();
    }

    private async Task SeedIdiomasAsync()
    {
        var existing = await database.Idiomas.Select(x => x.NomeBusca).ToHashSetAsync();
        foreach (var nome in IdiomaNames)
        {
            var busca = NameNormalizer.Normalize(nome);
            if (!existing.Contains(busca))
                database.Idiomas.Add(new() { Nome = nome, NomeBusca = busca });
        }
        await database.SaveChangesAsync();
    }
}
