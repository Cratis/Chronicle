# GH_PROJECT_TOKEN Setup

The [issue-analysis workflow](workflows/issue-analysis.yml) uses two separate tokens:

| Token | Source | Used for |
|-------|--------|----------|
| `github.token` | Built-in `GITHUB_TOKEN` | Listing issues and pull requests via `gh issue list` / `gh pr list` |
| `GH_PROJECT_TOKEN` | Repository secret (PAT) | Updating lanes in the org-level GitHub Projects V2 board |

`GITHUB_TOKEN` is scoped to the repository and **cannot** access org-level Projects V2, regardless
of the `permissions` block in the workflow. A Personal Access Token (PAT) with the `project` scope
is the only supported mechanism for GraphQL mutations against `ProjectV2`.

## Why a Separate Token?

GitHub Projects V2 lives at the organization level. The automatic `GITHUB_TOKEN` generated for each
workflow run is repository-scoped; it has no access to org resources. A classic PAT (or a
fine-grained PAT) that belongs to a user with write access to the project is required.

## Creating the PAT

### Option A — Classic PAT (simplest)

1. Sign in to GitHub as a user who has **write access** to
   [Cratis Projects #7](https://github.com/orgs/Cratis/projects/7).
2. Go to **Settings → Developer settings → Personal access tokens → Tokens (classic)**.
3. Click **Generate new token (classic)**.
4. Give it a descriptive name, e.g. `Chronicle issue-analysis (project write)`.
5. Set the expiration to a reasonable period (90 days, 1 year, or no expiration — your policy).
6. Under **Select scopes**, tick **`project`** (the full `project` group, which includes read and
   write access to org projects).
7. Click **Generate token** and copy the token value immediately — it is shown only once.

### Option B — Fine-Grained PAT

Fine-grained PATs do not yet support Projects V2 write access at the organization level; use a
classic PAT (Option A) until GitHub adds that capability.

## Storing the Token as a Repository Secret

1. In the **Chronicle** repository, go to
   **Settings → Secrets and variables → Actions → Secrets**.
2. Click **New repository secret**.
3. Set **Name** to `GH_PROJECT_TOKEN`.
4. Paste the token value into the **Secret** field.
5. Click **Add secret**.

> **Tip:** If the token will be reused across multiple repositories in the `Cratis` organization,
> store it as an **organization secret** instead (Organization → Settings → Secrets and variables →
> Actions) and grant access to the `Chronicle` repository.

## Verifying the Setup

1. Navigate to **Actions → Issue Analysis** in the repository.
2. Click **Run workflow** to trigger a manual run.
3. Open the run log and look for the step **Run issue analysis and update project**.
4. A successful run that updates the project board will contain:
   ```
   ==> Using GH_PROJECT_TOKEN for org project operations.
   ==> Updating GitHub Project (org=Cratis, project=7) …
   ```
5. If the secret is absent or blank you will instead see:
   ```
   ==> WARNING: GH_PROJECT_TOKEN is not set.
       GitHub Project V2 update requires a Personal Access Token (PAT)
       with the 'project' scope, stored as the GH_PROJECT_TOKEN secret.
       Issue classification completed but project was NOT updated.
   ==> Done (project update skipped – GH_PROJECT_TOKEN not configured).
   ```
   This is a graceful degradation — issue classification still runs and `IssueAnalysis.md` is
   still updated; only the project board update is skipped.

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| Warning: `GH_PROJECT_TOKEN is not set` | Secret not created | Follow the steps above |
| `GraphQL error: Could not resolve to a ProjectV2` | Wrong `PROJECT_NUMBER` in the workflow | Check that `PROJECT_NUMBER: 7` matches the org project URL |
| `Resource not accessible by integration` | PAT does not have the `project` scope | Regenerate the PAT and tick the `project` scope |
| PAT expired | Token expiry reached | Generate a new PAT and update the secret |

## References

- [GitHub Projects V2 GraphQL API](https://docs.github.com/en/graphql/reference/objects#projectv2)
- [Creating a personal access token (classic)](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#creating-a-personal-access-token-classic)
- [Using secrets in GitHub Actions](https://docs.github.com/en/actions/security-guides/using-secrets-in-github-actions)
