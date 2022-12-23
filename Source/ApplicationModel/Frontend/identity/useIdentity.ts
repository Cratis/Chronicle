// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { IdentityProviderContext } from './IdentityProvider';
import { IIdentityContext } from './IIdentityContext';

export function useIdentity<TDetails = {}>(): IIdentityContext<TDetails> {
    return React.useContext(IdentityProviderContext) as IIdentityContext<TDetails>;
}
