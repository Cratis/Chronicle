// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { ChangePasswordForUser } from 'Api/Security';
import { Button } from 'primereact/button';
import { Dialog } from 'Components/Dialogs';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';
import strings from 'Strings';
import { generatePassword } from '../../PasswordHelpers';
import { Guid } from '@cratis/fundamentals';

export interface ChangePasswordDialogProps {
    userId: Guid;
}

export const ChangePasswordDialog = () => {
    const { request } = useDialogContext<ChangePasswordDialogProps>();
    const [password, setPassword] = useState(generatePassword());
    const [confirmPassword, setConfirmPassword] = useState(generatePassword());
    const [showPassword, setShowPassword] = useState(false);
    const [changePassword] = ChangePasswordForUser.use();

    const handleGeneratePassword = () => {
        const newPassword = generatePassword();
        setPassword(newPassword);
        setConfirmPassword(newPassword);
    };

    const handleClose = async (result: DialogResult) => {
        if (result !== DialogResult.Ok) {
            return true;
        }

        if (password !== confirmPassword) {
            return false;
        }

        if (password && request.userId) {
            changePassword.userId = request.userId;
            changePassword.password = password;
            changePassword.confirmedPassword = confirmPassword;
            const executeResult = await changePassword.execute();
            return executeResult.isSuccess;
        }

        return false;
    };

    return (
        <Dialog
            title={strings.eventStore.system.users.dialogs.changePassword.title}
            onClose={handleClose}
            isValid={password.trim() !== '' && confirmPassword.trim() !== '' && password === confirmPassword}
            width="30vw">
            <div className="flex flex-column gap-3">
                <div className="p-inputgroup">
                    <span className="p-inputgroup-addon">
                        <i className="pi pi-lock"></i>
                    </span>
                    <InputText
                        type={showPassword ? 'text' : 'password'}
                        placeholder={strings.eventStore.system.users.dialogs.changePassword.password}
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
                <div className="p-inputgroup">
                    <span className="p-inputgroup-addon">
                        <i className="pi pi-lock"></i>
                    </span>
                    <InputText
                        type={showPassword ? 'text' : 'password'}
                        placeholder={strings.eventStore.system.users.dialogs.changePassword.confirmPassword}
                        value={confirmPassword}
                        onChange={e => setConfirmPassword(e.target.value)}
                    />
                </div>
                {password !== confirmPassword && (
                    <small className="p-error">Passwords do not match</small>
                )}
            </div>
        </Dialog>
    );
};
