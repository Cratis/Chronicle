// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useRef, useState } from 'react';
import strings from 'Strings';
import { EventTooltip } from './EventTooltip';
import { sampleBubbles } from './sampleBubbles';
import type { ScrubStep } from './ScrubStep';

/**
 * Props for {@link EventBubbles}.
 */
export interface EventBubblesProps {
    /** The steps being scrubbed, oldest first. */
    steps: ScrubStep[];
    /** The index currently shown. */
    current: number;
    /** Called with the index to move to. */
    onScrub: (index: number) => void;
}

/**
 * The scrubber - one bubble per event, along the track that moves through them.
 *
 * The range input covers the whole track and sits on top, so it takes every pointer interaction:
 * pressing anywhere - the handle included - starts a native drag that reports continuously, and
 * arrow keys, Home and End come for free. That leaves the bubbles unable to answer to hover
 * themselves, so hovering is resolved from the pointer's position to the nearest drawn bubble
 * instead, which also means the thin gap between two bubbles still describes one of them.
 * @param props The {@link EventBubblesProps}.
 * @returns The rendered scrubber.
 */
export const EventBubbles = ({ steps, current, onScrub }: EventBubblesProps) => {
    const [hovered, setHovered] = useState<number | null>(null);
    const bubbleElements = useRef(new Map<number, HTMLSpanElement>());

    const last = steps.length - 1;
    const positionOf = (index: number) => (last <= 0 ? 50 : (index / last) * 100);

    const drawn = useMemo(() => sampleBubbles(steps.length, current), [steps.length, current]);

    const registerBubble = (index: number) => (element: HTMLSpanElement | null) => {
        if (element) bubbleElements.current.set(index, element);
        else bubbleElements.current.delete(index);
    };

    const nearestBubbleTo = (clientX: number, track: HTMLElement) => {
        const bounds = track.getBoundingClientRect();
        if (bounds.width === 0) return null;

        const fraction = Math.min(1, Math.max(0, (clientX - bounds.left) / bounds.width));
        const target = fraction * last;

        return drawn.reduce(
            (nearest, index) => (Math.abs(index - target) < Math.abs(nearest - target) ? index : nearest),
            drawn[0] ?? 0);
    };

    // The hovered bubble may have been sampled away by a scrub since it was picked, which would
    // leave the tooltip pointing at an element no longer in the page.
    const anchor = hovered === null ? null : bubbleElements.current.get(hovered) ?? null;
    const hoveredStep = hovered === null ? null : steps[hovered];

    return (
        <div className='time-scrubber__scrubber'>
            <div className='time-scrubber__caption'>
                {steps[current] && (
                    <>
                        <span className='time-scrubber__caption-event'>{steps[current].event.type}</span>
                        <span className='time-scrubber__caption-occurred'>
                            {new Date(steps[current].event.occurred).toLocaleString()}
                        </span>
                        <span className='time-scrubber__caption-position'>
                            {strings.components.timeScrubber.position
                                .replace('{0}', String(current + 1))
                                .replace('{1}', String(steps.length))}
                        </span>
                    </>
                )}
            </div>

            <div
                className='time-scrubber__track'
                onPointerMove={event => setHovered(nearestBubbleTo(event.clientX, event.currentTarget))}
                onPointerLeave={() => setHovered(null)}>
                <div className='time-scrubber__line' />

                {drawn.map(index => (
                    <span
                        key={index}
                        ref={registerBubble(index)}
                        className='time-scrubber__bubble'
                        style={{ left: `${positionOf(index)}%` }}
                        data-current={index === current}
                        data-passed={index <= current} />
                ))}

                <input
                    type='range'
                    className='time-scrubber__range'
                    min={0}
                    max={Math.max(0, last)}
                    step={1}
                    value={current}
                    aria-label={strings.components.timeScrubber.scrub}
                    aria-valuetext={steps[current]?.event.type}
                    onChange={event => onScrub(Number(event.target.value))} />
            </div>

            <EventTooltip
                event={hoveredStep?.event ?? null}
                anchor={anchor?.isConnected ? anchor : null}
                position={(hovered ?? 0) + 1}
                total={steps.length} />
        </div>
    );
};
