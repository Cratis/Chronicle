// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverRunningState } from 'Features/Contracts/Observation';
import { ObserverHealth, summarizeObservers } from '../health';

const observer = (id: string, runningState: ObserverRunningState, next: number, tail: number) => ({
    id,
    runningState,
    nextEventSequenceNumber: next,
    tailEventSequenceNumber: tail
});

describe('when summarizing observers', () => {
    let health: ObserverHealth;

    beforeEach(() => {
        health = summarizeObservers([
            observer('caught-up', ObserverRunningState.active, 11, 10),
            observer('behind', ObserverRunningState.active, 6, 10),
            observer('replaying', ObserverRunningState.replaying, 0, 10),
            observer('suspended', ObserverRunningState.suspended, 11, 10),
            observer('disconnected', ObserverRunningState.disconnected, 11, 10),
            observer('quarantined', ObserverRunningState.quarantined, 11, 10),
            observer('empty-log', ObserverRunningState.active, 0, 2 ** 64)
        ]);
    });

    it('should count every observer', () => health.total.should.equal(7));
    it('should count the active observers', () => health.active.should.equal(3));
    it('should count the replaying observers', () => health.replaying.should.equal(1));
    it('should count the suspended observers', () => health.suspended.should.equal(1));
    it('should count the disconnected observers', () => health.disconnected.should.equal(1));
    it('should count the quarantined observers', () => health.quarantined.should.equal(1));
    it('should add up how far behind the tail the observers are, ignoring an unavailable tail', () => health.lag.should.equal(16));
});
