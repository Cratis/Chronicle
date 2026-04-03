// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChangePasswordForUser } from 'Api/Security';
import { Button } from 'primereact/button';
import { useMemo, useState } from 'react';
import strings from 'Strings';
import { generatePassword } from '../../PasswordHelpers';
import { Guid } from '@cratis/fundamentals';
import { useCommandFormContext } from '@cratis/arc.react/commands';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';

export interface ChangePasswordDialogRequest {
    userId: Guid;
}

interface PasswordActionsProps {
    showPassword: boolean;
    onToggleShow: () => void;
    onGenerate: () => void;
}

const PasswordActions = ({ showPassword, onToggleShow, onGenerate }: PasswordActionsProps) => {
    const { setCommandValues } = useCommandFormContext<ChangePasswordForUser>();

    const handleGenerate = () => {
        const pw = generatePassword();
        setCommandValues({ password: pw, confirmedPassword: pw } as unknown as ChangePasswordForUser);
        onGenerate();
    };

    return (
        <div className="flex gap-2 mt-2">
            <Button
                icon={showPassword ? 'pi pi-eye-slash' : 'pi pi-eye'}
                onClick={onToggleShow}
                className="p-button-text"
                type="button"
                tooltip={showPassword ? strings.eventStore.system.users.dialogs.changePassword.hidePassword : strings.eventStore.system.users.dialogs.changePassword.showPassword}
            />
            <Button
                icon="pi pi-refresh"
                onClick={handleGenerate}
                className="p-button-text"
                type="button"
                tooltip={strings.eventStore.system.users.dialogs.changePassword.generatePassword}
            />
        </div>
    );
};

export const ChangePasswordDialog = () => {
    const { request, closeDialog } = useDialogContext<ChangePasswordDialogRequest>();
    const [showPassword, setShowPassword] = useState(false);
    const [passwordsMatch, setPasswordsMatch] = useState(true);
    const initialPassword = useMemo(() => generatePassword(), []);

    return (
        <CommandDialog
            command={ChangePasswordForUser}
            initialValues={{ userId: request.userId, password: initialPassword, confirmedPassword: initialPassword }}
            isValid={passwordsMatch}
            title={strings.eventStore.system.users.dialogs.changePassword.title}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            width="450px"
            onFieldChange={(command) => {
                setPasswordsMatch(command.password === command.confirmedPassword);
            }}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <div className="p-fluid">
                <InputTextField<ChangePasswordForUser>
                    value={c => c.password}
                    title={strings.eventStore.system.users.dialogs.changePassword.password}
                    type={showPassword ? 'text' : 'password'}
                />
                <InputTextField<ChangePasswordForUser>
                    value={c => c.confirmedPassword}
                    title={strings.eventStore.system.users.dialogs.changePassword.confirmPassword}
                    type={showPassword ? 'text' : 'password'}
                />
                <PasswordActions
                    showPassword={showPassword}
                    onToggleShow={() => setShowPassword(v => !v)}
                    onGenerate={() => setPasswordsMatch(true)}
                />
                {!passwordsMatch && (
                    <small className="p-error">Passwords do not match</small>
                )}
            </div>
        </CommandDialog>
    );
};
