// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PivotDimension, PivotFilter, PivotViewer } from '@cratis/components/PivotViewer';
import { AppendedEvents, AppendedEventsParameters } from 'Api/EventSequences';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { Page } from 'Components/Common/Page';
import strings from 'Strings';
import { AppendedEvent } from 'Api/Events';

const pad = (value: number) => value.toString().padStart(2, '0');

const hourBucket = (isoDate: string) => {
    const date = new Date(isoDate);
    const bucket = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:00:00`;
    return bucket;
};

const dimensions: PivotDimension<AppendedEvent>[] = [
    {
        key: 'type',
        label: 'Event Type',
        getValue: (item) => item.context.eventType.id,
        sort: (a, b) => b.items.length - a.items.length,
    },
    {
        key: 'correlation',
        label: 'Correlation',
        getValue: (item) => item.context.correlationId.toString(),
        sort: (a, b) => a.label.localeCompare(b.label),
    },
    {
        key: 'timeline-hour',
        label: 'Occurred (Hour)',
        getValue: (item) => hourBucket(item.context.occurred.toISOString()),
        formatValue: (value) => {
            if (typeof value !== 'string') {
                return 'Unknown';
            }
            const date = new Date(value);
            return date.toLocaleString(undefined, {
                dateStyle: 'medium',
                timeStyle: 'short',
            });
        },
        sort: (a, b) => String(a.value).localeCompare(String(b.value)),
    },
    {
        key: 'origin',
        label: 'Origin Event',
        getValue: (item) => (item.context.causation.length > 0 ? item.context.causation[0].type : 'Origin'),
        sort: (a, b) => a.label.localeCompare(b.label),
    },
];

const filters: PivotFilter<AppendedEvent>[] = [
    {
        key: 'correlationId',
        label: 'Correlation ID',
        getValue: (item) => item.context.correlationId.toString(),
        multi: true,
    },
    {
        key: 'type',
        label: 'Event Type',
        getValue: (item) => item.context.eventType.id,
        multi: true,
    },
    {
        key: 'time',
        label: 'Time',
        getValue: (item) => item.context.occurred.getHours(),
        type: 'number',
        buckets: 15,
    },
];

const cardRenderer = (event: AppendedEvent) => {
    const properties: Array<[string, string]> = [
        ['Type', event.context.eventType.id],
        ['Occurred', event.context.occurred.toLocaleString()],
        ['Correlation', event.context.correlationId.toString()]
    ];

    const limited = properties.slice(0, 3);
    const hasMore = properties.length > 3;

    return (
        <div className="pv-card-body">
            <div className="pv-card-title">{event.context.eventType.id}</div>
            <dl>
                {limited.map(([key, value]) => (
                    <div key={key} className="pv-card-row">
                        <dt>{key}</dt>
                        <dd>{value}</dd>
                    </div>
                ))}
                {hasMore && <div className="pv-card-more">…</div>}
            </dl>
        </div>
    );
};


export const Pivot = () => {
    const params = useParams<EventStoreAndNamespaceParams>();

    const queryArgs: AppendedEventsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        eventSequenceId: 'event-log'
    };

    const [events] = AppendedEvents.use(queryArgs);

    return (
        <Page title={strings.mainMenu.pivot}>
            <div className="p-4 h-full">
                <PivotViewer<AppendedEvent>
                    data={events.data}
                    dimensions={dimensions}
                    filters={filters}
                    defaultDimensionKey="type"
                    cardRenderer={cardRenderer}
                    getItemId={(item) => String(item.context.sequenceNumber)}
                    searchFields={[
                        (_) => _.context.eventType.id,
                        (_) => _.context.correlationId.toString()
                    ]}
                    emptyContent={<span>No events match the current filters.</span>}
                    isLoading={events.isPerforming}
                />
            </div>
        </Page>
    );
};
