// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ChangeApplicationSecret } from 'Api/Security';
import { Button } from 'primereact/button';
import { useMemo, useState } from 'react';
import strings from 'Strings';
import { Guid } from '@cratis/fundamentals';
import { generatePassword } from '../../PasswordHelpers';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField, useCommandFormContext } from '@cratis/components/CommandForm';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';

export interface ChangeSecretDialogRequest {
    applicationId: Guid;
}

interface SecretActionsProps {
    showSecret: boolean;
    onToggleShow: () => void;
}

const SecretActions = ({ showSecret, onToggleShow }: SecretActionsProps) => {
    const { setCommandValues } = useCommandFormContext<ChangeApplicationSecret>();

    const handleGenerate = () => {
        setCommandValues({ clientSecret: generatePassword(32) } as unknown as ChangeApplicationSecret);
    };

    return (
        <div className="flex gap-2 mt-2">
            <Button
                icon={showSecret ? 'pi pi-eye-slash' : 'pi pi-eye'}
                onClick={onToggleShow}
                className="p-button-text"
                type="button"
                tooltip={showSecret ? strings.eventStore.system.applications.dialogs.changeSecret.hideSecret : strings.eventStore.system.applications.dialogs.changeSecret.showSecret}
            />
            <Button
                icon="pi pi-refresh"
                onClick={handleGenerate}
                className="p-button-text"
                type="button"
                tooltip={strings.eventStore.system.applications.dialogs.changeSecret.generateSecret}
            />
        </div>
    );
};

export const ChangeSecretDialog = () => {
    const { request, closeDialog } = useDialogContext<ChangeSecretDialogRequest>();
    const [showSecret, setShowSecret] = useState(false);
    const initialSecret = useMemo(() => generatePassword(32), []);

    return (
        <CommandDialog
            command={ChangeApplicationSecret}
            initialValues={{ id: request.applicationId, clientSecret: initialSecret }}
            title={strings.eventStore.system.applications.dialogs.changeSecret.title}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <InputTextField<ChangeApplicationSecret>
                value={c => c.clientSecret}
                title={strings.eventStore.system.applications.dialogs.changeSecret.clientSecret}
                type={showSecret ? 'text' : 'password'}
                icon={<i className="pi pi-lock" />}
            />
            <SecretActions showSecret={showSecret} onToggleShow={() => setShowSecret(v => !v)} />
        </CommandDialog>
    );
};
