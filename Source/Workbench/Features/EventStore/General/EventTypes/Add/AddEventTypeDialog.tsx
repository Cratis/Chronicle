// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { CreateEventType } from 'Api/Events';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';

export const AddEventTypeDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const { closeDialog } = useDialogContext();

    return (
        <CommandDialog
            command={CreateEventType}
            initialValues={{ eventStore: params.eventStore! }}
            visible={true}
            header={strings.eventStore.general.eventTypes.dialogs.addEventType.title}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <CommandDialog.Fields>
                <InputTextField
                    value={(c: CreateEventType) => c.name}
                    title={strings.eventStore.general.eventTypes.dialogs.addEventType.name}
                    icon={<i className="pi pi-code" />}
                />
            </CommandDialog.Fields>
        </CommandDialog>
    );
};
