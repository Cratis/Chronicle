// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useContext } from 'react';
import { ApplicationModelContext } from '@cratis/applications.react';

export const useRelativePath = (path: string) => {
    const applicationModel = useContext(ApplicationModelContext);
    return `/${applicationModel.basePath}/${path}`.replace(/\/+/g, '/');
};
