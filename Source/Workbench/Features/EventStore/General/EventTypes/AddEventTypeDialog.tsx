// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CreateEventType } from 'Api/Events';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';

export const AddEventTypeDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const { closeDialog } = useDialogContext<object>();

    return (
        <CommandDialog
            command={CreateEventType}
            initialValues={{ eventStore: params.eventStore }}
            title={strings.eventStore.general.eventTypes.dialogs.addEventType.title}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <InputTextField<CreateEventType>
                value={c => c.name}
                title={strings.eventStore.general.eventTypes.dialogs.addEventType.name}
                icon={<i className="pi pi-code" />} />
        </CommandDialog>
    );
};
