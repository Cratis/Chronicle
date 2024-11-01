// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Dialog } from 'primereact/dialog';
import { DialogButtons, ConfirmationDialogRequest, useDialogContext } from '@cratis/applications.react.mvvm/dialogs';
import { DialogResult } from '@cratis/applications.react/dialogs';
import { Button } from 'primereact/button';

export const ConfirmationDialog = () => {
    const { request, resolver } = useDialogContext<ConfirmationDialogRequest, DialogResult>();

    const headerElement = (
        <div className="inline-flex align-items-center justify-content-center gap-2">
            <span className="font-bold white-space-nowrap">{request.title}</span>
        </div>
    );

    const okFooter = (
        <>
            <Button label="Ok" icon="pi pi-check" onClick={() => resolver(DialogResult.Ok)} autoFocus />
        </>
    );

    const okCancelFooter = (
        <>
            <Button label="Ok" icon="pi pi-check" onClick={() => resolver(DialogResult.Ok)} autoFocus />
            <Button label="Cancel" icon="pi pi-times" severity='secondary' onClick={() => resolver(DialogResult.Cancelled)} />
        </>
    );

    const yesNoFooter = (
        <>
            <Button label="Yes" icon="pi pi-check" onClick={() => resolver(DialogResult.Yes)} autoFocus />
            <Button label="No" icon="pi pi-times" severity='secondary' onClick={() => resolver(DialogResult.No)} />
        </>
    );

    const yesNoCancelFooter = (
        <>
            <Button label="Yes" icon="pi pi-check" onClick={() => resolver(DialogResult.Yes)} autoFocus />
            <Button label="No" icon="pi pi-times" severity='secondary' onClick={() => resolver(DialogResult.No)} />
        </>
    );

    const getFooterInterior = () => {
        switch (request.buttons) {
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
        <div className="card flex flex-wrap justify-content-center gap-3">
            {getFooterInterior()}
        </div>
    );

    return (
        <>
            <Dialog header={headerElement} modal footer={footer} onHide={() => resolver(DialogResult.Cancelled)} visible={true}>
                <p className="m-0">
                    {request.message}
                </p>
            </Dialog>
        </>
    );
};
