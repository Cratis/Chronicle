// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AddApplication } from 'Api/Security';
import { Button } from 'primereact/button';
import { useState } from 'react';
import strings from 'Strings';
import { generatePassword } from '../../PasswordHelpers';
import { useCommandFormContext } from '@cratis/arc.react/commands';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';

interface SecretActionsProps {
    showSecret: boolean;
    onToggleShow: () => void;
}

const SecretActions = ({ showSecret, onToggleShow }: SecretActionsProps) => {
    const { setCommandValues } = useCommandFormContext<AddApplication>();

    const handleGenerate = () => {
        setCommandValues({ clientSecret: generatePassword(32) } as unknown as AddApplication);
    };

    return (
        <div className="flex gap-2 mt-2">
            <Button
                icon={showSecret ? 'pi pi-eye-slash' : 'pi pi-eye'}
                onClick={onToggleShow}
                className="p-button-text"
                type="button"
                tooltip={showSecret ? strings.eventStore.system.applications.dialogs.addApplication.hideSecret : strings.eventStore.system.applications.dialogs.addApplication.showSecret}
            />
            <Button
                icon="pi pi-refresh"
                onClick={handleGenerate}
                className="p-button-text"
                type="button"
                tooltip={strings.eventStore.system.applications.dialogs.addApplication.generateSecret}
            />
        </div>
    );
};

export const AddApplicationDialog = () => {
    const [id] = useState(crypto.randomUUID());
    const [showSecret, setShowSecret] = useState(false);
    const { closeDialog } = useDialogContext<object>();

    return (
        <CommandDialog
            command={AddApplication}
            initialValues={{ id }}
            title={strings.eventStore.system.applications.dialogs.addApplication.title}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <InputTextField<AddApplication>
                value={c => c.clientId}
                title={strings.eventStore.system.applications.dialogs.addApplication.clientId}
                icon={<i className="pi pi-user" />} />
            <InputTextField<AddApplication>
                value={c => c.clientSecret}
                title={strings.eventStore.system.applications.dialogs.addApplication.clientSecret}
                type={showSecret ? 'text' : 'password'}
                icon={<i className="pi pi-lock" />} />
            <SecretActions showSecret={showSecret} onToggleShow={() => setShowSecret(v => !v)} />
        </CommandDialog>
    );
};
