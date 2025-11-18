# NuGet Release Workflow

This workflow applies only when the user explicitly requests a NuGet release.

## 1. Pre‑release Verification
- Ensure README/USAGE matches current behavior.
- Update CHANGELOG if needed.
- Run all tests successfully.

## 2. Metadata Confirmation
Show user:
- `<PackageId>`
- `<Version>`
- `<Authors>`
- `<Description>`
and confirm correctness.

## 3. Local Packaging Test
- `dotnet pack`
- Create temp project:
  `dotnet new console -o ../TempTestApp`
- Add package from local nupkg:
  `dotnet add ../TempTestApp package <PackageName> --source ../<path>`
- Remove temp folder after user confirmation.

## 4. License & Vulnerability Check
- Ensure LICENSE/SPDX matches README.
- `dotnet list package --vulnerable`

## 5. Release Commit
Follow Conventional Commits. Avoid creating new version numbers unless user instructs.
