// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { AddUser } from 'Api/Security';
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';
import strings from 'Strings';
import { Guid } from '@cratis/fundamentals';
import { generatePassword } from '../../PasswordHelpers';

export const AddUserDialog = () => {
    const [userId] = useState(Guid.parse(crypto.randomUUID()));
    const [username, setUsername] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const { closeDialog } = useDialogContext();
    const [addUser] = AddUser.use();

    const handleGeneratePassword = () => {
        setPassword(generatePassword());
    };

    const handleOk = async () => {
        if (username && password) {
            addUser.userId = userId;
            addUser.username = username;
            addUser.email = email || '';
            addUser.password = password;
            const result = await addUser.execute();
            if (result.isSuccess) {
                closeDialog(DialogResult.Ok);
            }
        }
    };

    return (
        <Dialog
            header={strings.eventStore.system.users.dialogs.addUser.title}
            visible={true}
            style={{ width: '450px' }}
            modal
            resizable={false}
            onHide={() => closeDialog(DialogResult.Cancelled)}>
            <div className="card flex flex-column gap-3 mb-3">
                <div className="p-inputgroup flex-1">
                    <span className="p-inputgroup-addon">
                        <i className="pi pi-user"></i>
                    </span>
                    <InputText
                        placeholder={strings.eventStore.system.users.dialogs.addUser.username}
                        value={username}
                        onChange={e => setUsername(e.target.value)}
                    />
                </div>
            </div>

            <div className="card flex flex-column gap-3 mb-3">
                <div className="p-inputgroup flex-1">
                    <span className="p-inputgroup-addon">
                        <i className="pi pi-envelope"></i>
                    </span>
                    <InputText
                        placeholder={strings.eventStore.system.users.dialogs.addUser.email}
                        value={email}
                        onChange={e => setEmail(e.target.value)}
                    />
                </div>
            </div>

            <div className="card flex flex-column gap-3 mb-3">
                <div className="p-inputgroup flex-1">
                    <span className="p-inputgroup-addon">
                        <i className="pi pi-lock"></i>
                    </span>
                    <InputText
                        type={showPassword ? 'text' : 'password'}
                        placeholder={strings.eventStore.system.users.dialogs.addUser.password}
                        value={password}
                        onChange={e => setPassword(e.target.value)}
                    />
                    <Button
                        icon={showPassword ? 'pi pi-eye-slash' : 'pi pi-eye'}
                        onClick={() => setShowPassword(!showPassword)}
                        className="p-button-text"
                        type="button"
                        tooltip={showPassword ? strings.eventStore.system.users.dialogs.addUser.hidePassword : strings.eventStore.system.users.dialogs.addUser.showPassword}
                    />
                    <Button
                        icon="pi pi-refresh"
                        onClick={handleGeneratePassword}
                        className="p-button-text"
                        type="button"
                        tooltip={strings.eventStore.system.users.dialogs.addUser.generatePassword}
                    />
                </div>
            </div>

            <div className="card flex flex-wrap justify-content-center gap-3 mt-4">
                <Button
                    label={strings.general.buttons.ok}
                    icon="pi pi-check"
                    onClick={handleOk}
                    disabled={!username || !password}
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
