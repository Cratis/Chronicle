// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Dialog as PrimeDialog } from 'primereact/dialog';
import { Button } from 'primereact/button';
import { DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';
import { ReactNode } from 'react';

export type CloseDialog = (result: DialogResult) => boolean | void | Promise<boolean>;

export interface DialogProps {
    title: string;
    visible?: boolean;
    onClose: CloseDialog;
    buttons?: DialogButtons | ReactNode;
    children: ReactNode;
    width?: string;
    resizable?: boolean;
    isValid?: boolean;
}

export const Dialog = ({ title, visible = true, onClose, buttons = DialogButtons.OkCancel, children, width = '450px', resizable = false, isValid }: DialogProps) => {
    const isDialogValid = isValid !== false;
    const headerElement = (
        <div className="inline-flex align-items-center justify-content-center gap-2">
            <span className="font-bold white-space-nowrap">{title}</span>
        </div>
    );

    const handleClose = async (result: DialogResult) => {
        const shouldClose = await onClose(result);
        return shouldClose !== false;
    };

    const okFooter = (
        <>
            <Button label="Ok" icon="pi pi-check" onClick={() => handleClose(DialogResult.Ok)} disabled={!isDialogValid} autoFocus />
        </>
    );

    const okCancelFooter = (
        <>
            <Button label="Ok" icon="pi pi-check" onClick={() => handleClose(DialogResult.Ok)} disabled={!isDialogValid} autoFocus />
            <Button label="Cancel" icon="pi pi-times" outlined onClick={() => handleClose(DialogResult.Cancelled)} />
        </>
    );

    const yesNoFooter = (
        <>
            <Button label="Yes" icon="pi pi-check" onClick={() => handleClose(DialogResult.Yes)} disabled={!isDialogValid} autoFocus />
            <Button label="No" icon="pi pi-times" outlined onClick={() => handleClose(DialogResult.No)} />
        </>
    );

    const yesNoCancelFooter = (
        <>
            <Button label="Yes" icon="pi pi-check" onClick={() => handleClose(DialogResult.Yes)} disabled={!isDialogValid} autoFocus />
            <Button label="No" icon="pi pi-times" outlined onClick={() => handleClose(DialogResult.No)} />
            <Button label="Cancel" icon="pi pi-times" outlined onClick={() => handleClose(DialogResult.Cancelled)} />
        </>
    );

    const getFooterInterior = () => {
        // If buttons is a ReactNode (custom buttons), use it directly
        if (typeof buttons !== 'number') {
            return buttons;
        }

        // Otherwise, use predefined buttons based on DialogButtons enum
        switch (buttons) {
            case DialogButtons.Ok:
                return okFooter;
            case DialogButtons.OkCancel:
                return okCancelFooter;
            case DialogButtons.YesNo:
                return yesNoFooter;
            case DialogButtons.YesNoCancel:
                return yesNoCancelFooter;
        }

        return (<></>);
    };

    const footer = (
        <div className="flex flex-wrap justify-content-start gap-3">
            {getFooterInterior()}
        </div>
    );

    return (
        <PrimeDialog
            header={headerElement}
            modal
            footer={footer}
            onHide={() => handleClose(DialogResult.Cancelled)}
            visible={visible}
            style={{ width }}
            resizable={resizable}>
            {children}
        </PrimeDialog>
    );
};
