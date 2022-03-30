// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { CommandTrackerContext } from './CommandTracker';

export function useCommandTracker() {
    return React.useContext(CommandTrackerContext);
}
