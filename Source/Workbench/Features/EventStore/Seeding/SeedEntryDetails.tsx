// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IDetailsComponentProps } from 'Components/DataPage/DataPage';
import { SeedEntry } from 'Api/Seeding/SeedEntry';
import { AllEventTypesWithSchemas } from 'Api/EventTypes/AllEventTypesWithSchemas';
import { EventTypeRegistration } from 'Api/Events/EventTypeRegistration';
import { ObjectContent } from 'Components/ObjectContent';
import { useParams } from 'react-router-dom';
import { type EventStoreParams } from 'Shared';

export const SeedEntryDetails = ({ item }: IDetailsComponentProps<SeedEntry>) => {
    const params = useParams<EventStoreParams>();
    const [eventTypes] = AllEventTypesWithSchemas.use({ eventStore: params.eventStore! });

    const eventType = eventTypes.data?.find((et: EventTypeRegistration) => et.type.id === item.eventTypeId);
    const schema = eventType ? JSON.parse(eventType.schema) : { properties: {} };
    const content = typeof item.content === 'string' ? JSON.parse(item.content) : item.content;

    return (
        <div style={{ padding: '20px', height: '100%', overflow: 'auto' }}>
            <h2 style={{ marginTop: 0, marginBottom: '20px', color: 'var(--text-color)' }}>
                {item.eventTypeId}
            </h2>
            <div style={{ marginBottom: '10px', color: 'var(--text-color-secondary)' }}>
                <strong>Event Source:</strong> {item.eventSourceId}
            </div>
            <ObjectContent object={content} schema={schema} />
        </div>
    );
};
