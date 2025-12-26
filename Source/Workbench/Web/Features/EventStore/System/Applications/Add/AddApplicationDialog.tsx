// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { AddApplication } from 'Api/Security';
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';
import strings from 'Strings';
import { generatePassword } from '../../PasswordHelpers';

export const AddApplicationDialog = () => {
    const [id] = useState(crypto.randomUUID());
    const [clientId, setClientId] = useState('');
    const [clientSecret, setClientSecret] = useState('');
    const [showSecret, setShowSecret] = useState(false);
    const { closeDialog } = useDialogContext();
    const [addApplication] = AddApplication.use();

    const handleGenerateSecret = () => {
        setClientSecret(generatePassword(32));
    };

    const handleOk = async () => {
        if (clientId && clientSecret) {
            addApplication.id = id;
            addApplication.clientId = clientId;
            addApplication.clientSecret = clientSecret;
            const result = await addApplication.execute();
            if (result.isSuccess) {
                closeDialog(DialogResult.Ok);
            }
        }
    };

    return (
        <Dialog
            header={strings.eventStore.system.applications.dialogs.addApplication.title}
            visible={true}
            style={{ width: '450px' }}
            modal
            onHide={() => closeDialog(DialogResult.Cancelled)}>
            <div className="card flex flex-column gap-3 mb-3">
                <div className="p-inputgroup flex-1">
                    <span className="p-inputgroup-addon">
                        <i className="pi pi-user"></i>
                    </span>
                    <InputText
                        placeholder={strings.eventStore.system.applications.dialogs.addApplication.clientId}
                        value={clientId}
                        onChange={e => setClientId(e.target.value)}
                    />
                </div>
            </div>

            <div className="card flex flex-column gap-3 mb-3">
                <div className="p-inputgroup flex-1">
                    <span className="p-inputgroup-addon">
                        <i className="pi pi-lock"></i>
                    </span>
                    <InputText
                        type={showSecret ? 'text' : 'password'}
                        placeholder={strings.eventStore.system.applications.dialogs.addApplication.clientSecret}
                        value={clientSecret}
                        onChange={e => setClientSecret(e.target.value)}
                    />
                    <Button
                        icon={showSecret ? 'pi pi-eye-slash' : 'pi pi-eye'}
                        onClick={() => setShowSecret(!showSecret)}
                        className="p-button-text"
                        type="button"
                        tooltip={showSecret ? strings.eventStore.system.applications.dialogs.addApplication.hideSecret : strings.eventStore.system.applications.dialogs.addApplication.showSecret}
                    />
                    <Button
                        icon="pi pi-refresh"
                        onClick={handleGenerateSecret}
                        className="p-button-text"
                        type="button"
                        tooltip={strings.eventStore.system.applications.dialogs.addApplication.generateSecret}
                    />
                </div>
            </div>

            <div className="card flex flex-wrap justify-content-center gap-3 mt-4">
                <Button
                    label={strings.general.buttons.ok}
                    icon="pi pi-check"
                    onClick={handleOk}
                    disabled={!clientId || !clientSecret}
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
