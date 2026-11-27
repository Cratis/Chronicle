// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverLag } from 'Features/Observation';
import { ObserverLagController, type ObserverLagControllerOptions, type ObserverLagTarget } from '../../ObserverLagController';

export interface PendingRead {
    target: ObserverLagTarget;
    signal: AbortSignal;
    resolve: (lag: ObserverLag | undefined) => void;
    reject: (error: unknown) => void;
}

export const target = (observerId: string, namespace = 'ns', eventSequenceId = 'event-log'): ObserverLagTarget =>
    ({ eventStore: 'store', namespace, eventSequenceId, observerId });

export const measurement = (observerId: string) => Object.assign(new ObserverLag(), { observerId });

/** A controller whose reads stay pending until the spec settles them, so concurrency and ordering can be observed. */
export class a_controller {
    readonly reads: PendingRead[] = [];
    changes = 0;
    controller: ObserverLagController;

    constructor(options: Partial<ObserverLagControllerOptions> = {}) {
        this.controller = new ObserverLagController({
            reader: (readTarget, signal) => new Promise((resolve, reject) => this.reads.push({ target: readTarget, signal, resolve, reject })),
            onChange: () => this.changes++,
            ...options
        });
    }

    get requestedIds() {
        return this.reads.map(read => read.target.observerId);
    }

    show(...targets: ObserverLagTarget[]) {
        targets.forEach(shown => this.controller.setVisible(shown, true));
    }

    async settle() {
        await Promise.resolve();
        await Promise.resolve();
        await Promise.resolve();
    }

    async complete(index: number, lag: ObserverLag | undefined = measurement(this.reads[index].target.observerId)) {
        this.reads[index].resolve(lag);
        await this.settle();
    }
}
