// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The path prefix the Workbench is served under, normalized to either an empty string or a leading-slash path
 * with no trailing slash.
 *
 * The value comes from the `base-path` meta tag, which the server rewrites into `index.html` when it serves the
 * Workbench somewhere other than the root of its origin - behind a reverse proxy on a path prefix, for example.
 * Reading it from a module rather than a hook is what lets view models and other non-component code build the
 * same URLs the components do.
 */
export const basePath = normalizeBasePath(readConfiguredBasePath());

/**
 * Builds an absolute URL path for a Workbench route or endpoint, prefixed with {@link basePath}.
 * @param path The root-relative path, with or without a leading slash.
 * @returns The path to use against this origin.
 */
export function absolutePath(path: string): string {
    const relative = path.startsWith('/') ? path : `/${path}`;

    return `${basePath}${relative}`;
}

/**
 * Normalizes a configured base path to either an empty string, for the root, or a leading-slash path with no
 * trailing slash.
 * @param configured The value as configured, in whatever shape it was written.
 * @returns The normalized prefix.
 */
export function normalizeBasePath(configured: string | null | undefined): string {
    const trimmed = configured?.trim() ?? '';

    if (trimmed === '' || trimmed === '/') {
        return '';
    }

    const withLeadingSlash = trimmed.startsWith('/') ? trimmed : `/${trimmed}`;

    return withLeadingSlash.endsWith('/') ? withLeadingSlash.slice(0, -1) : withLeadingSlash;
}

function readConfiguredBasePath(): string {
    if (typeof document === 'undefined') {
        return '';
    }

    return (document.querySelector('meta[name="base-path"]') as HTMLMetaElement | null)?.content ?? '';
}
