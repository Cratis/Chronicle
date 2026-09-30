// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef, useState } from 'react';

/**
 * One sample in a history.
 */
export interface Sample<T> {
    at: Date;
    value: T;
}

/**
 * Keep a rolling history of a value, sampled on an interval.
 * @param value The current value, or undefined while there is none - nothing is sampled until there is.
 * @param intervalMilliseconds How often to sample.
 * @param capacity How many samples to keep.
 * @param resetKey Clear the history when this changes, so switching what is shown does not splice two series.
 * @returns The samples, oldest first.
 * @remarks For figures the server keeps no history of, such as observer states. The history covers the time the
 * page has been open, which is what the CLI workbench shows as well.
 */
export const useSampledHistory = <T>(value: T | undefined, intervalMilliseconds: number, capacity: number, resetKey: string): Sample<T>[] => {
    const [samples, setSamples] = useState<Sample<T>[]>([]);
    const valueRef = useRef(value);
    valueRef.current = value;

    useEffect(() => {
        setSamples([]);

        const sample = () => {
            const current = valueRef.current;
            if (current === undefined) return;
            setSamples(previous => [...previous, { at: new Date(), value: current }].slice(-capacity));
        };

        sample();
        const timer = setInterval(sample, intervalMilliseconds);
        return () => clearInterval(timer);
    }, [intervalMilliseconds, capacity, resetKey]);

    // The first value usually arrives after the effect ran, and waiting a whole interval for it leaves the chart
    // blank for no reason - so the first sample is taken as soon as there is something to take.
    useEffect(() => {
        if (value !== undefined && samples.length === 0) {
            setSamples([{ at: new Date(), value }]);
        }
    }, [value, samples.length]);

    return samples;
};
