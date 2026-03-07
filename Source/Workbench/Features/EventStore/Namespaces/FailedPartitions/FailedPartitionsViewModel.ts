// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { FailedPartition } from 'Api/Observation';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { TryRecoverFailedPartition } from 'Api/Observation';

@injectable()
export class FailedPartitionsViewModel {
    constructor(@inject('params') private readonly _params: EventStoreAndNamespaceParams) {

    }

    selectedFailedPartition: FailedPartition | undefined;

    async retry() {
        if (this.selectedFailedPartition) {
            const command = new TryRecoverFailedPartition();
            command.eventStore = this._params.eventStore!;
            command.namespace = this._params.namespace!;
            command.observerId = this.selectedFailedPartition.observerId;
            command.partition = this.selectedFailedPartition.partition;
            await command.execute();
        }
    }
}
