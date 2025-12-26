// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { CreateEventType } from 'Api/Events';
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
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

    return (
        <Dialog
            header={strings.eventStore.general.types.dialogs.addEventType.title}
            visible={true}
            style={{ width: '450px' }}
            modal
            resizable={false}
            onHide={() => closeDialog(DialogResult.Cancelled)}>
            <div className="card flex flex-column gap-3 mb-3">
                <div className="p-inputgroup flex-1">
                    <span className="p-inputgroup-addon">
                        <i className="pi pi-code"></i>
                    </span>
                    <InputText
                        placeholder={strings.eventStore.general.types.dialogs.addEventType.name}
                        value={name}
                        onChange={e => setName(e.target.value)}
                    />
                </div>
            </div>

            <div className="card flex flex-wrap justify-content-center gap-3 mt-4">
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
                    severity='secondary'
                    onClick={() => closeDialog(DialogResult.Cancelled)}
                />
            </div>
        </Dialog>
    );
};
