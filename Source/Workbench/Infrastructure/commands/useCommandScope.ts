// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { CommandScopeContext } from './CommandScope';

export function useCommandScope() {
    return React.useContext(CommandScopeContext);
}
