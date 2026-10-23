<!-- cratis-ai-managed: skills/cratis-engineering-local-cross-repo-verification/references/npm-yarn.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# npm and Yarn

## Pack the package, not a link

Requires node, tar and Yarn 4. Run the producer's normal build first. The helper does
not run a separate build; Yarn's normal pack lifecycle hooks still run.

```bash
stamp=$(bash "$local" stamp "$producer")
bash "$local" pack-npm "$producer" "$stamp"
```

It requires clean tracked workspace manifests, temporarily changes all non-private
workspace versions and packs actual tarballs. It restores those manifests on
success and failure; inspect diffs for other lifecycle-generated output. Yarn's
`%s-%v.tgz` output slugifies a scoped name (`@cratis/foo` becomes `@cratis-foo`).
Inspect dependency ranges in packed manifests: the helper changes versions, not
arbitrary inter-package dependency constraints. Include every required workspace
and verify the consumer resolves the intended tarballs.

Do not use `portal:` or `link:` for this proof: filesystem package boundaries can
create duplicate React instances or mismatched context providers, and may hide
missing packed files. A tarball exercises what a downstream install receives.

## Consumer overlay

```bash
bash "$local" use-npm "$consumer" "$stamp"
cd "$consumer"
yarn install
# Run the repository's frontend build, lint, specs and reproducing UI action.
```

The helper selects every tarball ending in `-<stamp>.tgz` in the npm feed, reads
each packed manifest for its package name, and adds root `resolutions` using
absolute `file:` tarball paths. This also overrides transitive packages absent
from consumer manifests, rather than silently falling back to published versions. It backs
up root `package.json`, `yarn.lock` if present, and the pre-existing diff in the
per-worktree session. It refuses another active npm backup. Check the install
output/lockfile for the selected local versions; a successful install alone is not
proof that the changed code was exercised.

If Vite still serves the old package, stop the dev server and clear only the
consumer's stale `node_modules/.vite` prebundle, then restart it. Never edit
`node_modules` files to implement the fix. `use-npm` does not run install or clear
caches itself. Unique stamps avoid tarball cache collisions.

## Restore without discarding work

```bash
bash "$local" restore "$consumer"
bash "$local" check "$consumer"
```

Restoration compares the manifest with the saved original and overlay. Only the
overlay's own resolution values may differ; unrelated manifest edits cause a
refusal. Formatting alone is not a conflict. For Yarn 4 lockfiles, unchanged
entries, metadata and headers must remain byte-identical. The guard accepts
entries resolving to the exact overlay tarballs (including Yarn locator/hash
suffixes) and removal of old entries for those same package names. Those local
package blocks are treated as overlay-owned, including checksums and dependencies.
Thus the normal `use-npm` → `yarn install` → `restore` flow puts back both original
files, including edits that existed before `use-npm`.

Unrelated lockfile edits, metadata/install churn, unsupported lockfile syntax, or
a newly created/deleted lockfile require manual review. The helper restores
neither file and retains backups on a conflict. Read the saved before-diff and
files, undo only local overlay changes manually, and retry. Merge later user
changes instead of replacing them with the backup. Do not blindly checkout the
consumer manifests. Reinstall released dependencies after restoration, then run
`check` before any commit/push.

Review `clean "$consumer" --stamp "$stamp"` before `--apply`. Passing the consumer
allows Yarn cache discovery; no consumer or unavailable Yarn yields a reported
skip. Unresolved backups block cleanup rather than silently losing recovery data.
