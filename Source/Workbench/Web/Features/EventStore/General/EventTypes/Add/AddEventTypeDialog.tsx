// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult } from '@cratis/arc.react/dialogs';
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
    const [createEventType] = CreateEventType.use();

    const handleClose = async (result: DialogResult) => {
        if (result !== DialogResult.Ok) {
            return true;
        }

        if (name && params.eventStore) {
            createEventType.eventStore = params.eventStore;
            createEventType.name = name;
            const executeResult = await createEventType.execute();
            return executeResult.isSuccess;
        }

        return false;
    };

    return (
        <Dialog
            title={strings.eventStore.general.eventTypes.dialogs.addEventType.title}
            onClose={handleClose}
            isValid={name.trim() !== ''}
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
