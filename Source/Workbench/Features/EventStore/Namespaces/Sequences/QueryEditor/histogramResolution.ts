// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequenceHistogramBucket } from 'Features/Sequences';
import type { HistogramBucket } from './HistogramBucket';

/** The bucket sizes the backend can group events into, coarsest last. */
export const histogramResolutions = ['minute', 'hour', 'day', 'week', 'month'] as const;

/** One of the bucket sizes the backend can group events into. */
export type HistogramResolutionName = typeof histogramResolutions[number];

const oneMinute = 60 * 1000;
const oneHour = 60 * oneMinute;
const oneDay = 24 * oneHour;

/**
 * Pick the bucket size that keeps a time span readable - roughly tens to low hundreds of bars
 * rather than one bar or thousands.
 * @param spanInMilliseconds How long the histogram covers.
 * @returns The resolution to request.
 */
export const resolutionForSpan = (spanInMilliseconds: number): HistogramResolutionName => {
    if (spanInMilliseconds <= 2 * oneHour) return 'minute';
    if (spanInMilliseconds <= 4 * oneDay) return 'hour';
    if (spanInMilliseconds <= 90 * oneDay) return 'day';
    if (spanInMilliseconds <= 730 * oneDay) return 'week';

    return 'month';
};

/**
 * Convert the buckets the backend returned into the shape the range filter renders.
 * @param buckets The buckets from the histogram query.
 * @returns Buckets on an epoch-millisecond scale.
 */
export const toHistogramBuckets = (buckets: SequenceHistogramBucket[]): HistogramBucket[] =>
    buckets.map(bucket => ({
        start: new Date(bucket.from).getTime(),
        end: new Date(bucket.to).getTime(),
        count: Number(bucket.count)
    }));

/**
 * Work out the full time span the buckets cover, which is the range the picker lets the user move within.
 * @param buckets The buckets on an epoch-millisecond scale.
 * @returns The span, or undefined when there is nothing to show.
 */
export const spanOf = (buckets: HistogramBucket[]): { min: number; max: number } | undefined => {
    if (buckets.length === 0) return undefined;

    return {
        min: Math.min(...buckets.map(bucket => bucket.start)),
        max: Math.max(...buckets.map(bucket => bucket.end))
    };
};
