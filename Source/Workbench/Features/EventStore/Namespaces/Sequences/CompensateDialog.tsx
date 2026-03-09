// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AppendedEvent } from 'Api/Events';
import { Compensate } from 'Api/EventSequences';
import { Dialog } from 'Components/Dialogs';
import { useState } from 'react';
import { InputTextarea } from 'primereact/inputtextarea';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import strings from 'Strings';

export interface CompensateDialogProps {
    event: AppendedEvent;
    eventStore: string;
    namespace: string;
}

export const CompensateDialog = () => {
    const { request } = useDialogContext<CompensateDialogProps>();
    const [compensate] = Compensate.use();
    const [contentStr, setContentStr] = useState(() =>
        request ? JSON.stringify(JSON.parse(request.event.content), null, 2) : ''
    );
    const [jsonError, setJsonError] = useState<string | undefined>();
    const [parsedContent, setParsedContent] = useState<Record<string, unknown>>(() =>
        request ? (JSON.parse(request.event.content) as Record<string, unknown>) : {}
    );

    const handleContentChange = (value: string) => {
        setContentStr(value);
        try {
            setParsedContent(JSON.parse(value) as Record<string, unknown>);
            setJsonError(undefined);
        } catch (e) {
            setJsonError((e as Error).message);
        }
    };

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
        <Dialog
            title={`${strings.eventStore.namespaces.sequences.dialogs.compensate.title} #${request.event.context.sequenceNumber}`}
            onClose={handleClose}
            isValid={!jsonError}
            width="50vw"
            okLabel={strings.eventStore.namespaces.sequences.dialogs.compensate.okLabel}>
            <div className="p-fluid">
                <div className="field">
                    <label htmlFor="eventType">{strings.eventStore.namespaces.sequences.dialogs.compensate.eventType}</label>
                    <input
                        id="eventType"
                        type="text"
                        className="p-inputtext"
                        value={request.event.context.eventType.id}
                        disabled
                    />
                </div>
                <div className="field">
                    <label htmlFor="content">{strings.eventStore.namespaces.sequences.dialogs.compensate.content}</label>
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
        </Dialog>
    );
};
