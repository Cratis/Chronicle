// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The most bubbles worth drawing on the scrubber's track.
 *
 * A busy instance has thousands of events, and a bubble each would draw them a fraction of a pixel
 * apart - a solid line that says nothing and cannot be pointed at.
 */
export const maximumBubbles = 160;

/**
 * Chooses which of a timeline's events get a bubble on the track.
 *
 * Everything up to {@link maximumBubbles} is drawn as it is. Past that the track shows an evenly
 * spread sample, always including the first, the last and wherever the scrubber currently sits - so
 * the handle never lands on a gap. Only what is drawn is thinned: the scrubber still moves through
 * every event.
 * @param total How many events the timeline holds.
 * @param current The index the scrubber currently sits on.
 * @returns The indices to draw, in order.
 */
export const sampleBubbles = (total: number, current: number): number[] => {
    if (total <= 0) return [];

    const last = total - 1;
    if (total <= maximumBubbles) {
        return Array.from({ length: total }, (_, index) => index);
    }

    const step = last / (maximumBubbles - 1);
    const sampled = new Set<number>();
    for (let bubble = 0; bubble < maximumBubbles; bubble++) {
        sampled.add(Math.round(bubble * step));
    }

    if (current >= 0 && current <= last) {
        sampled.add(current);
    }

    return [...sampled].sort((left, right) => left - right);
};
