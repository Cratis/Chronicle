// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { Tooltip } from 'primereact/tooltip';
import type { Event } from 'Api/ReadModels';
import strings from 'Strings';
import { summarizeProperties } from './eventProperties';

/**
 * Props for {@link EventTooltip}.
 */
export interface EventTooltipProps {
    /** The event being described, or null when nothing is hovered. */
    event: Event | null;
    /** The bubble the tooltip points at. */
    anchor: HTMLElement | null;
    /** Where the event sits in the timeline, one-based. */
    position: number;
    /** How many events the timeline holds. */
    total: number;
}

/**
 * The hover that describes one event on the scrubber.
 *
 * There is one tooltip for the whole track rather than one per bubble - a busy instance draws well
 * over a hundred bubbles, and it is only ever pointing at one of them. It anchors to the hovered
 * bubble so the arrow comes out of that point.
 * @param props The {@link EventTooltipProps}.
 * @returns The rendered tooltip.
 */
export const EventTooltip = ({ event, anchor, position, total }: EventTooltipProps) => {
    const summary = useMemo(
        () => summarizeProperties(event?.content as Record<string, unknown> | undefined),
        [event]);

    const isOpen = !!event && !!anchor;

    return (
        <Tooltip.Root open={isOpen} anchor={anchor ?? undefined} openDelay={0} closeDelay={0}>
            <Tooltip.Portal>
                <Tooltip.Positioner side='top' align='center' sideOffset={10}>
                    <Tooltip.Popup>
                        <Tooltip.Arrow />
                        {event && (
                            <div className='time-scrubber__hover'>
                                <div className='time-scrubber__hover-title'>{event.type}</div>
                                <div className='time-scrubber__hover-meta'>
                                    {strings.components.timeScrubber.position
                                        .replace('{0}', String(position))
                                        .replace('{1}', String(total))}
                                    {' · '}
                                    {new Date(event.occurred).toLocaleString()}
                                </div>

                                {summary.properties.length > 0 && (
                                    <dl className='time-scrubber__hover-properties'>
                                        {summary.properties.map(property => (
                                            <div key={property.name} className='time-scrubber__hover-property'>
                                                <dt>{property.name}</dt>
                                                <dd>{property.value}</dd>
                                            </div>
                                        ))}
                                    </dl>
                                )}

                                {summary.remaining > 0 && (
                                    <div className='time-scrubber__hover-remaining'>
                                        {strings.components.timeScrubber.moreProperties
                                            .replace('{0}', String(summary.remaining))}
                                    </div>
                                )}
                            </div>
                        )}
                    </Tooltip.Popup>
                </Tooltip.Positioner>
            </Tooltip.Portal>
        </Tooltip.Root>
    );
};
