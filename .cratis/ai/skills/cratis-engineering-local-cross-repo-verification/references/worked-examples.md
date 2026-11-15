<!-- cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/references/worked-examples.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Worked examples

## Contents

- Chronicle kernel → Studio integration specs
- Fundamentals → Arc (NuGet)
- Components → Studio frontend (npm)
- Chronicle clients → Direct (NuGet)
- Screenplay → Stage and Studio (NuGet and npm)
- Arc → Chronicle (NuGet)
- Fundamentals → Arc → Chronicle under one stamp

Run times below are rounded from one measured run on a shared machine; queue wait is not included.
Use absolute producer/consumer paths for your checkouts and the entry point's
`local` helper variable. These recipes define work to execute, not pre-verified
results. Substitute actual regression filters and CI commands; stop on a failed
step. Apply the entry point's check/restore/cleanup sequence to both repositories.


## Chronicle kernel → Studio integration specs

1. Reproduce Studio's integration bug against its released client/kernel. Record
   the regression filter and inspect Studio's CI build/test commands.
2. Generate the stamp and pack client packages plus their dependencies.
   A client solution filter can omit CodeAnalysis. Include its package explicitly:

   ```bash
   stamp=$(bash "$local" stamp "$producer")
   bash "$local" pack-nuget "$producer" "$stamp" path/to/client.slnf
   bash "$local" pack-nuget "$producer" "$stamp" \
     Source/Clients/DotNET.CodeAnalysis.Package/DotNET.CodeAnalysis.Package.csproj
   ```

   Create the filter in ignored scratch space with paths appropriate to that
   Chronicle checkout. Inspect the nuspec dependencies: Contracts and every other
   local dependency must exist in the feed at the same stamp. Do not proceed if a
   required package is missing simply because one pack command passed.
3. Publish the Release development kernel for the daemon's RID, build Workbench
   `wwwroot`, and build/inspect `cratis/chronicle:<stamp>-development-slim` as in
   [docker-images.md](docker-images.md). Preserve generated proxy edits.
4. Run `use-nuget` with the client IDs Studio needs. Include transitive IDs missing
   from Studio CPM explicitly. Source the printed env and use its generated NuGet
   config when source mapping requires it. Verify local assets after restore.
5. Select the image and run Studio's actual solution/spec project:

   ```bash
   export CHRONICLE_KERNEL_VERSION="$stamp"
   cd "$consumer"
   dotnet build path/to/Studio.slnx -c Release -p:StudioFullBuild=true -warnaserror
   ./<integration-project-dir>/bin/Release/<TFM>/<AssemblyName> <runner-specific-filter>
   ```

   Resolve the actual output and filter for `Source/Integrations/Specs/Specs.csproj`
   via [running-specs](../../cratis-specifications-csharp/references/running-specs.md).
   Without apphost use `dotnet <TargetPath.dll>`; only VSTest uses
   `dotnet test <project> -c Release --no-build --filter '<regression>'`.
   Inspect the current consumer workflow before claiming CI equivalence.
   Studio defaults kernel selection to the client version when the environment
   override is absent. Ensure logs show the stamped local image. Local Debug
   Studio builds skip analyzers; use `-p:StudioFullBuild=true` to match CI analyzer
   coverage. Then run all required consumer gates, not only the filtered spec.
6. If the bug remains, change Chronicle and repeat with a distinct stamp. If it
   passes, run `check` before commits/pushes, restore, unset overrides, check again,
   stop task-owned containers and review/apply stamp-scoped cleanup. Release the
   producer normally and bump Studio to released pins later; never commit local
   package versions or the local image selector.

In run time this loop takes minutes: client packs about 1–2 minutes, kernel
publish a few minutes, image build seconds with a warm cache, the Studio spec
project build about 6 minutes, and the spec class itself seconds. When the spec
fails because the local kernel refuses to start (for example a startup
requirement the consumer's container setup does not satisfy yet), that is a
real finding about the consumer, not about the overlay: read the container log
before changing the producer.

## Fundamentals → Arc (NuGet)

```bash
stamp=$(bash "$local" stamp "$fundamentals")
(cd "$fundamentals" && bash "$local" pack-nuget . "$stamp" Fundamentals.slnx)
bash "$local" use-nuget "$arc" "$stamp"
```

Gates (Arc `dotnet-build.yml`), run in this order after sourcing the printed env:
`yarn install --immutable`; in `Source/DotNET/Screenplay.Embedded`: `npm ci --workspaces=false`,
`npm run typecheck`, `npm test`, `npm run build`; `dotnet build --configuration Release
-p:CratisSpecsTargetFramework=net10.0`; then each project's evaluated Release/net10.0
spec executable (or `dotnet <TargetPath.dll>` without apphost) for each project that
`python3 scripts/ci-test-shards.py projects --shard specs-N` prints. Only identified
VSTest projects use `dotnet test <project> --no-build --configuration Release --framework
net10.0`. Inspect the current consumer workflow before claiming equivalence. Finish by listing Arc's `obj/project.assets.json` files for the stamp.

- Pass: the Fundamentals IDs in `project.assets.json` are at the stamp, build has 0 warnings and 0 errors, 18 spec projects run and none fail.
- Time: pack about 4 min, build about 6 min, the spec projects about 18 min.
- Pitfall: skipping the embedded viewer build makes four `Screenplay.Embedded.Specs` fail
  with a 404 for the viewer. It is the missing step, not the overlay.
- Pitfall: Arc's own `@cratis/fundamentals` npm line is out of scope for the NuGet stamp.
- Skip producer specs when the producer is unchanged `origin/main`; its CI covered that SHA.

For a JavaScript Fundamentals fix, run the producer's build and `pack-npm`, then `use-npm` in
Arc and install. Include every affected workspace tarball so no transitive copy stays released.

## Components → Studio frontend (npm)

```bash
(cd "$components" && yarn install --immutable && yarn workspace @cratis/components run ci && yarn build)
stamp=$(bash "$local" stamp "$components")
(cd "$components" && bash "$local" pack-npm . "$stamp")
bash "$local" use-npm "$studio" "$stamp"
(cd "$studio" && yarn install)        # not --immutable: the overlay edits the lock
yarn why @cratis/components
```

Gates: from `Source/<App>` run `yarn ci` for each Studio application that uses
`@cratis/components` (Core, Admin, Editor, EventModels, Viewer, ScreenMigration), `node
scripts/generate-scene-package-catalog.mjs --check`, then in Core `yarn build` and the
`initial-bundle-guard.mjs` budget check, and `yarn build-storybook` where the script exists.

- Pass: every installed `@cratis/components*` copy is at the stamp (`yarn why` and the installed `package.json` files),
  `yarn why` shows one resolution, and every gate above exits 0.
- Time: Components `ci` about 7 min, all-workspace build about 1 min, pack about 2 min,
  Studio install under 30 s, Studio gates about 8 min.
- Pitfall: `yarn workspace @cratis/components run ci` builds only the core package. Run
  `yarn build` at the Components root before `pack-npm`, or the adapter tarballs have no
  `dist` and Studio fails with `Failed to resolve entry for package
  "@cratis/components.primereact"`.
- Pitfall: the root `package.json` version is a placeholder, so the stamp base comes from tags.
  Resolutions bypass the prerelease ranges that npm would otherwise ignore.

## Chronicle clients → Direct (NuGet)

```bash
stamp=$(bash "$local" stamp "$chronicle")
(cd "$chronicle" && dotnet pack Source/Clients/Packages.slnf -c Release -p:PackageVersion=$stamp -p:Version=$stamp \
  -p:IsPackaging=true -p:DisableProxyGenerator=true -p:DisableDockerBuild=true -o "$CRATIS_LOCAL_FEED/nuget")
bash "$local" use-nuget "$direct" "$stamp"
```

Gates (Direct `build.yml`): `dotnet build Source/Direct/Direct.csproj -c Release
-p:DisableProxyGenerator=true`, the same in Debug with `-warnaserror`, then the project's
verified built Debug spec entrypoint. Build `Source/Assurance/Assurance.csproj` in the
matching configuration and run its verified spec entrypoint too; do not guess assembly
names or start an application server. See [running-specs](../../cratis-specifications-csharp/references/running-specs.md);
only VSTest uses `dotnet test <project> --no-build` in that configuration. Inspect the
current consumer workflow before claiming equivalence. Then check `project.assets.json`. Image-based specs also need the producer image tag.

- Pass for a consumer whose pins lag the producer: every Chronicle ID is in the feed at the
  stamp, the Release build with the owner's floors passes, and `project.assets.json` shows every
  stamped ID at the stamp. The Debug `-warnaserror` build and the specs are gated on the consumer owner's pin
  bump (Arc, Orleans, Screenplay, MongoDB.Driver) and on the analyzer fixes the newer pins
  bring. Record that dependency; do not edit the consumer's source or tracked pins.
- Time: pack about 5 min; the Release build about 2 min.
- Pitfall: pass pack properties as `-p:` arguments to `dotnet pack`, never
  as environment variables; `pack-nuget` takes none. An exported `IsPackaging=true` reaches Chronicle's nested
  `dotnet run` code generator and fails with a multi-target error.
- Pitfall: a consumer whose pins lag the producer's own pins fails with NU1109 or NU1605.
  That is the upgrade the consumer would need anyway; record the bump list.
- Pitfall: a released umbrella package can pull in a released package that is also stamped
  (here an AspNetCore package). Pin it with a direct reference or treat it as a finding.

## Screenplay → Stage and Studio (NuGet and npm)

```bash
stamp=$(bash "$local" stamp "$screenplay")
(cd "$screenplay" && yarn install && yarn workspaces foreach -Rt --from @cratis/screenplay-mcp-app run build \
  && bash "$local" pack-nuget . "$stamp" Screenplay.slnx && yarn build && bash "$local" pack-npm . "$stamp")
bash "$local" use-nuget "$stage" "$stamp"; bash "$local" use-nuget "$studio" "$stamp"
bash "$local" use-npm "$studio" "$stamp"       # at the workspace root, not the nested app
```

`closure` exits 2 for this chain until `dependency-map.tsv` has the Screenplay NuGet `ids` (it lists
only npm IDs); add them from the publish workflow first, then use the printed commands and its one
`verify-pack`.

Gates: Stage's frontend `npm ci`, test and build, then `dotnet build` in Debug and the repository's
own spec-shard helper; skip probes that need a live kernel or database and say so. Studio:
`dotnet build` of its solution in Debug with warnings as errors, then the frontend `yarn ci` and
`yarn build` of every workspace that consumes the language package.

- Pass: `project.assets.json` shows the stamp in both NuGet consumers and `yarn why` at the Studio workspace root shows one resolution; specs executed > 0.
- Pitfall: a repository without a committed lockfile fails `yarn install --immutable`; use plain
  `yarn install` there.
- Pitfall: yarn hoists to the root `node_modules`; check and install at the root.
- Pitfall: the packed language tarball must contain its built bundles. `pack-npm` uses `npm pack`;
  a package whose `files` lists a gitignored folder can ship only part of it. Compare the
  tarball file list with the package's `dist` folder.
- Pitfall: multi-hop. Studio keeps its released Stage; a binary break is a finding.

## Arc → Chronicle (NuGet)

```bash
stamp=$(bash "$local" stamp "$arc")
(cd "$arc" && bash "$local" pack-nuget . "$stamp" Arc.slnx)
bash "$local" use-nuget "$chronicle" "$stamp"
```

Gates: Chronicle `dotnet build` in Release with `DisableProxyGenerator=true` and
`DisableDockerBuild=true`, the non-integration specs; Workbench `yarn install` then `yarn ci`
with `NODE_OPTIONS=--max-old-space-size=4096`.

- Pass: Release build clean; every spec assembly passes; `project.assets.json` shows the stamp for
  every Arc ID.
- Finding when the consumer's other pins lag: the build passes, but the specs fail with
  `MissingMethodException` on a Fundamentals method the newer Arc calls, because Chronicle pins an
  older Fundamentals than Arc was built against. Record it; do not edit the consumer's pins.
- Pitfall: build the Arc embedded viewer before the .NET build, or the Arc build fails.
- Pitfall: Chronicle resolves Arc packages to the stamp only where its manifests pin them; a
  released package that depends on a released Arc package keeps that version. Record it.
- Pitfall: `use-npm` overlays only the consumer repository root `package.json` and has no option to
  target `Source/Workbench`, so this recipe does not put the Arc npm packages under the Workbench.
  To verify them there, run `pack-npm` in Arc, then add `file:` resolutions to the Workbench
  manifest by hand and run `yarn install`; do not claim the Workbench result without that step.
- Pitfall: the Workbench is its own Yarn project with its own lockfile. `restore` refuses to
  put back a changed lockfile; in a throwaway worktree check out the two manifests and remove the
  helper's backup folder.

## Fundamentals → Arc → Chronicle under one stamp

```bash
stamp=$(bash "$local" stamp "$arc")                 # one stamp, reused for every layer
(cd "$fundamentals" && bash "$local" pack-nuget . "$stamp" Fundamentals.slnx)
bash "$local" use-nuget "$arc" "$stamp"             # Arc builds against the new Fundamentals
(cd "$arc" && bash "$local" pack-nuget . "$stamp" Arc.slnx)
bash "$local" restore "$arc"
bash "$local" use-nuget "$chronicle" "$stamp"       # no IDs: every stamped Fundamentals and Arc ID
```

Gates: Chronicle Release build and the non-integration specs, as above.

Without package IDs `use-nuget` overrides every package in the feed carrying the stamp, so Chronicle
resolves Fundamentals at the stamp both directly and through Arc; naming only the Arc IDs would leave
its direct Fundamentals pin at the release and bring back the `MissingMethodException`.

- Pass: every spec assembly passes (no `MissingMethodException`), which the two-repository run does
  not achieve. A binary mismatch between layers disappears only when every layer carries the same stamp.
- Order: pack the upstream first, overlay it into the next repository, pack that, and so on;
  inspect the nuspec of each pack before overlaying it.
- A stamp is tied to the commit of the repository it was generated from. After changing any
  layer, generate a new stamp and repeat the chain.
- Time: each pack 15–30 minutes with queue waits; the Chronicle build and specs about 5 minutes
  of run time each.
