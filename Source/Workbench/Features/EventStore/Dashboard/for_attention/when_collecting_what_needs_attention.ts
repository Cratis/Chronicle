// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverRunningState } from 'Features/Contracts/Observation';
import { AttentionItem, collectAttention } from '../attention';

describe('when collecting what needs attention', () => {
    let items: AttentionItem[];

    beforeEach(() => {
        items = collectAttention(
            [
                { id: 'lagging', runningState: ObserverRunningState.active, nextEventSequenceNumber: 5, tailEventSequenceNumber: 9 },
                { id: 'quarantined', runningState: ObserverRunningState.quarantined, nextEventSequenceNumber: 10, tailEventSequenceNumber: 9 },
                { id: 'healthy', runningState: ObserverRunningState.active, nextEventSequenceNumber: 10, tailEventSequenceNumber: 9 }
            ],
            [
                { id: 'resolved', observerId: 'reactor', partition: 'first', isResolved: true, attempts: [] },
                { id: 'failing', observerId: 'reactor', partition: 'second', isResolved: false, attempts: [{ messages: ['Wrapper', 'Mail server unavailable'] }] }
            ],
            [
                { id: 'recommendation', name: 'Replay', description: 'The projection changed' }
            ],
            10);
    });

    it('should list the unresolved failed partition, quarantined observer, recommendation and lagging observer in that order', () =>
        items.map(_ => _.kind).should.deep.equal(['failedPartition', 'observer', 'recommendation', 'lagging']));
    it('should describe the failure by its innermost message', () => items[0].detail.should.equal('second · Mail server unavailable'));
    it('should say how far behind the lagging observer is', () => items[3].count!.should.equal(5));
    it('should not list the healthy observer', () => items.some(_ => _.subject === 'healthy').should.be.false);
});
