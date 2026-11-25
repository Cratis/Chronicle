// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useMemo } from 'react';
import { Tabs, TabPanel } from 'Components/Tabs';
import { Dropdown } from '@cratis/components/Dropdown';
import { AppendedEvent } from 'Features/Sequences';
import { EventRevision } from 'Features/Sequences';
import { IDetailsComponentProps } from '@cratis/components/DataPage';
import { ObserveEventTypes } from 'Features/EventTypes';
import { AllEventTypeGenerations } from 'Features/EventTypes';
import { EventTypeDetails } from 'Features/EventTypes';
import { ObserveEventSources } from 'Features/EventSources';
import { ObjectContentEditor } from '@cratis/components/ObjectContentEditor';
import type { Json } from '@cratis/components/types';
import { useParams } from 'react-router-dom';
import { type EventStoreParams } from 'Shared';
import strings from 'Strings';
import { applyCoordinateFormatting } from 'Components/CoordinateFormatter/CoordinateFormatter';
import { RevisionComparison } from './RevisionComparison/RevisionComparison';
import { ComparisonScope } from './RevisionComparison/ComparisonScope';
import { buildContextObject, buildGenerationOptions, buildRevisionOptions, contextSchema, deriveSchema, parseContent, revisionMetadata } from './eventDetailsModel';
import css from './EventDetails.module.css';

export interface EventDetailsProps extends IDetailsComponentProps<AppendedEvent> {
    /** Where the event was read from. When given, an optional read-only revision comparison is offered. */
    comparisonScope?: ComparisonScope;
}

export const EventDetails = ({ item, comparisonScope }: EventDetailsProps) => {
    const params = useParams<EventStoreParams>();
    const [eventTypes] = ObserveEventTypes.use({ eventStore: params.eventStore! });
    const [allGenerations] = AllEventTypeGenerations.use({ eventStore: params.eventStore!, eventTypeId: item.context.eventType.id });
    const [eventSources] = ObserveEventSources.use({ eventStore: params.eventStore! });
    const [selectedRevision, setSelectedRevision] = useState<number>(-1);
    const [selectedGeneration, setSelectedGeneration] = useState<number | null>(null);

    const revisions: EventRevision[] = item.revisions ?? [];
    const isRevised = revisions.length > 0;

    const generationEntries = item.generationalContent ?? [];
    const hasMultipleGenerations = generationEntries.length > 1;

    const revisionOptions = useMemo(() => buildRevisionOptions(revisions), [revisions]);

    const generationOptions = useMemo(
        () => hasMultipleGenerations ? buildGenerationOptions(generationEntries, item.context.eventType.generation) : [],
        [generationEntries, hasMultipleGenerations, item.context.eventType.generation]);

    // Default to latest revision when revised
    const effectiveRevision = selectedRevision === -1 && isRevised ? revisions.length : selectedRevision;

    // Effective generation for content display
    const effectiveGeneration = selectedGeneration ?? item.context.eventType.generation;

    const shownRevision = isRevised && effectiveRevision !== 0 ? revisions[effectiveRevision - 1] : undefined;
    const hasShownRevisionEntry = !isRevised || effectiveRevision === 0 || shownRevision !== undefined;

    const currentContent = useMemo(() => {
        // When a specific generation is selected and there are multiple generations, show that generation's raw content
        if (hasMultipleGenerations && selectedGeneration !== null) {
            const entry = generationEntries.find(g => g.key === effectiveGeneration);
            if (entry?.value) return parseContent(entry.value);
        }
        if (shownRevision) return parseContent(shownRevision.content);
        if (!hasShownRevisionEntry) return {};
        return parseContent(effectiveRevision === 0 && item.originalContent ? item.originalContent : item.content);
    }, [effectiveRevision, effectiveGeneration, selectedGeneration, generationEntries, hasMultipleGenerations, hasShownRevisionEntry, shownRevision, item.content, item.originalContent]);

    const currentMetadata = useMemo(() => revisionMetadata(item, shownRevision), [item, shownRevision]);

    // Schema: look for an exact generation match in allGenerations (historical) then in ObserveEventTypes (latest).
    // Never fall back to a different generation — that would show wrong property labels.
    // When no schema is registered for this generation, derive a basic schema from the content keys.
    const effectiveEventType = useMemo(() => {
        const fromGenerations = allGenerations.data?.find(
            (et: EventTypeDetails) => et.type.generation === effectiveGeneration
        );
        if (fromGenerations) return fromGenerations;
        return eventTypes.data?.find(
            (et: EventTypeDetails) => et.type.id === item.context.eventType.id && et.type.generation === effectiveGeneration
        );
    }, [allGenerations.data, eventTypes.data, effectiveGeneration, item.context.eventType.id]);

    const schema = useMemo(
        () => effectiveEventType ? JSON.parse(effectiveEventType.schema) : deriveSchema(currentContent),
        [effectiveEventType, currentContent]);

    // Pre-process content: replace coordinate object values with formatted strings so the editor
    // renders them as plain text instead of expanding sub-objects.
    const [formattedContent, formattedSchema] = useMemo(
        () => applyCoordinateFormatting(currentContent as Record<string, unknown>, schema),
        [currentContent, schema]
    );

    const eventSource = eventSources.data.find(source => source.name === item.context.eventSourceType);
    const eventStream = eventSource?.streams.find(stream => stream.name === item.context.eventStreamType);

    const contextObject = buildContextObject(item, currentMetadata, eventSource, eventStream);

    const revisionPlaceholder = isRevised ? revisionOptions[revisionOptions.length - 1]?.label : undefined;
    const generationPlaceholder = hasMultipleGenerations ? generationOptions.find(o => o.value === item.context.eventType.generation)?.label : undefined;

    return (
        <div className={css.eventDetails}>
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
                            <Dropdown<number>
                                value={effectiveRevision}
                                options={revisionOptions}
                                optionLabel='label'
                                optionValue='value'
                                onChange={(value) => setSelectedRevision(value)}
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
                            <Dropdown<number>
                                value={selectedGeneration ?? item.context.eventType.generation}
                                options={generationOptions}
                                optionLabel='label'
                                optionValue='value'
                                onChange={(value) => setSelectedGeneration(value)}
                                placeholder={generationPlaceholder}
                                style={{ minWidth: '200px' }}
                            />
                        </div>
                    )}
                </div>
            )}
            {comparisonScope && <RevisionComparison scope={comparisonScope} event={item} />}
            <Tabs
                className='flex flex-col flex-1 min-h-0'
                panelContainerClassName='flex-1 min-h-0 overflow-y-auto'>
                <TabPanel header="Context">
                    <ObjectContentEditor
                        object={contextObject as Json}
                        schema={contextSchema}
                    />
                </TabPanel>
                <TabPanel header="Content">
                    <ObjectContentEditor
                        object={formattedContent as Json}
                        schema={formattedSchema}
                    />
                </TabPanel>
            </Tabs>
        </div>
    );
};

