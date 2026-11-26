// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { ContentComparison } from './ContentComparison';
import { RevisionOffer } from './RevisionOffer';

const text = () => strings.eventStore.namespaces.sequences.details.compare;

/**
 * The sentence that says why a comparison is absent or incomplete, or undefined when it is complete.
 * @param revisions What the event offers for comparison.
 * @param comparison The comparison, when one is shown.
 * @returns The status sentence.
 */
export const statusMessage = (revisions: RevisionOffer, comparison?: ContentComparison): string | undefined => {
    const compare = text();
    if (revisions.state !== 'ready') return compare.status[revisions.state];
    if (!comparison) return undefined;
    const side = comparison.side ? compare.sides[comparison.side] : '';
    switch (comparison.outcome) {
        case 'limited': return compare.limits[comparison.limit ?? 'nodes'].replace('{side}', side);
        case 'identical': return compare.identical;
        case 'different': return compare.different.replace('{count}', String(comparison.rows.length));
        default: return compare.status[comparison.outcome].replace('{side}', side);
    }
};
