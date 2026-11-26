// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RevisionChoice } from './RevisionChoice';

/** Why comparison is or is not offered for an event. */
export type OfferState = 'ready' | 'redacted' | 'erased' | 'noHistory' | 'limited';

/** The revisions an event offers for comparison. */
export interface RevisionOffer {
    state: OfferState;
    /** Empty unless the state is `ready`. */
    choices: RevisionChoice[];
    /** Retained revisions left out because they cannot be told apart or identified. */
    ambiguous: number;
}
