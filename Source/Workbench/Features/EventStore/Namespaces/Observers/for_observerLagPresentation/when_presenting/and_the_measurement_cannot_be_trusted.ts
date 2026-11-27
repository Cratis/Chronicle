// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverLagCause, ObserverLagStatus } from 'Features/Observation';
import { expiresAt, lag, sampledAt } from '../given';
import { presentObserverLag } from '../../observerLagPresentation';

const during = sampledAt.getTime() + 30_000;

describe('when presenting a measurement that cannot be trusted', () => {
    it('should show it as unavailable when nothing was read', () => (presentObserverLag(undefined, during) === undefined).should.be.true);

    it('should not take the empty default of a failed query for a measurement', () => {
        (presentObserverLag({} as never, during) === undefined).should.be.true;
    });

    it('should show it as unavailable when a measured sample has no sample time', () => {
        (presentObserverLag(lag({ sampledAt: undefined }), during) === undefined).should.be.true;
    });

    it('should show it as unavailable when a measured sample has no expiry', () => {
        (presentObserverLag(lag({ expiresAt: undefined }), during) === undefined).should.be.true;
    });

    it('should show it as unavailable when a time is not a valid date', () => {
        (presentObserverLag(lag({ sampledAt: new Date('nonsense') }), during) === undefined).should.be.true;
    });

    it('should show it as unavailable when it expires before it was taken', () => {
        (presentObserverLag(lag({ sampledAt: expiresAt, expiresAt: sampledAt }), during) === undefined).should.be.true;
    });

    it('should show it as unavailable when the status is one this version does not know', () => {
        (presentObserverLag(lag({ status: 99 as ObserverLagStatus }), during) === undefined).should.be.true;
    });

    it('should still be current at the moment it expires', () => {
        presentObserverLag(lag({ distance: 1 }), expiresAt.getTime())!.state.should.equal('lagging');
    });

    it('should mask the status, distance and positions once it has expired', () => {
        const display = presentObserverLag(lag({ distance: 5, tailEventSequenceNumber: 9, nextEventSequenceNumber: 4 }), expiresAt.getTime() + 1)!;
        display.state.should.equal('expired');
        [display.distance, display.tail, display.next].every(value => value === undefined).should.be.true;
    });

    it('should keep the age of an expired sample', () => {
        presentObserverLag(lag(), expiresAt.getTime() + 1000)!.ageSeconds!.should.equal(121);
    });

    it('should accept times that arrive as text', () => {
        presentObserverLag(lag({ sampledAt: sampledAt.toISOString() as never, expiresAt: expiresAt.toISOString() as never }), during)!
            .ageSeconds!.should.equal(30);
    });

    it('should show a sample that expired as expired, not as no sample', () => {
        presentObserverLag(lag({ status: ObserverLagStatus.unmeasurable, cause: ObserverLagCause.noSample }), during)!
            .state.should.equal('expired');
    });

    it('should show no sample when there never was one', () => {
        const display = presentObserverLag(lag({ status: ObserverLagStatus.unmeasurable, cause: ObserverLagCause.noSample, sampledAt: undefined, expiresAt: undefined }), during)!;
        display.state.should.equal('noSample');
        (display.ageSeconds === undefined).should.be.true;
    });
});
