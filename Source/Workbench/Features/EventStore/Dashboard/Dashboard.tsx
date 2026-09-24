// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventTypeDistributionWidget } from './EventTypeDistributionWidget';
import { KeyFiguresWidget } from './KeyFiguresWidget';
import { NamespaceBreakdownWidget } from './NamespaceBreakdownWidget';
import { Page } from 'Components/Common/Page';
import { StatisticsForEventStore } from 'Features/Statistics';
import { ObserveNamespaces } from 'Features/Namespaces';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { useRelativePath } from '../../../Utils/useRelativePath';
import strings from 'Strings';

/**
 * The event store wide dashboard.
 *
 * Reads the globally scoped half of the statistics projection, which is materialized against the event store
 * rather than any one namespace - so the figures here are one read rather than a read per namespace.
 */
export const Dashboard = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const eventStore = params.eventStore!;
    const basePath = `${useRelativePath('event-store')}/${eventStore}`;

    const [statistics] = StatisticsForEventStore.use({ eventStore });
    const [namespaces] = ObserveNamespaces.use({ eventStore });

    return (
        <Page title={strings.eventStore.dashboard.title} noBackground>
            <div className='flex h-full flex-col gap-4 overflow-auto pb-4'>
                <KeyFiguresWidget statistics={statistics.data} subtitle={eventStore} />

                <div className='grid gap-4 lg:grid-cols-3'>
                    <EventTypeDistributionWidget
                        className='lg:col-span-2'
                        perEventType={statistics.data.perEventType ?? []} />

                    <NamespaceBreakdownWidget
                        namespaces={namespaces.data.map(_ => _.name)}
                        basePath={basePath} />
                </div>
            </div>
        </Page>
    );
};
