// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { CreateReadModel } from 'Api/ReadModelTypes';
import { Dialog } from 'primereact/dialog';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { ReadModelCreation } from 'Components/ReadModelCreation';
import type { ReadModelSchema } from 'Api/ReadModels';

export const AddReadModelDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const { closeDialog } = useDialogContext();
    const [createReadModel] = CreateReadModel.use();

    const handleSave = async (_displayName: string, _identifier: string, containerName: string, schema: ReadModelSchema) => {
        if (containerName && params.eventStore) {
            createReadModel.eventStore = params.eventStore;
            createReadModel.containerName = containerName;
            createReadModel.schema = JSON.stringify(schema);
            const result = await createReadModel.execute();
            if (result.isSuccess) {
                closeDialog(DialogResult.Ok);
            }
        }
    };

    const handleCancel = () => {
        closeDialog(DialogResult.Cancelled);
    };

    return (
        <Dialog
            header={strings.eventStore.general.readModels.dialogs.addReadModel.title}
            visible={true}
            style={{ width: '800px', maxHeight: '80vh' }}
            modal
            resizable={true}
            onHide={() => closeDialog(DialogResult.Cancelled)}>
            <ReadModelCreation onSave={handleSave} onCancel={handleCancel} />
        </Dialog>
    );
};
