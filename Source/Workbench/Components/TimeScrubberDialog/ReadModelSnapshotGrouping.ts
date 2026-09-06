// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * How the events that shaped a read model are grouped into snapshots.
 *
 * The values name the kernel's own grouping, which the query takes as a string - an enum query
 * parameter is dropped by the proxy generator, so the contract across the wire is the name.
 */
export enum ReadModelSnapshotGrouping {
    /** One snapshot per correlation - the events that were applied as a single action. */
    Correlation = 'Correlation',

    /** One snapshot per event, so every snapshot moves the read model by exactly one thing that happened. */
    Event = 'Event'
}
