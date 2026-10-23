<!-- cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/references/other-environments.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Other developer machines and agent hosts

The corpus has no dependency on a particular home directory, queue runner or
Docker desktop application. Execute the same helper from the installed skill.
Use macOS or Linux with bash/git; Windows requires WSL. Add the toolchains selected
by the recipe and check disk capacity before packing or building images.

## Workspace-local feed

On an ephemeral agent host, use an absolute ignored workspace directory:

```bash
export CRATIS_LOCAL_FEED=/absolute/workspace/.ai-work/local-feed
# No CRATIS_LOCAL_RUN is needed when this host has no queue wrapper.
stamp=$(bash "$local" stamp "$producer")
```

Keep the feed available for the entire consumer restore/test. `use-nuget` and
`use-npm` record absolute paths, so do not move the workspace halfway through.
Use the default per-worktree session inside the Git directory, or set
`CRATIS_LOCAL_SESSION_DIR` to a unique ignored/external directory for that checkout.
Do not share a session among consumers. Config at
`${XDG_CONFIG_HOME:-$HOME/.config}/cratis/local-packages.env` is trusted shell code;
on a host with no config, environment variables suffice. Do not put secrets in it.

## Images and architecture

A local tag must be loaded into the daemon used by the consumer tests. A remote
builder or separate job daemon does not automatically share images. When necessary,
transfer a task-owned `docker save` archive to the test job and `docker load` it
there without pushing to a registry. Inspect the loaded tag and architecture,
then run the actual container spec. Align `linux-arm64` publish with arm64 images,
or `linux-x64` with amd64. Report any emulation and Release/Debug difference when
comparing timings. Workspace sharing alone does not share a Docker image store.

## Optional queue adapter

`CRATIS_LOCAL_RUN` names an executable accepting `<kind> <label> -- <command...>`.
The helper sends pack operations through it. A host may choose its own bounded
queue and execution deadlines; do not copy another machine's process limits.
Separately wrap consumer builds, Docker builds and container specs according to
that host's policy. Queue timeout, cancellation and skipped checks are not passes.
No wrapper is installed or configured by this skill.

## Finish even on a failed proof

Run `check` before any consumer commit/push. Restore overlays, unset shell/IDE and
image selections, reinstall released dependencies where appropriate, then check
again. Review `clean "$consumer" --stamp "$stamp"` and apply only its expected
subjects. Stop task-owned containers first. Resolve npm backup conflicts rather
than bypassing them. Report unreachable Docker/Yarn cleanup as incomplete.

If a job is cancelled, preserve its session/backups and record what still needs
restoration rather than assuming workspace disposal cleaned a shared feed, global
NuGet cache or remote daemon. Do not delete artifacts another active task needs.
After successful local proof, the normal authorized producer release and consumer
released-version pin bump remain necessary.
