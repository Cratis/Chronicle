// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { AppendedEvent, EventRevision } from 'Features/Sequences';

export interface SelectOption {
    label: string;
    value: number;
}

const details = () => strings.eventStore.namespaces.sequences.details;

const toIso = (value: unknown): string =>
    value instanceof Date ? value.toISOString() : new Date(value as string).toISOString();

/**
 * Parses a raw payload when it is text, returning the raw value when it is not valid JSON.
 * @param raw The raw content.
 * @returns The parsed value, or the raw value.
 */
export const parseContent = (raw: unknown): unknown => {
    try {
        return typeof raw === 'string' ? JSON.parse(raw) : raw;
    } catch {
        return raw;
    }
};

/**
 * Builds the revision options: 0 is the original, 1..N are the revision entries.
 * @param revisions The retained revisions.
 * @returns The options, empty when there are no revisions.
 */
export const buildRevisionOptions = (revisions: EventRevision[]): SelectOption[] => {
    if (revisions.length === 0) return [];
    const options: SelectOption[] = [{ label: details().originalRevision, value: 0 }];
    revisions.forEach((_, index) => {
        const label = details().revisionLabel.replace('{n}', String(index + 1));
        options.push({ label: index === revisions.length - 1 ? `${label} (latest)` : label, value: index + 1 });
    });
    return options;
};

/**
 * Builds the generation options, marking the stored generation.
 * @param entries The generational content entries.
 * @param storedGeneration The generation the event is stored with.
 * @returns The options, sorted by generation.
 */
export const buildGenerationOptions = (entries: { key: number }[], storedGeneration: number): SelectOption[] =>
    entries
        .slice()
        .sort((a, b) => a.key - b.key)
        .map(({ key }) => {
            const baseLabel = details().generationLabel.replace('{n}', String(key));
            return { label: key === storedGeneration ? `${baseLabel} (stored)` : baseLabel, value: key };
        });

/**
 * Resolves the occurred, correlation and caused-by metadata of the shown revision.
 * @param item The event.
 * @param revision The shown revision, or undefined for the original.
 * @returns The metadata.
 */
export const revisionMetadata = (item: AppendedEvent, revision: EventRevision | undefined) =>
    revision
        ? { occurred: revision.occurred, correlationId: revision.correlationId, causedBy: revision.causedBy }
        : { occurred: item.context.occurred, correlationId: item.context.correlationId, causedBy: item.context.causedBy };

/**
 * Derives a basic schema from the content's own keys when no schema is registered.
 * @param content The content shown.
 * @returns A schema with a string property per key.
 */
export const deriveSchema = (content: unknown) => {
    if (content && typeof content === 'object' && !Array.isArray(content)) {
        return { properties: Object.fromEntries(Object.keys(content).map(key => [key, { type: 'string' }])) };
    }
    return { properties: {} };
};

type Metadata = ReturnType<typeof revisionMetadata>;

interface Described {
    description?: string;
}

/**
 * Builds the context object shown for the event; metadata reflects the shown revision.
 * @param item The event.
 * @param metadata The shown revision's metadata.
 * @param eventSource The matching event source, if known.
 * @param eventStream The matching event stream, if known.
 * @returns The context object.
 */
export const buildContextObject = (item: AppendedEvent, metadata: Metadata, eventSource?: Described, eventStream?: Described) => ({
    eventType: item.context.eventType.id,
    generation: item.context.eventType.generation,
    eventSourceType: item.context.eventSourceType,
    ...(eventSource?.description ? { eventSourceDescription: eventSource.description } : {}),
    ...(item.context.eventSource ? { eventSource: item.context.eventSource } : {}),
    eventSourceId: item.context.eventSourceId,
    sequenceNumber: item.context.sequenceNumber,
    eventStreamType: item.context.eventStreamType,
    ...(eventStream?.description ? { eventStreamDescription: eventStream.description } : {}),
    eventStreamId: item.context.eventStreamId,
    occurred: toIso(metadata.occurred),
    correlationId: metadata.correlationId?.toString() ?? '',
    causedBy: {
        name: metadata.causedBy?.name ?? '',
        subject: metadata.causedBy?.subject ?? ''
    },
    causation: item.context.causation.map(c => ({
        type: c.type,
        occurred: toIso(c.occurred),
        properties: c.properties
    }))
});

export const contextSchema = {
    type: 'object',
    properties: {
        eventType: { type: 'string', title: 'Event Type' },
        generation: { type: 'number', title: 'Generation' },
        eventSourceType: { type: 'string', title: 'Event Source Type' },
        eventSourceDescription: { type: 'string', title: 'Event Source Description' },
        eventSource: { type: 'string', title: 'Event Source' },
        eventSourceId: { type: 'string', title: 'Event Source ID' },
        sequenceNumber: { type: 'number', title: 'Sequence Number' },
        eventStreamType: { type: 'string', title: 'Event Stream Type' },
        eventStreamDescription: { type: 'string', title: 'Event Stream Description' },
        eventStreamId: { type: 'string', title: 'Event Stream ID' },
        occurred: { type: 'string', title: 'Occurred', format: 'date-time' },
        correlationId: { type: 'string', title: 'Correlation ID' },
        causedBy: {
            type: 'object',
            title: 'Caused By',
            properties: {
                name: { type: 'string', title: 'Name' },
                subject: { type: 'string', title: 'Subject' }
            }
        },
        causation: {
            type: 'array',
            title: 'Causation',
            items: {
                type: 'object',
                properties: {
                    type: { type: 'string', title: 'Type' },
                    occurred: { type: 'string', title: 'Occurred', format: 'date-time' },
                    properties: { type: 'object', title: 'Properties' }
                }
            }
        }
    }
};
