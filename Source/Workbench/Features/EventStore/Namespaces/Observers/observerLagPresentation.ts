// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverLag, ObserverLagCause, ObserverLagStatus } from 'Features/Observation';

/** What a sampled observer measurement says, in the terms the Workbench shows. */
export type ObserverLagState =
    | 'caughtUp'
    | 'lagging'
    | 'stalled'
    | 'noSample'
    | 'expired'
    | 'unknownState'
    | 'inactive'
    | 'disconnected'
    | 'replaying'
    | 'tailUnavailable'
    | 'unrecognized';

/** The sampled measurement of one observer, ready to show. Text fields are absent when the sample does not hold a real value. */
export interface ObserverLagDisplay {
    state: ObserverLagState;
    distance?: string;
    tail?: string;
    next?: string;
    lastHandled?: string;
    relevantPending?: string;

    /** Whole seconds since the sample was taken, never negative; absent when the sample carries no valid time. */
    ageSeconds?: number;
}

/**
 * Formats an event sequence position or distance. Only a nonnegative safe integer is shown, zero included; anything
 * else cannot be told apart from a value JavaScript has rounded, so it is not shown.
 */
export const formatPosition = (value: number | null | undefined): string | undefined =>
    typeof value === 'number' && Number.isSafeInteger(value) && value >= 0 ? value.toString() : undefined;

const toTime = (value: unknown): number => {
    if (value instanceof Date) return value.getTime();
    if (typeof value === 'string') return Date.parse(value);
    return Number.NaN;
};

const unmeasurableStates = new Map<ObserverLagCause, ObserverLagState>([
    [ObserverLagCause.unknownState, 'unknownState'],
    [ObserverLagCause.inactive, 'inactive'],
    [ObserverLagCause.disconnected, 'disconnected'],
    [ObserverLagCause.replayInProgress, 'replaying'],
    [ObserverLagCause.tailUnavailable, 'tailUnavailable']
]);

const measurableStates = new Map<ObserverLagStatus, ObserverLagState>([
    [ObserverLagStatus.caughtUp, 'caughtUp'],
    [ObserverLagStatus.lagging, 'lagging'],
    [ObserverLagStatus.stalled, 'stalled']
]);

const unmeasurableState = (lag: ObserverLag, hasSampleTime: boolean): ObserverLagState => {
    // An expired sample arrives as unmeasurable with no sample cause; the sample time is what says it existed.
    if (lag.cause === ObserverLagCause.noSample) return hasSampleTime ? 'expired' : 'noSample';
    return unmeasurableStates.get(lag.cause) ?? 'unrecognized';
};

const positionsOf = (lag: ObserverLag) => ({
    tail: formatPosition(lag.tailEventSequenceNumber),
    next: formatPosition(lag.nextEventSequenceNumber),
    lastHandled: formatPosition(lag.lastHandledEventSequenceNumber),
    relevantPending: formatPosition(lag.relevantPendingEventSequenceNumber)
});

/** The sample's times, with the moment they are judged at; a time that is absent or invalid is NaN. */
interface SampleClock {
    sampledAt: number;
    expiresAt: number;
    nowMs: number;
    ageSeconds?: number;
}

const clockOf = (lag: ObserverLag, nowMs: number): SampleClock => {
    const sampledAt = toTime(lag.sampledAt);
    const ageSeconds = Number.isNaN(sampledAt) ? undefined : Math.max(0, Math.floor((nowMs - sampledAt) / 1000));
    return { sampledAt, expiresAt: toTime(lag.expiresAt), nowMs, ageSeconds };
};

const hasValidWindow = (clock: SampleClock) =>
    !Number.isNaN(clock.sampledAt) && !Number.isNaN(clock.expiresAt) && clock.expiresAt >= clock.sampledAt;

const presentUnmeasurable = (lag: ObserverLag, clock: SampleClock): ObserverLagDisplay => {
    const state = unmeasurableState(lag, !Number.isNaN(clock.sampledAt));
    const isCurrent = hasValidWindow(clock) && clock.nowMs <= clock.expiresAt && state !== 'expired';
    return { state, ageSeconds: clock.ageSeconds, ...(isCurrent ? positionsOf(lag) : {}) };
};

const presentMeasured = (lag: ObserverLag, state: ObserverLagState, clock: SampleClock): ObserverLagDisplay | undefined => {
    if (!hasValidWindow(clock)) return undefined;
    if (clock.nowMs > clock.expiresAt) return { state: 'expired', ageSeconds: clock.ageSeconds };
    return { state, ageSeconds: clock.ageSeconds, distance: formatPosition(lag.distance), ...positionsOf(lag) };
};

/**
 * Presents a measurement as it stands at a moment. The measurement's own validity bound decides whether it still
 * describes the observer, so an old response never shows as current.
 * @param lag The response, or undefined when it could not be read.
 * @param nowMs The moment to present it at, in milliseconds since the epoch.
 * @returns The display, or undefined when nothing trustworthy can be said, which is not the same as no sample.
 */
export const presentObserverLag = (lag: ObserverLag | undefined, nowMs: number): ObserverLagDisplay | undefined => {
    if (!lag) return undefined;

    const clock = clockOf(lag, nowMs);
    if (lag.status === ObserverLagStatus.unmeasurable) return presentUnmeasurable(lag, clock);

    const state = measurableStates.get(lag.status);
    return state ? presentMeasured(lag, state, clock) : undefined;
};

/** Formats a whole-seconds age compactly, for example 45s, 3m 20s or 2h 5m. */
export const formatAge = (ageSeconds: number): string => {
    if (ageSeconds < 60) return `${ageSeconds}s`;
    const minutes = Math.floor(ageSeconds / 60);
    if (minutes < 60) return `${minutes}m ${ageSeconds % 60}s`;
    return `${Math.floor(minutes / 60)}h ${minutes % 60}m`;
};
