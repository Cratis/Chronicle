// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { FailedPartitionDetails as FailedPartition, GetFailedPartitions } from 'Features/Observation';
import { runInAction } from 'mobx';
import { Guid } from '@cratis/fundamentals';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { RetryPartition as TryRecoverFailedPartition } from 'Features/Observation';

@injectable()
export class FailedPartitionsViewModel {
    private _origin?: string;
    private _restoreKey = '';
    private _generation = 0;
    restorationState: 'idle' | 'loading' | 'ready' | 'missing' | 'denied' | 'failed' = 'idle';

    constructor(
        @inject('params') private _params: EventStoreAndNamespaceParams,
        @inject(GetFailedPartitions) private readonly _failures: GetFailedPartitions
    ) {}

    selectedFailedPartition: FailedPartition | undefined;

    handleParams(params: EventStoreAndNamespaceParams) {
        const changed = params.eventStore !== this._params.eventStore || params.namespace !== this._params.namespace;
        this._params = { ...params };
        if (changed) {
            this.selectFailedPartition(undefined);
            return this.restoreFromOrigin();
        }
    }

    handleQueryParams(query: { failedPartitionId?: string }) {
        this._origin = typeof query.failedPartitionId === 'string' && query.failedPartitionId.length > 0 ? query.failedPartitionId : undefined;
        return this.restoreFromOrigin();
    }

    selectFailedPartition(item: FailedPartition | null | undefined) {
        this._generation++;
        this.restorationState = 'idle';
        this.selectedFailedPartition = item ?? undefined;
    }

    async retryRestoration() {
        if (this.restorationState !== 'failed' && this.restorationState !== 'denied') return;
        this._restoreKey = '';
        await this.restoreFromOrigin();
    }

    detached() {
        this._generation++;
    }

    private async restoreFromOrigin() {
        const { eventStore, namespace } = this._params;
        const origin = this._origin;
        const key = JSON.stringify([eventStore, namespace, origin]);
        if (key === this._restoreKey) return;
        this._restoreKey = key;
        const generation = ++this._generation;
        this.selectedFailedPartition = undefined;
        this.restorationState = 'idle';
        if (!origin || !eventStore || !namespace) return;
        this.restorationState = 'loading';
        try {
            const result = await this._failures.perform({ eventStore, namespace });
            runInAction(() => {
                if (generation !== this._generation) return;
                if (!result.isSuccess) {
                    this.restorationState = result.isAuthorized === false ? 'denied' : 'failed';
                    return;
                }
                const originId = Guid.isGuid(origin) ? Guid.parse(origin) : undefined;
                this.selectedFailedPartition = originId ? result.data.find(failure => failure.id.toString() === originId.toString()) : undefined;
                this.restorationState = this.selectedFailedPartition ? 'ready' : 'missing';
            });
        } catch {
            runInAction(() => {
                if (generation === this._generation) this.restorationState = 'failed';
            });
        }
    }

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
