// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** One retained revision that can be placed on either side of a comparison. */
export interface RevisionChoice {
    /** Stable identity of the choice: `original`, or a scoped occurred/correlation/generation tuple. There is no durable revision id. */
    key: string;
    /** Display position only: 0 is the original, 1..n the retained revisions in response order. Not identity. */
    ordinal: number;
    /** The event type generation of the payload: 1 for the original, the entry's own generation otherwise. */
    generation: number;
    /** When the revision occurred, as an ISO 8601 string. */
    occurred: string;
    /** The correlation id of the revision. */
    correlationId: string;
    /** The raw payload from the response; undefined when the response carries none. */
    content?: string;
}
