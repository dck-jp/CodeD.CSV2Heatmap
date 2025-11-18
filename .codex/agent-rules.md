# AI Agent Core Rules (AGENTS.md)

This file defines **core mandatory rules** for all AI Agent operations.  
Detailed workflows (NuGet release, documentation updates, performance optimization) are in `docs/agents/`.

---

## 1. Branch Handling

- Always check current branch:
  `git branch --show-current`
- **Never modify `main` or `master`.**
  - Ensure local branch is up‑to‑date:
    - `git fetch origin`
    - `git status -uno`
    - If fast‑forwardable: `git pull --ff-only`
    - If not: report and wait for user instruction
- Before editing:
  - Ask: “Create a working branch? (feature/<task-slug>)”
  - Create only after approval:
    `git switch -c feature/<task-slug>`
- `<task-slug>`: 3–6 English words, lowercase, hyphen-separated.
- If switching/creation fails, show raw error and ask user.

---

## 2. Work Size & Planning

- If change exceeds **3 files** or **100 lines**, present a plan:
  1. Purpose  
  2. Files to edit  
  3. Main changes per file  
- Start only after user approval.

---

## 3. Minimal .NET Workflow

### With terminal permission
1. `dotnet format --verify-no-changes`
   - If formatting needed:  
     Ask permission → run `dotnet format`
2. `dotnet build`
3. `dotnet test`

### Without terminal permission
- Ask user to run the commands.

---

## 4. General Rules

- Ask if anything is unclear.
- Do not modify IDE artifacts (`.vscode/`, `.vs/`, `bin/`, `obj/`).
- Remove debug logs (`Console.WriteLine`, etc.) unless intended.
- Remove unused `using`.
- All files must be saved in **UTF-8 (no BOM)**.  
  If a file is in Shift_JIS or another encoding, convert it to UTF-8 before editing.
  
---

## 5. Detailed Workflows (Load On Demand)

For specialized tasks, load corresponding documents:

- NuGet release → `docs/agents/nuget-release.md`  
- Documentation updates → `docs/agents/docs-update.md`  
- Performance optimization → `docs/agents/perf-optimization.md`
