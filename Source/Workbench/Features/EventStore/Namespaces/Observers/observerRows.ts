// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { type FailedPartitionDetails, type ObserverInformation } from 'Features/Observation';
import { ObserverOwner, ObserverRunningState, ObserverType } from 'Features/Contracts/Observation';
import { ObserverSequenceType } from './ObserverSequenceType';
import { getObserverOwnerAsText } from './getObserverOwnerAsText';
import { getObserverRunningStateAsText } from './getObserverRunningStateAsText';
import { getObserverTypeAsText } from './getObserverTypeAsText';

/** An observer row, with the values the table derives for display, filtering and sorting. */
export type ObserverRow = ObserverInformation & {
    sequenceType: ObserverSequenceType;
    isQuarantined: boolean;
    failedPartitionCount: number;
};

/**
 * Counts failed partitions per observer, in the browser, because the hand-written ObserverInformation contract
 * does not carry a count. Every failed partition the query returns counts, tombstones included.
 */
export const countFailedPartitionsByObserver = (failedPartitions: FailedPartitionDetails[] | undefined) =>
    (failedPartitions ?? []).reduce((counts, failedPartition) => {
        counts.set(failedPartition.observerId, (counts.get(failedPartition.observerId) ?? 0) + 1);
        return counts;
    }, new Map<string, number>());

export const toObserverRow = (
    observer: ObserverInformation,
    sequenceType: ObserverSequenceType,
    failedPartitionCounts: Map<string, number>): ObserverRow => ({
    ...observer,
    sequenceType,
    isQuarantined: observer.runningState === ObserverRunningState.quarantined,
    failedPartitionCount: failedPartitionCounts.get(observer.id) ?? 0
});

/** The failed partitions page, narrowed to one observer. */
export const getFailedPartitionsPathForObserver = (namespacePath: string, observerId: string) =>
    `${namespacePath}/failed-partitions?observerId=${encodeURIComponent(observerId)}`;

const enumFilterOptions = <TEnum extends number>(values: TEnum[], toText: (value: TEnum) => string) =>
    values.map(value => ({ label: toText(value), value }));

export const observerTypeFilterOptions = enumFilterOptions(
    [ObserverType.unknown, ObserverType.reactor, ObserverType.projection, ObserverType.reducer, ObserverType.external],
    getObserverTypeAsText);

export const observerOwnerFilterOptions = enumFilterOptions(
    [ObserverOwner.none, ObserverOwner.client, ObserverOwner.kernel],
    getObserverOwnerAsText);

export const runningStateFilterOptions = enumFilterOptions(
    [
        ObserverRunningState.unknown,
        ObserverRunningState.active,
        ObserverRunningState.suspended,
        ObserverRunningState.replaying,
        ObserverRunningState.disconnected,
        ObserverRunningState.quarantined
    ],
    getObserverRunningStateAsText);

export const sequenceTypeFilterOptions = [
    { label: strings.eventStore.namespaces.observers.sequenceTypes.eventLog, value: ObserverSequenceType.eventLog },
    { label: strings.eventStore.namespaces.observers.sequenceTypes.system, value: ObserverSequenceType.system },
    { label: strings.eventStore.namespaces.observers.sequenceTypes.outbox, value: ObserverSequenceType.outbox },
    { label: strings.eventStore.namespaces.observers.sequenceTypes.inbox, value: ObserverSequenceType.inbox },
    { label: strings.eventStore.namespaces.observers.sequenceTypes.custom, value: ObserverSequenceType.custom }
];
