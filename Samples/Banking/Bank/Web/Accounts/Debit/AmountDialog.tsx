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
} from '@fluentui/react';

import { IDialogProps, DialogResult } from '@aksio/cratis-applications-frontend/dialogs';

const dialogContentProps: IDialogContentProps = {
    type: DialogType.normal,
    title: 'Create Debit Account',
    closeButtonAriaLabel: 'Close'
};

export type AmountDialogInput = {
    okTitle: string;
}

export type AmountDialogResult = {
    amount: number;
};

export const AmountDialog = (props: IDialogProps<AmountDialogInput, AmountDialogResult>) => {
    const [amount, setAmount] = useState('0');

    const create = () => {
        setAmount('0');
        props.onClose(DialogResult.Success, { amount: parseInt(amount) });
    };

    const cancel = () => {
        setAmount('0');
        props.onClose(DialogResult.Cancelled);
    };

    return (
        <Dialog
            minWidth={600}
            hidden={!props.visible}
            onDismiss={create}
            dialogContentProps={dialogContentProps}>

            <TextField label="Amount" value={amount} onChange={(ev, value) => setAmount(value!)} />

            <DialogFooter>
                <PrimaryButton onClick={create} text={props.input.okTitle} />
                <DefaultButton onClick={cancel} text="Cancel" />
            </DialogFooter>
        </Dialog>
    );
};
