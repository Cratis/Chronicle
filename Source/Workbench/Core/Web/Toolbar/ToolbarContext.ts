// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { ToolbarDirection } from './ToolbarDirection';

export interface IToolbarContext {
    direction: ToolbarDirection;
}

export const ToolbarContext = React.createContext<IToolbarContext>({
    direction: ToolbarDirection.horizontal
});