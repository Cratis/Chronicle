// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { AllUsers, User } from 'Api/Security';
import { DataPage, MenuItem } from 'Components';
import * as faIcons from 'react-icons/fa6';
import { AddUserDialog } from './Add/AddUserDialog';
import { ChangePasswordDialog, ChangePasswordDialogProps } from './ChangePassword';
import { RemoveUserDialog, RemoveUserDialogProps } from './Remove';
import { useDialog } from '@cratis/arc.react/dialogs';
import { useState } from 'react';

const formatDate = (date: Date) => {
    if (!date) return '';
    return new Date(date).toLocaleString();
};

export const Users = () => {
    const [selectedUser, setSelectedUser] = useState<User | null>(null);
    const [AddUserWrapper, showAddUser] = useDialog(AddUserDialog);
    const [ChangePasswordWrapper, showChangePassword] = useDialog<ChangePasswordDialogProps>(ChangePasswordDialog);
    const [RemoveUserWrapper, showRemoveUser] = useDialog<RemoveUserDialogProps>(RemoveUserDialog);

    const handleChangePassword = () => {
        if (selectedUser) {
            showChangePassword({ userId: selectedUser.id });
        }
    };

    const handleRemoveUser = () => {
        if (selectedUser) {
            showRemoveUser({ userId: selectedUser.id, username: selectedUser.username });
        }
    };

    return (
        <>
            <DataPage
                title="Users"
                query={AllUsers}
                emptyMessage="No users found"
                dataKey='id'
                selection={selectedUser}
                onSelectionChange={(e) => setSelectedUser(e.value as User)}>
                <DataPage.MenuItems>
                    <MenuItem
                        id="add"
                        label="Add User"
                        icon={faIcons.FaPlus}
                        command={() => showAddUser()} />
                    <MenuItem
                        id="changePassword"
                        label="Change Password"
                        icon={faIcons.FaKey}
                        disableOnUnselected
                        command={handleChangePassword} />
                    <MenuItem
                        id="remove"
                        label="Remove User"
                        icon={faIcons.FaTrash}
                        disableOnUnselected
                        command={handleRemoveUser} />
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
                        body={(user: User) => formatDate(user.lastModifiedAt || new Date())} />
                </DataPage.Columns>
            </DataPage>
            <AddUserWrapper />
            <ChangePasswordWrapper />
            <RemoveUserWrapper />
        </>
    );
};
