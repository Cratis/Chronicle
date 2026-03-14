// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { SaveProjectionWithInferredReadModel } from 'Api/Projections';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';

interface Props {
    declaration: string;
}

/// <summary>
/// Dialog shown when the user attempts to save a projection that has no explicit read model type.
/// Prompts for a name and lets the backend infer the read model schema from the projection's event types.
/// </summary>
export const SaveWithInferredReadModelDialog = ({ declaration }: Props) => {
    const params = useParams<EventStoreAndNamespaceParams>();

    return (
        <CommandDialog
            command={SaveProjectionWithInferredReadModel}
            header={strings.eventStore.general.projections.dialogs.saveWithInferredReadModel.title}
            onConfirm={async () => {}}
            width="35rem"
            onBeforeExecute={(command) => {
                command.declaration = declaration;
                command.eventStore = params.eventStore!;
                command.namespace = params.namespace!;
            }}
        >
            <p>{strings.eventStore.general.projections.dialogs.saveWithInferredReadModel.message}</p>
            <InputTextField
                label={strings.eventStore.general.projections.dialogs.saveWithInferredReadModel.readModelNameLabel}
                propertyName="readModelDisplayName"
            />
        </CommandDialog>
    );
};
