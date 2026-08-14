// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { DialogResult, useDialogContext } from '@cratis/arc.react/dialogs';
import { Dialog } from '@cratis/components/Dialogs';
import { Dropdown } from '@cratis/components/Dropdown';
import { InputText } from 'primereact/inputtext';
import { SelectButton } from 'primereact/selectbutton';
import strings from 'Strings';
import { SequenceQueryScope } from 'Api/SequenceQueries/SequenceQueryScope';
import './SaveQueryDialog.css';

/**
 * What the dialog needs to know about the query being saved.
 */
export interface SaveQueryDialogInput {
    /** The name to start from. */
    name: string;
    /** The scope to start from. */
    scope: SequenceQueryScope;
    /** The folder to start from. */
    folder: string;
    /** The folders that already exist, so the query can be filed into one of them. */
    folders: string[];
}

/**
 * What the user decided.
 */
export interface SaveQueryDialogResponse {
    /** The name to save the query under. */
    name: string;
    /** Who the query should be visible to. */
    scope: SequenceQueryScope;
    /** The folder to file the query under, or empty for the root of its scope. */
    folder: string;
}

/**
 * Asks where a query should live the first time it is saved.
 *
 * Only shown on the first save - after that the name and its place in the hierarchy are changed
 * where they are shown, by renaming the node or the tab.
 * @returns The rendered dialog.
 */
export const SaveQueryDialog = () => {
    const { request, closeDialog } = useDialogContext<SaveQueryDialogInput, SaveQueryDialogResponse>();
    const sequenceStrings = strings.eventStore.namespaces.sequences;

    const [name, setName] = useState(request.name);
    const [scope, setScope] = useState(request.scope);
    const [folder, setFolder] = useState(request.folder);

    const scopeOptions = [
        { label: sequenceStrings.scope.onlyMe, value: SequenceQueryScope.user },
        { label: sequenceStrings.scope.everyone, value: SequenceQueryScope.everyone }
    ];

    const folderOptions = [
        { label: sequenceStrings.save.rootFolder, value: '' },
        ...request.folders.map(candidate => ({ label: candidate, value: candidate }))
    ];

    return (
        <Dialog
            title={sequenceStrings.save.title}
            width='28rem'
            okLabel={sequenceStrings.actions.save}
            cancelLabel={strings.general.buttons.cancel}
            isValid={name.trim().length > 0}
            onConfirm={() => closeDialog(DialogResult.Ok, { name: name.trim(), scope, folder })}
            onCancel={() => closeDialog(DialogResult.Cancelled)}>

            <div className='save-query'>
                <label className='save-query__field'>
                    <span>{sequenceStrings.queryName}</span>
                    <InputText
                        value={name}
                        placeholder={sequenceStrings.newQuery}
                        autoFocus
                        onChange={event => setName(event.target.value)} />
                </label>

                <label className='save-query__field'>
                    <span>{sequenceStrings.save.visibleTo}</span>
                    <SelectButton
                        value={scope}
                        options={scopeOptions}
                        allowEmpty={false}
                        onChange={event => setScope(event.value as SequenceQueryScope)} />
                </label>

                <label className='save-query__field'>
                    <span>{sequenceStrings.save.folder}</span>
                    <Dropdown
                        value={folder}
                        options={folderOptions}
                        editable
                        optionLabel='label'
                        optionValue='value'
                        placeholder={sequenceStrings.save.rootFolder}
                        onChange={event => setFolder((event.value as string) ?? '')} />
                </label>
            </div>
        </Dialog>
    );
};
