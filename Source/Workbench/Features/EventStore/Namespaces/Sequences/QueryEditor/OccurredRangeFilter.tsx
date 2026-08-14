// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useRef, useState } from 'react';
import type { HistogramBucket } from './HistogramBucket';
import './OccurredRangeFilter.css';

/**
 * Props for {@link OccurredRangeFilter}.
 */
export interface OccurredRangeFilterProps {
    /** The pre-counted buckets to render, as counted by the backend over the whole sequence. */
    buckets: HistogramBucket[];
    /** The earliest instant the picker can select. */
    min: number;
    /** The latest instant the picker can select. */
    max: number;
    /** The currently selected range, or null when the whole span is selected. */
    selectedRange: [number, number] | null;
    /** Called when the user picks a different range. */
    onChange: (range: [number, number] | null) => void;
    /** Message shown when there is nothing to plot. */
    emptyMessage: string;
    /** Label for the button that clears the selection. */
    clearLabel: string;
}

const formatInstant = (value: number) => new Date(value).toLocaleString();

/**
 * A time range picker drawn over a histogram of when events actually occurred.
 *
 * The counts come from the backend rather than from the loaded page, so the shape of the data is
 * the shape of the whole sequence - dragging to a busy region selects a range that really does
 * contain those events.
 * @param props The {@link OccurredRangeFilterProps}.
 * @returns The rendered picker.
 */
export const OccurredRangeFilter = ({
    buckets,
    min,
    max,
    selectedRange,
    onChange,
    emptyMessage,
    clearLabel
}: OccurredRangeFilterProps) => {
    const barsRef = useRef<HTMLDivElement>(null);
    const [dragStartBucket, setDragStartBucket] = useState<number | null>(null);

    const currentRange = selectedRange ?? [min, max];
    const span = max - min;
    const tallest = Math.max(...buckets.map(bucket => bucket.count), 1);

    const positionOf = useCallback(
        (value: number) => (span <= 0 ? 0 : ((value - min) / span) * 100),
        [min, span]
    );

    const selectFromBuckets = (firstIndex: number, secondIndex: number) => {
        const from = Math.min(firstIndex, secondIndex);
        const to = Math.max(firstIndex, secondIndex);
        onChange([buckets[from].start, buckets[to].end]);
    };

    const handleBucketDown = (index: number) => setDragStartBucket(index);

    const handleBucketUp = (index: number) => {
        if (dragStartBucket === null) {
            selectFromBuckets(index, index);
        } else {
            selectFromBuckets(dragStartBucket, index);
        }
        setDragStartBucket(null);
    };

    if (buckets.length === 0) {
        return <div className='occurred-range__empty'>{emptyMessage}</div>;
    }

    const left = positionOf(currentRange[0]);
    const right = positionOf(currentRange[1]);

    return (
        <div className='occurred-range'>
            <div className='occurred-range__bars' ref={barsRef}>
                {buckets.map((bucket, index) => {
                    const isSelected = bucket.end > currentRange[0] && bucket.start < currentRange[1];
                    return (
                        <button
                            key={bucket.start}
                            type='button'
                            className={`occurred-range__bar ${isSelected ? 'is-selected' : ''}`}
                            style={{ height: `${Math.max((bucket.count / tallest) * 100, 2)}%` }}
                            title={`${formatInstant(bucket.start)} – ${formatInstant(bucket.end)}: ${bucket.count}`}
                            onMouseDown={() => handleBucketDown(index)}
                            onMouseUp={() => handleBucketUp(index)}
                        />
                    );
                })}
            </div>

            <div className='occurred-range__track'>
                <div
                    className='occurred-range__selection'
                    style={{ left: `${left}%`, width: `${Math.max(right - left, 0.5)}%` }}
                />
            </div>

            <div className='occurred-range__labels'>
                <span>{formatInstant(currentRange[0])}</span>
                <span>{formatInstant(currentRange[1])}</span>
            </div>

            {selectedRange !== null && (
                <button type='button' className='occurred-range__clear' onClick={() => onChange(null)}>
                    {clearLabel}
                </button>
            )}
        </div>
    );
};
