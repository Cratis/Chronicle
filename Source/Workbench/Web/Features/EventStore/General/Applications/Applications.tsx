// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { AllApplications, Application } from 'Api/Security';
import { DataPage, MenuItem } from 'Components';
import * as faIcons from 'react-icons/fa6';
import { AddApplicationDialog } from './Add/AddApplicationDialog';
import { ChangeSecretDialog  } from './ChangeSecret';
import { RemoveApplicationDialog } from './Remove';
import { useDialog } from '@cratis/arc.react/dialogs';
import { useState } from 'react';
import strings from 'Strings';

const formatDate = (date: Date | string) => {
    if (!date) return '';
    return new Date(date).toLocaleString();
};

export const Applications = () => {
    const [selectedApplication, setSelectedApplication] = useState<Application | null>(null);
    const [AddApplicationWrapper, showAddApplication] = useDialog(AddApplicationDialog);
    const [ChangeSecretWrapper, showChangeSecret] = useDialog(ChangeSecretDialog);
    const [RemoveApplicationWrapper, showRemoveApplication] = useDialog(RemoveApplicationDialog);

    const handleChangeSecret = () => {
        if (selectedApplication) {
            showChangeSecret({ applicationId: selectedApplication.id });
        }
    };

    const handleRemoveApplication = () => {
        if (selectedApplication) {
            showRemoveApplication({ applicationId: selectedApplication.id, clientId: selectedApplication.clientId });
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
                        command={() => showAddApplication()} />
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
                    <Column field='id' header={strings.eventStore.system.applications.columns.id} sortable />
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
            <AddApplicationWrapper />
            <ChangeSecretWrapper />
            <RemoveApplicationWrapper />
        </>
    );
};
