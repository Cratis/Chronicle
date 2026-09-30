// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverRunningState } from 'Features/Contracts/Observation';
import { JobStatus } from 'Features/Jobs/JobStatus';

/**
 * The shape of an observer the health summary needs - kept structural so it can be fed from any of the
 * observer read models.
 */
export interface ObserverLike {
    id: string;
    runningState: ObserverRunningState;
    nextEventSequenceNumber: number | bigint;
    tailEventSequenceNumber: number | bigint;
}

/**
 * The shape of a job the health summary needs.
 */
export interface JobLike {
    status: JobStatus;
}

/**
 * The shape of a failed partition the health summary needs.
 */
export interface FailedPartitionLike {
    isResolved: boolean;
    isQuarantined?: boolean;
}

/**
 * How the observers of a namespace are doing, bucketed the way the CLI workbench buckets them.
 */
export interface ObserverHealth {
    total: number;
    active: number;
    replaying: number;
    suspended: number;
    disconnected: number;
    quarantined: number;

    /**
     * Events appended that observers have yet to handle, summed across every observer.
     */
    lag: number;
}

/**
 * The health figures for one namespace.
 */
export interface NamespaceHealth {
    observers: ObserverHealth;
    failedPartitions: number;
    recommendations: number;
    runningJobs: number;
}

/**
 * An empty set of observer figures.
 */
export const noObservers: ObserverHealth = {
    total: 0,
    active: 0,
    replaying: 0,
    suspended: 0,
    disconnected: 0,
    quarantined: 0,
    lag: 0
};

/**
 * An empty set of namespace figures.
 */
export const noHealth: NamespaceHealth = {
    observers: noObservers,
    failedPartitions: 0,
    recommendations: 0,
    runningJobs: 0
};

/**
 * The kernel's sentinel for "no sequence number" is the largest unsigned 64 bit value, which must never be treated
 * as a position. It arrives as a number, and a number cannot hold it exactly, so anything at or beyond the largest
 * integer a number represents precisely is taken to be that sentinel - no real event log is that long.
 */
const unavailableSequenceNumber = BigInt(Number.MAX_SAFE_INTEGER);

/**
 * Work out how far behind the tail an observer is.
 * @param observer The observer to work it out for.
 * @returns The number of events it has yet to handle; zero when it is caught up or the positions are unknown.
 */
export const lagFor = (observer: ObserverLike): number => {
    const next = BigInt(observer.nextEventSequenceNumber ?? 0);
    const tail = BigInt(observer.tailEventSequenceNumber ?? 0);

    if (next >= unavailableSequenceNumber || tail >= unavailableSequenceNumber) return 0;

    // The tail is the last appended event and next is the one the observer will handle next, so an observer
    // that is caught up has next one past the tail.
    const behind = tail + 1n - next;
    return behind > 0n ? Number(behind) : 0;
};

/**
 * Summarize the observers of a namespace.
 * @param observers The observers to summarize.
 * @returns The {@link ObserverHealth}.
 */
export const summarizeObservers = (observers: ObserverLike[]): ObserverHealth => {
    const health = { ...noObservers, total: observers.length };

    for (const observer of observers) {
        switch (observer.runningState) {
            case ObserverRunningState.active:
                health.active++;
                break;
            case ObserverRunningState.replaying:
                health.replaying++;
                break;
            case ObserverRunningState.suspended:
                health.suspended++;
                break;
            case ObserverRunningState.disconnected:
                health.disconnected++;
                break;
            case ObserverRunningState.quarantined:
                health.quarantined++;
                break;
        }

        health.lag += lagFor(observer);
    }

    return health;
};

/**
 * Whether a job is still doing work.
 * @param job The job to check.
 * @returns True if it has not reached a final state.
 */
export const isJobRunning = (job: JobLike): boolean =>
    job.status === JobStatus.preparingJob ||
    job.status === JobStatus.preparingSteps ||
    job.status === JobStatus.startingSteps ||
    job.status === JobStatus.running;

/**
 * Count the failed partitions that still need attention.
 * @param partitions The failed partitions.
 * @returns How many are unresolved.
 */
export const countUnresolved = (partitions: FailedPartitionLike[]): number =>
    partitions.filter(_ => !_.isResolved).length;

/**
 * Add two sets of namespace figures together.
 * @param left The first.
 * @param right The second.
 * @returns The sum.
 */
export const addHealth = (left: NamespaceHealth, right: NamespaceHealth): NamespaceHealth => ({
    observers: {
        total: left.observers.total + right.observers.total,
        active: left.observers.active + right.observers.active,
        replaying: left.observers.replaying + right.observers.replaying,
        suspended: left.observers.suspended + right.observers.suspended,
        disconnected: left.observers.disconnected + right.observers.disconnected,
        quarantined: left.observers.quarantined + right.observers.quarantined,
        lag: left.observers.lag + right.observers.lag
    },
    failedPartitions: left.failedPartitions + right.failedPartitions,
    recommendations: left.recommendations + right.recommendations,
    runningJobs: left.runningJobs + right.runningJobs
});
