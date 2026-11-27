// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { type ObserverLag } from 'Features/Observation';
import { ObserverLagController, type ObserverLagReader, type ObserverLagTarget, observerLagKey } from './ObserverLagController';
import { readObserverLag } from './readObserverLag';

const refreshIntervalMs = 30_000;
const visibilityDebounceMs = 250;
const clockIntervalMs = 5_000;

/** What a view needs to show sampled lag: the measurement per observer, the moment to judge it at, and how to report what is on screen. */
export interface ObserverLagView {
    lagFor: (target: ObserverLagTarget) => ObserverLag | undefined;
    hasResult: (target: ObserverLagTarget) => boolean;
    now: number;
    /** Reports a rendered cell as on screen while it is intersecting; returns what stops reporting it. */
    trackCell: (element: HTMLElement, target: ObserverLagTarget) => () => void;
}

/**
 * Reads the sampled lag of the observers on screen and the selected one, for one event store and namespace, and stops when the
 * view unmounts or the page is hidden. Everything is scoped to the store and namespace: changing either starts over.
 * @param eventStore The routed event store.
 * @param namespace The routed namespace.
 * @param selected The observer whose details are shown, if any.
 * @param reader Reads one observer; replaceable so the view can be exercised without a server.
 */
export const useObserverLag = (
    eventStore: string,
    namespace: string,
    selected: ObserverLagTarget | undefined,
    reader: ObserverLagReader = readObserverLag): ObserverLagView => {
    const [, setVersion] = useState(0);
    const [now, setNow] = useState(() => Date.now());
    const elements = useRef(new Map<Element, ObserverLagTarget>());
    const observer = useRef<IntersectionObserver | null>(null);
    const debounce = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

    const controller = useMemo(
        () => new ObserverLagController({ reader, onChange: () => { setNow(Date.now()); setVersion(version => version + 1); } }),
        // A new store or namespace is a new controller: nothing read for the old scope can be shown in the new one.
        [eventStore, namespace, reader]);

    useEffect(() => {
        const refreshSoon = () => {
            clearTimeout(debounce.current);
            debounce.current = setTimeout(() => controller.refresh(), visibilityDebounceMs);
        };

        observer.current = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                const target = elements.current.get(entry.target);
                if (target) controller.setVisible(target, entry.isIntersecting);
            });
            refreshSoon();
        });
        elements.current.forEach((_, element) => observer.current?.observe(element));

        const onVisibilityChange = () => controller.setPaused(document.hidden);
        document.addEventListener('visibilitychange', onVisibilityChange);
        controller.setPaused(document.hidden);

        const refreshTimer = setInterval(() => controller.refresh(), refreshIntervalMs);
        const clockTimer = setInterval(() => { if (!document.hidden) setNow(Date.now()); }, clockIntervalMs);

        return () => {
            clearTimeout(debounce.current);
            clearInterval(refreshTimer);
            clearInterval(clockTimer);
            document.removeEventListener('visibilitychange', onVisibilityChange);
            observer.current?.disconnect();
            observer.current = null;
            controller.dispose();
        };
    }, [controller]);

    const selectedKey = selected ? observerLagKey(selected) : undefined;
    useEffect(() => {
        controller.select(selected);
        // The key is what identifies the selection; a new object for the same observer is not a new selection.
    }, [controller, selectedKey]);

    const trackCell = useCallback((element: HTMLElement, target: ObserverLagTarget) => {
        elements.current.set(element, target);
        observer.current?.observe(element);

        return () => {
            elements.current.delete(element);
            observer.current?.unobserve(element);
            controller.setVisible(target, false);
        };
    }, [controller]);

    const lagFor = useCallback((target: ObserverLagTarget) => controller.get(target), [controller]);

    const hasResult = useCallback((target: ObserverLagTarget) => controller.hasResult(target), [controller]);

    return { lagFor, hasResult, now, trackCell };
};
