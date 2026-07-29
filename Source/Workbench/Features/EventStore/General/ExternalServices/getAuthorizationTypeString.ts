// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthorizationType } from 'Api/Security';
import strings from 'Strings';

export const getAuthorizationTypeString = (authorizationType: AuthorizationType) => {
    switch (authorizationType) {
        case AuthorizationType.none:
            return strings.eventStore.general.externalServices.authTypes.none;
        case AuthorizationType.basic:
            return strings.eventStore.general.externalServices.authTypes.basic;
        case AuthorizationType.bearer:
            return strings.eventStore.general.externalServices.authTypes.bearer;
        case AuthorizationType.OAuth:
            return strings.eventStore.general.externalServices.authTypes.oauth;
        default:
            return strings.eventStore.general.externalServices.authTypes.none;
    }
};
