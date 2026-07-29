// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExternalServiceEndpointType } from 'Api/ExternalServices';
import strings from 'Strings';

export const getEndpointTypeString = (endpointType: ExternalServiceEndpointType) => {
    switch (endpointType) {
        case ExternalServiceEndpointType.http:
            return strings.eventStore.general.externalServices.endpointTypes.http;
        case ExternalServiceEndpointType.msSql:
            return strings.eventStore.general.externalServices.endpointTypes.msSql;
        case ExternalServiceEndpointType.postgreSql:
            return strings.eventStore.general.externalServices.endpointTypes.postgreSql;
        default:
            return strings.eventStore.general.externalServices.endpointTypes.http;
    }
};
