// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TabView, TabPanel } from 'primereact/tabview';
import { AppendedEvent } from 'Api/Events';
import { IDetailsComponentProps } from 'Components';
import { AllEventTypesWithSchemas } from 'Api/EventTypes/AllEventTypesWithSchemas';
import { EventTypeRegistration } from 'Api/Events/EventTypeRegistration';
import { ObjectContentViewer } from 'Components/ObjectContentViewer';
import { useParams } from 'react-router-dom';
import { type EventStoreParams } from 'Shared';

export const EventDetails = ({ item }: IDetailsComponentProps<AppendedEvent>) => {
    const params = useParams<EventStoreParams>();
    const [eventTypes] = AllEventTypesWithSchemas.use({ eventStore: params.eventStore! });

    const eventType = eventTypes.data?.find((et: EventTypeRegistration) => et.type.id === item.context.eventType.id);
    const schema = eventType ? JSON.parse(eventType.schema) : { properties: {} };
    const content = typeof item.content === 'string' ? JSON.parse(item.content) : item.content;

    // Build context object for display
    const contextObject = {
        eventType: item.context.eventType.id,
        eventSourceType: item.context.eventSourceType,
        eventSourceId: item.context.eventSourceId,
        sequenceNumber: item.context.sequenceNumber,
        eventStreamType: item.context.eventStreamType,
        eventStreamId: item.context.eventStreamId,
        occurred: item.context.occurred.toISOString(),
        correlationId: item.context.correlationId.toString(),
        causedBy: {
            name: item.context.causedBy.name,
            subject: item.context.causedBy.subject
        },
        causation: item.context.causation.map(c => ({
            type: c.type,
            occurred: c.occurred.toISOString(),
            properties: c.properties
        }))
    };

    // Schema for context (we can define a basic schema for better display)
    const contextSchema = {
        type: 'object',
        properties: {
            eventType: { type: 'string', title: 'Event Type' },
            eventSourceType: { type: 'string', title: 'Event Source Type' },
            eventSourceId: { type: 'string', title: 'Event Source ID' },
            sequenceNumber: { type: 'number', title: 'Sequence Number' },
            eventStreamType: { type: 'string', title: 'Event Stream Type' },
            eventStreamId: { type: 'string', title: 'Event Stream ID' },
            occurred: { type: 'string', title: 'Occurred', format: 'date-time' },
            correlationId: { type: 'string', title: 'Correlation ID' },
            causedBy: {
                type: 'object',
                title: 'Caused By',
                properties: {
                    name: { type: 'string', title: 'Name' },
                    subject: { type: 'string', title: 'Subject' }
                }
            },
            causation: {
                type: 'array',
                title: 'Causation',
                items: {
                    type: 'object',
                    properties: {
                        type: { type: 'string', title: 'Type' },
                        occurred: { type: 'string', title: 'Occurred', format: 'date-time' },
                        properties: { type: 'object', title: 'Properties' }
                    }
                }
            }
        }
    };

    return (
        <div style={{ height: '100%', display: 'flex', flexDirection: 'column', padding: '20px' }}>
            <h2 style={{ marginTop: 0, marginBottom: '20px', color: 'var(--text-color)' }}>
                {item.context.eventType.id}
            </h2>
            <TabView style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
                <TabPanel header="Context">
                    <div style={{ height: '100%', overflow: 'auto' }}>
                        <ObjectContentViewer
                            object={contextObject}
                            schema={contextSchema}
                            timestamp={item.context.occurred}
                        />
                    </div>
                </TabPanel>
                <TabPanel header="Content">
                    <div style={{ height: '100%', overflow: 'auto' }}>
                        <ObjectContentViewer
                            object={content}
                            schema={schema}
                            timestamp={item.context.occurred}
                        />
                    </div>
                </TabPanel>
            </TabView>
        </div>
    );
};
