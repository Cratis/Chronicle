// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Features/Observation';
import { ObserverRunningState } from 'Features/Contracts/Observation';
import { groupObserversByRunningState, totalObservers } from '../dashboardAggregations';

const observerWithState = (runningState: ObserverRunningState) => {
    const observer = new ObserverInformation();
    observer.runningState = runningState;
    return observer;
};

describe('when grouping observers by running state', () => {
    const observers = [
        observerWithState(ObserverRunningState.active),
        observerWithState(ObserverRunningState.active),
        observerWithState(ObserverRunningState.suspended),
        observerWithState(ObserverRunningState.replaying),
        observerWithState(ObserverRunningState.disconnected),
        observerWithState(ObserverRunningState.disconnected),
        observerWithState(ObserverRunningState.disconnected),
        observerWithState(ObserverRunningState.quarantined),
        observerWithState(ObserverRunningState.unknown)
    ];

    const breakdown = groupObserversByRunningState(observers);

    it('should count active observers', () => breakdown.active.should.equal(2));
    it('should count suspended observers', () => breakdown.suspended.should.equal(1));
    it('should count replaying observers', () => breakdown.replaying.should.equal(1));
    it('should count disconnected observers', () => breakdown.disconnected.should.equal(3));
    it('should count quarantined observers', () => breakdown.quarantined.should.equal(1));
    it('should count unknown observers', () => breakdown.unknown.should.equal(1));
    it('should total to the number of observers', () => totalObservers(breakdown).should.equal(observers.length));
});

describe('when grouping no observers', () => {
    const breakdown = groupObserversByRunningState([]);

    it('should total to zero', () => totalObservers(breakdown).should.equal(0));
});
