// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { RemoveUser } from 'Api/Security';
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import strings from 'Strings';

export interface RemoveUserDialogProps {
    userId: string;
    username: string;
}

export const RemoveUserDialog = () => {
    const { request, closeDialog } = useDialogContext<RemoveUserDialogProps>();
    const [removeUser] = RemoveUser.use();

    const handleOk = async () => {
        if (request.userId) {
            removeUser.userId = request.userId;
            const result = await removeUser.execute();
            if (result.isSuccess) {
                closeDialog(DialogResult.Ok);
            }
        }
    };

    return (
        <Dialog
            header={strings.eventStore.system.users.dialogs.removeUser.title}
            visible={true}
            style={{ width: '30vw' }}
            modal
            onHide={() => closeDialog(DialogResult.Cancelled)}>
            <div className="flex flex-column gap-3">
                <p>
                    {strings.eventStore.system.users.dialogs.removeUser.message.replace('{username}', request.username)}
                </p>
            </div>

            <div className="flex flex-wrap justify-content-center gap-3 mt-4">
                <Button
                    label={strings.general.buttons.ok}
                    icon="pi pi-check"
                    onClick={handleOk}
                    severity="danger"
                    autoFocus
                />
                <Button
                    label={strings.general.buttons.cancel}
                    icon="pi pi-times"
                    severity='secondary'
                    onClick={() => closeDialog(DialogResult.Cancelled)}
                />
            </div>
        </Dialog>
    );
};
