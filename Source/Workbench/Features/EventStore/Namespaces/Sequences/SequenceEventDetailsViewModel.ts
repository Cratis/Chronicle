// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { inject, injectable } from 'tsyringe';
import { runInAction } from 'mobx';
import { Paging } from '@cratis/arc/queries';
import { AppendedEvent, AppendedEvents } from 'Features/Sequences';
import { failedPartitionsPath, isInspectableSequenceNumber } from './sequenceEventPath';

export interface SequenceEventDetailsProps {
    eventStore: string;
    namespace: string;
    eventSequenceId?: string;
    sequenceNumber?: string;
    failedPartitionId?: string;
}

@injectable()
export class SequenceEventDetailsViewModel {
    private _props?: SequenceEventDetailsProps;
    private _key = '';
    private _generation = 0;
    event: AppendedEvent | undefined;
    state: 'unavailable' | 'loading' | 'ready' | 'missing' | 'denied' | 'failed' = 'unavailable';

    constructor(@inject(AppendedEvents) private readonly _query: AppendedEvents) {}

    get returnPath(): string {
        const { eventStore = '', namespace = '', failedPartitionId } = this._props ?? {};
        return failedPartitionId
            ? failedPartitionsPath(eventStore, namespace, failedPartitionId)
            : `/event-store/${encodeURIComponent(eventStore)}/${encodeURIComponent(namespace)}/sequences`;
    }

    async handleProps(props: SequenceEventDetailsProps) {
        const key = JSON.stringify([props.eventStore, props.namespace, props.eventSequenceId, props.sequenceNumber]);
        this._props = { ...props };
        if (key === this._key) return;
        this._key = key;
        await this.load();
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
        this.event = undefined;
        this.state = 'unavailable';
        if (!props?.eventStore || !props.namespace || !props.eventSequenceId || !props.sequenceNumber || !/^\d+$/.test(props.sequenceNumber)) return;
        const number = Number(props.sequenceNumber);
        if (!isInspectableSequenceNumber(number)) return;
        this.state = 'loading';
        this._query.paging = new Paging(number, 1);
        try {
            const result = await this._query.perform({ eventStore: props.eventStore, namespace: props.namespace, eventSequenceId: props.eventSequenceId });
            runInAction(() => {
                if (generation !== this._generation) return;
                if (!result.isSuccess) {
                    this.state = result.isAuthorized === false ? 'denied' : 'failed';
                    return;
                }
                this.event = result.data.find(event => event.context.sequenceNumber === number);
                this.state = this.event ? 'ready' : 'missing';
            });
        } catch {
            runInAction(() => {
                if (generation === this._generation) this.state = 'failed';
            });
        }
    }
}
