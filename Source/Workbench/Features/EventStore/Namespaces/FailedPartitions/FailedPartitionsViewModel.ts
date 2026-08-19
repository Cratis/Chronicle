// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { FailedPartitionDetails as FailedPartition } from 'Features/Observation';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { RetryPartition as TryRecoverFailedPartition } from 'Features/Observation';

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
            command.eventSequenceId = '';
            command.partition = this.selectedFailedPartition.partition;
            await command.execute();
        }
    }
}
