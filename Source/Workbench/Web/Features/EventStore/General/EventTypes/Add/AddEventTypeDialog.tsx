// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { CreateEventType } from 'Api/Events';
import { Button } from 'primereact/button';
import { Dialog } from 'Components/Dialogs';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';

export const AddEventTypeDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [name, setName] = useState('');
    const { closeDialog } = useDialogContext();
    const [createEventType] = CreateEventType.use();

    const handleOk = async () => {
        if (name && params.eventStore) {
            createEventType.eventStore = params.eventStore;
            createEventType.name = name;
            const result = await createEventType.execute();
            if (result.isSuccess) {
                closeDialog(DialogResult.Ok);
            }
        }
    };

    const customButtons = (
        <>
            <Button
                label={strings.general.buttons.ok}
                icon="pi pi-check"
                onClick={handleOk}
                disabled={!name}
                autoFocus
            />
            <Button
                label={strings.general.buttons.cancel}
                icon="pi pi-times"
                onClick={() => closeDialog(DialogResult.Cancelled)}
                outlined
            />
        </>
    );

    return (
        <Dialog
            title={strings.eventStore.general.eventTypes.dialogs.addEventType.title}
            onClose={closeDialog}
            buttons={customButtons}
            resizable={false}>
            <div className="card flex flex-column gap-3 mb-3">
                <div className="p-inputgroup flex-1">
                    <span className="p-inputgroup-addon">
                        <i className="pi pi-code"></i>
                    </span>
                    <InputText
                        placeholder={strings.eventStore.general.eventTypes.dialogs.addEventType.name}
                        value={name}
                        onChange={e => setName(e.target.value)}
                    />
                </div>
            </div>
        </Dialog>
    );
};
