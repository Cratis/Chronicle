// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Dialog } from 'Components/Dialogs';
import { AppendedEvent } from 'Api/Events';
import { Compensate } from 'Api/EventSequences';
import { useState } from 'react';
import { InputTextarea } from 'primereact/inputtextarea';

export interface CompensateDialogProps {
    event: AppendedEvent;
    eventStore: string;
    namespace: string;
    onClose: () => void;
}

export const CompensateDialog = ({ event, eventStore, namespace, onClose }: CompensateDialogProps) => {
    const [content, setContent] = useState(JSON.stringify(JSON.parse(event.content), null, 2));
    const [isSubmitting, setIsSubmitting] = useState(false);

    const handleCompensate = async () => {
        setIsSubmitting(true);
        try {
            const [command] = Compensate.use({
                eventStore,
                namespace,
                eventSequenceId: 'event-log',
                sequenceNumber: event.context.sequenceNumber,
                eventType: event.context.eventType,
                content: JSON.parse(content),
                causation: [],
                causedBy: undefined
            });

            await command.execute();
            onClose();
        } catch (error) {
            console.error('Failed to compensate event:', error);
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <Dialog
            header={`Compensate Event #${event.context.sequenceNumber}`}
            visible
            style={{ width: '50vw' }}
            onHide={onClose}
            footer={
                <div>
                    <button
                        className="p-button p-button-text"
                        onClick={onClose}
                        disabled={isSubmitting}>
                        Cancel
                    </button>
                    <button
                        className="p-button"
                        onClick={handleCompensate}
                        disabled={isSubmitting}>
                        {isSubmitting ? 'Compensating...' : 'Compensate'}
                    </button>
                </div>
            }>
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
                        value={content}
                        onChange={(e) => setContent(e.target.value)}
                        rows={15}
                        autoResize
                    />
                </div>
            </div>
        </Dialog>
    );
};
