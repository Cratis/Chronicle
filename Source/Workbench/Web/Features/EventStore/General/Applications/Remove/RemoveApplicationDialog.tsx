// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { RemoveApplication } from 'Api/Security';
import { Button } from 'primereact/button';
import { Dialog } from 'primereact/dialog';
import strings from 'Strings';
import { Guid } from '@cratis/fundamentals';

export interface RemoveApplicationDialogProps {
    applicationId: Guid;
    clientId: string;
}

export const RemoveApplicationDialog = ({ applicationId, clientId }: RemoveApplicationDialogProps) => {
    const { closeDialog } = useDialogContext();
    const [removeApplication] = RemoveApplication.use();

    const handleOk = async () => {
        removeApplication.id = applicationId;
        const result = await removeApplication.execute();
        if (result.isSuccess) {
            closeDialog(DialogResult.Ok);
        }
    };

    return (
        <Dialog
            header={strings.eventStore.system.applications.dialogs.removeApplication.title}
            visible={true}
            style={{ width: '450px' }}
            modal
            onHide={() => closeDialog(DialogResult.Cancelled)}>
            <p>{strings.eventStore.system.applications.dialogs.removeApplication.message.replace('{clientId}', clientId)}</p>

            <div className="card flex flex-wrap justify-content-center gap-3 mt-4">
                <Button
                    label={strings.general.buttons.yes}
                    icon="pi pi-check"
                    onClick={handleOk}
                    severity="danger"
                    autoFocus
                />
                <Button
                    label={strings.general.buttons.no}
                    icon="pi pi-times"
                    severity='secondary'
                    onClick={() => closeDialog(DialogResult.Cancelled)}
                />
            </div>
        </Dialog>
    );
};
