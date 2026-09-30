// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { Page } from 'Components/Common/Page';
import { StatisticsForNamespace } from 'Features/Statistics';
import { AllFailedPartitions, ObserveObservers } from 'Features/Observation';
import { AllRecommendations } from 'Features/Recommendations';
import { ObserveJobs } from 'Features/Jobs';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useRelativePath } from '../../../../Utils/useRelativePath';
import { AttentionWidget } from '../../Dashboard/AttentionWidget';
import { collectAttention } from '../../Dashboard/attention';
import { countUnresolved, isJobRunning, NamespaceHealth, summarizeObservers } from '../../Dashboard/health';
import { HealthTiles } from '../../Dashboard/HealthTiles';
import { JobsWidget } from '../../Dashboard/JobsWidget';
import { ObserversOverTimeWidget } from '../../Dashboard/ObserversOverTimeWidget';
import { readTail } from '../../Dashboard/readers';
import { ServerHealthWidget } from '../../Dashboard/ServerHealthWidget';
import { ThroughputWidget } from '../../Dashboard/ThroughputWidget';
import { TopEventTypesWidget } from '../../Dashboard/TopEventTypesWidget';
import { usePolling } from '../../Dashboard/usePolling';
import strings from 'Strings';
import '../../Dashboard/Dashboard.css';

/**
 * How many items the attention list shows.
 */
const attentionLimit = 8;

/**
 * The namespace dashboard - the landing page of an event store. Laid out like the overview of the CLI workbench:
 * server health on the left, observer, failure, recommendation and job tiles beside it, and what is happening over
 * time below them.
 */
export const Dashboard = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const eventStore = params.eventStore!;
    const namespace = params.namespace!;
    const basePath = `${useRelativePath('event-store')}/${eventStore}`;
    const namespacePath = `${basePath}/${namespace}`;

    const [statistics] = StatisticsForNamespace.use({ eventStore, namespace });
    const [observers] = ObserveObservers.use({ eventStore, namespace });
    const [failedPartitions] = AllFailedPartitions.use({ eventStore, namespace });
    const [recommendations] = AllRecommendations.use({ eventStore, namespace });
    const [jobs] = ObserveJobs.use({ eventStore, namespace });
    const tail = usePolling(() => readTail(eventStore, namespace), 5_000, [eventStore, namespace]);

    const ready = observers.isReady && failedPartitions.isReady && recommendations.isReady && jobs.isReady;

    const health = useMemo<NamespaceHealth | undefined>(() => ready
        ? {
            observers: summarizeObservers(observers.data),
            failedPartitions: countUnresolved(failedPartitions.data),
            recommendations: recommendations.data.length,
            runningJobs: jobs.data.filter(isJobRunning).length
        }
        : undefined, [ready, observers.data, failedPartitions.data, recommendations.data, jobs.data]);

    const attention = useMemo(() => ready
        ? collectAttention(observers.data, failedPartitions.data, recommendations.data, attentionLimit)
        : undefined, [ready, observers.data, failedPartitions.data, recommendations.data]);

    const namespaces = useMemo(() => [namespace], [namespace]);

    return (
        <Page title={strings.eventStore.dashboard.title} noBackground>
            <div className='dashboard'>
                <ServerHealthWidget
                    className='dashboard__rail'
                    eventStore={eventStore}
                    namespace={namespace}
                    totalEvents={statistics.isReady && statistics.isSuccess ? Number(statistics.data.totalEvents ?? 0) : undefined}
                    tail={tail.value}
                    basePath={basePath}
                    updatedAt={tail.updatedAt} />

                <HealthTiles
                    health={health}
                    links={{
                        observers: `${namespacePath}/observers`,
                        failedPartitions: `${namespacePath}/failed-partitions`,
                        recommendations: `${namespacePath}/recommendations`,
                        jobs: `${namespacePath}/jobs`
                    }} />

                <ThroughputWidget className='dashboard__wide' eventStore={eventStore} namespaces={namespaces} />

                <ObserversOverTimeWidget observers={health?.observers} scope={`${eventStore}/${namespace}`} />
                <TopEventTypesWidget
                    perEventType={statistics.isReady && statistics.isSuccess ? statistics.data.perEventType ?? [] : undefined}
                    failed={statistics.isReady && !statistics.isSuccess} />

                <AttentionWidget className='dashboard__wide' items={attention} namespacePath={namespacePath} />
                <JobsWidget jobs={jobs.isReady ? jobs.data : undefined} namespacePath={namespacePath} />
            </div>
        </Page>
    );
};
