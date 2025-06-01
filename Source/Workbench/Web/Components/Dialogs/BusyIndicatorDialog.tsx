// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Dialog } from 'primereact/dialog';
import { BusyIndicatorDialogRequest } from '@cratis/applications.react/dialogs';
import { ProgressSpinner } from 'primereact/progressspinner';

export const BusyIndicatorDialog = (props: BusyIndicatorDialogRequest) => {

    const headerElement = (
        <div className="inline-flex align-items-center justify-content-center gap-2">
            <span className="font-bold white-space-nowrap">{props.title}</span>
        </div>
    );

    return (
        <>
            <Dialog header={headerElement} modal visible={true} onHide={() => { }}>
                <ProgressSpinner />
                <p className="m-0">
                    {props.message}
                </p>
            </Dialog>
        </>
    );
};
