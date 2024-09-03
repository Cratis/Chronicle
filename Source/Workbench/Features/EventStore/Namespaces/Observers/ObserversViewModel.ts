// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { ObserverInformation } from 'Api/Concepts/Observation/ObserverInformation';
import { Namespace } from 'Api/Namespaces';
import { Replay } from 'Api/Observation';
import { INamespaces } from 'State/Namespaces';
import { DialogButtons, IDialogs } from '@cratis/applications.react.mvvm/dialogs';
import { DialogResult } from '@cratis/applications.react/dialogs';
import { type EventStoreAndNamespaceParams } from 'Shared';

@injectable()
export class ObserversViewModel {

    constructor(
        namespaces: INamespaces,
        private readonly _replay: Replay,
        private readonly _dialogs: IDialogs,
        @inject('params') private readonly _params: EventStoreAndNamespaceParams) {
        this.currentNamespace = { name: '', description: '' };

        namespaces.currentNamespace.subscribe(namespace => {
            this.currentNamespace = namespace;
        });
    }

    currentNamespace: Namespace;
    selectedObserver: ObserverInformation | undefined;

    async replay() {
        if (this.selectedObserver) {
            const observerId = this.selectedObserver.observerId;
            const result = await this._dialogs.showConfirmation('Replay?', `Are you sure you want to replay ${observerId}?`, DialogButtons.YesNo);
            if (result == DialogResult.Yes) {
                this._replay.eventStore = this._params.eventStore!;
                this._replay.namespace = this.currentNamespace.name;
                const commandResult = await this._replay.execute();
                commandResult
                    .onSuccess(() => {
                        this._dialogs.showConfirmation('Replay', `Replay ${observerId} has started`, DialogButtons.Ok);
                    })
                    .onException((error) => {
                        this._dialogs.showConfirmation('Replay', `Replay ${observerId} failed: ${error}`, DialogButtons.Ok);
                    });
            }
        }
    }
}
