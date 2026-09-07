// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { WebhookDetails } from 'Features/Observation/Webhooks';
import { ExternalService } from 'Features/ExternalServices';
import { countSubscriptions } from '../dashboardAggregations';

describe('when counting subscriptions', () => {
    const webhooks = [new WebhookDetails(), new WebhookDetails()];
    const externalServices = [new ExternalService()];

    it('should sum webhooks and external services', () => countSubscriptions(webhooks, externalServices).should.equal(3));
});

describe('when counting subscriptions with none of either', () => {
    it('should be zero', () => countSubscriptions([], []).should.equal(0));
});
