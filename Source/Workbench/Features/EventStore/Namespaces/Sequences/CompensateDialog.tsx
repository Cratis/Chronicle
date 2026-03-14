// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AppendedEvent } from 'Api/Events';
import { Compensate } from 'Api/EventSequences';
import { AllEventTypesWithSchemas } from 'Api/EventTypes';
import { useState, useEffect } from 'react';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { ObjectContentEditor as _OCE } from '@cratis/components';
const ObjectContentEditor = _OCE.ObjectContentEditor;
import type { JsonSchema, Json } from '@cratis/components/types';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import strings from 'Strings';

export interface CompensateDialogProps {
    event: AppendedEvent;
    eventStore: string;
    namespace: string;
}

export const CompensateDialog = () => {
    const { request, closeDialog } = useDialogContext<CompensateDialogProps>();
    const [parsedContent, setParsedContent] = useState<Record<string, unknown>>({});
    const [schema, setSchema] = useState<JsonSchema>({ type: 'object', properties: {} });
    const [hasValidationErrors, setHasValidationErrors] = useState(false);

    const [allEventTypes] = AllEventTypesWithSchemas.use({ eventStore: request?.eventStore ?? '' });

    useEffect(() => {
        if (request) {
            setParsedContent(JSON.parse(request.event.content));
        }
    }, [request]);

    useEffect(() => {
        const registration = allEventTypes.data.find(et => et.type.id === request?.event.context.eventType.id);
        if (registration) {
            try {
                setSchema(JSON.parse(registration.schema) as JsonSchema);
            } catch {
                setSchema({ type: 'object', properties: {} });
            }
        }
    }, [allEventTypes.data, request?.event.context.eventType.id]);

    if (!request) return null;

    return (
        <CommandDialog
            command={Compensate}
            initialValues={{
                eventStore: request.eventStore,
                namespace: request.namespace,
                eventSequenceId: 'event-log',
                sequenceNumber: request.event.context.sequenceNumber,
                eventType: request.event.context.eventType
            }}
            currentValues={{ content: parsedContent }}
            title={`Compensate Event #${request.event.context.sequenceNumber}`}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            width="50vw"
            isValid={!hasValidationErrors}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <div style={{
                border: '1px solid var(--surface-border)',
                borderRadius: '4px',
                padding: '1rem',
                maxHeight: '400px',
                overflow: 'auto'
            }}>
                <ObjectContentEditor
                    object={parsedContent as Json}
                    schema={schema}
                    editMode={true}
                    onChange={(obj) => setParsedContent(obj as Record<string, unknown>)}
                    onValidationChange={setHasValidationErrors}
                />
            </div>
        </CommandDialog>
    );
};
