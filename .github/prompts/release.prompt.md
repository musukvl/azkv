---
description: "Prepare and publish azkv release (notes, tag, GitHub release, Homebrew formula update)."
name: "release"
argument-hint: "<version> (example: 1.0.4)"
agent: "agent"
---
Prepare a full release for version `${input:version:1.0.4}`.

Treat the input as the target semantic version (for example `1.0.4`). If extra text is provided, extract the first valid semantic version token and use it.

Workflow:

1. Validate prerequisites in `azure-kv-viewer`:
- Ensure the working tree is clean or only contains expected release files.
- Ensure required tools are available (`git`, `gh`, `dotnet`).

2. Build release notes:
- Determine commits since the latest tag before the target version.
- Create `releases/v<version>.md` in the same style as previous release notes.
- Include user-facing changes, install/update instructions, and direct download links for `v<version>` artifacts.

3. Commit and push release source commit:
- Add and commit `releases/v<version>.md` on `main`.
- If `releases/` is ignored, use force add for that file only.
- Push `main` to `origin`.
- Capture the commit SHA that will be tagged for this release.

4. Run release script:
- Execute: `./release.sh <version> releases/v<version>.md`.
- If interactive confirmation appears, continue the release.
- Ensure it completes successfully (artifacts built, tag pushed, GitHub release created).

5. Verify tag and release:
- Verify git tag `v<version>` points to the release source commit from step 3.
- Verify GitHub release `v<version>` exists and is not draft/prerelease.

6. Update Homebrew tap:
- Open local repo `../homebrew-azkv`.
- Update `Formula/azkv.rb` with `version "<version>"`.
- Update macOS ARM64, macOS x64, and Linux x64 URLs and SHA256 values to the newly published release assets.
- Commit and push to `origin/main` in `homebrew-azkv`.

7. Final report:
- Return a concise summary containing:
  - release notes file path,
  - azkv release commit SHA,
  - tag SHA and release URL,
  - homebrew commit SHA,
  - any warnings or recovery actions taken.

Execution rules:
- Do not rewrite or revert unrelated local changes.
- If a step fails, diagnose and retry with a safe fix, then continue.
- Keep changes minimal and scoped to release-related files only.
