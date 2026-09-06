// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Event } from 'Features/ReadModelExplorer';
import type { Json } from '@cratis/components/types';

/**
 * One step of the scrubber - an event, and the read model as it stood once that event's snapshot
 * was applied.
 */
export interface ScrubStep {
    /** The event this step sits on. */
    event: Event;
    /** The read model as it stood after the snapshot this event belongs to. */
    instance: Json;
    /** When the snapshot this event belongs to was taken. */
    occurred: Date;
}
