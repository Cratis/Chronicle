// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useState } from 'react';
import { AppendedEvent, Compensate } from 'Features/Sequences';
import { EventTypeDetails } from 'Features/EventTypes';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { Dropdown } from '@cratis/components/Dropdown';
import { ObjectContentEditor } from '@cratis/components/ObjectContentEditor';
import type { JsonSchema, Json } from '@cratis/components/types';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import strings from 'Strings';

export interface CompensateDialogProps {
    event: AppendedEvent;
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    compensationTypes: EventTypeDetails[];
}

const emptySchema: JsonSchema = { type: 'object', properties: {} };

const parseSchema = (schema: string | undefined): JsonSchema => {
    try {
        return schema ? JSON.parse(schema) as JsonSchema : emptySchema;
    } catch {
        return emptySchema;
    }
};

export const CompensateDialog = () => {
    const { request, closeDialog } = useDialogContext<CompensateDialogProps>();
    const dialogStrings = strings.eventStore.namespaces.sequences.dialogs.compensate;
    const [selectedTypeId, setSelectedTypeId] = useState<string>(request?.compensationTypes[0]?.type.id ?? '');
    const [content, setContent] = useState<Record<string, Record<string, unknown>>>({});
    const [hasValidationErrors, setHasValidationErrors] = useState(false);

    const selectedType = useMemo(
        () => request?.compensationTypes.find(compensationType => compensationType.type.id === selectedTypeId),
        [request?.compensationTypes, selectedTypeId]);
    const schema = useMemo(() => parseSchema(selectedType?.schema), [selectedType]);

    useEffect(() => {
        setContent({});
    }, [selectedTypeId]);

    if (!request || !selectedType) return null;

    return (
        <CommandDialog
            command={Compensate}
            initialValues={{
                eventStore: request.eventStore,
                namespace: request.namespace,
                eventSequenceId: request.eventSequenceId,
                compensationFor: request.event.context.sequenceNumber,
                eventType: selectedType.type,
                content: JSON.stringify(content)
            }}
            onBeforeExecute={(command) => {
                command.eventType = selectedType.type;
                command.content = JSON.stringify(content);
                return command;
            }}
            title={`${dialogStrings.title} #${request.event.context.sequenceNumber}`}
            okLabel={dialogStrings.okLabel}
            cancelLabel={strings.general.buttons.cancel}
            width="50vw"
            isValid={!hasValidationErrors}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <div className='flex flex-col gap-3'>
                <label htmlFor='compensation-event-type'>{dialogStrings.eventType}</label>
                <Dropdown
                    id='compensation-event-type'
                    value={selectedTypeId}
                    options={request.compensationTypes.map(compensationType => compensationType.type.id)}
                    aria-label={dialogStrings.eventType}
                    onChange={value => setSelectedTypeId(value as string)} />
                <div style={{
                    border: '1px solid var(--surface-border)',
                    borderRadius: '4px',
                    padding: '1rem',
                    maxHeight: '400px',
                    overflow: 'auto'
                }}>
                    <ObjectContentEditor
                        object={content as Json}
                        schema={schema}
                        editMode={true}
                        onChange={(obj) => setContent(obj as Record<string, Record<string, unknown>>)}
                        onValidationChange={setHasValidationErrors}
                    />
                </div>
            </div>
        </CommandDialog>
    );
};
