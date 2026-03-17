// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AddEventStore as AddEventStoreCommand } from 'Api/EventStores';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import strings from 'Strings';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';

export const AddEventStoreDialog = () => {
    const { closeDialog } = useDialogContext<object>();

    return (
        <CommandDialog
            command={AddEventStoreCommand}
            title={strings.home.dialogs.addEventStore.title}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            width="20vw"
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <InputTextField<AddEventStoreCommand>
                value={c => c.name}
                title={strings.home.dialogs.addEventStore.name}
                icon={<i className="pi pi-pencil" />} />
        </CommandDialog>
    );
};
