// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * A time window the throughput chart can show.
 */
export type ThroughputRange = 'hour' | 'day' | 'week';

/**
 * How a {@link ThroughputRange} is laid out: the histogram resolution to ask for, the size of each bucket and how
 * many of them the window holds.
 */
export interface ThroughputRangeLayout {
    resolution: 'minute' | 'hour' | 'day';
    bucketMilliseconds: number;
    buckets: number;
}

const minute = 60_000;
const hour = 60 * minute;
const day = 24 * hour;

/**
 * The layout of every {@link ThroughputRange}.
 */
export const throughputRanges: Record<ThroughputRange, ThroughputRangeLayout> = {
    hour: { resolution: 'minute', bucketMilliseconds: minute, buckets: 60 },
    day: { resolution: 'hour', bucketMilliseconds: hour, buckets: 24 },
    week: { resolution: 'day', bucketMilliseconds: day, buckets: 7 }
};

/**
 * A bucket as the histogram query returns it.
 */
export interface HistogramBucketLike {
    from: Date | string;
    count: number | bigint;
}

/**
 * One point on the throughput chart.
 */
export interface ThroughputPoint {
    from: Date;
    count: number;
}

/**
 * Work out where the window for a range starts, aligned to its bucket size so buckets line up with the histogram.
 * @param range The range.
 * @param now What time it is.
 * @returns The start of the first bucket in the window.
 */
export const windowStartFor = (range: ThroughputRange, now: Date): Date => {
    const layout = throughputRanges[range];

    // Day buckets are aligned in UTC by the server, and so are the others - aligning on the epoch matches all three.
    const currentBucket = Math.floor(now.getTime() / layout.bucketMilliseconds) * layout.bucketMilliseconds;
    return new Date(currentBucket - (layout.buckets - 1) * layout.bucketMilliseconds);
};

/**
 * Lay histogram buckets out over a window, filling the buckets the histogram leaves out with zero.
 * @param range The range the window covers.
 * @param now What time it is.
 * @param histograms The histograms to lay out - one per namespace; counts falling in the same bucket are added up.
 * @returns One point per bucket in the window, oldest first.
 * @remarks The histogram only returns buckets holding at least one event, which plotted as-is would draw a straight
 * line across every quiet period. Quiet is a fact worth showing, so every bucket gets a point.
 */
export const layOutThroughput = (range: ThroughputRange, now: Date, histograms: HistogramBucketLike[][]): ThroughputPoint[] => {
    const layout = throughputRanges[range];
    const start = windowStartFor(range, now).getTime();
    const counts = new Array<number>(layout.buckets).fill(0);

    for (const histogram of histograms) {
        for (const bucket of histogram) {
            const index = Math.floor((new Date(bucket.from).getTime() - start) / layout.bucketMilliseconds);
            if (index >= 0 && index < layout.buckets) {
                counts[index] += Number(bucket.count);
            }
        }
    }

    return counts.map((count, index) => ({ from: new Date(start + index * layout.bucketMilliseconds), count }));
};
