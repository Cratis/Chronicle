// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult } from '@cratis/arc.react/dialogs';
import { Append } from 'Api/EventSequences';
import { AllEventTypesWithSchemas } from 'Api/EventTypes';
import { EventTypeRegistration } from 'Api/Events';
import { Dialog } from 'Components/Dialogs';
import { InputText } from 'primereact/inputtext';
import { Dropdown } from 'primereact/dropdown';
import { Button } from 'primereact/button';
import { useState } from 'react';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';

export const AddEventDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [appendEvent] = Append.use();

    const [allEventTypes] = AllEventTypesWithSchemas.use({ eventStore: params.eventStore! });

    const [selectedEventType, setSelectedEventType] = useState<EventTypeRegistration | null>(null);
    const [eventSourceId, setEventSourceId] = useState('');
    const [eventContent, setEventContent] = useState('{}');
    const [contentError, setContentError] = useState('');

    const validateContent = (content: string): boolean => {
        try {
            JSON.parse(content);
            setContentError('');
            return true;
        } catch (error) {
            const message = error instanceof Error ? error.message : 'Invalid JSON format';
            setContentError(`Invalid JSON: ${message}`);
            return false;
        }
    };

    const handleContentChange = (value: string) => {
        setEventContent(value);
        validateContent(value);
    };

    const handleClose = async (result: DialogResult) => {
        if (result !== DialogResult.Ok) {
            return true;
        }

        if (!selectedEventType || !eventSourceId || !validateContent(eventContent)) {
            return false;
        }

        appendEvent.eventStore = params.eventStore!;
        appendEvent.namespace = params.namespace!;
        appendEvent.eventSequenceId = 'event-log';
        appendEvent.eventSourceId = eventSourceId;
        // EventStreamType and EventStreamId are optional and not used for basic event appending
        appendEvent.eventStreamType = '';
        appendEvent.eventStreamId = '';
        appendEvent.eventType = selectedEventType.type;
        appendEvent.content = JSON.parse(eventContent);

        const executeResult = await appendEvent.execute();
        return executeResult.isSuccess;
    };

    const isValid = selectedEventType !== null && eventSourceId.trim() !== '' && contentError === '';

    return (
        <Dialog
            title={strings.eventStore.namespaces.sequences.dialogs.addEvent.title}
            onClose={handleClose}
            isValid={isValid}
            width="600px"
            resizable={true}>
            <div className="card flex flex-column gap-3 mb-3">
                <div className="field mb-3">
                    <label htmlFor="eventType">{strings.eventStore.namespaces.sequences.dialogs.addEvent.eventType}</label>
                    <Dropdown
                        id="eventType"
                        value={selectedEventType}
                        options={allEventTypes.data}
                        onChange={(e) => setSelectedEventType(e.value)}
                        optionLabel="type.id"
                        placeholder={strings.eventStore.namespaces.sequences.dialogs.addEvent.selectEventType}
                        className="w-full"
                    />
                </div>

                <div className="field mb-3">
                    <label htmlFor="eventSourceId">{strings.eventStore.namespaces.sequences.dialogs.addEvent.eventSourceId}</label>
                    <InputText
                        id="eventSourceId"
                        value={eventSourceId}
                        onChange={(e) => setEventSourceId(e.target.value)}
                        placeholder={strings.eventStore.namespaces.sequences.dialogs.addEvent.eventSourceIdPlaceholder}
                        className="w-full"
                    />
                </div>

                <div className="field mb-3">
                    <label htmlFor="eventContent">{strings.eventStore.namespaces.sequences.dialogs.addEvent.content}</label>
                    <textarea
                        id="eventContent"
                        value={eventContent}
                        onChange={(e) => handleContentChange(e.target.value)}
                        placeholder={strings.eventStore.namespaces.sequences.dialogs.addEvent.contentPlaceholder}
                        className="w-full p-inputtext"
                        rows={10}
                        style={{ fontFamily: 'monospace', fontSize: '12px' }}
                    />
                    {contentError && <small className="p-error">{contentError}</small>}
                </div>

                {selectedEventType && (
                    <div className="field mb-3">
                        <Button
                            label={strings.eventStore.namespaces.sequences.dialogs.addEvent.viewSchema}
                            icon="pi pi-eye"
                            className="p-button-text"
                            onClick={() => {
                                try {
                                    const schema = JSON.parse(selectedEventType.schema);
                                    const formattedSchema = JSON.stringify(schema, null, 2);
                                    const blob = new Blob([formattedSchema], { type: 'application/json' });
                                    const url = URL.createObjectURL(blob);
                                    const newWindow = window.open(url, '_blank');
                                    if (newWindow) {
                                        setTimeout(() => URL.revokeObjectURL(url), 1000);
                                    }
                                } catch (error) {
                                    console.error('Failed to parse schema:', error);
                                }
                            }}
                        />
                    </div>
                )}
            </div>
        </Dialog>
    );
};
