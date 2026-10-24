// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AuthorizationType } from 'Features/Contracts/Security';
import strings from 'Strings';
import { getAuthorizationTypeString } from '../getAuthorizationTypeString';

describe('when formatting authorization types', () => {
    it('should label managed identity', () => getAuthorizationTypeString(AuthorizationType.managedIdentity).should.equal(strings.eventStore.general.webhooks.authTypes.managedIdentity));
    it('should preserve the none label', () => getAuthorizationTypeString(AuthorizationType.none).should.equal(strings.eventStore.general.webhooks.authTypes.none));
    it('should preserve the basic label', () => getAuthorizationTypeString(AuthorizationType.basic).should.equal(strings.eventStore.general.webhooks.authTypes.basic));
    it('should preserve the bearer label', () => getAuthorizationTypeString(AuthorizationType.bearer).should.equal(strings.eventStore.general.webhooks.authTypes.bearer));
    it('should preserve the OAuth label', () => getAuthorizationTypeString(AuthorizationType.OAuth).should.equal(strings.eventStore.general.webhooks.authTypes.oauth));
});
