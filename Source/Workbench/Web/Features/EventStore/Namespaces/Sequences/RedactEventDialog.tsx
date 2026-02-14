// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { Redact } from 'Api/EventSequences';
import { Dialog } from 'Components/Dialogs';
import { InputTextarea } from 'primereact/inputtextarea';
import { useState } from 'react';
import strings from 'Strings';

export interface RedactEventDialogProps {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    sequenceNumber: number;
}

export const RedactEventDialog = () => {
    const { request } = useDialogContext<RedactEventDialogProps>();
    const [reason, setReason] = useState('');
    const [redact] = Redact.use();

    const handleClose = async (result: DialogResult) => {
        if (result !== DialogResult.Ok) {
            return true;
        }

        if (reason.trim() && request) {
            redact.eventStore = request.eventStore;
            redact.namespace = request.namespace;
            redact.eventSequenceId = request.eventSequenceId;
            redact.sequenceNumber = request.sequenceNumber;
            redact.reason = reason;
            const executeResult = await redact.execute();
            return executeResult.isSuccess;
        }

        return false;
    };

    return (
        <Dialog
            title={strings.eventStore.namespaces.sequences.dialogs.redact.title}
            onClose={handleClose}
            isValid={reason.trim() !== ''}
            width="30vw">
            <div className="flex flex-column gap-3">
                <label htmlFor="reason">{strings.eventStore.namespaces.sequences.dialogs.redact.reason}</label>
                <InputTextarea
                    id="reason"
                    rows={5}
                    placeholder={strings.eventStore.namespaces.sequences.dialogs.redact.reasonPlaceholder}
                    value={reason}
                    onChange={e => setReason(e.target.value)}
                    autoFocus
                />
            </div>
        </Dialog>
    );
};
