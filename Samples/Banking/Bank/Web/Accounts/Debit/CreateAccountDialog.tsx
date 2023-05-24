// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';

import {
    Dialog,
    DialogFooter,
    DialogType,
    DefaultButton,
    IDialogContentProps,
    PrimaryButton,
    TextField,
    Checkbox,
} from '@fluentui/react';

import { IDialogProps, DialogResult } from '@aksio/cratis-applications-frontend/dialogs';

const dialogContentProps: IDialogContentProps = {
    type: DialogType.normal,
    title: 'Create Debit Account',
    closeButtonAriaLabel: 'Close'
};

export type CreateAccountDialogResult = {
    name: string;
    includeCard: boolean;
};


export const CreateAccountDialog = (props: IDialogProps<any, CreateAccountDialogResult>) => {
    const [name, setName] = useState('');
    const [includeCard, setIncludeCard] = useState(false);

    const create = () => {
        setName('');
        setIncludeCard(false);
        props.onClose(DialogResult.Success, { name, includeCard });
    };

    const cancel = () => {
        setName('');
        setIncludeCard(false);
        props.onClose(DialogResult.Cancelled);
    };

    return (
        <Dialog
            minWidth={600}
            hidden={!props.visible}
            onDismiss={create}
            dialogContentProps={dialogContentProps}>

            <TextField label="Name" value={name} onChange={(ev, value) => setName(value!)} />
            <Checkbox onChange={(ev, checked) => setIncludeCard(checked!) } label="Include card"/>

            <DialogFooter>
                <PrimaryButton onClick={create} text="Create" />
                <DefaultButton onClick={cancel} text="Cancel" />
            </DialogFooter>
        </Dialog>
    );
};
