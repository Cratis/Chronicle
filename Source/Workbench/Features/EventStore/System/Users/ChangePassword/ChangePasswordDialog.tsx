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

export interface ChangePasswordDialogProps {
    visible: boolean;
    userId: Guid;
    onClose: () => void;
}

export const ChangePasswordDialog = ({ visible, userId, onClose }: ChangePasswordDialogProps) => {
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
            currentValues={{ userId, password, confirmedPassword: confirmPassword }}
            visible={visible}
            header={strings.eventStore.system.users.dialogs.changePassword.title}
            width="30vw"
            onConfirm={result => { if (result.isSuccess) onClose(); }}
            onCancel={onClose}>
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
        </CommandDialog>
    );
};
