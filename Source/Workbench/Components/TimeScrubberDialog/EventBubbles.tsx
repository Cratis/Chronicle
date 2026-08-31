// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState } from 'react';
import type { ReadModelTimelineEntry } from 'Api/ReadModels';
import strings from 'Strings';
import { sampleBubbles } from './sampleBubbles';

/**
 * Props for {@link EventBubbles}.
 */
export interface EventBubblesProps {
    /** The timeline being scrubbed, oldest first. */
    entries: ReadModelTimelineEntry[];
    /** The index currently shown. */
    current: number;
    /** Called with the index to move to. */
    onScrub: (index: number) => void;
}

/**
 * The scrubber - one bubble per event, along the track that moves through them.
 *
 * The bubbles sit on the same track the range input covers, so the handle lands on the bubble it
 * belongs to. The range input is what actually takes the interaction: it already answers to
 * dragging, arrow keys, Home and End, which a row of divs would each have to reimplement badly.
 * @param props The {@link EventBubblesProps}.
 * @returns The rendered scrubber.
 */
export const EventBubbles = ({ entries, current, onScrub }: EventBubblesProps) => {
    const [hovered, setHovered] = useState<number | null>(null);

    const last = entries.length - 1;
    const positionOf = (index: number) => (last === 0 ? 50 : (index / last) * 100);
    const described = hovered ?? current;
    const describedEntry = entries[described];

    const drawn = useMemo(() => sampleBubbles(entries.length, current), [entries.length, current]);

    return (
        <div className='time-scrubber__scrubber'>
            <div className='time-scrubber__caption'>
                {describedEntry && (
                    <>
                        <span className='time-scrubber__caption-event'>{describedEntry.event.type}</span>
                        <span className='time-scrubber__caption-occurred'>
                            {new Date(describedEntry.event.occurred).toLocaleString()}
                        </span>
                        <span className='time-scrubber__caption-position'>
                            {strings.components.timeScrubber.position
                                .replace('{0}', String(described + 1))
                                .replace('{1}', String(entries.length))}
                        </span>
                    </>
                )}
            </div>

            <div className='time-scrubber__track'>
                <div className='time-scrubber__line' />

                {drawn.map(index => (
                    <button
                        key={index}
                        type='button'
                        className='time-scrubber__bubble'
                        style={{ left: `${positionOf(index)}%` }}
                        data-current={index === current}
                        data-passed={index <= current}
                        aria-label={`${entries[index].event.type} — ${new Date(entries[index].event.occurred).toLocaleString()}`}
                        onMouseEnter={() => setHovered(index)}
                        onMouseLeave={() => setHovered(null)}
                        onFocus={() => setHovered(index)}
                        onBlur={() => setHovered(null)}
                        onClick={() => onScrub(index)} />
                ))}

                <input
                    type='range'
                    className='time-scrubber__range'
                    min={0}
                    max={last}
                    step={1}
                    value={current}
                    aria-label={strings.components.timeScrubber.scrub}
                    onChange={event => onScrub(Number(event.target.value))} />
            </div>
        </div>
    );
};
