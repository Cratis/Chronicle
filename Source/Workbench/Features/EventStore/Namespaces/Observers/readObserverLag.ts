// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GetObserverLag, type ObserverLag } from 'Features/Observation';
import { type ObserverLagTarget } from './ObserverLagController';

/**
 * Reads the sampled lag of one observer with its own query instance, because an instance aborts its own request in flight.
 * Resolves undefined for a failed, denied or aborted read: the query's default value is an empty response, and it must
 * not be taken for a measurement.
 */
export const readObserverLag = async (target: ObserverLagTarget, signal: AbortSignal): Promise<ObserverLag | undefined> => {
    const query = new GetObserverLag();
    const abort = () => query.abortController?.abort();
    signal.addEventListener('abort', abort, { once: true });

    try {
        const result = await query.perform({
            eventStore: target.eventStore,
            namespace: target.namespace,
            eventSequenceId: target.eventSequenceId,
            observerId: target.observerId
        });
        return result.isSuccess && !signal.aborted ? result.data : undefined;
    } catch {
        return undefined;
    } finally {
        signal.removeEventListener('abort', abort);
    }
};
