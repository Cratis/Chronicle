// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CreateReadModel } from 'Api/ReadModelTypes';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { ReadModelTypeEditor } from 'Components/ReadModelTypeEditor/ReadModelTypeEditor';
import { type JsonSchema } from 'Components/JsonSchema';
import { useState } from 'react';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';

export const AddReadModelDialog = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [readModelValues, setReadModelValues] = useState<{ displayName: string; identifier: string; containerName: string; schema: JsonSchema } | null>(null);
    const { closeDialog } = useDialogContext<object>();

    const handleChanged = (displayName: string, identifier: string, containerName: string, schema: JsonSchema) => {
        const trimmedDisplayName = displayName.trim();
        const trimmedIdentifier = identifier.trim();
        const trimmedContainerName = containerName.trim();
        if (!trimmedDisplayName || !trimmedIdentifier || !trimmedContainerName) {
            setReadModelValues(null);
            return;
        }
        setReadModelValues({
            displayName: trimmedDisplayName,
            identifier: trimmedIdentifier,
            containerName: trimmedContainerName,
            schema
        });
    };

    const currentValues = readModelValues ? {
        eventStore: params.eventStore,
        identifier: readModelValues.identifier,
        displayName: readModelValues.displayName,
        containerName: readModelValues.containerName,
        schema: JSON.stringify(readModelValues.schema)
    } : { eventStore: params.eventStore };

    return (
        <CommandDialog
            command={CreateReadModel}
            currentValues={currentValues}
            title={strings.eventStore.general.readModels.dialogs.addReadModel.title}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            width="800px"
            onConfirm={() => closeDialog(DialogResult.Ok)}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>
            <ReadModelTypeEditor onChanged={handleChanged} />
        </CommandDialog>
    );
};
