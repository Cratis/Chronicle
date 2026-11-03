// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { Message } from '@cratis/components/Display';
import { ProjectionDeclarationSyntaxError, SaveProjectionWithInferredReadModel } from 'Features/ProjectionEditor';
import { useRef, useState } from 'react';
import strings from 'Strings';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';

export interface SaveWithInferredReadModelDialogProps {
    declaration: string;
}

/// <summary>
/// Dialog shown when the user attempts to save a projection that has no explicit read model type.
/// Prompts for a name and lets the backend infer the read model schema from the projection's event types.
/// </summary>
export const SaveWithInferredReadModelDialog = () => {
    const { request, closeDialog } = useDialogContext<SaveWithInferredReadModelDialogProps>();
    const params = useParams<EventStoreAndNamespaceParams>();
    const responseErrors = useRef<ProjectionDeclarationSyntaxError[]>([]);
    const [errors, setErrors] = useState<ProjectionDeclarationSyntaxError[]>([]);

    return (
        <CommandDialog<SaveProjectionWithInferredReadModel, ProjectionDeclarationSyntaxError[]>
            command={SaveProjectionWithInferredReadModel}
            initialValues={{
                declaration: request.declaration,
                eventStore: params.eventStore!,
                namespace: params.namespace!
            }}
            title={strings.eventStore.general.projections.dialogs.saveWithInferredReadModel.title}
            okLabel={strings.general.buttons.ok}
            cancelLabel={strings.general.buttons.cancel}
            width="35rem"
            onSuccess={response => {
                responseErrors.current = response ?? [];
                setErrors(responseErrors.current);
            }}
            onConfirm={() => {
                if (responseErrors.current.length > 0) return false;
                return closeDialog(DialogResult.Ok);
            }}
            onCancel={() => closeDialog(DialogResult.Cancelled)}
        >
            <p>{strings.eventStore.general.projections.dialogs.saveWithInferredReadModel.message}</p>
            <InputTextField<SaveProjectionWithInferredReadModel>
                value={c => c.readModelDisplayName}
                title={strings.eventStore.general.projections.dialogs.saveWithInferredReadModel.readModelNameLabel}
            />
            {errors.map((error, index) => <Message key={index} severity="error" text={error.message} className="mb-2" />)}
        </CommandDialog>
    );
};
