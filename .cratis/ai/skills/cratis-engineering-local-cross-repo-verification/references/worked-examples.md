<!-- cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/references/worked-examples.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Worked examples

Use absolute producer/consumer paths for your checkouts and the entry point's
`local` helper variable. These recipes define work to execute, not pre-verified
results. Substitute actual regression filters and CI commands; stop on a failed
step. Apply the entry point's check/restore/cleanup sequence to both repositories.

## Chronicle kernel → Studio integration specs

1. Reproduce Studio's integration bug against its released client/kernel. Record
   the regression filter and inspect Studio's CI build/test commands.
2. Generate the stamp and pack client packages plus their full dependency closure.
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
   dotnet test Source/Integrations/Specs/Specs.csproj -c Release --no-build \
     --filter '<regression>'
   ```

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

## Fundamentals → Arc

For a .NET Fundamentals fix, build/spec the producer, generate its stamp and
`pack-nuget` the affected projects and dependency closure. In Arc, run `use-nuget`
with the required Fundamentals package IDs, source the printed env/config, restore,
then build/spec Arc with its CI configuration. Inspect `project.assets.json` to
confirm the actual stamp. No image is required unless the reproducer needs one.

For a JavaScript Fundamentals fix, run the producer's normal build and
`pack-npm`, then `use-npm` in Arc and install. Run Arc frontend build/specs and the
reproducer. Include all affected workspace tarballs so a transitive dependency
cannot silently remain released. A green producer-only spec is not consumer proof.
Finish with the entry point's commit checks, restoration and cleanup.

## Components → Studio frontend

Build/spec Components using its own package scripts, including any required
Storybook gate. Then:

```bash
stamp=$(bash "$local" stamp "$producer")
bash "$local" pack-npm "$producer" "$stamp"
bash "$local" use-npm "$consumer" "$stamp"
cd "$consumer"
yarn install
```

Confirm the Components `file:` tarball resolutions. Run Studio's frontend build,
lint/specs and exercise the broken dialog/page. If Vite serves stale assets, stop
the dev server, clear only its `.vite` prebundle and restart. Avoid `link:` and
`portal:` because duplicate React/context instances distort the reproduction.
Overlay-only lockfile changes from Yarn install are restored automatically.
Review npm backups if unrelated changes cause `restore` to refuse; preserve
that work, restore only local changes and retry. Reinstall released packages,
run `check`, and review/apply cleanup for this stamp.
