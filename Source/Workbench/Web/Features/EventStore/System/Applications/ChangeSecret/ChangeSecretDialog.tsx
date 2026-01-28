// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { ChangeApplicationSecret } from 'Api/Security';
import { Button } from 'primereact/button';
import { Dialog } from 'Components/Dialogs';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';
import strings from 'Strings';
import { Guid } from '@cratis/fundamentals';
import { generatePassword } from '../../PasswordHelpers';

export interface ChangeSecretDialogProps {
    applicationId: Guid;
}

export const ChangeSecretDialog = ({ applicationId }: ChangeSecretDialogProps) => {
    const [clientSecret, setClientSecret] = useState(generatePassword(32));
    const [showSecret, setShowSecret] = useState(false);
    const { closeDialog } = useDialogContext();
    const [changeSecret] = ChangeApplicationSecret.use();

    const handleGenerateSecret = () => {
        setClientSecret(generatePassword(32));
    };

    const handleOk = async () => {
        if (clientSecret) {
            changeSecret.id = applicationId;
            changeSecret.clientSecret = clientSecret;
            const result = await changeSecret.execute();
            if (result.isSuccess) {
                closeDialog(DialogResult.Ok);
            }
        }
    };

    const customButtons = (
        <>
            <Button
                label={strings.general.buttons.save}
                icon="pi pi-check"
                onClick={handleOk}
                disabled={!clientSecret}
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
            title={strings.eventStore.system.applications.dialogs.changeSecret.title}
            onClose={closeDialog}
            buttons={customButtons}>
            <div className="card flex flex-column gap-3 mb-3">
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
            </div>
        </Dialog>
    );
};
