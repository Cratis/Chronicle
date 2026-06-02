// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/arc.react.mvvm';
import { QueryViewModel } from './QueryViewModel';
import { EventList } from './EventList';
import { EventDetails } from './EventDetails';
import { Button } from 'primereact/button';
import { AppendedEvent } from 'Api/Events';
import { QueryDefinition } from './QueryDefinition';
import { OverlayPanel } from 'primereact/overlaypanel';
import { EventHistogram } from './Histogram/Histogram';
import { SequenceSelector } from './SequenceSelector';
import { MenuItem } from 'primereact/menuitem';
import { useRef, useState } from 'react';
import { useToggle } from 'usehooks-ts';
import { Menubar } from 'primereact/menubar';
import { InputText } from 'primereact/inputtext';
import { MultiSelect } from 'primereact/multiselect';
import { Calendar } from 'primereact/calendar';
import { AllEventTypes } from 'Api/EventTypes/AllEventTypes';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';

export interface QueryProps {
    query: QueryDefinition;
    onSave?: () => Promise<void>;
}

export const Query = withViewModel<QueryViewModel, QueryProps>(QueryViewModel, ({ viewModel, props }) => {
    const [showTimeRange, toggleTimeRange] = useToggle(false);
    const [showFilters, toggleFilters] = useToggle(false);
    const selectSequencePanelRef = useRef<OverlayPanel>(null);
    const params = useParams<EventStoreAndNamespaceParams>();
    const { query } = props;

    const [appliedFilter, setAppliedFilter] = useState<{
        eventSourceId?: string;
        eventTypes?: string[];
        startTime?: Date;
        endTime?: Date;
    }>({});

    const [selectedEvent, setSelectedEvent] = useState<AppendedEvent | null>(null);

    const [eventTypes] = AllEventTypes.use({
        eventStore: params.eventStore!
    });

    const hasUnsavedChanges = query.isUnsaved === true || viewModel.hasChanges;

    const items: MenuItem[] = [
        {
            id: 'selectSequence',
            label: 'Event log',
            icon: 'pi pi-list',
            command: (e) => selectSequencePanelRef.current?.toggle(e.originalEvent),
        },
        {
            id: 'run',
            label: 'Run',
            icon: 'pi pi-play',
            command: () => setAppliedFilter({
                eventSourceId: viewModel.eventSourceId,
                eventTypes: viewModel.eventTypes,
                startTime: viewModel.startTime ?? undefined,
                endTime: viewModel.endTime ?? undefined,
            })
        },
        {
            id: 'filters',
            label: 'Filters',
            icon: 'pi pi-filter',
            className: showFilters ? 'highlight' : '',
            command: () => toggleFilters(),
        },
        {
            id: 'timeRange',
            label: 'Time range',
            icon: 'pi pi-chart-line',
            className: showTimeRange ? 'highlight' : '',
            command: () => toggleTimeRange(),
        },
        {
            id: 'save',
            label: 'Save',
            icon: 'pi pi-save',
            disabled: !hasUnsavedChanges,
            command: async () => {
                if (props.onSave !== undefined) {
                    await props.onSave();
                }
                viewModel.save();
            }
        },
    ];

    const filter = appliedFilter;

    return (
        <>
            <div className="px-4 py-2">
                <Menubar model={items} />
                <OverlayPanel ref={selectSequencePanelRef}>
                    <SequenceSelector />
                </OverlayPanel>

                {showFilters && (
                    <div className="flex flex-row gap-4 mt-2 items-center">
                        <div className="flex flex-col gap-1">
                            <label className="text-xs text-gray-500">Event Source ID</label>
                            <InputText
                                value={viewModel.eventSourceId}
                                onChange={(e) => { viewModel.eventSourceId = e.target.value; }}
                                placeholder="Filter by event source ID"
                                className="p-inputtext-sm w-64" />
                        </div>
                        <div className="flex flex-col gap-1">
                            <label className="text-xs text-gray-500">Event Types</label>
                            <MultiSelect
                                value={viewModel.eventTypes}
                                options={eventTypes.data ?? []}
                                onChange={(e) => { viewModel.eventTypes = e.value; }}
                                optionLabel="id"
                                optionValue="id"
                                placeholder="Filter by event type"
                                maxSelectedLabels={2}
                                className="p-inputtext-sm w-72" />
                        </div>
                    </div>
                )}

                {showTimeRange && (
                    <>
                        <EventHistogram
                            eventStore={params.eventStore!}
                            namespace={params.namespace!}
                            eventSequenceId={query.eventSequenceId}
                        />
                        <div className="flex flex-row gap-4 mt-2 items-center">
                            <div className="flex flex-col gap-1">
                                <label className="text-xs text-gray-500">Start time</label>
                                <Calendar
                                    value={viewModel.startTime}
                                    onChange={(e) => { viewModel.startTime = e.value as Date | null; }}
                                    showTime
                                    hourFormat="24"
                                    placeholder="Start time"
                                    className="p-inputtext-sm" />
                            </div>
                            <div className="flex flex-col gap-1">
                                <label className="text-xs text-gray-500">End time</label>
                                <Calendar
                                    value={viewModel.endTime}
                                    onChange={(e) => { viewModel.endTime = e.value as Date | null; }}
                                    showTime
                                    hourFormat="24"
                                    placeholder="End time"
                                    className="p-inputtext-sm" />
                            </div>
                        </div>
                    </>
                )}
            </div>

            <div className="flex flex-row flex-1 overflow-hidden">
                <div className="flex-1 overflow-hidden min-w-0">
                    <EventList
                        key={JSON.stringify(filter)}
                        filter={filter}
                        eventSequenceId={query.eventSequenceId}
                        selection={selectedEvent}
                        onSelectionChange={setSelectedEvent} />
                </div>
                {selectedEvent !== null && (
                    <div className="w-[40%] min-w-[360px] overflow-auto border-l border-[var(--surface-border)]">
                        <div className="flex items-center justify-between px-3 py-2 border-b border-[var(--surface-border)]">
                            <span className="text-sm">Event #{selectedEvent.context.sequenceNumber} — {selectedEvent.context.eventType.id}</span>
                            <Button
                                icon="pi pi-times"
                                className="p-button-text p-button-sm"
                                onClick={() => setSelectedEvent(null)} />
                        </div>
                        <EventDetails item={selectedEvent} />
                    </div>
                )}
            </div>
        </>
    );
});

