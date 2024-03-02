// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useParams } from 'react-router-dom';

export interface RouteParams {
    microserviceId: string;
}

export const useRouteParams = () => useParams() as unknown as RouteParams;
