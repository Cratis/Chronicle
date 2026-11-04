// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventTypeCount } from 'Features/Statistics';

/**
 * The part of a statistics query result the dashboards read - kept structural so it can be fed from either
 * statistics query.
 */
export interface StatisticsResultLike {
    isReady: boolean;
    isSuccess: boolean;
    data?: {
        totalEvents?: number | bigint | null;
        perEventType?: EventTypeCount[] | null;
    } | null;
}

/**
 * What the dashboards show of the statistics.
 */
export interface DashboardStatistics {

    /**
     * The total number of events, or undefined while loading or when the statistics could not be read.
     */
    totalEvents: number | undefined;

    /**
     * The per event type counts, or undefined while loading or when the statistics could not be read.
     */
    perEventType: EventTypeCount[] | undefined;

    /**
     * Whether the statistics could not be read, for instance because the read model is not available.
     */
    failed: boolean;
}

/**
 * Reads what the dashboards need from a statistics query result. A failed query, a missing result or
 * missing fields never throw; they read as unavailable, or as empty.
 * @param result The query result.
 * @returns The {@link DashboardStatistics}.
 */
export const readStatistics = (result: StatisticsResultLike | undefined | null): DashboardStatistics => {
    if (!result || !result.isReady) return { totalEvents: undefined, perEventType: undefined, failed: false };
    if (!result.isSuccess) return { totalEvents: undefined, perEventType: undefined, failed: true };

    const data = result.data;
    return {
        totalEvents: Number(data?.totalEvents ?? 0),
        perEventType: (data?.perEventType ?? []).filter(_ => _ !== null && _ !== undefined),
        failed: false
    };
};
