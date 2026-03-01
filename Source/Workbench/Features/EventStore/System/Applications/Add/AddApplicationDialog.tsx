// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AddApplication } from 'Api/Security';
import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';
import strings from 'Strings';
import { generatePassword } from '../../PasswordHelpers';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';

export interface AddApplicationDialogProps {
    visible: boolean;
    onClose: () => void;
}

export const AddApplicationDialog = ({ visible, onClose }: AddApplicationDialogProps) => {
    const [id] = useState(crypto.randomUUID());
    const [clientSecret, setClientSecret] = useState('');
    const [showSecret, setShowSecret] = useState(false);

    const handleGenerateSecret = () => {
        setClientSecret(generatePassword(32));
    };

    return (
        <CommandDialog
            command={AddApplication}
            currentValues={{ id, clientSecret }}
            visible={visible}
            header={strings.eventStore.system.applications.dialogs.addApplication.title}
            confirmLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            onConfirm={result => { if (result.isSuccess) onClose(); }}
            onCancel={onClose}>
            <InputTextField<AddApplication>
                value={c => c.clientId}
                title={strings.eventStore.system.applications.dialogs.addApplication.clientId}
                icon={<i className="pi pi-user" />} />
            <div className="p-inputgroup flex-1 mt-3">
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
        </CommandDialog>
    );
};
