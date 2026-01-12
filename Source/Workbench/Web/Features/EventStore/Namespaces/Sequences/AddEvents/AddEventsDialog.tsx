// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { AppendMany, EventToAppend } from 'Api/EventSequences';
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import { useState } from 'react';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import { Dropdown } from 'primereact/dropdown';
import { InputTextarea } from 'primereact/inputtextarea';
import { AllEventTypes } from 'Api/EventTypes';
import { EventType } from 'Api/Events';

interface EventRow {
    id: string;
    eventType: EventType | null;
    content: string;
    isValid: boolean;
}

export const AddEventsDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const { closeDialog } = useDialogContext();
    const [appendMany] = AppendMany.use();
    const [events, setEvents] = useState<EventRow[]>([]);
    const [selectedEventType, setSelectedEventType] = useState<EventType | null>(null);

    const [eventTypes] = AllEventTypes.use({
        eventStore: params.eventStore!
    });

    const handleAddRow = () => {
        const newEvent: EventRow = {
            id: crypto.randomUUID(),
            eventType: selectedEventType,
            content: '',
            isValid: false
        };
        setEvents([...events, newEvent]);
    };

    const handleDeleteRow = (rowId: string) => {
        setEvents(events.filter(e => e.id !== rowId));
    };

    const validateContent = (content: string): boolean => {
        if (!content.trim()) {
            return false;
        }
        try {
            JSON.parse(content);
            return true;
        } catch {
            return false;
        }
    };

    const handleContentChange = (rowId: string, content: string) => {
        setEvents(events.map(e => {
            if (e.id === rowId) {
                return {
                    ...e,
                    content,
                    isValid: validateContent(content)
                };
            }
            return e;
        }));
    };

    const handleEventTypeChange = (rowId: string, eventType: EventType) => {
        setEvents(events.map(e => {
            if (e.id === rowId) {
                return {
                    ...e,
                    eventType,
                    isValid: validateContent(e.content)
                };
            }
            return e;
        }));
    };

    const handleOk = async () => {
        const validEvents = events.filter(e => e.isValid && e.eventType);
        
        if (validEvents.length === 0) {
            return;
        }

        const eventsToAppend: EventToAppend[] = validEvents.map(e => {
            const eventToAppend = new EventToAppend();
            eventToAppend.eventType = e.eventType!;
            eventToAppend.content = JSON.parse(e.content);
            return eventToAppend;
        });

        appendMany.eventStore = params.eventStore!;
        appendMany.namespace = params.namespace!;
        appendMany.eventSequenceId = 'event-log';
        appendMany.events = eventsToAppend;

        const result = await appendMany.execute();
        if (result.isSuccess) {
            closeDialog(DialogResult.Ok);
        } else {
            console.error('Failed to append events:', result);
        }
    };

    const eventTypeBodyTemplate = (rowData: EventRow) => {
        return (
            <Dropdown
                value={rowData.eventType}
                options={eventTypes.data}
                onChange={(e) => handleEventTypeChange(rowData.id, e.value)}
                optionLabel='id'
                placeholder={strings.eventStore.namespaces.sequences.dialogs.addEvents.selectEventType}
                className="w-full"
            />
        );
    };

    const contentBodyTemplate = (rowData: EventRow) => {
        return (
            <InputTextarea
                value={rowData.content}
                onChange={(e) => handleContentChange(rowData.id, e.target.value)}
                placeholder={strings.eventStore.namespaces.sequences.dialogs.addEvents.contentPlaceholder}
                rows={3}
                className={`w-full ${rowData.content && !rowData.isValid ? 'p-invalid' : ''}`}
            />
        );
    };

    const actionsBodyTemplate = (rowData: EventRow) => {
        return (
            <Button
                icon="pi pi-trash"
                severity="danger"
                text
                onClick={() => handleDeleteRow(rowData.id)}
            />
        );
    };

    const isOkDisabled = events.length === 0 || !events.some(e => e.isValid && e.eventType);

    return (
        <Dialog
            header={strings.eventStore.namespaces.sequences.dialogs.addEvents.title}
            visible={true}
            style={{ width: '80vw', height: '80vh' }}
            modal
            resizable={false}
            onHide={() => closeDialog(DialogResult.Cancelled)}>
            
            <div className="flex flex-column h-full gap-3">
                <div className="flex gap-3 align-items-center">
                    <label>{strings.eventStore.namespaces.sequences.dialogs.addEvents.defaultEventType}</label>
                    <Dropdown
                        value={selectedEventType}
                        options={eventTypes.data}
                        onChange={(e) => setSelectedEventType(e.value)}
                        optionLabel='id'
                        placeholder={strings.eventStore.namespaces.sequences.dialogs.addEvents.selectEventType}
                        className="flex-1"
                    />
                    <Button
                        label={strings.eventStore.namespaces.sequences.dialogs.addEvents.addRow}
                        icon="pi pi-plus"
                        onClick={handleAddRow}
                    />
                </div>

                <div className="flex-1 overflow-auto">
                    <DataTable
                        value={events}
                        dataKey="id"
                        emptyMessage={strings.eventStore.namespaces.sequences.dialogs.addEvents.noEvents}
                        scrollable
                        scrollHeight="flex">
                        <Column
                            header={strings.eventStore.namespaces.sequences.dialogs.addEvents.eventType}
                            body={eventTypeBodyTemplate}
                            style={{ width: '30%' }}
                        />
                        <Column
                            header={strings.eventStore.namespaces.sequences.dialogs.addEvents.content}
                            body={contentBodyTemplate}
                            style={{ width: '60%' }}
                        />
                        <Column
                            body={actionsBodyTemplate}
                            style={{ width: '10%' }}
                        />
                    </DataTable>
                </div>

                <div className="flex flex-wrap justify-content-center gap-3">
                    <Button
                        label={strings.general.buttons.ok}
                        icon="pi pi-check"
                        onClick={handleOk}
                        disabled={isOkDisabled}
                        autoFocus
                    />
                    <Button
                        label={strings.general.buttons.cancel}
                        icon="pi pi-times"
                        severity='secondary'
                        onClick={() => closeDialog(DialogResult.Cancelled)}
                    />
                </div>
            </div>
        </Dialog>
    );
};
