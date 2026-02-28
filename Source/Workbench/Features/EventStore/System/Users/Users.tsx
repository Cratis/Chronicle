// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import { AllUsers, User, RemoveUser, RequirePasswordChange } from 'Api/Security';
import { DataPage, MenuItem } from 'Components';
import * as faIcons from 'react-icons/fa6';
import { AddUserDialog } from './Add/AddUserDialog';
import { ChangePasswordDialog } from './ChangePassword';
import { useConfirmationDialog, DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';
import { useState } from 'react';
import strings from 'Strings';
import { Guid } from '@cratis/fundamentals';

const formatDate = (date: Date) => {
    if (!date) return '';
    return new Date(date).toLocaleString();
};

export const Users = () => {
    const [selectedUser, setSelectedUser] = useState<User | null>(null);
    const [showAddUser, setShowAddUser] = useState(false);
    const [showChangePassword, setShowChangePassword] = useState(false);
    const [showConfirmation] = useConfirmationDialog();
    const [removeUser] = RemoveUser.use();
    const [requirePasswordChange] = RequirePasswordChange.use();

    const handleChangePassword = () => {
        if (selectedUser) {
            setShowChangePassword(true);
        }
    };

    const handleRequirePasswordChange = async () => {
        if (selectedUser) {
            const result = await showConfirmation(
                'Require Password Change',
                `Are you sure you want to require ${selectedUser.username} to change their password on next login?`,
                DialogButtons.YesNo
            );

            if (result === DialogResult.Yes) {
                requirePasswordChange.userId = selectedUser.id;
                await requirePasswordChange.execute();
            }
        }
    };

    const handleRemoveUser = async () => {
        if (selectedUser) {
            const result = await showConfirmation(
                strings.eventStore.system.users.dialogs.removeUser.title,
                strings.eventStore.system.users.dialogs.removeUser.message.replace('{username}', selectedUser.username),
                DialogButtons.YesNo
            );

            if (result === DialogResult.Yes) {
                removeUser.userId = selectedUser.id;
                await removeUser.execute();
            }
        }
    };

    return (
        <>
            <DataPage
                title={strings.eventStore.system.users.title}
                query={AllUsers}
                emptyMessage={strings.eventStore.system.users.empty}
                dataKey='id'
                selection={selectedUser}
                onSelectionChange={(e) => setSelectedUser(e.value as User)}>
                <DataPage.MenuItems>
                    <MenuItem
                        id="add"
                        label={strings.eventStore.system.users.actions.add}
                        icon={faIcons.FaPlus}
                        command={() => setShowAddUser(true)} />
                    <MenuItem
                        id="changePassword"
                        label={strings.eventStore.system.users.actions.changePassword}
                        icon={faIcons.FaKey}
                        disableOnUnselected
                        command={handleChangePassword} />
                    <MenuItem
                        id="requirePasswordChange"
                        label="Require Password Change"
                        icon={faIcons.FaTriangleExclamation}
                        disableOnUnselected
                        command={handleRequirePasswordChange} />
                    <MenuItem
                        id="remove"
                        label={strings.eventStore.system.users.actions.remove}
                        icon={faIcons.FaTrash}
                        disableOnUnselected
                        command={handleRemoveUser} />
                </DataPage.MenuItems>
                <DataPage.Columns>
                    <Column field='username' header={strings.eventStore.system.users.columns.username} sortable />
                    <Column field='email' header={strings.eventStore.system.users.columns.email} sortable />
                    <Column
                        field='isActive'
                        header={strings.eventStore.system.users.columns.active}
                        sortable
                        body={(user: User) => user.isActive ? strings.eventStore.system.users.columns.yes : strings.eventStore.system.users.columns.no} />
                    <Column
                        field='createdAt'
                        header={strings.eventStore.system.users.columns.created}
                        sortable
                        body={(user: User) => formatDate(user.createdAt)} />
                    <Column
                        field='lastModifiedAt'
                        header={strings.eventStore.system.users.columns.lastModified}
                        sortable
                        body={(user: User) => formatDate(user.lastModifiedAt || new Date())} />
                </DataPage.Columns>
            </DataPage>
            <AddUserDialog visible={showAddUser} onClose={() => setShowAddUser(false)} />
            <ChangePasswordDialog
                visible={showChangePassword}
                userId={selectedUser?.id ?? Guid.empty}
                onClose={() => setShowChangePassword(false)} />
        </>
    );
};
