// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AppendedEvent } from 'Api/Events';

/**
 * The shape an exported event takes in the downloaded file.
 */
export interface ExportedEvent {
    /** The identifier of the event's type. */
    eventType: string;
    /** The event source the event belongs to. */
    eventSourceId: string;
    /** The event's position in the sequence. */
    sequenceNumber: string;
    /** When the event occurred. */
    occurred: Date;
    /** The event's content, parsed when it is valid JSON and left as-is when it is not. */
    content: unknown;
}

/**
 * Convert events into the shape they are exported in.
 *
 * Content is stored as a JSON string; it is parsed so the export nests naturally rather than
 * embedding an escaped string, and left alone when it turns out not to be JSON at all.
 * @param events The events to convert.
 * @returns The exportable events.
 */
export const toExportedEvents = (events: AppendedEvent[]): ExportedEvent[] =>
    events.map(event => ({
        eventType: event.context.eventType.id,
        eventSourceId: event.context.eventSourceId,
        sequenceNumber: event.context.sequenceNumber.toString(),
        occurred: event.context.occurred,
        content: parseContent(event.content)
    }));

/**
 * Build the file name an export downloads as.
 * @param eventStore The event store the events came from.
 * @param namespace The namespace the events came from.
 * @param today The day the export was taken.
 * @returns The file name.
 */
export const exportFileName = (eventStore: string, namespace: string, today: Date): string =>
    `events-${sanitize(eventStore)}-${sanitize(namespace)}-${today.toISOString().slice(0, 10)}.json`;

const sanitize = (value: string) => value.replace(/[^a-zA-Z0-9_-]/g, '-');

const parseContent = (content: string): unknown => {
    try {
        return JSON.parse(content);
    } catch {
        return content;
    }
};
