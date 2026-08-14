// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * One pre-counted bar of the time range histogram, on an epoch-millisecond scale.
 */
export interface HistogramBucket {
    /** Inclusive start of the bucket. */
    start: number;
    /** Exclusive end of the bucket. */
    end: number;
    /** Number of events that occurred within the bucket. */
    count: number;
}
