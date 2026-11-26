// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Where the compared event lives; part of the event's identity for comparison. */
export interface ComparisonScope {
    eventStore: string;
    namespace: string;
    eventSequenceId: string;
}
