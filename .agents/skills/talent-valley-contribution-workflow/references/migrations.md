# Database migrations (one per pull request)

Moved from `SKILL.md` section 3 without changing the rules. Read it only when entities, configurations or `AppDbContext` change.

Applies when entities, configurations or `AppDbContext` change. The rules of `talent-valley-database` still apply. Warn the person clearly that the change touches the database.

1. A pull request adds **at most one** new migration.
2. Bring the branch up to date with `origin/main` so you see every migration already merged by others. Do it at the start of the phase, **before** editing anything, while the working tree is clean: `git fetch origin`, then `git rebase origin/main` if the branch was never pushed, or `git merge origin/main` if it was already pushed (check with `git ls-remote --heads origin <branch>`; empty output means never pushed). Never force-push to make a rebase work. A merge creates a commit: ask the person to approve it first. If the working tree is not clean when you need this step, stop and ask the person; do not commit, stash or discard changes on your own.
3. Generate the migration with the commands in `AGENTS.md` ("Checks before saying a task is done"), with `ASPNETCORE_ENVIRONMENT=Development`. `dotnet ef migrations has-pending-model-changes` must report no pending changes. If the model changes again after you generated the migration, do not add a second one: remove yours as in step 6 (items 2 and 3) and generate it again.
4. Never edit, rename, delete or regenerate a migration that is already in `origin/main`, unless the maintainer decides it (`AGENTS.md`, "Ask the maintainer first"). To see which migrations are in `main`: `git ls-tree --name-only origin/main backend/TalentValley.Api/Data/Migrations/`.
5. Never edit the model snapshot by hand. Only the EF tool changes it.
6. **If another pull request with a migration reaches `main` after you generated yours** (updating your branch conflicts on the snapshot), do not resolve the conflict by hand. If the update works without a conflict, still re-run `dotnet build backend/TalentValley.slnx` and `dotnet ef migrations has-pending-model-changes`. If the latter reports changes, do not undo the update (no `git reset`); skip item 1 below and follow items 2 to 4 only when the newest migration is yours and is not in `origin/main`; otherwise stop and show the real output to the person. These are the items:
   1. When there is a conflict, cancel the update: `git rebase --abort` or `git merge --abort`.
   2. List the migrations of your branch (`ls backend/TalentValley.Api/Data/Migrations/`) and check, with the command in step 4, that the newest one by timestamp is yours and is **not** in `origin/main`. `dotnet ef migrations remove` always removes the newest one, so removing a migration that is in `main` would break rule 4.
   3. From the repository root, with `ASPNETCORE_ENVIRONMENT=Development`, run `dotnet tool restore` and `dotnet ef migrations remove --project backend/TalentValley.Api`. If it fails (for example because the migration was applied to your local database), stop and show the real error to the person.
   4. Ask the person to approve a commit of the removal, update the branch with `origin/main` as in step 2, and generate the migration again (step 3).

   If anything is unclear, stop and ask.

