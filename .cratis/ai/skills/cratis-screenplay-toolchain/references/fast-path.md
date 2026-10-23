<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/references/fast-path.md -->
# Fast path: requirements to a running Cratis app

Run these in order and stop at the first failure; each step catches what the one before cannot.
`<root>` is the model folder, `<dir>` the render destination.

1. **Model** the requirements (`cratis-screenplay-modeling-lifecycle`). Enum members lowercase,
   specifications state every property, no optional event property, list queries `observable`
   (`references/traps.md` 46-47).
2. **Validate:** `cratis screenplay validate --executable --warnings-as-errors <root>`. Pass: exit 0 and no error.
   It binds the model but does not run the renderer's admission.
3. **Admit:** `cratis render <root> --target cratis --name <App> --destination <dir> --check`.
   It plans without writing; a refusal (for example `STAGE-ESM-005`, an optional event
   property) lists every blocker at once. Fix and repeat until it reports no error diagnostic. Exit 6 on a
   clean plan means only that changes are pending; nothing is published.
4. **Render:** the same command without `--check`. Publication succeeded when it exits 0 and the receipt shows
   `refused: 0` and `recoveryPending: false`.
5. **Build:** `dotnet build <dir>/<App>.csproj` (Debug).
6. **Services:** `docker compose up -d` in `<dir>` (Chronicle and MongoDB).
7. **Backend:** `dotnet run --project <App>.csproj` in `<dir>`.
8. **Frontend:** in `<dir>`, `yarn` then `yarn dev` (npm: `npm install`, `npm run dev`); open the URL Vite
   prints. It proxies API calls to the backend.

The rendered `README.md` repeats steps 6-8 for the pinned scaffold. Re-render and gap-fill rules:
`cratis-screenplay-render-and-gap-fill`.
