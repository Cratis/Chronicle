// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Link } from 'react-router-dom';
import { MdApps } from 'react-icons/md';
import { NamespaceHealth } from './health';
import { WidgetShell } from './WidgetShell';
import { pollFailure } from './pollFailure';
import strings from 'Strings';

export interface INamespacesWidget {

    /**
     * The namespaces of the event store.
     */
    namespaces: string[];

    /**
     * The health figures per namespace, or undefined while loading.
     */
    health: Record<string, NamespaceHealth> | undefined;

    /**
     * How many events each namespace holds, or undefined while loading.
     */
    eventCounts: Record<string, number> | undefined;

    /**
     * Whether the latest combined health read failed, making all retained health figures stale.
     */
    healthFailed?: boolean;

    /**
     * Whether the latest event count read failed, independently of health.
     */
    eventCountsFailed?: boolean;

    /**
     * The path the event store pages live under.
     */
    basePath: string;

    /**
     * Additional class names.
     */
    className?: string;
}

const texts = strings.eventStore.dashboard;

const figure = (value: number | undefined, tone?: 'danger' | 'warning', failed = false) => {
    if (failed) return <span className='dashboard-table__muted'>{value?.toLocaleString() ?? '–'} ({pollFailure(value, failed)})</span>;
    if (value === undefined) return <span className='dashboard-table__muted'>–</span>;
    if (value === 0) return <span className='dashboard-table__muted'>0</span>;
    return <span className={tone ? `dashboard-table__${tone}` : undefined}>{value.toLocaleString()}</span>;
};

const eventCountFor = (counts: Record<string, number> | undefined, namespace: string, failed: boolean) => {
    if (counts === undefined) return undefined;
    return failed ? counts[namespace] : counts[namespace] ?? 0;
};

interface NamespaceRowProps {
    namespace: string;
    figures: NamespaceHealth | undefined;
    eventCount: number | undefined;
    basePath: string;
    healthFailed: boolean;
    eventCountsFailed: boolean;
}

const NamespaceRow = ({ namespace, figures, eventCount, basePath, healthFailed, eventCountsFailed }: NamespaceRowProps) => {
    const stopped = figures ? figures.observers.disconnected + figures.observers.quarantined : undefined;
    return (
        <tr>
            <td><Link to={`${basePath}/${namespace}/dashboard`}>{namespace}</Link></td>
            <td>{figure(eventCount, undefined, eventCountsFailed)}</td>
            <td>{figure(figures?.observers.total, undefined, healthFailed)}{stopped ? <span className='dashboard-table__danger'> ({stopped} {texts.stopped})</span> : null}</td>
            <td>{figure(figures?.observers.active, undefined, healthFailed)}</td>
            <td>{figure(figures?.observers.lag, 'warning', healthFailed)}</td>
            <td>{figure(figures?.failedPartitions, 'danger', healthFailed)}</td>
            <td>{figure(figures?.recommendations, 'warning', healthFailed)}</td>
            <td>{figure(figures?.runningJobs, undefined, healthFailed)}</td>
        </tr>
    );
};

/**
 * Every namespace of an event store with its health, each leading to its own dashboard.
 */
export const NamespacesWidget = ({ namespaces, health, eventCounts, healthFailed = false, eventCountsFailed = false, basePath, className }: INamespacesWidget) => (
    <WidgetShell title={texts.namespaces} icon={<MdApps />} tone='info' subtitle={texts.namespacesSubtitle} className={className}>
        {namespaces.length === 0
            ? <span className='dashboard-widget__empty'>{texts.noNamespaces}</span>
            : (
                <div className='overflow-auto'>
                    <table className='dashboard-table'>
                        <thead>
                            <tr>
                                <th>{texts.namespace}</th>
                                <th>{texts.events}</th>
                                <th>{texts.observers}</th>
                                <th>{texts.active}</th>
                                <th>{texts.eventsBehind}</th>
                                <th>{texts.failedPartitionsColumn}</th>
                                <th>{texts.recommendations}</th>
                                <th>{texts.jobs}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {namespaces.map(namespace => (
                                <NamespaceRow
                                    key={namespace}
                                    namespace={namespace}
                                    figures={health?.[namespace]}
                                    eventCount={eventCountFor(eventCounts, namespace, eventCountsFailed)}
                                    basePath={basePath}
                                    healthFailed={healthFailed}
                                    eventCountsFailed={eventCountsFailed} />
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
    </WidgetShell>
);
