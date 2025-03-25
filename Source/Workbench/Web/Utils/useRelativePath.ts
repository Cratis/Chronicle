// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMatch } from 'react-router-dom';

export const useRelativePath = (path: string) => {
    const resolvedPath = useMatch('/:basePath/*');
    if (!resolvedPath) {
        return path;
    }

    const basePath = resolvedPath.params.basePath;
    return `/${basePath}/${path}`.replace(/\/+/g, '/');
};
