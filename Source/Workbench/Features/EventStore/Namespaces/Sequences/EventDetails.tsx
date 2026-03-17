// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useMemo } from 'react';
import { TabView, TabPanel } from 'primereact/tabview';
import { Dropdown } from 'primereact/dropdown';
import { AppendedEvent } from 'Api/Events';
import { EventRevision } from 'Api/Events/EventRevision';
import { IDetailsComponentProps } from '@cratis/components/DataPage';
import { AllEventTypesWithSchemas } from 'Api/EventTypes/AllEventTypesWithSchemas';
import { EventTypeRegistration } from 'Api/Events/EventTypeRegistration';
import { ObjectContentEditor as _OCE } from '@cratis/components';
const ObjectContentEditor = _OCE.ObjectContentEditor;
import type { Json } from '@cratis/components/types';
import { useParams } from 'react-router-dom';
import { type EventStoreParams } from 'Shared';
import strings from 'Strings';

interface RevisionOption {
    label: string;
    value: number;
}

interface GenerationOption {
    label: string;
    value: number;
}

export const EventDetails = ({ item }: IDetailsComponentProps<AppendedEvent>) => {
    const params = useParams<EventStoreParams>();
    const [eventTypes] = AllEventTypesWithSchemas.use({ eventStore: params.eventStore! });
    const [selectedRevision, setSelectedRevision] = useState<number>(-1);
    const [selectedGeneration, setSelectedGeneration] = useState<number | null>(null);

    const revisions: EventRevision[] = item.revisions ?? [];
    const isRevised = revisions.length > 0;

    const generationEntries = item.generationalContent ?? [];
    const hasMultipleGenerations = generationEntries.length > 1;

    // Build revision options: -1 = latest (default), 0 = original, 1..N = revision entries
    const revisionOptions: RevisionOption[] = useMemo(() => {
        if (!isRevised) return [];

        const options: RevisionOption[] = [
            { label: strings.eventStore.namespaces.sequences.details.originalRevision, value: 0 }
        ];
        revisions.forEach((_, index) => {
            const isLatest = index === revisions.length - 1;
            const label = isLatest
                ? `${strings.eventStore.namespaces.sequences.details.revisionLabel.replace('{n}', String(index + 1))} (latest)`
                : strings.eventStore.namespaces.sequences.details.revisionLabel.replace('{n}', String(index + 1));
            options.push({ label, value: index + 1 });
        });
        return options;
    }, [revisions, isRevised]);

    // Build generation options from available generational content
    const generationOptions: GenerationOption[] = useMemo(() => {
        if (!hasMultipleGenerations) return [];
        return generationEntries
            .slice()
            .sort((a, b) => a.key - b.key)
            .map(({ key }) => {
                const isStored = key === item.context.eventType.generation;
                const baseLabel = strings.eventStore.namespaces.sequences.details.generationLabel.replace('{n}', String(key));
                return { label: isStored ? `${baseLabel} (stored)` : baseLabel, value: key };
            });
    }, [generationEntries, hasMultipleGenerations, item.context.eventType.generation]);

    // Default to latest revision when revised
    const effectiveRevision = selectedRevision === -1 && isRevised ? revisions.length : selectedRevision;

    // Effective generation for content display
    const effectiveGeneration = selectedGeneration ?? item.context.eventType.generation;

    // Get current revision content
    const currentContent = useMemo(() => {
        // When a specific generation is selected and there are multiple generations, show that generation's raw content
        if (hasMultipleGenerations && selectedGeneration !== null) {
            const entry = generationEntries.find(g => g.key === effectiveGeneration);
            if (entry?.value) {
                try { return JSON.parse(entry.value); } catch { return entry.value; }
            }
        }
        if (!isRevised || effectiveRevision === 0) {
            const rawContent = effectiveRevision === 0 && item.originalContent ? item.originalContent : item.content;
            try {
                return typeof rawContent === 'string' ? JSON.parse(rawContent) : rawContent;
            } catch {
                return rawContent;
            }
        }
        const revision = revisions[effectiveRevision - 1];
        if (!revision) return {};
        try {
            return typeof revision.content === 'string' ? JSON.parse(revision.content) : revision.content;
        } catch {
            return revision.content;
        }
    }, [effectiveRevision, effectiveGeneration, selectedGeneration, generationEntries, hasMultipleGenerations, isRevised, item.content, item.originalContent, revisions]);

    // Get metadata (occurred, correlationId, causedBy) for the current revision
    const currentMetadata = useMemo(() => {
        if (!isRevised || effectiveRevision === 0) {
            return {
                occurred: item.context.occurred,
                correlationId: item.context.correlationId,
                causedBy: item.context.causedBy
            };
        }
        const revision = revisions[effectiveRevision - 1];
        if (!revision) return { occurred: item.context.occurred, correlationId: item.context.correlationId, causedBy: item.context.causedBy };
        return {
            occurred: revision.occurred,
            correlationId: revision.correlationId,
            causedBy: revision.causedBy
        };
    }, [effectiveRevision, isRevised, item.context, revisions]);

    // Schema: always use the effective generation's schema (defaults to stored generation, overridden by user selection)
    const effectiveEventType = useMemo(() => {
        if (!eventTypes.data) return undefined;
        return eventTypes.data.find(
            (et: EventTypeRegistration) => et.type.id === item.context.eventType.id && et.type.generation === effectiveGeneration
        ) ?? eventTypes.data.find((et: EventTypeRegistration) => et.type.id === item.context.eventType.id);
    }, [eventTypes.data, effectiveGeneration, item.context.eventType.id]);

    const schema = effectiveEventType ? JSON.parse(effectiveEventType.schema) : { properties: {} };

    // Build context object for display - metadata reflects the current revision
    const contextObject = {
        eventType: item.context.eventType.id,
        generation: item.context.eventType.generation,
        eventSourceType: item.context.eventSourceType,
        eventSourceId: item.context.eventSourceId,
        sequenceNumber: item.context.sequenceNumber,
        eventStreamType: item.context.eventStreamType,
        eventStreamId: item.context.eventStreamId,
        occurred: currentMetadata.occurred instanceof Date
            ? (currentMetadata.occurred as Date).toISOString()
            : new Date(currentMetadata.occurred as string).toISOString(),
        correlationId: currentMetadata.correlationId?.toString() ?? '',
        causedBy: {
            name: currentMetadata.causedBy?.name ?? '',
            subject: currentMetadata.causedBy?.subject ?? ''
        },
        causation: item.context.causation.map(c => ({
            type: c.type,
            occurred: c.occurred instanceof Date ? (c.occurred as Date).toISOString() : new Date(c.occurred as string).toISOString(),
            properties: c.properties
        }))
    };

    const contextSchema = {
        type: 'object',
        properties: {
            eventType: { type: 'string', title: 'Event Type' },
            generation: { type: 'number', title: 'Generation' },
            eventSourceType: { type: 'string', title: 'Event Source Type' },
            eventSourceId: { type: 'string', title: 'Event Source ID' },
            sequenceNumber: { type: 'number', title: 'Sequence Number' },
            eventStreamType: { type: 'string', title: 'Event Stream Type' },
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

    const revisionPlaceholder = isRevised ? revisionOptions[revisionOptions.length - 1]?.label : undefined;
    const generationPlaceholder = hasMultipleGenerations ? generationOptions.find(o => o.value === item.context.eventType.generation)?.label : undefined;

    return (
        <div style={{ height: '100%', display: 'flex', flexDirection: 'column', padding: '20px' }}>
            <h2 style={{ marginTop: 0, marginBottom: '12px', color: 'var(--text-color)' }}>
                {item.context.eventType.id}
            </h2>
            {(isRevised || hasMultipleGenerations) && (
                <div style={{ marginBottom: '12px', display: 'flex', alignItems: 'center', gap: '16px', flexWrap: 'wrap' }}>
                    {isRevised && (
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                            <label style={{ color: 'var(--text-color-secondary)', fontSize: '0.875rem' }}>
                                {strings.eventStore.namespaces.sequences.details.revision}:
                            </label>
                            <Dropdown
                                value={effectiveRevision}
                                options={revisionOptions}
                                onChange={(e) => setSelectedRevision(e.value)}
                                placeholder={revisionPlaceholder}
                                style={{ minWidth: '200px' }}
                            />
                        </div>
                    )}
                    {hasMultipleGenerations && (
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                            <label style={{ color: 'var(--text-color-secondary)', fontSize: '0.875rem' }}>
                                {strings.eventStore.namespaces.sequences.details.generation}:
                            </label>
                            <Dropdown
                                value={selectedGeneration ?? item.context.eventType.generation}
                                options={generationOptions}
                                onChange={(e) => setSelectedGeneration(e.value)}
                                placeholder={generationPlaceholder}
                                style={{ minWidth: '200px' }}
                            />
                        </div>
                    )}
                </div>
            )}
            <TabView style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
                <TabPanel header="Context">
                    <div style={{ height: '100%', overflow: 'auto' }}>
                        <ObjectContentEditor
                            object={contextObject as Json}
                            schema={contextSchema}
                            timestamp={currentMetadata.occurred instanceof Date ? currentMetadata.occurred as Date : new Date(currentMetadata.occurred as string)}
                        />
                    </div>
                </TabPanel>
                <TabPanel header="Content">
                    <div style={{ height: '100%', overflow: 'auto' }}>
                        <ObjectContentEditor
                            object={currentContent}
                            schema={schema}
                            timestamp={currentMetadata.occurred instanceof Date ? currentMetadata.occurred as Date : new Date(currentMetadata.occurred as string)}
                        />
                    </div>
                </TabPanel>
            </TabView>
        </div>
    );
};

