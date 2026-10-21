// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { isWebhookGone } from '../webhookGone';

describe('when identifying a stopped webhook', () => {
    it('should identify its terminal incident', () =>
        isWebhookGone([{ condition: 'webhook-gone', target: { eventSequenceId: 'log' } }], 'log').should.be.true);
    it('should not mistake another event sequence for this webhook', () =>
        isWebhookGone([{ condition: 'webhook-gone', target: { eventSequenceId: 'other' } }], 'log').should.be.false);
    it('should not mistake a retryable partition failure for a stopped webhook', () =>
        isWebhookGone([{ condition: 'partition-failing', target: { eventSequenceId: 'log' } }], 'log').should.be.false);
    it('should not show retirement after its incident is cleared', () =>
        isWebhookGone([], 'log').should.be.false);
});
