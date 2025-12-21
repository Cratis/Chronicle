// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/arc.react.mvvm';
import { ApplicationsViewModel } from './ApplicationsViewModel';
import { Column } from 'primereact/column';
import { AllApplications, Applications } from 'Api/Security';
import { DataPage, MenuItem } from 'Components';
import * as faIcons from 'react-icons/fa6';

const formatDate = (date: string) => {
    if (!date) return '';
    return new Date(date).toLocaleString();
};

export const ApplicationsPage = withViewModel(ApplicationsViewModel, ({ viewModel }) => {
    return (
        <DataPage
            title="Client Credentials"
            query={AllApplications}
            emptyMessage="No applications found"
            dataKey='id'
            onSelectionChange={(e) => (viewModel.selectedClient = e.value as Applications)}>
            <DataPage.MenuItems>
                <MenuItem
                    id="add"
                    label="Add Client Credentials"
                    icon={faIcons.FaPlus}
                    command={() => viewModel.addApplications()} />
                <MenuItem
                    id="changeSecret"
                    label="Change Secret"
                    icon={faIcons.FaKey}
                    disableOnUnselected
                    command={() => viewModel.changeSecret()} />
                <MenuItem
                    id="remove"
                    label="Remove Client Credentials"
                    icon={faIcons.FaTrash}
                    disableOnUnselected
                    command={() => viewModel.removeApplications()} />
            </DataPage.MenuItems>
            <DataPage.Columns>
                <Column field='id' header="ID" sortable />
                <Column field='clientId' header="Client ID" sortable />
                <Column
                    field='isActive'
                    header="Active"
                    sortable
                    body={(client: Applications) => client.isActive ? 'Yes' : 'No'} />
                <Column
                    field='createdAt'
                    header="Created"
                    sortable
                    body={(client: Applications) => formatDate(client.createdAt)} />
                <Column
                    field='lastModifiedAt'
                    header="Last Modified"
                    sortable
                    body={(client: Applications) => formatDate(client.lastModifiedAt || '')} />
            </DataPage.Columns>
        </DataPage>
    );
});
