// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DependencyList, useEffect, useRef, useState } from 'react';

/**
 * The state of a polled value.
 */
export interface Polled<T> {
    /**
     * The latest value, or undefined until the first poll has completed.
     */
    value: T | undefined;

    /**
     * When the latest value was read.
     */
    updatedAt: Date | undefined;

    /**
     * Whether the latest poll failed. The previous value is kept, so a transient failure does not blank the view.
     */
    failed: boolean;
}

/**
 * Read a value on an interval, for the figures on a dashboard that have no observable query behind them.
 * @param read Reads the value; a rejected promise marks the poll as failed.
 * @param intervalMilliseconds How long to wait between reads.
 * @param dependencies Restart polling, discarding the previous value, when any of these change.
 * @returns The {@link Polled} state.
 */
export const usePolling = <T>(read: () => Promise<T>, intervalMilliseconds: number, dependencies: DependencyList): Polled<T> => {
    const [state, setState] = useState<Polled<T>>({ value: undefined, updatedAt: undefined, failed: false });
    const readRef = useRef(read);
    readRef.current = read;

    useEffect(() => {
        let cancelled = false;
        let timer: ReturnType<typeof setTimeout> | undefined;
        setState({ value: undefined, updatedAt: undefined, failed: false });

        const poll = async () => {
            try {
                const value = await readRef.current();
                if (!cancelled) setState({ value, updatedAt: new Date(), failed: false });
            } catch {
                if (!cancelled) setState(previous => ({ ...previous, failed: true }));
            }

            // Scheduled after the read rather than on a fixed interval, so a slow server is never asked again while it
            // is still answering the previous request.
            if (!cancelled) timer = setTimeout(poll, intervalMilliseconds);
        };

        poll();

        return () => {
            cancelled = true;
            if (timer) clearTimeout(timer);
        };
    }, [intervalMilliseconds, ...dependencies]);

    return state;
};
