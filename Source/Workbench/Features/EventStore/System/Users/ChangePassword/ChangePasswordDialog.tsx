// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChangePasswordForUser } from 'Api/Security';
import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';
import strings from 'Strings';
import { generatePassword } from '../../PasswordHelpers';
import { Guid } from '@cratis/fundamentals';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';

export interface ChangePasswordDialogRequest {
    userId: Guid;
}

export const ChangePasswordDialog = () => {
    const { request, closeDialog } = useDialogContext<ChangePasswordDialogRequest>();
    const [password, setPassword] = useState(generatePassword());
    const [confirmPassword, setConfirmPassword] = useState(generatePassword());
    const [showPassword, setShowPassword] = useState(false);

    const handleGeneratePassword = () => {
        const newPassword = generatePassword();
        setPassword(newPassword);
        setConfirmPassword(newPassword);
    };

    return (
        <CommandDialog
            command={ChangePasswordForUser}
            initialValues={{ userId: request.userId }}
            currentValues={{ password, confirmedPassword: confirmPassword }}
            title={strings.eventStore.system.users.dialogs.changePassword.title}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            width="450px"
            isValid={password === confirmPassword && password.trim() !== ''}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <div className="p-fluid">
                <div className="field mb-3">
                    <label>{strings.eventStore.system.users.dialogs.changePassword.password}</label>
                    <div className="p-inputgroup">
                        <span className="p-inputgroup-addon">
                            <i className="pi pi-lock"></i>
                        </span>
                        <InputText
                            type={showPassword ? 'text' : 'password'}
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                        />
                        <Button
                            icon={showPassword ? 'pi pi-eye-slash' : 'pi pi-eye'}
                            onClick={() => setShowPassword(!showPassword)}
                            className="p-button-text"
                            type="button"
                            tooltip={showPassword ? strings.eventStore.system.users.dialogs.changePassword.hidePassword : strings.eventStore.system.users.dialogs.changePassword.showPassword}
                        />
                        <Button
                            icon="pi pi-refresh"
                            onClick={handleGeneratePassword}
                            className="p-button-text"
                            type="button"
                            tooltip={strings.eventStore.system.users.dialogs.changePassword.generatePassword}
                        />
                    </div>
                </div>
                <div className="field mb-3">
                    <label>{strings.eventStore.system.users.dialogs.changePassword.confirmPassword}</label>
                    <div className="p-inputgroup">
                        <span className="p-inputgroup-addon">
                            <i className="pi pi-lock"></i>
                        </span>
                        <InputText
                            type={showPassword ? 'text' : 'password'}
                            value={confirmPassword}
                            onChange={e => setConfirmPassword(e.target.value)}
                        />
                    </div>
                </div>
                {password !== confirmPassword && (
                    <small className="p-error">Passwords do not match</small>
                )}
            </div>
        </CommandDialog>
    );
};
