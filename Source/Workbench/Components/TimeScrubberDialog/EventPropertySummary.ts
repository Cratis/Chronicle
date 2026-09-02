// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { EventProperty } from './EventProperty';

/**
 * What a hover shows for one event's content.
 */
export interface EventPropertySummary {
    /** The properties to render. */
    properties: EventProperty[];
    /** How many properties were left out, zero when all of them fit. */
    remaining: number;
}
