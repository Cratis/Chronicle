// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { IdentityProviderContext } from './IdentityProvider';
import { IIdentityContext } from './IIdentityContext';

/**
 * Hook to get the identity context.
 * @param defaultDetails Optional default details to use if the context is not set.
 * @returns An identity context.
 */
export function useIdentity<TDetails = {}>(defaultDetails?: TDetails | undefined | null): IIdentityContext<TDetails> {
    const context = React.useContext(IdentityProviderContext) as IIdentityContext<TDetails>;
    if (context.isSet === false && defaultDetails !== undefined) {
        context.details = defaultDetails!;
    }
    return context;
}
