// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PivotDimension, PivotFilter, PivotViewer } from '@cratis/components/PivotViewer';
import { AppendedEvents, AppendedEventsParameters } from 'Api/EventSequences';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { Page } from 'Components/Common/Page';
import strings from 'Strings';
import { AppendedEvent } from 'Api/Events';
import { ObjectContentViewer } from 'Components/ObjectContentViewer';
import { AllEventTypesWithSchemas } from 'Api/EventTypes/AllEventTypesWithSchemas';
import { EventTypeRegistration } from 'Api/Events/EventTypeRegistration';
import { QueryResultWithState } from '@cratis/arc/queries';

const pad = (value: number) => value.toString().padStart(2, '0');

const ellipsis = (value: string) => {
    const maxLength = 20;
    return value.length > maxLength ? `${value.substring(0, maxLength)}...` : value;
};

const hourBucket = (isoDate: string) => {
    const date = new Date(isoDate);
    const bucket = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:00:00`;
    return bucket;
};

const dimensions: PivotDimension<AppendedEvent>[] = [
    {
        key: 'sequence',
        label: 'Sequence',
        getValue: (item) => item.context.sequenceNumber.toString(),
        sort: (a, b) => a.label.localeCompare(b.label),
    },
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

const detailRenderer = (event: AppendedEvent, eventTypes: QueryResultWithState<EventTypeRegistration[]>) => {
    const eventType = eventTypes.data?.find((et: EventTypeRegistration) => et.type.id === event.context.eventType.id);
    const schema = eventType ? JSON.parse(eventType.schema) : { properties: {} };
    const content = typeof event.content === 'string' ? JSON.parse(event.content) : event.content;

    return (
        <div style={{ padding: '20px', height: '100%', overflow: 'auto' }}>
            <h2 style={{ marginTop: 0, marginBottom: '20px', color: 'var(--text-color)' }}>{event.context.eventType.id}</h2>
            <ObjectContentViewer object={content} schema={schema} />
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
    const [eventTypes] = AllEventTypesWithSchemas.use({ eventStore: params.eventStore! });

    return (
        <Page title={strings.mainMenu.pivot} noBackground noPadding>
            <div className="p-4 h-full flex flex-col min-h-0">
                <PivotViewer<AppendedEvent>
                    data={events.data ?? []}
                    dimensions={dimensions}
                    filters={filters}
                    defaultDimensionKey="type"
                    cardRenderer={(event) => ({
                        title: ellipsis(event.context.eventType.id),
                        labels: ['Sequence #', 'Occurred'],
                        values: [String(event.context.sequenceNumber), event.context.occurred.toLocaleString()],
                    })}

                    detailRenderer={(item) => detailRenderer(item, eventTypes)}
                    getItemId={(item) => String(item.context.sequenceNumber)}
                    searchFields={[
                        (_) => _.context.eventType.id,
                        (_) => _.context.correlationId.toString()
                    ]}
                    className="flex-1 min-h-0"
                    emptyContent={<span>No events match the current filters.</span>}
                    isLoading={events.isPerforming}
                />
            </div>
        </Page>
    );
};
