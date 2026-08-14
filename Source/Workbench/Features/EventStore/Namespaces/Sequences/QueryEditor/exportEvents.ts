// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ExportEvents } from 'Api/EventSequences/ExportEvents';
import { ExportedEvent } from 'Api/EventSequences/ExportedEvent';
import { QueryEventsParameters } from 'Api/Events/QueryEvents';

/**
 * Build the file name an export downloads as.
 * @param eventStore The event store the events came from.
 * @param namespace The namespace the events came from.
 * @param today The day the export was taken.
 * @returns The file name.
 */
export const exportFileName = (eventStore: string, namespace: string, today: Date): string =>
    `events-${sanitize(eventStore)}-${sanitize(namespace)}-${today.toISOString().slice(0, 10)}.json`;

/**
 * Render exported events as the JSON the file holds.
 *
 * Content arrives as the JSON string it is stored as; it is parsed so the file nests naturally
 * rather than embedding an escaped string, and left alone when it turns out not to be JSON at all.
 * @param events The events the server exported.
 * @returns The file contents.
 */
export const toExportedJson = (events: ExportedEvent[]): string =>
    JSON.stringify(events.map(event => ({ ...event, content: parseContent(event.content) })), null, 2);

/**
 * Export everything a query matches, as a file the browser saves.
 *
 * The whole matching set is assembled by the server rather than paged through here - the browser
 * only ever holds one page of results, so it has nothing to export from on its own.
 * @param queryArguments The arguments the query is currently running with.
 * @param eventStore The event store the events come from.
 * @param namespace The namespace the events come from.
 * @returns Awaitable promise.
 */
export const exportQueryToFile = async (
    queryArguments: QueryEventsParameters,
    eventStore: string,
    namespace: string): Promise<void> => {
    const result = await new ExportEvents().perform(queryArguments);
    if (!result.hasData || result.data.length === 0) return;

    const blob = new Blob([toExportedJson(result.data)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = exportFileName(eventStore, namespace, new Date());
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

const sanitize = (value: string) => value.replace(/[^a-zA-Z0-9_-]/g, '-');

const parseContent = (content: string): unknown => {
    try {
        return JSON.parse(content);
    } catch {
        return content;
    }
};
