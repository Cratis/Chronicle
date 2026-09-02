// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorPatternDetails } from 'Features/Patterns/BehaviorPatternDetails';

/**
 * The order facets read in when a pattern is summarized.
 *
 * What happened comes before who did it, which comes before what it was done to, which comes before when - the
 * order somebody would say it out loud. Anything not listed follows in whatever order it arrives, so a facet
 * added to the miner still shows up rather than silently disappearing from every card.
 */
const readingOrder = ['CommandType', 'InitiatorType', 'AggregateType', 'CausedByCommand', 'Day', 'TimeBucket'];

const positionOf = (name: string) => {
    const index = readingOrder.indexOf(name);
    return index === -1 ? readingOrder.length : index;
};

/**
 * A one-line description of what a pattern constrains.
 *
 * A pattern is only meaningfully identified by its whole facet set. Naming a card after any single facet makes
 * every pattern that leaves that facet unconstrained look identical - a wall of cards all reading "Any" - which
 * is exactly the case for the broader patterns a scope establishes.
 *
 * @param pattern The {@link BehaviorPatternDetails} to describe.
 * @returns The description.
 */
export const summaryOf = (pattern: BehaviorPatternDetails): string => {
    const facets = Object.entries(pattern.facets ?? {})
        .filter(([, value]) => !!value)
        .sort(([first], [second]) => positionOf(first) - positionOf(second) || first.localeCompare(second));

    return facets.length === 0 ? 'Any context' : facets.map(([, value]) => value).join(' · ');
};
