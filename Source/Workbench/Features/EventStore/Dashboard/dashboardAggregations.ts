// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Features/Observation';
import { ObserverRunningState } from 'Features/Contracts/Observation';
import { JobSummary } from 'Features/Jobs';
import { JobStatus } from 'Features/Contracts/Jobs';
import { WebhookDetails } from 'Features/Observation/Webhooks';
import { ExternalService } from 'Features/ExternalServices';
import { type ObserverRunningStateBreakdown } from './ObserverRunningStateBreakdown';

/**
 * Groups observers by their {@link ObserverRunningState}, so the dashboard can show a
 * breakdown without a dedicated backend aggregation.
 */
export const groupObserversByRunningState = (observers: ObserverInformation[]): ObserverRunningStateBreakdown => {
    const breakdown: ObserverRunningStateBreakdown = {
        active: 0,
        suspended: 0,
        replaying: 0,
        disconnected: 0,
        quarantined: 0,
        unknown: 0
    };

    for (const observer of observers) {
        switch (observer.runningState) {
            case ObserverRunningState.active:
                breakdown.active++;
                break;
            case ObserverRunningState.suspended:
                breakdown.suspended++;
                break;
            case ObserverRunningState.replaying:
                breakdown.replaying++;
                break;
            case ObserverRunningState.disconnected:
                breakdown.disconnected++;
                break;
            case ObserverRunningState.quarantined:
                breakdown.quarantined++;
                break;
            default:
                breakdown.unknown++;
                break;
        }
    }

    return breakdown;
};

/**
 * The total number of observers represented by a {@link ObserverRunningStateBreakdown}.
 */
export const totalObservers = (breakdown: ObserverRunningStateBreakdown): number =>
    breakdown.active + breakdown.suspended + breakdown.replaying + breakdown.disconnected + breakdown.quarantined + breakdown.unknown;

/**
 * Counts jobs currently in the {@link JobStatus.running} state.
 */
export const countRunningJobs = (jobs: JobSummary[]): number =>
    jobs.filter(job => job.status === JobStatus.running).length;

/**
 * There is no unified "subscriptions" read model in Chronicle - webhooks and external
 * services are the two mechanisms that let something outside the event store subscribe
 * to it, so the dashboard groups their counts under that single label.
 */
export const countSubscriptions = (webhooks: WebhookDetails[], externalServices: ExternalService[]): number =>
    webhooks.length + externalServices.length;
