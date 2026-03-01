// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChangeApplicationSecret } from 'Api/Security';
import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';
import strings from 'Strings';
import { Guid } from '@cratis/fundamentals';
import { generatePassword } from '../../PasswordHelpers';
import { CommandDialog } from '@cratis/components/CommandDialog';

export interface ChangeSecretDialogProps {
    visible: boolean;
    applicationId: Guid;
    onClose: () => void;
}

export const ChangeSecretDialog = ({ visible, applicationId, onClose }: ChangeSecretDialogProps) => {
    const [clientSecret, setClientSecret] = useState(generatePassword(32));
    const [showSecret, setShowSecret] = useState(false);

    const handleGenerateSecret = () => {
        setClientSecret(generatePassword(32));
    };

    return (
        <CommandDialog
            command={ChangeApplicationSecret}
            currentValues={{ id: applicationId, clientSecret }}
            visible={visible}
            header={strings.eventStore.system.applications.dialogs.changeSecret.title}
            confirmLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            onConfirm={result => { if (result.isSuccess) onClose(); }}
            onCancel={onClose}>
            <div className="p-inputgroup flex-1">
                <span className="p-inputgroup-addon">
                    <i className="pi pi-lock"></i>
                </span>
                <InputText
                    type={showSecret ? 'text' : 'password'}
                    placeholder={strings.eventStore.system.applications.dialogs.changeSecret.clientSecret}
                    value={clientSecret}
                    onChange={e => setClientSecret(e.target.value)}
                />
                <Button
                    icon={showSecret ? 'pi pi-eye-slash' : 'pi pi-eye'}
                    onClick={() => setShowSecret(!showSecret)}
                    className="p-button-text"
                    type="button"
                    tooltip={showSecret ? strings.eventStore.system.applications.dialogs.changeSecret.hideSecret : strings.eventStore.system.applications.dialogs.changeSecret.showSecret}
                />
                <Button
                    icon="pi pi-refresh"
                    onClick={handleGenerateSecret}
                    className="p-button-text"
                    type="button"
                    tooltip={strings.eventStore.system.applications.dialogs.changeSecret.generateSecret}
                />
            </div>
        </CommandDialog>
    );
};
