// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IDialogs } from '@cratis/arc.react.mvvm/dialogs';
import { inject, injectable } from 'tsyringe';
import { AddNamespaceRequest, AddNamespaceResponse } from './AddNamespace';
import { EnsureNamespace } from 'Api/Namespaces';
import { type EventStoreAndNamespaceParams } from 'Shared';

@injectable()
export class NamespacesViewModel {

    constructor(
        @inject('params') private readonly _params: EventStoreAndNamespaceParams,
        private readonly _dialogs: IDialogs) {
    }

    async addNamespace() {
        const [, response] = await this._dialogs.show<AddNamespaceRequest, AddNamespaceResponse>(new AddNamespaceRequest());
        if (response!.create === false) return;

        const command = new EnsureNamespace();
        command.eventStore = this._params.eventStore!;
        command.namespace = response!.name;
        await command.execute();
    }
}
