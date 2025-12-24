// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { User, AddUser, RemoveUser, ChangePasswordForUser } from 'Api/Security';
import { IDialogs } from '@cratis/arc.react.mvvm/dialogs';
import { DialogButtons, DialogResult } from '@cratis/arc.react/dialogs';

@injectable()
export class UsersViewModel {
    constructor(
        private readonly _add: AddUser,
        private readonly _remove: RemoveUser,
        private readonly _changePassword: ChangePasswordForUser,
        private readonly _dialogs: IDialogs) {
    }

    selectedUser: User | undefined;

    async addUser() {
        const userId = crypto.randomUUID();

        const username = await this._dialogs.showTextInput('Add User', 'Enter username:');
        if (!username) return;

        const email = await this._dialogs.showTextInput('Add User', 'Enter email (optional):');

        const password = await this._dialogs.showTextInput('Add User', 'Enter password:');
        if (!password) return;

        this._add.userId = userId;
        this._add.username = username;
        this._add.email = email || undefined;
        this._add.password = password;

        const result = await this._add.execute();
        result.onException((error) => {
            this._dialogs.showConfirmation('Add User', `Failed to add user: ${error}`, DialogButtons.Ok);
        });
    }

    async removeUser() {
        if (this.selectedUser) {
            const result = await this._dialogs.showConfirmation(
                'Remove User',
                `Are you sure you want to remove user ${this.selectedUser.username}?`,
                DialogButtons.YesNo);

            if (result === DialogResult.Yes) {
                this._remove.userId = this.selectedUser.id;
                const commandResult = await this._remove.execute();
                commandResult.onException((error) => {
                    this._dialogs.showConfirmation('Remove User', `Failed to remove user: ${error}`, DialogButtons.Ok);
                });
            }
        }
    }

    async changePassword() {
        if (this.selectedUser) {
            const password = await this._dialogs.showTextInput('Change Password', 'Enter new password:');
            if (!password) return;

            this._changePassword.userId = this.selectedUser.id;
            this._changePassword.password = password;

            const result = await this._changePassword.execute();
            result.onException((error) => {
                this._dialogs.showConfirmation('Change Password', `Failed to change password: ${error}`, DialogButtons.Ok);
            });
        }
    }
}
