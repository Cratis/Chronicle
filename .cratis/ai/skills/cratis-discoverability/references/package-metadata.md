<!-- cratis-ai-managed: skills/cratis-discoverability/references/package-metadata.md -->
# Package metadata

## Contents

- Field map
- Writing the description
- Repository and distribution surfaces
- Verification
- Pitfalls

## Field map

| Field | NuGet (`.csproj`, `Directory.Build.props`) | npm (`package.json`) |
| --- | --- | --- |
| Description | `<Description>`, 80-250 characters; the first 100 are what shows | `description`, 80-160 characters |
| Tags | `<PackageTags>`, 5-10, `cratis` plus category terms | `keywords`, the same list |
| Project URL | `<PackageProjectUrl>` = `https://www.cratis.io/<product>/` | `homepage`, the same |
| Repository | `RepositoryUrl` | `repository` |
| Authors | `<Authors>Cratis</Authors>` (plural) | `author` `Cratis` |
| Company | `<Company>Cratis</Company>` | No standard field; do not invent one |
| License | `PackageLicenseExpression` `MIT` | `license` `MIT` |
| Readme | A packed README with the same opener | `README.md` |

Use a `https://www.cratis.io/<product>/` page that exists; check the docs site rather than guessing
a slug. If none exists, leave the existing URL and report it.

## Writing the description

One sentence: what it is, for whom, and which Cratis product it belongs to - "<what it is> for
<audience>, part of Cratis <Parent>". Name the parent so the package disambiguates ("the client for
Cratis Chronicle, the open-source event-sourcing database"). The text equals the README opener and
the GitHub About text. Tags are specific ("event-sourcing", "cqrs", "dotnet"); never "utilities".

## Repository and distribution surfaces

- Set GitHub About's description to the same sentence, its website to the product docs page on
  `https://www.cratis.io`, and topics to `cratis` plus accurate category terms.
- Docker Hub's short description defines the image and names the parent product. Its full
  description explains what it runs, how to start it and where the product docs live; keep the
  definition and links aligned with the README. Do not add unsupported adoption or performance claims.
- Analyzer `helpLinkUri` values link to the relevant diagnostic documentation on
  `https://www.cratis.io`; keep the diagnostic path, not just the product homepage.
- Template post-action messages and README footers link to the relevant docs on
  `https://www.cratis.io`. Check nested READMEs and image READMEs, not just the repository root.
- Preserve repository URLs as repository URLs. Do not rewrite schema identifiers, test fixtures,
  negative examples or managed corpus copies while fixing user-facing links.

## Verification

Inspect effective NuGet properties on a packable project, including inherited values:

```sh
dotnet msbuild <packable.csproj> -getProperty:Authors -getProperty:Company -getProperty:Description -getProperty:PackageProjectUrl
```

A pass shows `Cratis` for Authors and Company, a nonempty parent-product definition and the
existing product docs URL. Check npm `author`, `description`, `homepage` and `keywords` against the
field map. Request each changed www URL and require HTTP 200 at the intended destination; fix
broken paths rather than merely swapping hosts. Inspect GitHub About and Docker Hub metadata
where available. Report remote metadata that cannot be checked or needs owner approval to change;
a local README edit is not proof that a registry or repository setting changed.

## Pitfalls

- An empty `Description`.
- Mixed `Authors` values across packages of one product.
- `PackageProjectUrl` pointing only at GitHub.
- Two packages with near-identical descriptions. Where `@cratis/arc` (the browser client of Arc
  .NET) and `@cratis/arc.core` (the Node server package) could be confused, each description says
  which one it is.
