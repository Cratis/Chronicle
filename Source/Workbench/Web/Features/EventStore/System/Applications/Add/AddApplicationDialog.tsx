// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult } from '@cratis/arc.react/dialogs';
import { AddApplication } from 'Api/Security';
import { Button } from 'primereact/button';
import { Dialog } from 'Components/Dialogs';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';
import strings from 'Strings';
import { generatePassword } from '../../PasswordHelpers';

export const AddApplicationDialog = () => {
    const [id] = useState(crypto.randomUUID());
    const [clientId, setClientId] = useState('');
    const [clientSecret, setClientSecret] = useState('');
    const [showSecret, setShowSecret] = useState(false);
    const [addApplication] = AddApplication.use();

    const handleGenerateSecret = () => {
        setClientSecret(generatePassword(32));
    };

    const handleClose = async (result: DialogResult) => {
        if (result !== DialogResult.Ok) {
            return true;
        }

        if (clientId && clientSecret) {
            addApplication.id = id;
            addApplication.clientId = clientId;
            addApplication.clientSecret = clientSecret;
            const executeResult = await addApplication.execute();
            return executeResult.isSuccess;
        }

        return false;
    };

    return (
        <Dialog
            title={strings.eventStore.system.applications.dialogs.addApplication.title}
            onClose={handleClose}
            isValid={clientId.trim() !== '' && clientSecret.trim() !== ''}>
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
        </Dialog>
    );
};
