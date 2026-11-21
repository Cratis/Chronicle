# Cratis Workbench

## Prerequisites

## Building

## Regenerating Workbench proxies

After changing a Core command, query, or read model exposed to Workbench, regenerate the Arc TypeScript proxies from the repository root:

```sh
dotnet build Source/Kernel/Core/Core.csproj -c Debug -p:DisableProxyGenerator=false
```

Use Debug so development-only proxies are included, and leave `OutputPath` unset: an absolute output path disables proxy generation. Review and commit the generated changes under `Source/Workbench/Features`; do not edit generated files by hand. CI does not regenerate these proxies or check their drift.

After staging the generated changes, run the local check from the repository root:

```sh
node Source/Workbench/scripts/check-proxies.mjs
```

The check rebuilds Core, compares its output with the Git index, and exits non-zero on unstaged changes or untracked files under `Features`. It also reports unstaged edits present before regeneration, even if the build repairs them. Stage feature changes before running it; if it finds drift, review and stage the regenerated proxies, then run it again. Commit the verified changes afterward.

To exercise the drift detector without building Core:

```sh
node Source/Workbench/scripts/check-proxies.mjs --self-test
```

## Components

https://primereact.org
https://allotment.mulberryhousesoftware.com

## Icons

https://react-icons.github.io/react-icons/icons/md/

## MVVM

https://mobx.js.org

### Dependency Inversion - IoC

https://github.com/microsoft/tsyringe

## Structure
