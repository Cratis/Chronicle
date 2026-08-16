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
 * and it does not belong in the repository. Vite is configured with `envPrefix: 'CHRONICLE_'`, so this is
 * supplied as `CHRONICLE_PRIMEUI_LICENSE` - from a local `.env` when developing, and from a secret in CI.
 *
 * Leaving it unset is a supported state: the application runs, and the banner is the visible consequence.
 * That is deliberately not suppressed - hiding it would leave a licensing problem invisible.
 */
export const primeUiLicense: string | undefined = import.meta.env.CHRONICLE_PRIMEUI_LICENSE || undefined;
