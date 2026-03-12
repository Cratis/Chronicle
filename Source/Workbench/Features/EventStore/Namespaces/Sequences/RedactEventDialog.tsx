// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { Redact } from 'Api/EventSequences';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { TextAreaField } from '@cratis/components/CommandForm';
import strings from 'Strings';

export interface RedactEventDialogProps {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
    sequenceNumber: number;
}

export const RedactEventDialog = () => {
    const { request, closeDialog } = useDialogContext<RedactEventDialogProps>();

    return (
        <CommandDialog
            command={Redact}
            currentValues={{
                eventStore: request.eventStore,
                namespace: request.namespace,
                eventSequenceId: request.eventSequenceId,
                sequenceNumber: request.sequenceNumber
            }}
            title={strings.eventStore.namespaces.sequences.dialogs.redact.title}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            width="30vw"
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <TextAreaField<Redact>
                value={c => c.reason}
                title={strings.eventStore.namespaces.sequences.dialogs.redact.reason}
                placeholder={strings.eventStore.namespaces.sequences.dialogs.redact.reasonPlaceholder}
                required
                rows={5} />
        </CommandDialog>
    );
};
