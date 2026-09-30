// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResult } from '@cratis/arc/queries';
import { AllObservers, GetFailedPartitions } from 'Features/Observation';
import { AllJobs } from 'Features/Jobs';
import { GetRecommendations } from 'Features/Recommendations';
import { SequenceHistogram, TailSequenceNumber } from 'Features/Sequences';
import { StatisticsForNamespace } from 'Features/Statistics';
import { countUnresolved, isJobRunning, NamespaceHealth, summarizeObservers } from './health';
import { layOutThroughput, ThroughputPoint, ThroughputRange, throughputRanges, windowStartFor } from './throughput';

/**
 * The event sequence every dashboard figure is about.
 */
export const eventLog = 'event-log';

/**
 * Take the data from a query result, turning a failed query into a rejected read.
 * @param result The result.
 * @returns The data.
 */
const dataOf = <T>(result: QueryResult<T>): T => {
    if (!result.isSuccess) throw new Error('The query did not succeed');
    return result.data;
};

/**
 * Read the health figures of one namespace.
 * @param eventStore The event store.
 * @param namespace The namespace.
 * @returns The {@link NamespaceHealth}.
 */
export const readNamespaceHealth = async (eventStore: string, namespace: string): Promise<NamespaceHealth> => {
    const [observers, failedPartitions, recommendations, jobs] = await Promise.all([
        new AllObservers().perform({ eventStore, namespace }),
        new GetFailedPartitions().perform({ eventStore, namespace }),
        new GetRecommendations().perform({ eventStore, namespace }),
        new AllJobs().perform({ eventStore, namespace })
    ]);

    return {
        observers: summarizeObservers(dataOf(observers)),
        failedPartitions: countUnresolved(dataOf(failedPartitions)),
        recommendations: dataOf(recommendations).length,
        runningJobs: dataOf(jobs).filter(isJobRunning).length
    };
};

/**
 * Read the health figures of several namespaces.
 * @param eventStore The event store.
 * @param namespaces The namespaces.
 * @returns The figures, keyed by namespace.
 */
export const readHealthForNamespaces = async (eventStore: string, namespaces: string[]): Promise<Record<string, NamespaceHealth>> => {
    const figures = await Promise.all(namespaces.map(namespace => readNamespaceHealth(eventStore, namespace)));
    return Object.fromEntries(namespaces.map((namespace, index) => [namespace, figures[index]]));
};

/**
 * Read how many events each of several namespaces holds.
 * @param eventStore The event store.
 * @param namespaces The namespaces.
 * @returns The event count, keyed by namespace.
 */
export const readEventCountsForNamespaces = async (eventStore: string, namespaces: string[]): Promise<Record<string, number>> => {
    const statistics = await Promise.all(namespaces.map(async namespace =>
        dataOf(await new StatisticsForNamespace().perform({ eventStore, namespace }))));
    return Object.fromEntries(namespaces.map((namespace, index) => [namespace, Number(statistics[index].totalEvents ?? 0)]));
};

/**
 * Read the event throughput of one or more namespaces over a range.
 * @param eventStore The event store.
 * @param namespaces The namespaces to add up.
 * @param range The range to read.
 * @returns One point per bucket in the range.
 */
export const readThroughput = async (eventStore: string, namespaces: string[], range: ThroughputRange): Promise<ThroughputPoint[]> => {
    const now = new Date();
    const occurredFrom = windowStartFor(range, now);
    const histograms = await Promise.all(namespaces.map(async namespace => dataOf(await new SequenceHistogram().perform({
        eventStore,
        namespace,
        eventSequenceId: eventLog,
        resolution: throughputRanges[range].resolution,
        occurredFrom
    }))));

    return layOutThroughput(range, now, histograms);
};

/**
 * Read the tail sequence number of the event log of a namespace.
 * @param eventStore The event store.
 * @param namespace The namespace.
 * @returns The tail, or undefined when the log holds no events.
 */
export const readTail = async (eventStore: string, namespace: string): Promise<number | undefined> => {
    const tail = dataOf(await new TailSequenceNumber().perform({ eventStore, namespace, eventSequenceId: eventLog }));
    const sequenceNumber = Number(tail.sequenceNumber);

    // An empty log reports the kernel's "unavailable" sentinel, which is not a position.
    return Number.isSafeInteger(sequenceNumber) ? sequenceNumber : undefined;
};
