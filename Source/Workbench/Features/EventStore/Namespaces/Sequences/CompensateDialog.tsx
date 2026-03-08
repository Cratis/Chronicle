// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AppendedEvent } from 'Api/Events';
import { Compensate } from 'Api/EventSequences';
import { useState } from 'react';
import { InputTextarea } from 'primereact/inputtextarea';
import { CommandDialog } from '@cratis/components/CommandDialog';

export interface CompensateDialogProps {
    event: AppendedEvent;
    eventStore: string;
    namespace: string;
    visible: boolean;
    onClose: () => void;
}

export const CompensateDialog = ({ event, eventStore, namespace, visible, onClose }: CompensateDialogProps) => {
    const [contentStr, setContentStr] = useState(JSON.stringify(JSON.parse(event.content), null, 2));
    const [jsonError, setJsonError] = useState<string | undefined>();
    const [parsedContent, setParsedContent] = useState<Record<string, unknown>>(() => JSON.parse(event.content));

    const handleContentChange = (value: string) => {
        setContentStr(value);
        try {
            setParsedContent(JSON.parse(value));
            setJsonError(undefined);
        } catch (e) {
            setJsonError((e as Error).message);
        }
    };

    const currentValues = {
        eventStore,
        namespace,
        eventSequenceId: 'event-log',
        sequenceNumber: event.context.sequenceNumber,
        eventType: event.context.eventType,
        content: parsedContent,
        causation: [],
        causedBy: undefined
    };

    return (
        <CommandDialog
            command={Compensate}
            currentValues={currentValues}
            visible={visible}
            title={`Compensate Event #${event.context.sequenceNumber}`}
            width="50vw"
            isValid={!jsonError}
            onConfirm={onClose}
            onCancel={onClose}>
            <div className="p-fluid">
                <div className="field">
                    <label htmlFor="eventType">Event Type</label>
                    <input
                        id="eventType"
                        type="text"
                        className="p-inputtext"
                        value={event.context.eventType.id}
                        disabled
                    />
                </div>
                <div className="field">
                    <label htmlFor="content">Compensating Content (JSON)</label>
                    <InputTextarea
                        id="content"
                        value={contentStr}
                        onChange={(e) => handleContentChange(e.target.value)}
                        rows={15}
                        autoResize
                        className={jsonError ? 'p-invalid' : ''}
                    />
                    {jsonError && <small className="p-error">{jsonError}</small>}
                </div>
            </div>
        </CommandDialog>
    );
};
