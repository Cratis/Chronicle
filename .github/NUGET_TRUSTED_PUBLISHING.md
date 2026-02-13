# NuGet Trusted Publishing Setup

This repository uses NuGet trusted publishing to publish packages to NuGet.org securely without requiring API keys.

## How It Works

NuGet trusted publishing uses OpenID Connect (OIDC) to authenticate GitHub Actions workflows. When the workflow runs with the `id-token: write` permission, GitHub automatically provides an OIDC token that NuGet.org validates to authenticate the package push.

## Benefits

- **Improved security**: No need to store and manage API keys as secrets
- **Reduced maintenance**: No need to rotate API keys
- **Better audit trail**: OIDC tokens are tied to specific workflows and runs
- **Automatic token management**: GitHub handles token generation and expiration

## Configuration Required on NuGet.org

For trusted publishing to work, **the package owner must configure it on NuGet.org**:

1. Go to [NuGet.org](https://www.nuget.org/) and sign in
2. Navigate to your package's management page
3. Go to the "Trusted Publishers" section
4. Add a new trusted publisher with these details:
   - **Provider**: GitHub
   - **Owner**: `Cratis`
   - **Repository**: `Chronicle`
   - **Workflow**: `.github/workflows/publish.yml`
   - **Environment**: (leave blank, or specify if you add one)

## Workflow Configuration

The publish workflow (`.github/workflows/publish.yml`) has been configured with:

```yaml
permissions:
  # Required for NuGet trusted publishing using OIDC tokens
  id-token: write
```

And the push command no longer requires an API key:

```yaml
- name: Push NuGet packages
  run: dotnet nuget push --skip-duplicate '${{ env.NUGET_OUTPUT }}/*.nupkg' --timeout 900 --source https://api.nuget.org/v3/index.json
```

## Troubleshooting

If package publishing fails:

1. Verify that trusted publishing is configured correctly on NuGet.org for all packages
2. Check that the workflow file path matches exactly: `.github/workflows/publish.yml`
3. Ensure the repository owner and name are correct: `Cratis/Chronicle`
4. Verify that the job has `id-token: write` permission

## References

- [NuGet Trusted Publishing Documentation](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
- [GitHub OIDC Documentation](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/about-security-hardening-with-openid-connect)
