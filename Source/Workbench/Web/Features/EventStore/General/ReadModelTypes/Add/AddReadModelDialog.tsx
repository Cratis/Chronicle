// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { CreateReadModel } from 'Api/ReadModelTypes';
import { Dialog } from 'Components/Dialogs';
import { Button } from 'primereact/button';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { ReadModelTypeEditor } from 'Components/ReadModelTypeEditor/ReadModelTypeEditor';
import { type JsonSchema } from 'Components/JsonSchema';
import { useRef } from 'react';

export const AddReadModelDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const { closeDialog } = useDialogContext();
    const [createReadModel] = CreateReadModel.use();
    const savedDataRef = useRef<{ displayName: string; identifier: string; containerName: string; schema: JsonSchema } | null>(null);

    const handleSave = async (displayName: string, identifier: string, containerName: string, schema: JsonSchema) => {
        if (containerName && params.eventStore) {
            createReadModel.eventStore = params.eventStore;
            createReadModel.identifier = identifier;
            createReadModel.displayName = displayName;
            createReadModel.containerName = containerName;
            createReadModel.schema = JSON.stringify(schema);
            const result = await createReadModel.execute();
            if (result.isSuccess) {
                closeDialog(DialogResult.Ok);
            }
        }
    };

    const handleChanged = (displayName: string, identifier: string, containerName: string, schema: JsonSchema) => {
        const trimmedDisplayName = displayName.trim();
        const trimmedIdentifier = identifier.trim();
        const trimmedContainerName = containerName.trim();
        if (!trimmedDisplayName) {
            savedDataRef.current = null;
            return;
        }
        savedDataRef.current = {
            displayName: trimmedDisplayName,
            identifier: trimmedIdentifier,
            containerName: trimmedContainerName,
            schema
        };
    };

    const customButtons = (
        <>
            <Button
                label={strings.general.buttons.ok}
                icon="pi pi-check"
                onClick={() => savedDataRef.current && handleSave(savedDataRef.current.displayName, savedDataRef.current.identifier, savedDataRef.current.containerName, savedDataRef.current.schema)}
                disabled={!savedDataRef.current?.displayName || !savedDataRef.current?.identifier || !savedDataRef.current?.containerName}
                autoFocus
            />
            <Button
                label={strings.general.buttons.cancel}
                icon="pi pi-times"
                onClick={() => closeDialog(DialogResult.Cancelled)}
                outlined
            />
        </>
    );

    return (
        <Dialog
            title={strings.eventStore.general.readModels.dialogs.addReadModel.title}
            onClose={closeDialog}
            buttons={customButtons}
            width="800px"
            resizable={true}>
            <ReadModelTypeEditor onChanged={handleChanged} />
        </Dialog>
    );
};
