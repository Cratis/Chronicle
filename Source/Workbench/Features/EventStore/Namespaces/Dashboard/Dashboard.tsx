// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventTypeDistributionWidget } from '../../Dashboard/EventTypeDistributionWidget';
import { KeyFiguresWidget } from '../../Dashboard/KeyFiguresWidget';
import { Page } from 'Components/Common/Page';
import { StatisticsForNamespace } from 'Features/Statistics';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import strings from 'Strings';

/**
 * The dashboard for a single namespace.
 *
 * The same figures as the event store wide dashboard, read from the namespaced half of the same projection - so
 * drilling in shows the same numbers computed the same way, rather than a second definition of what they mean.
 */
export const Dashboard = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const eventStore = params.eventStore!;
    const namespace = params.namespace!;

    const [statistics] = StatisticsForNamespace.use({ eventStore, namespace });

    return (
        <Page title={strings.eventStore.dashboard.title} noBackground>
            <div className='flex h-full flex-col gap-4 overflow-auto pb-4'>
                <KeyFiguresWidget statistics={statistics.data} subtitle={`${eventStore} / ${namespace}`} />

                <EventTypeDistributionWidget perEventType={statistics.data.perEventType ?? []} />
            </div>
        </Page>
    );
};
