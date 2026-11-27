// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverLagCause, ObserverLagStatus } from 'Features/Observation';
import { presentObserverLag, type ObserverLagState } from '../../observerLagPresentation';
import { during, lag } from '../given';

const causes: [ObserverLagCause, ObserverLagState][] = [
    [ObserverLagCause.unknownState, 'unknownState'],
    [ObserverLagCause.inactive, 'inactive'],
    [ObserverLagCause.disconnected, 'disconnected'],
    [ObserverLagCause.replayInProgress, 'replaying'],
    [ObserverLagCause.tailUnavailable, 'tailUnavailable'],
    [ObserverLagCause.unknown, 'unrecognized'],
    [99 as ObserverLagCause, 'unrecognized']
];

describe('when presenting an observer that cannot be measured', () => {
    causes.forEach(([cause, state]) => {
        it(`should show ${ObserverLagCause[cause] ?? cause} as ${state}`, () => {
            presentObserverLag(lag({ status: ObserverLagStatus.unmeasurable, cause }), during)!.state.should.equal(state);
        });
    });

    it('should never show a distance of zero for it', () => {
        const display = presentObserverLag(lag({ status: ObserverLagStatus.unmeasurable, cause: ObserverLagCause.inactive, distance: 0 }), during)!;
        (display.distance === undefined).should.be.true;
    });

    it('should show the positions it carries while the sample is current', () => {
        presentObserverLag(lag({ status: ObserverLagStatus.unmeasurable, cause: ObserverLagCause.inactive, nextEventSequenceNumber: 7 }), during)!
            .next!.should.equal('7');
    });
});
