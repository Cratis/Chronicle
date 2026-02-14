// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult } from '@cratis/arc.react/dialogs';
import { Guid } from '@cratis/fundamentals';
import { Append } from 'Api/EventSequences';
import { AllEventTypesWithSchemas } from 'Api/EventTypes';
import { EventTypeRegistration } from 'Api/Events';
import { Dialog } from 'Components/Dialogs';
import { ObjectContent } from 'Components/ObjectContent';
import { InputText } from 'primereact/inputtext';
import { Dropdown } from 'primereact/dropdown';
import { Button } from 'primereact/button';
import { useState, useEffect } from 'react';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { Json } from 'Features/index';
import { JsonSchema } from 'Components/JsonSchema';

export const AppendEventDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [appendEvent] = Append.use();

    const [allEventTypes] = AllEventTypesWithSchemas.use({ eventStore: params.eventStore! });

    const [selectedEventType, setSelectedEventType] = useState<EventTypeRegistration | null>(null);
    const [eventSourceId, setEventSourceId] = useState('');
    const [eventContent, setEventContent] = useState<Json>({});
    const [schema, setSchema] = useState<JsonSchema>({ type: 'object', properties: {} });
    const [hasValidationErrors, setHasValidationErrors] = useState(false);

    useEffect(() => {
        if (selectedEventType) {
            try {
                const parsedSchema = JSON.parse(selectedEventType.schema) as JsonSchema;
                setSchema(parsedSchema);

                // Initialize eventContent with default values based on schema
                const initialContent: Record<string, Json> = {};
                if (parsedSchema.properties) {
                    Object.entries(parsedSchema.properties).forEach(([key, prop]) => {
                        if (prop.type === 'string') {
                            initialContent[key] = '';
                        } else if (prop.type === 'number' || prop.type === 'integer') {
                            initialContent[key] = 0;
                        } else if (prop.type === 'boolean') {
                            initialContent[key] = false;
                        } else if (prop.type === 'array') {
                            initialContent[key] = [];
                        } else if (prop.type === 'object') {
                            initialContent[key] = {};
                        } else {
                            initialContent[key] = null;
                        }
                    });
                }
                setEventContent(initialContent);
            } catch (error) {
                console.error('Failed to parse schema:', error);
                setSchema({ type: 'object', properties: {} });
                setEventContent({});
            }
        } else {
            setSchema({ type: 'object', properties: {} });
            setEventContent({});
        }
    }, [selectedEventType]);

    const generateGuid = () => {
        setEventSourceId(Guid.create().toString());
    };

    const handleClose = async (result: DialogResult) => {
        if (result !== DialogResult.Ok) {
            return true;
        }

        if (!selectedEventType || !eventSourceId) {
            return false;
        }

        appendEvent.eventStore = params.eventStore!;
        appendEvent.namespace = params.namespace!;
        appendEvent.eventSequenceId = 'event-log';
        appendEvent.eventSourceId = eventSourceId;
        appendEvent.eventStreamType = '';
        appendEvent.eventStreamId = '';
        appendEvent.eventType = selectedEventType.type;
        appendEvent.content = eventContent;

        const executeResult = await appendEvent.execute();
        return executeResult.isSuccess;
    };

    const isValid = selectedEventType !== null && eventSourceId.trim() !== '' && !hasValidationErrors;

    return (
        <Dialog
            title={strings.eventStore.namespaces.sequences.dialogs.appendEvent.title}
            onClose={handleClose}
            isValid={isValid}
            width="600px"
            resizable={true}
            okLabel="Append">
            <div className="card flex flex-column gap-3 mb-3">
                <div className="field mb-3">
                    <label htmlFor="eventType">{strings.eventStore.namespaces.sequences.dialogs.appendEvent.eventType}</label>
                    <Dropdown
                        id="eventType"
                        value={selectedEventType}
                        options={allEventTypes.data}
                        onChange={(e) => setSelectedEventType(e.value)}
                        optionLabel="type.id"
                        placeholder={strings.eventStore.namespaces.sequences.dialogs.appendEvent.selectEventType}
                        className="w-full"
                    />
                </div>

                <div className="field mb-3">
                    <label htmlFor="eventSourceId">{strings.eventStore.namespaces.sequences.dialogs.appendEvent.eventSourceId}</label>
                    <div className="p-inputgroup">
                        <InputText
                            id="eventSourceId"
                            value={eventSourceId}
                            onChange={(e) => setEventSourceId(e.target.value)}
                            placeholder={strings.eventStore.namespaces.sequences.dialogs.appendEvent.eventSourceIdPlaceholder}
                            className="w-full"
                        />
                        <Button
                            icon="pi pi-refresh"
                            onClick={generateGuid}
                            tooltip="Generate GUID"
                            tooltipOptions={{ position: 'top' }}
                        />
                    </div>
                </div>

                {selectedEventType && (
                    <div className="field mb-3">
                        <label>{strings.eventStore.namespaces.sequences.dialogs.appendEvent.content}</label>
                        <div style={{
                            border: '1px solid var(--surface-border)',
                            borderRadius: '4px',
                            padding: '1rem',
                            maxHeight: '400px',
                            overflow: 'auto'
                        }}>
                            <ObjectContent
                                object={eventContent}
                                schema={schema}
                                editMode={true}
                                onChange={setEventContent}
                                onValidationChange={setHasValidationErrors}
                            />
                        </div>
                    </div>
                )}
            </div>
        </Dialog>
    );
};
