// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { ChangePasswordForUser } from 'Api/Security';
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import { Password } from 'primereact/password';
import { useState } from 'react';
import strings from 'Strings';

export interface ChangePasswordDialogProps {
    userId: Guid;
}

export const ChangePasswordDialog = () => {
    const { request, closeDialog } = useDialogContext<ChangePasswordDialogProps>();
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const [changePassword] = ChangePasswordForUser.use();

    const generatePassword = () => {
        const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{}|;:,.<>?';
        const length = 16;
        let password = '';
        const array = new Uint32Array(length);
        crypto.getRandomValues(array);
        for (let i = 0; i < length; i++) {
            password += chars[array[i] % chars.length];
        }
        setPassword(password);
    };

    const handleOk = async () => {
        if (password && request.userId) {
            changePassword.userId = request.userId;
            changePassword.password = password;
            const result = await changePassword.execute();
            if (result.isSuccess) {
                closeDialog(DialogResult.Ok);
            }
        }
    };

    return (
        <Dialog
            header={strings.eventStore.system.users.dialogs.changePassword.title}
            visible={true}
            style={{ width: '30vw' }}
            modal
            onHide={() => closeDialog(DialogResult.Cancelled)}>
            <div className="flex flex-column gap-3">
                <div className="p-inputgroup">
                    <span className="p-inputgroup-addon">
                        <i className="pi pi-lock"></i>
                    </span>
                    <Password
                        placeholder={strings.eventStore.system.users.dialogs.changePassword.password}
                        value={password}
                        onChange={e => setPassword(e.target.value)}
                        feedback={false}
                        toggleMask={showPassword}
                        pt={{
                            input: { className: 'w-full' }
                        }}
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
                        onClick={generatePassword}
                        className="p-button-text"
                        type="button"
                        tooltip={strings.eventStore.system.users.dialogs.changePassword.generatePassword}
                    />
                </div>
            </div>

            <div className="flex flex-wrap justify-content-center gap-3 mt-4">
                <Button
                    label={strings.general.buttons.ok}
                    icon="pi pi-check"
                    onClick={handleOk}
                    disabled={!password}
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
