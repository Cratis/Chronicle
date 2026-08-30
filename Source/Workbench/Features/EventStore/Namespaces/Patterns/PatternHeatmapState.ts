// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { BehaviorPattern } from 'Api/Patterns/BehaviorPattern';

export const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

export const timeBuckets = ['EarlyMorning', 'Morning', 'Midday', 'Afternoon', 'Evening', 'Night'];

export const timeBucketLabels: Record<string, string> = {
    EarlyMorning: 'Early morning',
    Morning: 'Morning',
    Midday: 'Midday',
    Afternoon: 'Afternoon',
    Evening: 'Evening',
    Night: 'Night',
};

export interface Slot {
    day: string;
    timeBucket: string;
}

/**
 * The part of the day an hour falls in.
 *
 * Derived the same way the kernel derives it from an event's occurred timestamp. Both sides have to agree, or the
 * grid would highlight a slot the mining never used.
 */
export const timeBucketForHour = (hour: number): string => {
    if (hour >= 5 && hour < 8) return 'EarlyMorning';
    if (hour >= 8 && hour < 11) return 'Morning';
    if (hour >= 11 && hour < 14) return 'Midday';
    if (hour >= 14 && hour < 17) return 'Afternoon';
    if (hour >= 17 && hour < 22) return 'Evening';
    return 'Night';
};

/**
 * The slot a moment falls in.
 *
 * The days array starts on Monday because that is how a working week reads, while `getDay()` starts on Sunday.
 */
export const slotFor = (moment: Date): Slot => ({
    day: days[(moment.getDay() + 6) % 7],
    timeBucket: timeBucketForHour(moment.getHours()),
});

export const slotKey = (slot: Slot) => `${slot.day}|${slot.timeBucket}`;

export const isInSlot = (pattern: BehaviorPattern, slot: Slot) =>
    pattern.facets?.['Day'] === slot.day && pattern.facets?.['TimeBucket'] === slot.timeBucket;

/**
 * Whether one pattern outranks another as the one that speaks for its slot.
 *
 * Occurrences lead because that is what the grid shades by, and because confidence saturates: a scope that
 * reliably does something every Monday morning has a hundred percent confidence in every slot it acts in, so
 * ranking on confidence alone would pick arbitrarily among ties. Confidence breaks the tie.
 */
const outranks = (candidate: BehaviorPattern, current: BehaviorPattern) =>
    candidate.occurrences !== current.occurrences
        ? candidate.occurrences > current.occurrences
        : candidate.confidence > current.confidence;

/**
 * The pattern that speaks for each slot, keyed by slot.
 *
 * A pattern constraining no day or time belongs to no cell and is left out rather than being drawn somewhere
 * arbitrary - it is still in the pivot view, which is the one that shows everything.
 */
export const strongestBySlot = (patterns: BehaviorPattern[]): Map<string, BehaviorPattern> => {
    const strongest = new Map<string, BehaviorPattern>();

    for (const pattern of patterns) {
        const day = pattern.facets?.['Day'];
        const timeBucket = pattern.facets?.['TimeBucket'];
        if (!day || !timeBucket) {
            continue;
        }

        const key = slotKey({ day, timeBucket });
        const current = strongest.get(key);
        if (!current || outranks(pattern, current)) {
            strongest.set(key, pattern);
        }
    }

    return strongest;
};

/**
 * How busy the busiest slot is, which the shading is scaled against.
 *
 * Scaling against the scope rather than a fixed number is what makes the grid readable for everyone: a person
 * with a few hundred occurrences and one with a few thousand both get the full range of the scale, and the
 * question the grid answers - when is this scope most active - is about that scope, not about how it compares to
 * the busiest person in the store.
 */
export const busiestSlot = (strongest: Map<string, BehaviorPattern>) =>
    [...strongest.values()].reduce((most, pattern) => Math.max(most, pattern.occurrences), 0);

/**
 * How strongly a slot should be shaded, from 0 to 1.
 *
 * Scaled by square root rather than straight proportion. Activity is heavily skewed - a habit tends to be an order
 * of magnitude busier than the incidental activity around it - and against a linear scale that one slot takes the
 * whole top of the range while every other slot collapses into the darkest step, hiding the difference between a
 * slot somebody visits regularly and one they barely touch. The square root lifts the quiet end enough to read
 * while still leaving the busiest slot clearly on its own.
 *
 * @param pattern The pattern speaking for the slot, if any.
 * @param busiest The occurrence count of the busiest slot in the scope.
 * @returns The intensity, or undefined when the slot holds nothing.
 */
export const intensityOf = (pattern: BehaviorPattern | undefined, busiest: number) =>
    pattern === undefined || busiest <= 0 ? undefined : Math.sqrt(pattern.occurrences / busiest);

export const patternsInSlot = (patterns: BehaviorPattern[], slot: Slot): BehaviorPattern[] =>
    patterns
        .filter((pattern) => isInSlot(pattern, slot))
        .slice()
        .sort((first, second) => second.confidence - first.confidence);
