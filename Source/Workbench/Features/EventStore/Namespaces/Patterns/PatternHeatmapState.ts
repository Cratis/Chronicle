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
 * The strongest pattern in each slot, keyed by slot.
 *
 * A slot can hold several patterns, and the grid has one cell per slot to say how strongly it is established.
 * Confidence decides which one speaks for the slot; a pattern constraining no day or time belongs to no cell and
 * is left out rather than being drawn somewhere arbitrary.
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
        if (!current || pattern.confidence > current.confidence) {
            strongest.set(key, pattern);
        }
    }

    return strongest;
};

export const patternsInSlot = (patterns: BehaviorPattern[], slot: Slot): BehaviorPattern[] =>
    patterns
        .filter((pattern) => isInSlot(pattern, slot))
        .slice()
        .sort((first, second) => second.confidence - first.confidence);
