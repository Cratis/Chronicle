// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { AddApplication } from 'Api/Security';
import { Button } from 'primereact/button';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { useState } from 'react';
import strings from 'Strings';
import { generatePassword } from '../../PasswordHelpers';

export const AddApplicationDialog = () => {
    const [id] = useState(crypto.randomUUID());
    const [clientSecret, setClientSecret] = useState('');
    const [showSecret, setShowSecret] = useState(false);
    const { closeDialog } = useDialogContext();

    const handleGenerateSecret = () => {
        setClientSecret(generatePassword(32));
    };

    return (
        <CommandDialog
            command={AddApplication}
            initialValues={{ id }}
            currentValues={clientSecret.length > 0 ? { clientSecret } : undefined}
            visible={true}
            header={strings.eventStore.system.applications.dialogs.addApplication.title}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <CommandDialog.Fields>
                <InputTextField
                    value={(c: AddApplication) => c.clientId}
                    title={strings.eventStore.system.applications.dialogs.addApplication.clientId}
                    icon={<i className="pi pi-user" />}
                />
                <InputTextField
                    value={(c: AddApplication) => c.clientSecret}
                    title={strings.eventStore.system.applications.dialogs.addApplication.clientSecret}
                    type={showSecret ? 'text' : 'password'}
                    icon={<i className="pi pi-lock" />}
                />
            </CommandDialog.Fields>
            <div className="flex gap-2 mt-2">
                <Button
                    icon={showSecret ? 'pi pi-eye-slash' : 'pi pi-eye'}
                    onClick={() => setShowSecret(!showSecret)}
                    className="p-button-text"
                    type="button"
                    tooltip={showSecret ? strings.eventStore.system.applications.dialogs.addApplication.hideSecret : strings.eventStore.system.applications.dialogs.addApplication.showSecret}
                />
                <Button
                    icon="pi pi-refresh"
                    onClick={handleGenerateSecret}
                    className="p-button-text"
                    type="button"
                    tooltip={strings.eventStore.system.applications.dialogs.addApplication.generateSecret}
                />
            </div>
        </CommandDialog>
    );
};
