// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AddUser } from 'Api/Security';
import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import { useState } from 'react';
import strings from 'Strings';
import { Guid } from '@cratis/fundamentals';
import { generatePassword } from '../../PasswordHelpers';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';

export const AddUserDialog = () => {
    const [userId] = useState(Guid.parse(crypto.randomUUID()));
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const { closeDialog } = useDialogContext<object>();

    const handleGeneratePassword = () => {
        setPassword(generatePassword());
    };

    return (
        <CommandDialog
            command={AddUser}
            currentValues={{ userId, password }}
            title={strings.eventStore.system.users.dialogs.addUser.title}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <InputTextField<AddUser>
                value={c => c.username}
                title={strings.eventStore.system.users.dialogs.addUser.username}
                icon={<i className="pi pi-user" />} />
            <InputTextField<AddUser>
                value={c => c.email}
                title={strings.eventStore.system.users.dialogs.addUser.email}
                icon={<i className="pi pi-envelope" />}
                required={false} />
            <div className="p-inputgroup flex-1 mt-3">
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
        </CommandDialog>
    );
};
