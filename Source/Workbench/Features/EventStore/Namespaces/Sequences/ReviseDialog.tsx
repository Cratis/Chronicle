// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AppendedEvent } from 'Api/Events';
import { Revise } from 'Api/EventSequences';
import { AllEventTypesWithSchemas } from 'Api/EventTypes';
import { useState, useEffect, useMemo } from 'react';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { ObjectContentEditor as _OCE } from '@cratis/components';
const ObjectContentEditor = _OCE.ObjectContentEditor;
import type { JsonSchema, Json } from '@cratis/components/types';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import strings from 'Strings';

export interface ReviseDialogProps {
    event: AppendedEvent;
    eventStore: string;
    namespace: string;
}

export const ReviseDialog = () => {
    const { request, closeDialog } = useDialogContext<ReviseDialogProps>();
    const originalContent = useMemo(() => {
        try {
            return JSON.parse(request?.event.content ?? '{}') as Record<string, unknown>;
        } catch {
            return {};
        }
    }, [request?.event.content]);
    const [parsedContent, setParsedContent] = useState<Record<string, Record<string, unknown>>>(() =>
        originalContent as Record<string, Record<string, unknown>>
    );
    const [schema, setSchema] = useState<JsonSchema>({ type: 'object', properties: {} });
    const [hasValidationErrors, setHasValidationErrors] = useState(false);

    const hasContentChanged = useMemo(
        () => JSON.stringify(parsedContent) !== JSON.stringify(originalContent),
        [parsedContent, originalContent]
    );

    const [allEventTypes] = AllEventTypesWithSchemas.use({ eventStore: request?.eventStore ?? '' });

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
            command={Revise}
            initialValues={{
                eventStore: request.eventStore,
                namespace: request.namespace,
                eventSequenceId: 'event-log',
                sequenceNumber: request.event.context.sequenceNumber,
                eventType: request.event.context.eventType,
                content: parsedContent
            }}
            onBeforeExecute={(command) => {
                command.content = parsedContent;
                return command;
            }}
            title={`Revise Event #${request.event.context.sequenceNumber}`}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            width="50vw"
            isValid={hasContentChanged && !hasValidationErrors}
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
                    onChange={(obj) => setParsedContent(obj as Record<string, Record<string, unknown>>)}
                    onValidationChange={setHasValidationErrors}
                />
            </div>
        </CommandDialog>
    );
};
