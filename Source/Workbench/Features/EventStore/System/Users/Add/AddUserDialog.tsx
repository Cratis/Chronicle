// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AddUser } from 'Api/Security';
import { Button } from 'primereact/button';
import { useState } from 'react';
import strings from 'Strings';
import { Guid } from '@cratis/fundamentals';
import { generatePassword } from '../../PasswordHelpers';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField, useCommandFormContext } from '@cratis/components/CommandForm';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';

interface PasswordActionsProps {
    showPassword: boolean;
    onToggleShow: () => void;
}

const PasswordActions = ({ showPassword, onToggleShow }: PasswordActionsProps) => {
    const { setCommandValues } = useCommandFormContext<AddUser>();

    const handleGenerate = () => {
        setCommandValues({ password: generatePassword() } as unknown as AddUser);
    };

    return (
        <div className="flex gap-2 mt-2">
            <Button
                icon={showPassword ? 'pi pi-eye-slash' : 'pi pi-eye'}
                onClick={onToggleShow}
                className="p-button-text"
                type="button"
                tooltip={showPassword ? strings.eventStore.system.users.dialogs.addUser.hidePassword : strings.eventStore.system.users.dialogs.addUser.showPassword}
            />
            <Button
                icon="pi pi-refresh"
                onClick={handleGenerate}
                className="p-button-text"
                type="button"
                tooltip={strings.eventStore.system.users.dialogs.addUser.generatePassword}
            />
        </div>
    );
};

export const AddUserDialog = () => {
    const [userId] = useState(Guid.parse(crypto.randomUUID()));
    const [showPassword, setShowPassword] = useState(false);
    const { closeDialog } = useDialogContext<object>();

    return (
        <CommandDialog
            command={AddUser}
            initialValues={{ userId }}
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
            <InputTextField<AddUser>
                value={c => c.password}
                title={strings.eventStore.system.users.dialogs.addUser.password}
                type={showPassword ? 'text' : 'password'}
                icon={<i className="pi pi-lock" />} />
            <PasswordActions showPassword={showPassword} onToggleShow={() => setShowPassword(v => !v)} />
        </CommandDialog>
    );
};
