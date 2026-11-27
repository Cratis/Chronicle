// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverLagStatus } from 'Features/Observation';
import { presentObserverLag } from '../../observerLagPresentation';
import { during, lag } from '../given';

describe('when presenting a measured observer', () => {
    it('should show caught up with a distance of zero', () => {
        const display = presentObserverLag(lag({ status: ObserverLagStatus.caughtUp, distance: 0 }), during)!;
        display.state.should.equal('caughtUp');
        display.distance!.should.equal('0');
    });

    it('should show lagging with its distance', () => {
        const display = presentObserverLag(lag({ distance: 17 }), during)!;
        display.state.should.equal('lagging');
        display.distance!.should.equal('17');
    });

    it('should show stalled', () => presentObserverLag(lag({ status: ObserverLagStatus.stalled, distance: 3 }), during)!.state.should.equal('stalled'));

    it('should not show a distance of zero when none was measured', () => {
        (presentObserverLag(lag({ status: ObserverLagStatus.caughtUp }), during)!.distance === undefined).should.be.true;
    });

    it('should not derive a distance from the positions', () => {
        const display = presentObserverLag(lag({ tailEventSequenceNumber: 100, nextEventSequenceNumber: 50 }), during)!;
        (display.distance === undefined).should.be.true;
    });

    it('should show positions, a first position of zero included', () => {
        const display = presentObserverLag(lag({
            tailEventSequenceNumber: 10,
            nextEventSequenceNumber: 0,
            lastHandledEventSequenceNumber: undefined,
            relevantPendingEventSequenceNumber: 4
        }), during)!;
        display.tail!.should.equal('10');
        display.next!.should.equal('0');
        (display.lastHandled === undefined).should.be.true;
        display.relevantPending!.should.equal('4');
    });

    it('should not show a position that is not a safe integer', () => {
        (presentObserverLag(lag({ tailEventSequenceNumber: 2 ** 64 }), during)!.tail === undefined).should.be.true;
    });

    it('should show the age in whole seconds', () => presentObserverLag(lag(), during)!.ageSeconds!.should.equal(30));

    it('should never show a negative age', () => presentObserverLag(lag(), during - 10 * 60_000)!.ageSeconds!.should.equal(0));
});
