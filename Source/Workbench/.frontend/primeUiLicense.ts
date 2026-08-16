// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The PrimeUI license key, read from the environment.
 *
 * PrimeReact 11 is PrimeTek's commercial PrimeUI rather than the MIT of version 10, and it verifies a key
 * when its provider mounts - unconditionally, with no dependence on styling or on `NODE_ENV`. Without one
 * it logs a warning and injects an "Invalid PrimeUI License" banner, in development and in production
 * alike.
 *
 * The key is configuration, not source: it differs between a contributor's machine and a release build,
 * and it does not belong in the repository. Vite exposes the `PRIMEUI_` prefix, so it is supplied under
 * PrimeTek's own name, `PRIMEUI_LICENSE`, rather than a Chronicle-specific copy of it. Vite reads the
 * process environment and not only `.env` files, so one export in a shell profile serves every Cratis
 * application on this machine, and the identical name works as a CI secret.
 *
 * Leaving it unset is a supported state: the application runs, and the banner is the visible consequence.
 * That is deliberately not suppressed - hiding it would leave a licensing problem invisible.
 */
export const primeUiLicense: string | undefined = import.meta.env.PRIMEUI_LICENSE || undefined;
