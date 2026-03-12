// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ConfirmationDialogRequest, useDialogContext } from '@cratis/arc.react/dialogs';
import { Dialog } from './Dialog';

export const ConfirmationDialog = () => {
    const { request, closeDialog } = useDialogContext<ConfirmationDialogRequest>();

    return (
        <Dialog title={request.title} onClose={closeDialog} buttons={request.buttons}>
            <p className="m-0">
                {request.message}
            </p>
        </Dialog>
    );
};
