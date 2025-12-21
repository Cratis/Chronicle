// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/arc.react.mvvm';
import { UsersViewModel } from './UsersViewModel';
import { Column } from 'primereact/column';
import { AllUsers, User } from 'Api/Security';
import { DataPage, MenuItem } from 'Components';
import * as faIcons from 'react-icons/fa6';

const formatDate = (date: string) => {
    if (!date) return '';
    return new Date(date).toLocaleString();
};

export const Users = withViewModel(UsersViewModel, ({ viewModel }) => {
    return (
        <DataPage
            title="Users"
            query={AllUsers}
            emptyMessage="No users found"
            dataKey='id'
            onSelectionChange={(e) => (viewModel.selectedUser = e.value as User)}>
            <DataPage.MenuItems>
                <MenuItem
                    id="add"
                    label="Add User"
                    icon={faIcons.FaPlus}
                    command={() => viewModel.addUser()} />
                <MenuItem
                    id="changePassword"
                    label="Change Password"
                    icon={faIcons.FaKey}
                    disableOnUnselected
                    command={() => viewModel.changePassword()} />
                <MenuItem
                    id="remove"
                    label="Remove User"
                    icon={faIcons.FaTrash}
                    disableOnUnselected
                    command={() => viewModel.removeUser()} />
            </DataPage.MenuItems>
            <DataPage.Columns>
                <Column field='id' header="ID" sortable />
                <Column field='username' header="Username" sortable />
                <Column field='email' header="Email" sortable />
                <Column
                    field='isActive'
                    header="Active"
                    sortable
                    body={(user: User) => user.isActive ? 'Yes' : 'No'} />
                <Column
                    field='createdAt'
                    header="Created"
                    sortable
                    body={(user: User) => formatDate(user.createdAt)} />
                <Column
                    field='lastModifiedAt'
                    header="Last Modified"
                    sortable
                    body={(user: User) => formatDate(user.lastModifiedAt || '')} />
            </DataPage.Columns>
        </DataPage>
    );
});
