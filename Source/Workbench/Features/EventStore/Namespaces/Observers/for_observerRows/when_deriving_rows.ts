// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { FailedPartitionDetails, ObserverInformation } from 'Features/Observation';
import { ObserverOwner, ObserverRunningState, ObserverType } from 'Features/Contracts/Observation';
import { ObserverSequenceType } from '../ObserverSequenceType';
import {
    countFailedPartitionsByObserver,
    getFailedPartitionsPathForObserver,
    observerOwnerFilterOptions,
    observerTypeFilterOptions,
    runningStateFilterOptions,
    sequenceTypeFilterOptions,
    toObserverRow
} from '../observerRows';

const observer = (id: string, runningState: ObserverRunningState) =>
    Object.assign(new ObserverInformation(), { id, runningState });

const failure = (observerId: string) => Object.assign(new FailedPartitionDetails(), { observerId });

describe('when deriving observer rows', () => {
    it('should count nothing when failures have not loaded', () => {
        countFailedPartitionsByObserver(undefined).size.should.equal(0);
    });

    it('should count nothing when there are no failures', () => {
        countFailedPartitionsByObserver([]).size.should.equal(0);
    });

    it('should count failures per observer', () => {
        const counts = countFailedPartitionsByObserver([failure('a'), failure('b'), failure('a')]);
        counts.get('a')!.should.equal(2);
        counts.get('b')!.should.equal(1);
    });

    it('should give an observer without failures a zero count', () => {
        toObserverRow(observer('a', ObserverRunningState.active), ObserverSequenceType.eventLog, new Map([['b', 3]]))
            .failedPartitionCount.should.equal(0);
    });

    it('should give an observer the count of its own failures', () => {
        toObserverRow(observer('b', ObserverRunningState.active), ObserverSequenceType.eventLog, new Map([['b', 3]]))
            .failedPartitionCount.should.equal(3);
    });

    it('should mark only the quarantined state as quarantined', () => {
        const quarantined = (state: ObserverRunningState) =>
            toObserverRow(observer('a', state), ObserverSequenceType.eventLog, new Map()).isQuarantined;
        quarantined(ObserverRunningState.quarantined).should.be.true;
        [ObserverRunningState.unknown, ObserverRunningState.active, ObserverRunningState.suspended,
            ObserverRunningState.replaying, ObserverRunningState.disconnected]
            .every(state => !quarantined(state)).should.be.true;
    });

    it('should keep the sequence type it is given', () => {
        toObserverRow(observer('a', ObserverRunningState.active), ObserverSequenceType.outbox, new Map())
            .sequenceType.should.equal(ObserverSequenceType.outbox);
    });

    it('should encode the observer id in the scoped failed partitions path', () => {
        getFailedPartitionsPathForObserver('/event-store/store/ns', 'My.Observer/1 & more')
            .should.equal('/event-store/store/ns/failed-partitions?observerId=My.Observer%2F1%20%26%20more');
    });

    it('should offer every observer type with a label', () => {
        observerTypeFilterOptions.map(option => option.value).should.deep.equal(
            [ObserverType.unknown, ObserverType.reactor, ObserverType.projection, ObserverType.reducer, ObserverType.external]);
        observerTypeFilterOptions.every(option => option.label !== '[N/A]' && option.label !== '').should.be.true;
    });

    it('should offer every owner with a label', () => {
        observerOwnerFilterOptions.map(option => option.value).should.deep.equal(
            [ObserverOwner.none, ObserverOwner.client, ObserverOwner.kernel]);
        observerOwnerFilterOptions.every(option => option.label !== '[N/A]' && option.label !== '').should.be.true;
    });

    it('should offer every running state with a label', () => {
        runningStateFilterOptions.map(option => option.value).should.deep.equal([0, 1, 2, 3, 4, 5]);
        runningStateFilterOptions.every(option => option.label !== '[N/A]' && option.label !== '').should.be.true;
    });

    it('should offer every sequence type with a label', () => {
        sequenceTypeFilterOptions.map(option => option.value).should.deep.equal(Object.values(ObserverSequenceType));
        sequenceTypeFilterOptions.every(option => option.label !== '').should.be.true;
    });
});
