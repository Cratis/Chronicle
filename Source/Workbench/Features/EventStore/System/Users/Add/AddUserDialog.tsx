// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { AddUser } from 'Api/Security';
import { Button } from 'primereact/button';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { useState } from 'react';
import strings from 'Strings';
import { Guid } from '@cratis/fundamentals';
import { generatePassword } from '../../PasswordHelpers';

export const AddUserDialog = () => {
    const [userId] = useState(Guid.parse(crypto.randomUUID()));
    const [password, setPassword] = useState('');
    const [showPassword, setShowPassword] = useState(false);
    const { closeDialog } = useDialogContext();

    const handleGeneratePassword = () => {
        setPassword(generatePassword());
    };

    return (
        <CommandDialog
            command={AddUser}
            initialValues={{ userId }}
            currentValues={password.length > 0 ? { password } : undefined}
            visible={true}
            header={strings.eventStore.system.users.dialogs.addUser.title}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <CommandDialog.Fields>
                <InputTextField
                    value={(c: AddUser) => c.username}
                    title={strings.eventStore.system.users.dialogs.addUser.username}
                    icon={<i className="pi pi-user" />}
                />
                <InputTextField
                    value={(c: AddUser) => c.email}
                    title={strings.eventStore.system.users.dialogs.addUser.email}
                    icon={<i className="pi pi-envelope" />}
                    required={false}
                />
                <InputTextField
                    value={(c: AddUser) => c.password}
                    title={strings.eventStore.system.users.dialogs.addUser.password}
                    type={showPassword ? 'text' : 'password'}
                    icon={<i className="pi pi-lock" />}
                />
            </CommandDialog.Fields>
            <div className="flex gap-2 mt-2">
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
