// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { ClientCredentials, Add, Remove, ChangeSecret } from 'Api/Security';
import { IDialogs } from '@cratis/arc.react.mvvm/dialogs';
import { DialogButtons, DialogResult } from '@cratis/arc.react/dialogs';

@injectable()
export class ClientCredentialsViewModel {
    constructor(
        private readonly _add: Add,
        private readonly _remove: Remove,
        private readonly _changeSecret: ChangeSecret,
        private readonly _dialogs: IDialogs) {
    }

    selectedClient: ClientCredentials | undefined;

    async addClientCredentials() {
        const id = crypto.randomUUID();
        
        const clientId = await this._dialogs.showTextInput('Add Client Credentials', 'Enter client ID:');
        if (!clientId) return;

        const clientSecret = await this._dialogs.showTextInput('Add Client Credentials', 'Enter client secret:');
        if (!clientSecret) return;

        this._add.id = id;
        this._add.clientId = clientId;
        this._add.clientSecret = clientSecret;

        const result = await this._add.execute();
        result.onException((error) => {
            this._dialogs.showConfirmation('Add Client Credentials', `Failed to add client credentials: ${error}`, DialogButtons.Ok);
        });
    }

    async removeClientCredentials() {
        if (this.selectedClient) {
            const result = await this._dialogs.showConfirmation(
                'Remove Client Credentials',
                `Are you sure you want to remove client credentials ${this.selectedClient.clientId}?`,
                DialogButtons.YesNo);
                
            if (result === DialogResult.Yes) {
                this._remove.id = this.selectedClient.id;
                const commandResult = await this._remove.execute();
                commandResult.onException((error) => {
                    this._dialogs.showConfirmation('Remove Client Credentials', `Failed to remove client credentials: ${error}`, DialogButtons.Ok);
                });
            }
        }
    }

    async changeSecret() {
        if (this.selectedClient) {
            const clientSecret = await this._dialogs.showTextInput('Change Secret', 'Enter new client secret:');
            if (!clientSecret) return;

            this._changeSecret.id = this.selectedClient.id;
            this._changeSecret.clientSecret = clientSecret;

            const result = await this._changeSecret.execute();
            result.onException((error) => {
                this._dialogs.showConfirmation('Change Secret', `Failed to change client secret: ${error}`, DialogButtons.Ok);
            });
        }
    }
}
