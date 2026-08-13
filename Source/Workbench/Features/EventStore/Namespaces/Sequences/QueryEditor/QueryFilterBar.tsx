// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useRef, useState } from 'react';
import { FilterEditor, FilterPanel } from '@cratis/components/Filter';
import { InputText } from 'primereact/inputtext';
import * as faIcons from 'react-icons/fa6';
import strings from 'Strings';
import { SequenceHistogram } from 'Api/EventSequences/SequenceHistogram';
import { SequenceQueryState } from './SequenceQueryState';
import {
    buildFilterDefinitions,
    clearFilter,
    countActiveFilters,
    eventSourceFilterKey,
    occurredFilterKey,
    toFilterValues,
    toggleEventType
} from './queryFilters';
import { OccurredRangeFilter } from './OccurredRangeFilter';
import { resolutionForSpan, spanOf, toHistogramBuckets } from './histogramResolution';
import { toHistogramArguments } from './toQueryArguments';
import './QueryFilterBar.css';

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
}

/**
 * The filter dropdown for one query, built on the shared filter panel the pivot viewer uses.
 *
 * Every change goes straight into the query state, which is what gets persisted - there is no
 * separate "applied" copy of the filters to keep in step.
 * @param props The {@link QueryFilterBarProps}.
 * @returns The rendered filter bar.
 */
export const QueryFilterBar = ({ state, eventStore, eventTypeIds, onChange }: QueryFilterBarProps) => {
    const anchorRef = useRef<HTMLButtonElement>(null);
    const [isOpen, setIsOpen] = useState(false);
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
            occurred: filterStrings.groups.occurred,
            searchEventTypes: filterStrings.placeholders.eventType
        }),
        [eventTypeIds, filterStrings]
    );

    const activeCount = countActiveFilters(state);
    const selectedRange: [number, number] | null =
        state.occurredFrom !== undefined && state.occurredTo !== undefined
            ? [state.occurredFrom, state.occurredTo]
            : null;

    return (
        <>
            <button
                type='button'
                ref={anchorRef}
                className='query-filter-bar__trigger'
                onClick={() => setIsOpen(open => !open)}>
                <faIcons.FaFilter />
                <span>{filterStrings.title}</span>
                {activeCount > 0 && <span className='query-filter-bar__count'>{activeCount}</span>}
            </button>

            <FilterPanel
                isOpen={isOpen}
                filters={filters}
                filterValues={toFilterValues(state)}
                rangeValues={{}}
                customValues={{
                    [eventSourceFilterKey]: state.eventSourceId || undefined,
                    [occurredFilterKey]: selectedRange ?? undefined
                }}
                expandedFilterKey={expandedFilterKey}
                anchorRef={anchorRef}
                onClose={() => setIsOpen(false)}
                onFilterToggle={(_, optionKey) => onChange(toggleEventType(state, optionKey))}
                onFilterClear={filterKey => onChange(clearFilter(state, filterKey))}
                onRangeChange={() => undefined}
                onExpandedFilterChange={setExpandedFilterKey}
                onCustomValueChange={(filterKey, value) => {
                    if (filterKey === eventSourceFilterKey) {
                        onChange({ ...state, eventSourceId: (value as string) ?? '' });
                    } else if (filterKey === occurredFilterKey) {
                        const range = value as [number, number] | undefined;
                        onChange({ ...state, occurredFrom: range?.[0], occurredTo: range?.[1] });
                    }
                }}>

                <FilterEditor filterKey={eventSourceFilterKey}>
                    {({ value, onChange: onEditorChange }) => (
                        <InputText
                            className='w-full'
                            value={(value as string) ?? ''}
                            placeholder={filterStrings.placeholders.eventSourceId}
                            onChange={event => onEditorChange(event.target.value)} />
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
