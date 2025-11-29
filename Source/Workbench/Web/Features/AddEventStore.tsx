// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';
import strings from 'Strings';
import { AddEventStore as AddEventStoreCommand } from 'Api/EventStores';

export const AddEventStore = () => {
    const [name, setName] = useState('');
    const [addEventStore] = AddEventStoreCommand.use();

    const { closeDialog } = useDialogContext();

    const add = async () => {
        addEventStore.name = name;
        await addEventStore.execute();
        closeDialog(DialogResult.Ok);
    };

    return (
        <Dialog header={strings.home.dialogs.addEventStore.title} visible={true} style={{ width: '20vw' }} modal onHide={() => closeDialog(DialogResult.Cancelled)}>
            <div className="card flex flex-column md:flex-row gap-3">
                <div className="p-inputgroup flex-1">
                    <span className="p-inputgroup-addon">
                        <i className="pi pi-user"></i>
                    </span>
                    <InputText placeholder={strings.home.dialogs.addEventStore.name} value={name} onChange={e => setName(e.target.value)} />
                </div>
            </div>

            <div className="card flex flex-wrap justify-content-center gap-3 mt-8">
                <Button label={strings.general.buttons.ok} icon="pi pi-check" onClick={() => add()} autoFocus />
                <Button label={strings.general.buttons.cancel} icon="pi pi-times" severity='secondary' onClick={() => closeDialog(DialogResult.Cancelled)} />
            </div>
        </Dialog>
    );
};
