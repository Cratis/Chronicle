// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Json } from '@cratis/components/types';
import type { ReadModelSnapshot } from 'Features/ReadModelExplorer';
import type { ScrubStep } from './ScrubStep';

/**
 * Flattens a read model instance's snapshots into the single line of events the scrubber moves through.
 *
 * A snapshot groups the events that were applied together and carries the read model as it stood once
 * the whole group had been applied. Scrubbing wants one step per event, so every event in a group
 * becomes its own step and they all carry that group's state - the content changes as the scrubber
 * crosses into the next snapshot, not on every step.
 * @param snapshots The instance's snapshots, oldest first.
 * @returns One step per event, in the order the snapshots and their events arrived.
 */
export const flattenSnapshots = (snapshots: ReadModelSnapshot[]): ScrubStep[] =>
    snapshots.flatMap(snapshot =>
        (snapshot.events ?? []).map(event => ({
            event,
            instance: snapshot.instance as Json,
            occurred: snapshot.occurred
        })));
