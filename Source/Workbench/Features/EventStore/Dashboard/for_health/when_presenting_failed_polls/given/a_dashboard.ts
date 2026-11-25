// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement, type ReactNode } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { vi } from 'vitest';
import { QueryResult, QueryResultWithState } from '@cratis/arc/queries';
import { ObserveNamespaces } from 'Features/Namespaces';
import { AllObservers, GetFailedPartitions } from 'Features/Observation';
import { AllJobs } from 'Features/Jobs';
import { GetRecommendations } from 'Features/Recommendations';
import { StatisticsForEventStore, StatisticsForNamespace } from 'Features/Statistics';
import { Dashboard } from '../../../Dashboard';

// Keep the real polling hook and readers, running their effects against deterministic hook slots.
const hooks = vi.hoisted(() => ({
    states: [] as unknown[],
    refs: [] as { current: unknown }[],
    effects: [] as (() => void | (() => void))[],
    cleanups: [] as (() => void)[],
    stateIndex: 0,
    refIndex: 0,
    mounted: false
}));
vi.mock('react', async importOriginal => ({
    ...await importOriginal<typeof import('react')>(),
    useState: (initial: unknown) => {
        const index = hooks.stateIndex++;
        if (!(index in hooks.states)) hooks.states[index] = initial;
        return [hooks.states[index], (next: unknown) => {
            hooks.states[index] = typeof next === 'function' ? next(hooks.states[index]) : next;
        }];
    },
    useRef: (initial: unknown) => {
        const index = hooks.refIndex++;
        hooks.refs[index] ??= { current: initial };
        return hooks.refs[index];
    },
    useEffect: (effect: () => void | (() => void)) => {
        if (!hooks.mounted) hooks.effects.push(effect);
    }
}));
vi.mock('react-router-dom', () => ({
    useParams: () => ({ eventStore: 'the-store' }),
    Link: ({ to, children, className }: { to: string; children: ReactNode; className?: string }) =>
        createElement('a', { href: to, className }, children)
}));
vi.mock('Components/Card', () => ({
    Card: ({ header, children, className }: { header: ReactNode; children: ReactNode; className?: string }) =>
        createElement('section', { className }, header, children)
}));
vi.mock('Components/Common/Page', () => ({ Page: ({ children }: { children: ReactNode }) => children }));
vi.mock('../../../ServerHealthWidget', () => ({ ServerHealthWidget: () => null }));
vi.mock('../../../ThroughputWidget', () => ({ ThroughputWidget: () => null }));
vi.mock('../../../ObserversOverTimeWidget', () => ({ ObserversOverTimeWidget: () => null }));
vi.mock('../../../TopEventTypesWidget', () => ({ TopEventTypesWidget: () => null }));

export class a_dashboard {
    observers = vi.spyOn(AllObservers.prototype, 'perform');
    partitions = vi.spyOn(GetFailedPartitions.prototype, 'perform');
    counts = vi.spyOn(StatisticsForNamespace.prototype, 'perform');
    html = '';

    constructor() {
        hooks.states = [];
        hooks.refs = [];
        hooks.effects = [];
        hooks.cleanups = [];
        hooks.mounted = false;
        vi.useFakeTimers();
        vi.spyOn(StatisticsForEventStore, 'use').mockReturnValue([
            QueryResultWithState.empty({ totalEvents: 17, eventTypes: 0, namespaces: 2, perEventType: [] }), vi.fn(), vi.fn()
        ]);
        vi.spyOn(ObserveNamespaces, 'use').mockReturnValue([
            QueryResultWithState.empty([{ id: 'first', name: 'first' }, { id: 'second', name: 'second' }]), vi.fn()
        ]);
        this.observers.mockResolvedValue(QueryResult.empty([]));
        this.partitions.mockResolvedValue(QueryResult.empty([]));
        vi.spyOn(GetRecommendations.prototype, 'perform').mockResolvedValue(QueryResult.empty([]));
        vi.spyOn(AllJobs.prototype, 'perform').mockResolvedValue(QueryResult.empty([]));
        this.counts.mockResolvedValue(QueryResult.empty({ totalEvents: 17, eventTypes: 0, namespaces: 1, perEventType: [] }));
    }

    render() {
        hooks.stateIndex = 0;
        hooks.refIndex = 0;
        this.html = renderToStaticMarkup(createElement(Dashboard));
        if (!hooks.mounted) {
            hooks.mounted = true;
            hooks.effects.forEach(effect => {
                const cleanup = effect();
                if (cleanup) hooks.cleanups.push(cleanup);
            });
        }
        return this.html;
    }

    async settle() {
        await vi.advanceTimersByTimeAsync(0);
        return this.render();
    }

    async poll(milliseconds = 10_000) {
        await vi.advanceTimersByTimeAsync(milliseconds);
        return this.render();
    }

    widget(title: string) {
        return [...this.html.matchAll(/<section\b[^>]*>[\s\S]*?<\/section>/g)]
            .map(match => match[0]).find(widget => widget.includes(title))!;
    }

    row(namespace = 'first') {
        return [...this.html.matchAll(/<tr>[\s\S]*?<\/tr>/g)]
            .map(match => match[0]).find(row => row.includes(`>${namespace}</a>`))!;
    }

    cells(namespace = 'first') {
        return [...this.row(namespace).matchAll(/<td>[\s\S]*?<\/td>/g)].map(match => match[0]);
    }

    dispose() {
        hooks.cleanups.forEach(cleanup => cleanup());
        vi.useRealTimers();
        vi.restoreAllMocks();
    }
}
