// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The number of observers currently in each {@link ObserverRunningState}.
 */
export interface ObserverRunningStateBreakdown {
    active: number;
    suspended: number;
    replaying: number;
    disconnected: number;
    quarantined: number;
    unknown: number;
}
