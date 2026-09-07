// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { Page } from 'Components/Common/Page';
import strings from 'Strings';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { TailSequenceNumber, type TailSequenceNumberParameters } from 'Features/Sequences';
import { ObserveObservers, AllFailedPartitions, type ObserveObserversParameters, type AllFailedPartitionsParameters } from 'Features/Observation';
import { AllRecommendations, type AllRecommendationsParameters } from 'Features/Recommendations';
import { ObserveJobs, type ObserveJobsParameters } from 'Features/Jobs';
import { AllEventTypes, type AllEventTypesParameters } from 'Features/EventTypes';
import { AllProjections, type AllProjectionsParameters } from 'Features/ProjectionEditor';
import { AllReadModelDefinitions, type AllReadModelDefinitionsParameters } from 'Features/ReadModelDefinitions';
import { GetWebhooks, type GetWebhooksParameters } from 'Features/Observation/Webhooks';
import { GetExternalServices, type GetExternalServicesParameters } from 'Features/ExternalServices';
import { groupObserversByRunningState, countRunningJobs, countSubscriptions } from './dashboardAggregations';
import { DashboardTimeSeriesViewModel } from './DashboardTimeSeriesViewModel';
import { ServerHealthWidget } from './widgets/ServerHealthWidget';
import { ObserversWidget } from './widgets/ObserversWidget';
import { FailuresWidget } from './widgets/FailuresWidget';
import { RecommendationsWidget } from './widgets/RecommendationsWidget';
import { JobsWidget } from './widgets/JobsWidget';
import { EventThroughputWidget } from './widgets/EventThroughputWidget';
import { ObserversOverTimeWidget } from './widgets/ObserversOverTimeWidget';
import css from './Dashboard.module.css';

const pollIntervalMilliseconds = 5000;

export const Dashboard = withViewModel(DashboardTimeSeriesViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const navigate = useNavigate();
    const eventStore = params.eventStore!;
    const namespace = params.namespace!;

    const tailArgs: TailSequenceNumberParameters = { eventStore, namespace, eventSequenceId: 'event-log' };
    const [tail, performTail] = TailSequenceNumber.use(tailArgs);

    const performTailRef = useRef(performTail);
    performTailRef.current = performTail;

    useEffect(() => {
        const interval = setInterval(() => {
            void performTailRef.current({ eventStore, namespace, eventSequenceId: 'event-log' });
        }, pollIntervalMilliseconds);
        return () => clearInterval(interval);
    }, [eventStore, namespace]);

    useEffect(() => {
        if (tail.hasData) {
            viewModel.recordThroughputSample(tail.data.sequenceNumber);
        }
    }, [tail.hasData, tail.data.sequenceNumber, viewModel]);

    const observersArgs: ObserveObserversParameters = { eventStore, namespace };
    const [observers] = ObserveObservers.use(observersArgs);
    const observerBreakdown = groupObserversByRunningState(observers.data ?? []);

    useEffect(() => {
        if (observers.hasData) {
            viewModel.recordObserverSample(groupObserversByRunningState(observers.data));
        }
    }, [observers.hasData, observers.data, viewModel]);

    const failedPartitionsArgs: AllFailedPartitionsParameters = { eventStore, namespace };
    const [failedPartitions] = AllFailedPartitions.use(failedPartitionsArgs);

    const recommendationsArgs: AllRecommendationsParameters = { eventStore, namespace };
    const [recommendations] = AllRecommendations.use(recommendationsArgs);

    const jobsArgs: ObserveJobsParameters = { eventStore, namespace };
    const [jobs] = ObserveJobs.use(jobsArgs);

    const eventTypesArgs: AllEventTypesParameters = { eventStore };
    const [eventTypes] = AllEventTypes.use(eventTypesArgs);

    const projectionsArgs: AllProjectionsParameters = { eventStore };
    const [projections] = AllProjections.use(projectionsArgs);

    const readModelsArgs: AllReadModelDefinitionsParameters = { eventStore };
    const [readModels] = AllReadModelDefinitions.use(readModelsArgs);

    const webhooksArgs: GetWebhooksParameters = { eventStore };
    const [webhooks] = GetWebhooks.use(webhooksArgs);

    const externalServicesArgs: GetExternalServicesParameters = { eventStore };
    const [externalServices] = GetExternalServices.use(externalServicesArgs);

    const isConnected = tail.isSuccess || observers.isSuccess;

    return (
        <Page title={strings.eventStore.namespaces.dashboard.title} noBackground>
            <div className={css.dashboard}>
                <ServerHealthWidget
                    className={css.serverHealth}
                    eventStore={eventStore}
                    namespace={namespace}
                    isConnected={isConnected}
                    tailSequenceNumber={tail.hasData ? tail.data.sequenceNumber : undefined}
                    eventTypeCount={eventTypes.data?.length ?? 0}
                    projectionCount={projections.data?.length ?? 0}
                    readModelCount={readModels.data?.length ?? 0}
                    subscriptionCount={countSubscriptions(webhooks.data ?? [], externalServices.data ?? [])} />

                <ObserversWidget
                    className={css.observers}
                    breakdown={observerBreakdown}
                    onViewAll={() => navigate('../observers')} />

                <FailuresWidget
                    className={css.failures}
                    failedPartitionCount={(failedPartitions.data ?? []).length}
                    onViewAll={() => navigate('../failed-partitions')} />

                <RecommendationsWidget
                    className={css.recommendations}
                    pendingCount={recommendations.data?.length ?? 0}
                    onViewAll={() => navigate('../recommendations')} />

                <JobsWidget
                    className={css.jobs}
                    runningCount={countRunningJobs(jobs.data ?? [])}
                    totalCount={jobs.data?.length ?? 0}
                    onViewAll={() => navigate('../jobs')} />

                <EventThroughputWidget className={css.eventThroughput} samples={viewModel.throughputSamples} />

                <ObserversOverTimeWidget className={css.observersOverTime} samples={viewModel.observerSamples} />
            </div>
        </Page>
    );
});
