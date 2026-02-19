// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult } from '@cratis/arc.react/dialogs';
import { CreateReadModel } from 'Api/ReadModelTypes';
import { Dialog } from 'Components/Dialogs';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { ReadModelTypeEditor } from 'Components/ReadModelTypeEditor/ReadModelTypeEditor';
import { type JsonSchema } from 'Components/JsonSchema';
import { useState } from 'react';

export const AddReadModelDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [createReadModel] = CreateReadModel.use();
    const [validationState, setValidationState] = useState<{ displayName: string; identifier: string; containerName: string; schema: JsonSchema } | null>(null);

    const handleSave = async (displayName: string, identifier: string, containerName: string, schema: JsonSchema) => {
        if (containerName && params.eventStore) {
            createReadModel.eventStore = params.eventStore;
            createReadModel.identifier = identifier;
            createReadModel.displayName = displayName;
            createReadModel.containerName = containerName;
            createReadModel.schema = JSON.stringify(schema);
            const result = await createReadModel.execute();
            return result.isSuccess;
        }
        return false;
    };

    const handleChanged = (displayName: string, identifier: string, containerName: string, schema: JsonSchema) => {
        const trimmedDisplayName = displayName.trim();
        const trimmedIdentifier = identifier.trim();
        const trimmedContainerName = containerName.trim();
        if (!trimmedDisplayName || !trimmedIdentifier || !trimmedContainerName) {
            setValidationState(null);
            return;
        }
        setValidationState({
            displayName: trimmedDisplayName,
            identifier: trimmedIdentifier,
            containerName: trimmedContainerName,
            schema
        });
    };

    const handleClose = async (result: DialogResult) => {
        if (result !== DialogResult.Ok) {
            return true;
        }

        if (validationState) {
            return await handleSave(validationState.displayName, validationState.identifier, validationState.containerName, validationState.schema);
        }

        return false;
    };

    return (
        <Dialog
            title={strings.eventStore.general.readModels.dialogs.addReadModel.title}
            onClose={handleClose}
            isValid={!!validationState}
            width="800px"
            resizable={true}>
            <ReadModelTypeEditor onChanged={handleChanged} />
        </Dialog>
    );
};
