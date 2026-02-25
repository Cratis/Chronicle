// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { AllApplications, Application, RemoveApplication } from 'Api/Security';
import { DataPage, MenuItem } from 'Components';
import * as faIcons from 'react-icons/fa6';
import { AddApplicationDialog } from './Add/AddApplicationDialog';
import { ChangeSecretDialog } from './ChangeSecret';
import { useConfirmationDialog, DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';
import { useState } from 'react';
import strings from 'Strings';
import { Guid } from '@cratis/fundamentals';

const formatDate = (date: Date | string) => {
    if (!date) return '';
    return new Date(date).toLocaleString();
};

export const Applications = () => {
    const [selectedApplication, setSelectedApplication] = useState<Application | null>(null);
    const [showAddApplication, setShowAddApplication] = useState(false);
    const [showChangeSecret, setShowChangeSecret] = useState(false);
    const [showConfirmation] = useConfirmationDialog();
    const [removeApplication] = RemoveApplication.use();

    const handleChangeSecret = () => {
        if (selectedApplication) {
            setShowChangeSecret(true);
        }
    };

    const handleRemoveApplication = async () => {
        if (selectedApplication) {
            const result = await showConfirmation(
                strings.eventStore.system.applications.dialogs.removeApplication.title,
                strings.eventStore.system.applications.dialogs.removeApplication.message.replace('{clientId}', selectedApplication.clientId),
                DialogButtons.YesNo
            );

            if (result === DialogResult.Yes) {
                removeApplication.id = selectedApplication.id;
                await removeApplication.execute();
            }
        }
    };

    return (
        <>
            <DataPage
                title={strings.eventStore.system.applications.title}
                query={AllApplications}
                emptyMessage={strings.eventStore.system.applications.empty}
                dataKey='id'
                selection={selectedApplication}
                onSelectionChange={(e) => setSelectedApplication(e.value as Application)}>
                <DataPage.MenuItems>
                    <MenuItem
                        id="add"
                        label={strings.eventStore.system.applications.actions.add}
                        icon={faIcons.FaPlus}
                        command={() => setShowAddApplication(true)} />
                    <MenuItem
                        id="changeSecret"
                        label={strings.eventStore.system.applications.actions.changeSecret}
                        icon={faIcons.FaKey}
                        disableOnUnselected
                        command={handleChangeSecret} />
                    <MenuItem
                        id="remove"
                        label={strings.eventStore.system.applications.actions.remove}
                        icon={faIcons.FaTrash}
                        disableOnUnselected
                        command={handleRemoveApplication} />
                </DataPage.MenuItems>
                <DataPage.Columns>
                    <Column field='clientId' header={strings.eventStore.system.applications.columns.clientId} sortable />
                    <Column
                        field='isActive'
                        header={strings.eventStore.system.applications.columns.active}
                        sortable
                        body={(application: Application) => application.isActive ? strings.eventStore.system.applications.columns.yes : strings.eventStore.system.applications.columns.no} />
                    <Column
                        field='createdAt'
                        header={strings.eventStore.system.applications.columns.created}
                        sortable
                        body={(application: Application) => formatDate(application.createdAt)} />
                    <Column
                        field='lastModifiedAt'
                        header={strings.eventStore.system.applications.columns.lastModified}
                        sortable
                        body={(application: Application) => formatDate(application.lastModifiedAt || '')} />
                </DataPage.Columns>
            </DataPage>
            <AddApplicationDialog visible={showAddApplication} onClose={() => setShowAddApplication(false)} />
            <ChangeSecretDialog
                visible={showChangeSecret}
                applicationId={selectedApplication?.id ?? Guid.empty}
                onClose={() => setShowChangeSecret(false)} />
        </>
    );
};
