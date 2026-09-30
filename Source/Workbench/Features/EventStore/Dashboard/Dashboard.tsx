// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { Page } from 'Components/Common/Page';
import { StatisticsForEventStore } from 'Features/Statistics';
import { ObserveNamespaces } from 'Features/Namespaces';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useRelativePath } from '../../../Utils/useRelativePath';
import { addHealth, noHealth } from './health';
import { HealthTiles } from './HealthTiles';
import { NamespacesWidget } from './NamespacesWidget';
import { ObserversOverTimeWidget } from './ObserversOverTimeWidget';
import { readEventCountsForNamespaces, readHealthForNamespaces } from './readers';
import { ServerHealthWidget } from './ServerHealthWidget';
import { ThroughputWidget } from './ThroughputWidget';
import { TopEventTypesWidget } from './TopEventTypesWidget';
import { usePolling } from './usePolling';
import strings from 'Strings';
import './Dashboard.css';

/**
 * The event store wide dashboard - the same layout as a namespace dashboard, with every figure added up across the
 * namespaces, and a breakdown of each namespace below it to drill into.
 */
export const Dashboard = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const eventStore = params.eventStore!;
    const basePath = `${useRelativePath('event-store')}/${eventStore}`;

    const [statistics] = StatisticsForEventStore.use({ eventStore });
    const [namespacesResult] = ObserveNamespaces.use({ eventStore });
    const namespaces = useMemo(() => namespacesResult.data.map(_ => _.name), [namespacesResult.data]);
    const namespacesKey = namespaces.join('|');

    const perNamespace = usePolling(() => readHealthForNamespaces(eventStore, namespaces), 10_000, [eventStore, namespacesKey]);
    const eventCounts = usePolling(() => readEventCountsForNamespaces(eventStore, namespaces), 30_000, [eventStore, namespacesKey]);

    const health = useMemo(() => perNamespace.value === undefined || namespaces.length === 0
        ? undefined
        : Object.values(perNamespace.value).reduce(addHealth, noHealth), [perNamespace.value, namespaces.length]);

    return (
        <Page title={strings.eventStore.dashboard.title} noBackground>
            <div className='dashboard'>
                <ServerHealthWidget
                    className='dashboard__rail'
                    eventStore={eventStore}
                    totalEvents={statistics.isReady && statistics.isSuccess ? Number(statistics.data.totalEvents ?? 0) : undefined}
                    basePath={basePath}
                    updatedAt={perNamespace.updatedAt} />

                <HealthTiles health={health} />

                {namespaces.length > 0
                    ? <ThroughputWidget className='dashboard__wide' eventStore={eventStore} namespaces={namespaces} />
                    : <div className='dashboard__wide' />}

                <ObserversOverTimeWidget observers={health?.observers} scope={eventStore} />
                <TopEventTypesWidget
                    perEventType={statistics.isReady && statistics.isSuccess ? statistics.data.perEventType ?? [] : undefined}
                    failed={statistics.isReady && !statistics.isSuccess} />

                <NamespacesWidget
                    className='dashboard__full'
                    namespaces={namespaces}
                    health={perNamespace.value}
                    eventCounts={eventCounts.value}
                    basePath={basePath} />
            </div>
        </Page>
    );
};
