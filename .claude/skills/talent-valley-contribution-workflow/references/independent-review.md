# Independent review request

Moved from `SKILL.md` section 5. Use it when your tool cannot spawn subagents.

Review request to give the person (fill the values):

> Review a Talent Valley change that is not committed yet. Read `AGENTS.md` first (the section "Working on a change" does not apply to you, you are only a reviewer), then `.agents/skills/talent-valley-quality-gate/SKILL.md`. Branch: `<branch>`, base: `origin/main`. Do not edit, commit, stash, fetch, checkout or create branches; only report. Run `git status` and `git diff $(git merge-base HEAD origin/main)` to see the changes, and read every new (untracked) file directly. Re-run the checks from `AGENTS.md` yourself, but for the database only run `dotnet tool restore` and then `dotnet ef migrations has-pending-model-changes` with `ASPNETCORE_ENVIRONMENT=Development`; never run `dotnet ef migrations add` or `remove`. Planned phase: `<where / what / checks>`. Draft report: `<report>`. Answer in pt-BR, with file and line for each finding: (1) was everything in the phase done and nothing outside its scope changed; (2) does every claim in the report match the changes and the real command output; (3) is the change compliant with `AGENTS.md` and the relevant skills; (4) bugs and risks; (5) code quality by SonarQube criteria (`talent-valley-quality-gate` section 7).
