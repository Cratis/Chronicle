// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useRef, useState } from 'react';
import { FilterEditor, FilterPanel } from '@cratis/components/Filter';
import { Chip } from '@cratis/components/Display';
import { InputTags, type InputTagsRootValueChangeEvent } from 'primereact/inputtags';
import { InputText } from 'primereact/inputtext';
import strings from 'Strings';
import { SequenceHistogram } from 'Api/EventSequences/SequenceHistogram';
import { SequenceQueryState, areSequenceQueryStatesEqual } from './SequenceQueryState';
import {
    applyCustomFilterValue,
    buildFilterDefinitions,
    clearFilter,
    correlationFilterKey,
    countActiveFilters,
    eventSourceFilterKey,
    eventSourceTypeFilterKey,
    eventStreamTypeFilterKey,
    occurredFilterKey,
    tagsFilterKey,
    toCustomFilterValues,
    toFilterValues,
    toggleEventType
} from './queryFilters';
import { FilterIcon } from './FilterIcon';
import { OccurredRangeFilter } from './OccurredRangeFilter';
import { resolutionForSpan, spanOf, toHistogramBuckets } from './histogramResolution';
import { toHistogramArguments } from './toQueryArguments';
import './QueryFilterBar.css';

/** Marks the document body while this bar is on the page, so the panel it portals out can be reached. */
const panelAnchoredClass = 'query-filter-bar-anchored';

/** How far the panel's right edge sits from the right of the window. */
const panelRightProperty = '--query-filter-bar-right';

/**
 * Props for {@link QueryFilterBar}.
 */
export interface QueryFilterBarProps {
    /** The query being edited. */
    state: SequenceQueryState;
    /** The event store the query runs against. */
    eventStore: string;
    /** The event type identifiers registered in the event store. */
    eventTypeIds: string[];
    /** Called with the query state after the user changes a filter. */
    onChange: (state: SequenceQueryState) => void;
    /** Called with the query state once the user is done changing filters, so it can be run. */
    onFiltersSettled: (state: SequenceQueryState) => void;
}

/**
 * The filter dropdown for one query, built on the shared filter panel the pivot viewer uses.
 *
 * Every change goes straight into the query state, which is what gets persisted - there is no
 * separate "applied" copy of the filters to keep in step.
 * @param props The {@link QueryFilterBarProps}.
 * @returns The rendered filter bar.
 */
export const QueryFilterBar = ({ state, eventStore, eventTypeIds, onChange, onFiltersSettled }: QueryFilterBarProps) => {
    const anchorRef = useRef<HTMLButtonElement>(null);
    const [isOpen, setIsOpen] = useState(false);

    // The state the panel opened on, so closing it can tell whether anything actually changed and
    // only re-run the query when it did.
    const openedOn = useRef(state);
    const [expandedFilterKey, setExpandedFilterKey] = useState<string | null>(null);

    const filterStrings = strings.eventStore.namespaces.sequences.filters;

    // The resolution is picked from the span the histogram already covers, so a sequence spanning
    // an hour gets minute bars and one spanning years gets month bars.
    const [coarseHistogram] = SequenceHistogram.use(toHistogramArguments(state, eventStore, 'day'));
    const coarseBuckets = useMemo(() => toHistogramBuckets(coarseHistogram.data), [coarseHistogram.data]);
    const coarseSpan = useMemo(() => spanOf(coarseBuckets), [coarseBuckets]);
    const resolution = coarseSpan ? resolutionForSpan(coarseSpan.max - coarseSpan.min) : 'day';

    const [histogram] = SequenceHistogram.use(toHistogramArguments(state, eventStore, resolution));
    const buckets = useMemo(() => toHistogramBuckets(histogram.data), [histogram.data]);
    const span = useMemo(() => spanOf(buckets), [buckets]);

    const filters = useMemo(
        () => buildFilterDefinitions(eventTypeIds, {
            eventType: filterStrings.groups.eventType,
            eventSource: filterStrings.groups.eventSource,
            eventSourceType: filterStrings.groups.eventSourceType,
            eventStreamType: filterStrings.groups.eventStreamType,
            correlation: filterStrings.groups.correlation,
            tags: filterStrings.groups.tags,
            occurred: filterStrings.groups.occurred,
            searchEventTypes: filterStrings.placeholders.eventType
        }),
        [eventTypeIds, filterStrings]
    );

    const activeCount = countActiveFilters(state);

    const open = () => {
        openedOn.current = state;
        anchorPanelToTrigger();
        setIsOpen(true);
    };

    const close = () => {
        setIsOpen(false);
        if (!areSequenceQueryStatesEqual(openedOn.current, state)) onFiltersSettled(state);
    };

    // The panel opens from the trigger's left edge and is rendered into the document body, where it
    // cannot be reached by a class of ours. With the trigger at the right of the toolbar that runs
    // it off the side of the window, so its right edge is lined up with the trigger's instead.
    const anchorPanelToTrigger = () => {
        const trigger = anchorRef.current;
        if (!trigger) return;

        const distanceFromRight = window.innerWidth - trigger.getBoundingClientRect().right;
        document.body.style.setProperty(panelRightProperty, `${Math.max(distanceFromRight, 0)}px`);
    };

    // The panel fades out rather than disappearing, so the marker stays for as long as the bar is on
    // the page. Taking it away as the panel closes would drop the positioning halfway through the
    // fade, and the panel would visibly slide off to the right on its way out.
    useEffect(() => {
        document.body.classList.add(panelAnchoredClass);
        return () => document.body.classList.remove(panelAnchoredClass);
    }, []);

    const textEditor = (filterKey: string, placeholder: string) => (
        <FilterEditor key={filterKey} filterKey={filterKey}>
            {({ value, onChange: onEditorChange }) => (
                <InputText
                    className='w-full'
                    value={(value as string) ?? ''}
                    placeholder={placeholder}
                    onChange={(event: React.ChangeEvent<HTMLInputElement>) => onEditorChange(event.target.value)} />
            )}
        </FilterEditor>
    );

    return (
        <>
            <button
                type='button'
                ref={anchorRef}
                className={`query-filter-bar__trigger ${isOpen ? 'is-open' : ''}`}
                title={filterStrings.title}
                onClick={() => (isOpen ? close() : open())}>
                <FilterIcon />
                <span>{filterStrings.title}</span>
                {activeCount > 0 && <span className='query-filter-bar__count'>{activeCount}</span>}
            </button>

            <FilterPanel
                isOpen={isOpen}
                filters={filters}
                filterValues={toFilterValues(state)}
                rangeValues={{}}
                customValues={toCustomFilterValues(state)}
                expandedFilterKey={expandedFilterKey}
                anchorRef={anchorRef}
                onClose={close}
                onFilterToggle={(_, optionKey) => onChange(toggleEventType(state, optionKey))}
                onFilterClear={filterKey => onChange(clearFilter(state, filterKey))}
                onRangeChange={() => undefined}
                onExpandedFilterChange={setExpandedFilterKey}
                onCustomValueChange={(filterKey, value) => onChange(applyCustomFilterValue(state, filterKey, value))}>

                {textEditor(eventSourceFilterKey, filterStrings.placeholders.eventSourceId)}
                {textEditor(eventSourceTypeFilterKey, filterStrings.placeholders.eventSourceType)}
                {textEditor(eventStreamTypeFilterKey, filterStrings.placeholders.eventStreamType)}
                {textEditor(correlationFilterKey, filterStrings.placeholders.correlationId)}

                <FilterEditor filterKey={tagsFilterKey}>
                    {({ value, onChange: onEditorChange }) => (
                        <InputTags.Root
                            className='w-full'
                            value={(value as string[]) ?? []}
                            onValueChange={(event: InputTagsRootValueChangeEvent) =>
                                onEditorChange(event.value?.length ? event.value : undefined)}>
                            <InputTags.Items>
                                {({ item, remove }) => (
                                    <Chip label={item} removable onRemove={remove} removeAriaLabel={item} />
                                )}
                            </InputTags.Items>
                            <InputTags.Control>
                                {({ controlProps }) => (
                                    <input {...controlProps} placeholder={filterStrings.placeholders.tags} />
                                )}
                            </InputTags.Control>
                        </InputTags.Root>
                    )}
                </FilterEditor>

                <FilterEditor filterKey={occurredFilterKey}>
                    {({ value, onChange: onEditorChange }) => (
                        <OccurredRangeFilter
                            buckets={buckets}
                            min={span?.min ?? 0}
                            max={span?.max ?? 0}
                            selectedRange={(value as [number, number] | undefined) ?? null}
                            onChange={range => onEditorChange(range ?? undefined)}
                            emptyMessage={filterStrings.noEventsToPlot}
                            clearLabel={filterStrings.clearRange} />
                    )}
                </FilterEditor>
            </FilterPanel>
        </>
    );
};
