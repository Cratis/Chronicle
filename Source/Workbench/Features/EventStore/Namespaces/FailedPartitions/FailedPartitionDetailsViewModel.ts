// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { runInAction } from 'mobx';
import { AllObservers, FailedPartitionDetails as FailedPartition } from 'Features/Observation';
import { sequenceEventPath } from '../Sequences/sequenceEventPath';

export interface FailedPartitionDetailsProps {
    eventStore: string;
    namespace: string;
    item: FailedPartition;
}

@injectable()
export class FailedPartitionDetailsViewModel {
    private _props?: FailedPartitionDetailsProps;
    private _key = '';
    private _generation = 0;
    private _eventSequenceId?: string;
    state: 'loading' | 'ready' | 'missing' | 'denied' | 'failed' = 'loading';

    constructor(@inject(AllObservers) private readonly _observers: AllObservers) {}

    async handleProps(props: FailedPartitionDetailsProps) {
        const key = JSON.stringify([props.eventStore, props.namespace, props.item.id, props.item.observerId]);
        this._props = { ...props };
        if (key === this._key) return;
        this._key = key;
        await this.load();
    }

    get eventSequenceId(): string | undefined {
        return this._eventSequenceId;
    }

    eventPath(sequenceNumber: number): string | undefined {
        const props = this._props;
        if (!props || !this._eventSequenceId) return undefined;
        return sequenceEventPath(props.eventStore, props.namespace, this._eventSequenceId, sequenceNumber, props.item.id.toString());
    }

    async retry() {
        await this.load();
    }

    detached() {
        this._generation++;
    }

    private async load() {
        const generation = ++this._generation;
        const props = this._props;
        this._eventSequenceId = undefined;
        this.state = 'loading';
        if (!props?.eventStore || !props.namespace || !props.item.observerId) {
            this.state = 'missing';
            return;
        }
        const observerId = props.item.observerId;
        try {
            const result = await this._observers.perform({ eventStore: props.eventStore, namespace: props.namespace });
            runInAction(() => {
                if (generation !== this._generation) return;
                if (!result.isSuccess) {
                    this.state = result.isAuthorized === false ? 'denied' : 'failed';
                    return;
                }
                this._eventSequenceId = result.data.find(observer => observer.id === observerId)?.eventSequenceId;
                this.state = this._eventSequenceId ? 'ready' : 'missing';
            });
        } catch {
            runInAction(() => {
                if (generation === this._generation) this.state = 'failed';
            });
        }
    }
}
