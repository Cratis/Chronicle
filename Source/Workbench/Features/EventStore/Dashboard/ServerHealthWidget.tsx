// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Link } from 'react-router-dom';
import { MdDns } from 'react-icons/md';
import { AllServerInstances } from 'Features/Servers';
import { AllEventTypes } from 'Features/EventTypes';
import { AllProjections } from 'Features/ProjectionEditor';
import { AllReadModelDefinitions } from 'Features/ReadModelDefinitions';
import { ObserveNamespaces } from 'Features/Namespaces';
import { version } from '../../../version';
import { WidgetShell } from './WidgetShell';
import strings from 'Strings';

export interface IServerHealthWidget {

    /**
     * The event store the dashboard is for.
     */
    eventStore: string;

    /**
     * The namespace the dashboard is for, when it is for one.
     */
    namespace?: string;

    /**
     * How many events are held, or undefined while loading.
     */
    totalEvents?: number;

    /**
     * The tail sequence number of the event log, when the dashboard is for a namespace and it holds events.
     */
    tail?: number;

    /**
     * The path the event store pages live under, for the catalog links.
     */
    basePath: string;

    /**
     * When the figures were last read.
     */
    updatedAt?: Date;

    /**
     * Additional class names.
     */
    className?: string;
}

const texts = strings.eventStore.dashboard;

const formatBytes = (bytes: number): string => {
    if (bytes >= 1024 ** 3) return `${(bytes / 1024 ** 3).toFixed(1)} GB`;
    if (bytes >= 1024 ** 2) return `${(bytes / 1024 ** 2).toFixed(0)} MB`;
    return `${(bytes / 1024).toFixed(0)} KB`;
};

const formatUpdated = (updatedAt: Date | undefined): string => {
    if (!updatedAt) return texts.updatedNever;
    const seconds = Math.max(0, Math.round((Date.now() - updatedAt.getTime()) / 1000));
    return seconds <= 1 ? texts.updatedJustNow : texts.updatedSecondsAgo.replace('{seconds}', seconds.toString());
};

/**
 * The health rail: whether the server is there, what it is running on, what the dashboard is looking at and what
 * the event store holds.
 */
export const ServerHealthWidget = ({ eventStore, namespace, totalEvents, tail, basePath, updatedAt, className }: IServerHealthWidget) => {
    const [servers] = AllServerInstances.use();
    const [eventTypes] = AllEventTypes.use({ eventStore });
    const [projections] = AllProjections.use({ eventStore });
    const [readModels] = AllReadModelDefinitions.use({ eventStore });
    const [namespaces] = ObserveNamespaces.use({ eventStore });

    const instances = servers.data ?? [];
    const connected = servers.isSuccess && instances.length > 0;
    const cpu = instances.length > 0 ? instances.reduce((sum, _) => sum + _.cpuUsagePercentage, 0) / instances.length : undefined;
    const memory = instances.reduce((sum, _) => sum + _.memoryUsageBytes, 0);
    const namespacePath = `${basePath}/${namespace ?? 'Default'}`;

    const catalog = [
        { label: texts.eventTypes, count: eventTypes.data?.length, to: `${basePath}/event-types` },
        { label: texts.projections, count: projections.data?.length, to: `${namespacePath}/projections` },
        { label: texts.readModels, count: readModels.data?.length, to: `${basePath}/read-model-types` },
        { label: texts.catalogNamespaces, count: namespaces.data?.length, to: `${basePath}/namespaces` }
    ];

    return (
        <WidgetShell title={texts.serverHealth} icon={<MdDns />} tone={connected ? 'info' : 'danger'} className={className}>
            <div className='dashboard-rail__status'>
                <span className='dashboard-stat'>
                    <span className={`dashboard-dot dashboard-dot--${connected ? 'success' : 'danger'}`} aria-hidden='true' />
                    <span className='dashboard-stat__value'>{connected ? texts.connected : texts.disconnected}</span>
                </span>
                <span className='dashboard-rail__label'>v{version.version}</span>
            </div>

            {instances.length > 0 && (
                <div className='dashboard-rail__section'>
                    <div className='dashboard-rail__section-title'>{texts.cluster}</div>
                    <div className='dashboard-rail__rows'>
                        <span className='dashboard-rail__label'>{texts.servers}</span>
                        <span className='dashboard-rail__value'>{instances.length}</span>
                        <span className='dashboard-rail__label'>{texts.cpu}</span>
                        <span className='dashboard-rail__value'>
                            {cpu!.toFixed(1)}%
                            <div className='dashboard-meter'>
                                <div
                                    className={`dashboard-meter__fill ${cpu! > 85 ? 'dashboard-meter__fill--danger' : cpu! > 60 ? 'dashboard-meter__fill--warning' : ''}`}
                                    style={{ width: `${Math.min(100, cpu!)}%` }} />
                            </div>
                        </span>
                        <span className='dashboard-rail__label'>{texts.memory}</span>
                        <span className='dashboard-rail__value'>{formatBytes(memory)}</span>
                    </div>
                </div>
            )}

            <div className='dashboard-rail__section'>
                <div className='dashboard-rail__section-title'>{texts.context}</div>
                <div className='dashboard-rail__rows'>
                    <span className='dashboard-rail__label'>{texts.store}</span>
                    <span className='dashboard-rail__value' title={eventStore}>{eventStore}</span>
                    {namespace && (<>
                        <span className='dashboard-rail__label'>{texts.namespace}</span>
                        <span className='dashboard-rail__value' title={namespace}>{namespace}</span>
                        <span className='dashboard-rail__label'>{texts.tailSequence}</span>
                        <span className='dashboard-rail__value'>{tail === undefined ? '–' : `#${tail.toLocaleString()}`}</span>
                    </>)}
                    <span className='dashboard-rail__label'>{texts.events}</span>
                    <span className='dashboard-rail__value'>{totalEvents === undefined ? '–' : totalEvents.toLocaleString()}</span>
                </div>
            </div>

            <div className='dashboard-rail__section'>
                <div className='dashboard-rail__section-title'>{texts.catalog}</div>
                <div className='dashboard-rail__catalog'>
                    {catalog.map(entry => (
                        <Link key={entry.label} to={entry.to} className='contents'>
                            <span className='dashboard-rail__count'>{entry.count === undefined ? '–' : entry.count.toLocaleString()}</span>
                            <span className='dashboard-rail__label'>{entry.label}</span>
                        </Link>
                    ))}
                </div>
            </div>

            <div className='dashboard-rail__footer'>⟳ {formatUpdated(updatedAt)}</div>
        </WidgetShell>
    );
};
