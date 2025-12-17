// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext } from 'react';
import { ArcContext } from '@cratis/arc.react';

export const useRelativePath = (path: string) => {
    const arc = useContext(ArcContext);
    return `/${arc.basePath}/${path}`.replace(/\/+/g, '/');
};
