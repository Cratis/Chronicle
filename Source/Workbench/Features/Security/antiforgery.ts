// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { absolutePath } from '../../Utils/basePath';

const antiforgeryHeaderName = 'X-CSRF-TOKEN';
let requestToken = '';

export const getAntiforgeryHeaders = (): Record<string, string> =>
    requestToken ? { [antiforgeryHeaderName]: requestToken } : {};

export const clearAntiforgeryToken = () => {
    requestToken = '';
};

export const refreshAntiforgeryToken = async (): Promise<void> => {
    const endpoint = new URL(absolutePath('/.cratis/antiforgery'), location.origin);
    if (endpoint.origin !== location.origin) {
        throw new Error('The request protection endpoint must be same-origin.');
    }

    // SAFETY: endpoint is constructed against and explicitly restricted to the current browser origin.
    const response = await fetch(endpoint, {
        credentials: 'include',
    });
    if (!response.ok) {
        clearAntiforgeryToken();
        throw new Error('Failed to establish request protection.');
    }

    const payload: unknown = await response.json();
    if (
        typeof payload !== 'object' ||
        payload === null ||
        !('requestToken' in payload) ||
        typeof payload.requestToken !== 'string' ||
        !payload.requestToken
    ) {
        clearAntiforgeryToken();
        throw new Error('The request protection response was invalid.');
    }

    requestToken = payload.requestToken;
};
