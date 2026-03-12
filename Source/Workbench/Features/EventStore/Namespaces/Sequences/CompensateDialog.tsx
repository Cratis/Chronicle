// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AppendedEvent } from 'Api/Events';
import { Compensate } from 'Api/EventSequences';
import { AllEventTypesWithSchemas } from 'Api/EventTypes';
import { useState, useEffect } from 'react';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { ObjectContent } from 'Components/ObjectContentViewer';
import { JsonSchema } from 'Components/JsonSchema';
import { Json } from 'Features/index';

export interface CompensateDialogProps {
    event: AppendedEvent;
    eventStore: string;
    namespace: string;
}

export const CompensateDialog = ({ event, eventStore, namespace, visible, onClose }: CompensateDialogProps) => {
    const [parsedContent, setParsedContent] = useState<Record<string, unknown>>(() => JSON.parse(event.content));
    const [schema, setSchema] = useState<JsonSchema>({ type: 'object', properties: {} });
    const [hasValidationErrors, setHasValidationErrors] = useState(false);

    const [allEventTypes] = AllEventTypesWithSchemas.use({ eventStore });

    useEffect(() => {
        const registration = allEventTypes.data.find(et => et.type.id === event.context.eventType.id);
        if (registration) {
            try {
                setSchema(JSON.parse(registration.schema) as JsonSchema);
            } catch {
                setSchema({ type: 'object', properties: {} });
            }
        }
    }, [allEventTypes.data, event.context.eventType.id]);

    const handleClose = async (result: DialogResult) => {
        if (result !== DialogResult.Ok || !request) {
            return true;
        }

        compensate.eventStore = request.eventStore;
        compensate.namespace = request.namespace;
        compensate.eventSequenceId = 'event-log';
        compensate.sequenceNumber = request.event.context.sequenceNumber;
        compensate.eventType = request.event.context.eventType;
        compensate.content = parsedContent;

        const executeResult = await compensate.execute();
        return executeResult.isSuccess;
    };

    if (!request) return null;

    return (
        <CommandDialog
            command={Compensate}
            currentValues={currentValues}
            visible={visible}
            title={`Compensate Event #${event.context.sequenceNumber}`}
            width="50vw"
            isValid={!hasValidationErrors}
            onConfirm={onClose}
            onCancel={onClose}>
            <div style={{
                border: '1px solid var(--surface-border)',
                borderRadius: '4px',
                padding: '1rem',
                maxHeight: '400px',
                overflow: 'auto'
            }}>
                <ObjectContent
                    object={parsedContent as Json}
                    schema={schema}
                    editMode={true}
                    onChange={(obj) => setParsedContent(obj as Record<string, unknown>)}
                    onValidationChange={setHasValidationErrors}
                />
            </div>
        </Dialog>
    );
};
