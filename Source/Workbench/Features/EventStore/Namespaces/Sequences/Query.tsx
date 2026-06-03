// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { EventList } from './EventList';
import { EventDetails } from './EventDetails';
import { Allotment } from 'allotment';
import { InputText } from 'primereact/inputtext';
import { Menubar } from 'primereact/menubar';
import { MenuItem } from 'primereact/menuitem';
import { AppendedEvent } from 'Api/Events';
import { QueryDefinition } from './QueryDefinition';
import { FilterPanel, FilterEditor, useFilterState } from '@cratis/components/Filter';
import type { FilterDefinition } from '@cratis/components/Filter';
import { SequenceSelector } from './SequenceSelector';
import { useMemo, useRef, useState } from 'react';
import { AllEventTypes } from 'Api/EventTypes/AllEventTypes';
import { ForSequence } from 'Api/EventSequences/ForSequence';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';

export interface QueryProps {
    query: QueryDefinition;
    onSave?: (query: QueryDefinition) => Promise<void>;
}

const FILTER_KEY_EVENT_SEQUENCE = 'eventSequence';
const FILTER_KEY_EVENT_SOURCE_ID = 'eventSourceId';
const FILTER_KEY_EVENT_TYPES = 'eventTypes';
const FILTER_KEY_TIME_RANGE = 'timeRange';

const FilterIcon = () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
        <polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3" />
    </svg>
);

export const Query = ({ query, onSave }: QueryProps) => {
    const params = useParams<EventStoreAndNamespaceParams>();

    const filterButtonRef = useRef<HTMLButtonElement>(null);
    const [filtersOpen, setFiltersOpen] = useState(false);
    const [selectedEvent, setSelectedEvent] = useState<AppendedEvent | null>(null);
    const [appliedFilter, setAppliedFilter] = useState<{
        eventSourceId?: string;
        eventTypes?: string[];
        startTime?: Date;
        endTime?: Date;
    }>({});
    const [appliedEventSequenceId, setAppliedEventSequenceId] = useState<string>(query.eventSequenceId);
    const [isDirty, setIsDirty] = useState<boolean>(query.isUnsaved === true);

    const [eventTypes] = AllEventTypes.use({ eventStore: params.eventStore! });

    const [histogram] = ForSequence.use({
        eventStore: params.eventStore!,
        namespace: params.namespace!,
        eventSequenceId: appliedEventSequenceId,
        resolution: 'Hour',
    });

    const histogramData = histogram.data ?? [];

    const timeRange = useMemo(() => {
        if (histogramData.length === 0) {
            const now = Date.now();
            return { min: now - 24 * 60 * 60 * 1000, max: now, values: [] as number[] };
        }
        const times = histogramData.map(bucket => {
            const occurred = bucket.occurred;
            return occurred instanceof Date ? occurred.getTime() : new Date(occurred as unknown as string).getTime();
        });
        const expanded: number[] = [];
        histogramData.forEach((bucket, index) => {
            const count = Number(bucket.count ?? 0);
            for (let i = 0; i < count; i++) {
                expanded.push(times[index]);
            }
        });
        return {
            min: Math.min(...times),
            max: Math.max(...times),
            values: expanded.length > 0 ? expanded : times,
        };
    }, [histogramData]);

    const filters: FilterDefinition[] = useMemo(() => [
        { key: FILTER_KEY_EVENT_SEQUENCE, label: 'Event Sequence', type: 'custom' },
        { key: FILTER_KEY_EVENT_SOURCE_ID, label: 'Event source id', type: 'custom' },
        {
            key: FILTER_KEY_EVENT_TYPES,
            label: 'Event types',
            type: 'string',
            multi: true,
            searchable: true,
            searchPlaceholder: 'Search event types…',
            options: (eventTypes.data ?? [])
                .map(type => ({
                    key: type.id,
                    label: type.id,
                    value: type.id,
                }))
                .sort((a, b) => a.label.localeCompare(b.label)),
        },
        {
            key: FILTER_KEY_TIME_RANGE,
            label: 'Time range',
            type: 'date',
            buckets: 20,
            numericRange: {
                min: timeRange.min,
                max: timeRange.max,
                values: timeRange.values,
            },
        },
    ], [eventTypes.data, timeRange]);

    const filterState = useFilterState(filters);

    const activeFilterCount =
        ((filterState.filterValues[FILTER_KEY_EVENT_TYPES]?.size ?? 0) > 0 ? 1 : 0) +
        (filterState.rangeValues[FILTER_KEY_TIME_RANGE] ? 1 : 0) +
        (((filterState.customValues[FILTER_KEY_EVENT_SOURCE_ID] as string | undefined)?.trim().length ?? 0) > 0 ? 1 : 0) +
        (filterState.customValues[FILTER_KEY_EVENT_SEQUENCE] ? 1 : 0);

    const markDirty = () => setIsDirty(true);

    const runQuery = () => {
        const selectedTypes = Array.from(filterState.filterValues[FILTER_KEY_EVENT_TYPES] ?? []);
        const range = filterState.rangeValues[FILTER_KEY_TIME_RANGE] ?? null;
        const eventSourceId = (filterState.customValues[FILTER_KEY_EVENT_SOURCE_ID] as string | undefined) ?? '';
        const eventSequenceId = (filterState.customValues[FILTER_KEY_EVENT_SEQUENCE] as string | undefined) ?? query.eventSequenceId;

        setAppliedEventSequenceId(eventSequenceId);
        setAppliedFilter({
            eventSourceId: eventSourceId || undefined,
            eventTypes: selectedTypes.length > 0 ? selectedTypes : undefined,
            startTime: range ? new Date(range[0]) : undefined,
            endTime: range ? new Date(range[1]) : undefined,
        });
    };

    const save = async () => {
        if (onSave === undefined) {
            return;
        }
        const selectedTypes = Array.from(filterState.filterValues[FILTER_KEY_EVENT_TYPES] ?? []);
        const range = filterState.rangeValues[FILTER_KEY_TIME_RANGE] ?? null;
        const eventSourceId = (filterState.customValues[FILTER_KEY_EVENT_SOURCE_ID] as string | undefined) ?? '';
        const eventSequenceId = (filterState.customValues[FILTER_KEY_EVENT_SEQUENCE] as string | undefined) ?? query.eventSequenceId;
        await onSave({
            ...query,
            eventSequenceId,
            eventSourceId: eventSourceId || undefined,
            eventTypes: selectedTypes,
            startTime: range ? new Date(range[0]) : undefined,
            endTime: range ? new Date(range[1]) : undefined,
        });
        setIsDirty(false);
    };

    return (
        <div className="flex flex-col h-full w-full">
            <div className="px-4 py-2" onMouseDownCapture={markDirty} onKeyDownCapture={markDirty}>
                <Menubar model={[
                    {
                        id: 'run',
                        label: 'Run',
                        icon: 'pi pi-play',
                        command: runQuery,
                    },
                    {
                        id: 'filters',
                        label: 'Filters',
                        className: filtersOpen ? 'highlight' : '',
                        template: (item, options) => (
                            <button
                                ref={filterButtonRef}
                                type="button"
                                className={options.className}
                                onClick={() => setFiltersOpen(open => !open)}>
                                <span className={options.iconClassName} style={{ display: 'inline-flex', alignItems: 'center', marginRight: '0.5rem' }}>
                                    <FilterIcon />
                                </span>
                                <span className={options.labelClassName}>{item.label}</span>
                                {activeFilterCount > 0 && (
                                    <span
                                        style={{
                                            display: 'inline-flex',
                                            alignItems: 'center',
                                            justifyContent: 'center',
                                            marginLeft: '0.5rem',
                                            minWidth: '1.25rem',
                                            height: '1.25rem',
                                            padding: '0 0.4rem',
                                            borderRadius: '999px',
                                            background: 'var(--primary-color)',
                                            color: 'var(--primary-color-text)',
                                            fontSize: '0.75rem',
                                            fontWeight: 600,
                                            lineHeight: 1,
                                        }}>
                                        {activeFilterCount}
                                    </span>
                                )}
                            </button>
                        ),
                    },
                    {
                        id: 'save',
                        label: 'Save',
                        icon: 'pi pi-save',
                        disabled: !isDirty,
                        command: save,
                    },
                ] as MenuItem[]} />
            </div>

            <FilterPanel
                isOpen={filtersOpen}
                filters={filters}
                anchorRef={filterButtonRef}
                filterValues={filterState.filterValues}
                rangeValues={filterState.rangeValues}
                customValues={filterState.customValues}
                expandedFilterKey={filterState.expandedFilterKey}
                onClose={() => setFiltersOpen(false)}
                onFilterToggle={(key, optionKey, multi) => { markDirty(); filterState.handleToggleFilter(key, optionKey, multi); }}
                onFilterClear={(key) => { markDirty(); filterState.handleClearFilter(key); }}
                onRangeChange={(key, range) => { markDirty(); filterState.handleRangeChange(key, range); }}
                onExpandedFilterChange={filterState.setExpandedFilterKey}
                onCustomValueChange={(key, value) => { markDirty(); filterState.handleCustomValueChange(key, value); }}>
                <FilterEditor filterKey={FILTER_KEY_EVENT_SEQUENCE}>
                    {({ value, onChange }) => (
                        <SequenceSelector
                            value={(value as string | undefined) ?? query.eventSequenceId}
                            onChange={onChange} />
                    )}
                </FilterEditor>
                <FilterEditor filterKey={FILTER_KEY_EVENT_SOURCE_ID}>
                    {({ value, onChange }) => (
                        <InputText
                            value={(value as string | undefined) ?? ''}
                            onChange={(event) => onChange(event.target.value)}
                            placeholder="Filter by event source id"
                            className="p-inputtext-sm w-full" />
                    )}
                </FilterEditor>
            </FilterPanel>

            <div className="flex-1 min-h-0">
                <Allotment className="h-full" proportionalLayout={false}>
                    <Allotment.Pane>
                        <EventList
                            key={`${appliedEventSequenceId}:${JSON.stringify(appliedFilter)}`}
                            filter={appliedFilter}
                            eventSequenceId={appliedEventSequenceId}
                            selection={selectedEvent}
                            onSelectionChange={setSelectedEvent} />
                    </Allotment.Pane>
                    {selectedEvent !== null && (
                        <Allotment.Pane preferredSize="40%" minSize={360}>
                            <div className="h-full overflow-auto">
                                <EventDetails item={selectedEvent} />
                            </div>
                        </Allotment.Pane>
                    )}
                </Allotment>
            </div>
        </div>
    );
};
