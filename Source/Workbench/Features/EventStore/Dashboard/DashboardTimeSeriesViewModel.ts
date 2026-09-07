// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { type ObserverRunningStateBreakdown } from './ObserverRunningStateBreakdown';
import { type ObserverSample } from './ObserverSample';
import { totalObservers } from './dashboardAggregations';

/**
 * The number of samples kept per rolling chart before the oldest one is dropped.
 */
export const maxDashboardSamples = 30;

/**
 * Owns the dashboard's client-side rolling sample buffers.
 *
 * Chronicle has no persisted throughput/observer-state metrics history (see the PR
 * description), so the dashboard approximates "over time" charts by sampling the tail
 * sequence number and the observer state breakdown on an interval and keeping a bounded,
 * session-scoped ring buffer of the results. This resets on navigation/reload by design.
 */
@injectable()
export class DashboardTimeSeriesViewModel {
    throughputSamples: number[] = [];
    observerSamples: ObserverSample[] = [];

    private _lastSequenceNumber: number | undefined;

    /**
     * Records the latest tail sequence number, deriving the throughput sample from its
     * delta against the previously recorded sequence number.
     */
    recordThroughputSample(sequenceNumber: number) {
        const delta = this._lastSequenceNumber === undefined ? 0 : Math.max(0, sequenceNumber - this._lastSequenceNumber);
        this._lastSequenceNumber = sequenceNumber;
        this.throughputSamples = [...this.throughputSamples, delta].slice(-maxDashboardSamples);
    }

    /**
     * Records the latest observer running-state breakdown as an active/total sample.
     */
    recordObserverSample(breakdown: ObserverRunningStateBreakdown) {
        const sample: ObserverSample = { active: breakdown.active, total: totalObservers(breakdown) };
        this.observerSamples = [...this.observerSamples, sample].slice(-maxDashboardSamples);
    }
}
