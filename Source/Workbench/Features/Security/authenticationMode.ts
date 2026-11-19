// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { absolutePath } from '../../Utils/basePath';

/**
 * How the Workbench's users sign in and out.
 * - `chronicle`: Chronicle signs users in with a username and password.
 * - `trustedProxy`: a reverse proxy in front of Chronicle signs users in; Chronicle trusts the user it forwards.
 */
export type AuthenticationModeKind = 'chronicle' | 'trustedProxy';

export interface AuthenticationMode {
    mode: AuthenticationModeKind;
    signOutUrl?: string;
}

const chronicle: AuthenticationMode = { mode: 'chronicle' };

/**
 * Asks the server how its users sign in. Servers that predate the endpoint, or any failure, mean Chronicle's own sign-in.
 * @returns The authentication mode.
 */
export const getAuthenticationMode = async (): Promise<AuthenticationMode> => {
    try {
        const endpoint = new URL(absolutePath('/.cratis/authentication'), location.origin);
        if (endpoint.origin !== location.origin) return chronicle;

        // SAFETY: endpoint is constructed against and explicitly restricted to the current browser origin.
        const response = await fetch(endpoint, { credentials: 'include' });
        if (!response.ok) return chronicle;

        const payload: unknown = await response.json();
        if (typeof payload !== 'object' || payload === null || !('mode' in payload) || payload.mode !== 'trustedProxy') return chronicle;

        const signOutUrl = 'signOutUrl' in payload && typeof payload.signOutUrl === 'string' && isSafeSignOutUrl(payload.signOutUrl)
            ? payload.signOutUrl
            : undefined;
        return { mode: 'trustedProxy', signOutUrl };
    } catch {
        return chronicle;
    }
};

const isSafeSignOutUrl = (url: string): boolean => {
    // Browsers read '//host' and '/\host' as another origin; only a path on this origin is a relative sign-out.
    if (url.startsWith('/')) return !url.startsWith('//') && !url.startsWith('/\\');
    try {
        const parsed = new URL(url);
        return parsed.protocol === 'https:' || parsed.protocol === 'http:';
    } catch {
        return false;
    }
};
