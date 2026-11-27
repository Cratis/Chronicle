// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { type ObserverLag } from 'Features/Observation';
import { type ObserverLagReader, type ObserverLagTarget, observerLagKey } from './ObserverLagController';
import { ObserverLagSession, type PageVisibility } from './ObserverLagSession';
import { readObserverLag } from './readObserverLag';

const pageVisibility: PageVisibility = {
    isHidden: () => document.hidden,
    subscribe: onChange => {
        document.addEventListener('visibilitychange', onChange);
        return () => document.removeEventListener('visibilitychange', onChange);
    }
};

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
    const elements = useRef(new Map<Element, ObserverLagTarget>());
    const intersection = useRef<IntersectionObserver | null>(null);

    // A new store or namespace is a new session: nothing read for the old scope can be shown in the new one.
    const session = useMemo(
        () => new ObserverLagSession({ reader, visibility: pageVisibility, onChange: () => setVersion(version => version + 1) }),
        [eventStore, namespace, reader]);

    useEffect(() => {
        const observer = new IntersectionObserver(entries => entries.forEach(entry => {
            const target = elements.current.get(entry.target);
            if (target) session.setVisible(target, entry.isIntersecting);
        }));
        intersection.current = observer;
        elements.current.forEach((_, element) => observer.observe(element));
        session.attach();

        return () => {
            observer.disconnect();
            intersection.current = null;
            session.detach();
        };
    }, [session]);

    const selectedKey = selected ? observerLagKey(selected) : undefined;
    useEffect(() => {
        session.select(selected);
    }, [session, selectedKey]);

    const trackCell = useCallback((element: HTMLElement, target: ObserverLagTarget) => {
        elements.current.set(element, target);
        intersection.current?.observe(element);

        return () => {
            elements.current.delete(element);
            intersection.current?.unobserve(element);
            session.setVisible(target, false);
        };
    }, [session]);

    const lagFor = useCallback((target: ObserverLagTarget) => session.lagFor(target), [session]);
    const hasResult = useCallback((target: ObserverLagTarget) => session.hasResult(target), [session]);

    return { lagFor, hasResult, now: session.now, trackCell };
};
