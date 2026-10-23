<!-- cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/references/docker-images.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Docker images

## Chronicle kernel

Use the producer's current Dockerfile as the source for build inputs. For
`Docker/DevelopmentSlim/Dockerfile`, both published kernel output and Workbench
assets must exist before `docker build`. From the Chronicle root:

```bash
# Native arm64 daemon; use linux-x64 and out/x64 for an amd64 daemon.
dotnet publish Source/Kernel/Server/Server.csproj -c Release -r linux-arm64 \
  -p:PublishReadyToRun=true -p:DefineConstants=DEVELOPMENT \
  -o Source/Kernel/Server/out/arm64 \
  "-p:Version=$stamp" "-p:PackageVersion=$stamp"
cd Source/Workbench
yarn install --immutable
yarn build
cd ../..
docker build --platform linux/arm64 -f Docker/DevelopmentSlim/Dockerfile \
  -t "cratis/chronicle:$stamp-development-slim" .
docker image inspect "cratis/chronicle:$stamp-development-slim"
```

Run each heavy command through the host's queue wrapper when one exists. The
Workbench build produces `Source/Workbench/wwwroot`; do not substitute an empty
folder to get a green Docker build. Its build can regenerate tracked proxies.
Check the diff immediately; use an isolated worktree or revert only known
disposable generated drift before repeating. A broad checkout can destroy work.

Keep the development publish flags aligned with the producer's CI: the
`DEVELOPMENT` constant enables test reset APIs; ReadyToRun affects throughput.
The Docker copy script selects `out/arm64` or `out/x64` according to the image
architecture. Keep the publish RID, output folder and Docker platform aligned.
The development-slim image has no embedded MongoDB: the test/compose network must
supply it. `Docker/Development/Dockerfile` is the bundled-development alternative;
build it with `-f Docker/Development/Dockerfile` and tag it
`cratis/chronicle:$stamp-development`. Inspect its current inputs too; do not
substitute that tag for a fixture requiring `-development-slim`. Debug vs Release builds can differ materially in throughput; keep
Release for comparable kernel performance and state the actual architecture.

## Consumer selection

Studio integration specs select the local stamped kernel through:

```bash
export CHRONICLE_KERNEL_VERSION="$stamp"
# Run Studio's actual regression spec and integration gates in this shell.
```

Its fixture selects `cratis/chronicle:<version>-development-slim`. Do not put the
suffix in `CHRONICLE_KERNEL_VERSION` twice. Testcontainers can use the pre-existing
tag without a registry push. Inspect its logs to confirm that tag was started;
a custom pull policy may override local reuse. The image must be in the daemon
the test process actually uses, not merely another Docker context.

Chronicle Aspire hosting follows the package version for kernel image selection:
consume the stamped Aspire package too, and ensure its selected image suffix
exists locally. Inspect the generated resource/image configuration rather than
assuming Studio's environment override applies to Aspire.

Kotlin consumers with the kernel-image Gradle option can pass the full local tag:

```bash
./gradlew test "-PchronicleKernelImage=cratis/chronicle:$stamp-development-slim"
```

Confirm that the consumer build actually wires this property into its fixture;
Gradle can accept unused project properties without selecting another image.

Other consumers can use an untracked compose override with the stamped `image:`
value, or their fixture's documented image option. Run `docker compose config`
with the same compose files/options to inspect the resulting selection. Never
commit a local tag or push the image as part of local proof.

## Stop, restore, clean

After proof, stop task-owned containers, unset `CHRONICLE_KERNEL_VERSION`, remove
only task-owned compose overrides and run the helper's `restore` plus `check`.
Review `clean --stamp "$stamp"` and then `clean --stamp "$stamp" --apply`.
The helper lists matching tags and runs `docker rmi` without force; images still
in use can fail removal. Resolve the owning container instead of adding `-f`.
A Docker probe unavailable or over its deadline is a reported skip, not evidence
that no local tags remain. Do not replace this with global Docker pruning.
