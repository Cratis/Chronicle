// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Link } from 'react-router-dom';
import { MdApps } from 'react-icons/md';
import { NamespaceHealth } from './health';
import { WidgetShell } from './WidgetShell';
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
     * The path the event store pages live under.
     */
    basePath: string;

    /**
     * Additional class names.
     */
    className?: string;
}

const texts = strings.eventStore.dashboard;

const figure = (value: number | undefined, tone?: 'danger' | 'warning') => {
    if (value === undefined) return <span className='dashboard-table__muted'>–</span>;
    if (value === 0) return <span className='dashboard-table__muted'>0</span>;
    return <span className={tone ? `dashboard-table__${tone}` : undefined}>{value.toLocaleString()}</span>;
};

/**
 * Every namespace of an event store with its health, each leading to its own dashboard.
 */
export const NamespacesWidget = ({ namespaces, health, eventCounts, basePath, className }: INamespacesWidget) => (
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
                            {namespaces.map(namespace => {
                                const figures = health?.[namespace];
                                const stopped = figures ? figures.observers.disconnected + figures.observers.quarantined : undefined;
                                return (
                                    <tr key={namespace}>
                                        <td><Link to={`${basePath}/${namespace}/dashboard`}>{namespace}</Link></td>
                                        <td>{figure(eventCounts === undefined ? undefined : eventCounts[namespace] ?? 0)}</td>
                                        <td>{figure(figures?.observers.total)}{stopped ? <span className='dashboard-table__danger'> ({stopped} {texts.stopped})</span> : null}</td>
                                        <td>{figure(figures?.observers.active)}</td>
                                        <td>{figure(figures?.observers.lag, 'warning')}</td>
                                        <td>{figure(figures?.failedPartitions, 'danger')}</td>
                                        <td>{figure(figures?.recommendations, 'warning')}</td>
                                        <td>{figure(figures?.runningJobs)}</td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                </div>
            )}
    </WidgetShell>
);
