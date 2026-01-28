// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { Button } from 'primereact/button';
import { Dialog } from 'Components/Dialogs';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';
import strings from 'Strings';

export class AddNamespaceRequest {
}

export class AddNamespaceResponse {
    static readonly Canceled = new AddNamespaceResponse(false, '');
    static Ok = (name: string) => new AddNamespaceResponse(true, name);

    constructor(readonly create: boolean, readonly name: string) {
    }
}

export const AddNamespace = () => {
    const [name, setName] = useState('');
    const { closeDialog } = useDialogContext<AddNamespaceRequest, AddNamespaceResponse>();

    const customButtons = (
        <>
            <Button
                label={strings.general.buttons.ok}
                icon="pi pi-check"
                onClick={() => closeDialog(DialogResult.Ok, AddNamespaceResponse.Ok(name))}
                disabled={!name.trim()}
                autoFocus />
            <Button label={strings.general.buttons.cancel} icon="pi pi-times" className="p-button-text" onClick={() => closeDialog(DialogResult.Cancelled, AddNamespaceResponse.Canceled)} />
        </>
    );

    return (
        <Dialog title={strings.eventStore.general.namespaces.dialogs.addNamespace.title} onClose={(result) => closeDialog(result, result === DialogResult.Ok ? AddNamespaceResponse.Ok(name) : AddNamespaceResponse.Canceled)} buttons={customButtons} width="20vw">
            <div className="card flex flex-column md:flex-row gap-3">
                <div className="p-inputgroup flex-1">
                    <span className="p-inputgroup-addon">
                        <i className="pi pi-user"></i>
                    </span>
                    <InputText placeholder={strings.eventStore.general.namespaces.dialogs.addNamespace.name} value={name} onChange={e => setName(e.target.value)} />
                </div>
            </div>
        </Dialog>
    );
};
