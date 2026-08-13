// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { DataPage, MenuItem } from '@cratis/components/DataPage';
import { Column } from 'primereact/column';
import { InputText } from 'primereact/inputtext';
import { SelectButton } from 'primereact/selectbutton';
import * as faIcons from 'react-icons/fa6';
import strings from 'Strings';
import { AppendedEvent } from 'Api/Events';
import { QueryEvents } from 'Api/EventSequences/QueryEvents';
import { SequenceQueryScope } from 'Api/SequenceQueries/SequenceQueryScope';
import { EventDetails } from '../EventDetails';
import { QueryFilterBar } from './QueryFilterBar';
import { SequenceQueryState } from './SequenceQueryState';
import { toQueryArguments } from './toQueryArguments';
import { useQueryAutoSave } from './useQueryAutoSave';
import './QueryEditor.css';

/**
 * Props for {@link QueryEditor}.
 */
export interface QueryEditorProps {
    /** The query being edited. */
    state: SequenceQueryState;
    /** The event store the query runs against. */
    eventStore: string;
    /** The event type identifiers registered in the event store. */
    eventTypeIds: string[];
    /** Called with the query state whenever the user changes it. */
    onChange: (state: SequenceQueryState) => void;
}

const occurred = (event: AppendedEvent) => event.context.occurred.toLocaleString();

/**
 * One open query: its name, its filters, how it is sorted, and the events it currently matches.
 *
 * Changing anything here rewrites the query and schedules it to be saved. Results, on the other
 * hand, only refresh when the user asks - a query over a large sequence is worth running
 * deliberately rather than on every keystroke.
 * @param props The {@link QueryEditorProps}.
 * @returns The rendered editor.
 */
export const QueryEditor = ({ state, eventStore, eventTypeIds, onChange }: QueryEditorProps) => {
    const sequenceStrings = strings.eventStore.namespaces.sequences;

    // The arguments the results are currently showing, which only move when the user runs the query.
    const [runArguments, setRunArguments] = useState(() => toQueryArguments(state, eventStore));
    const [runCount, setRunCount] = useState(0);
    const [selectedEvent, setSelectedEvent] = useState<AppendedEvent | null>(null);

    useQueryAutoSave(state, eventStore, true);

    const run = () => {
        setRunArguments(toQueryArguments(state, eventStore));
        setRunCount(count => count + 1);
    };

    const sortOptions = [
        { label: sequenceStrings.sorting.newestFirst, value: true },
        { label: sequenceStrings.sorting.oldestFirst, value: false }
    ];

    const scopeOptions = [
        { label: sequenceStrings.scope.onlyMe, value: SequenceQueryScope.user },
        { label: sequenceStrings.scope.everyone, value: SequenceQueryScope.everyone }
    ];

    return (
        <div className='query-editor'>
            <div className='query-editor__toolbar'>
                <InputText
                    className='query-editor__name'
                    value={state.name}
                    aria-label={sequenceStrings.queryName}
                    placeholder={sequenceStrings.queryName}
                    onChange={event => onChange({ ...state, name: event.target.value })} />

                <QueryFilterBar
                    state={state}
                    eventStore={eventStore}
                    eventTypeIds={eventTypeIds}
                    onChange={onChange} />

                <SelectButton
                    value={state.descending}
                    options={sortOptions}
                    allowEmpty={false}
                    onChange={event => onChange({ ...state, descending: event.value as boolean })} />

                <div className='query-editor__spacer' />

                <SelectButton
                    value={state.scope}
                    options={scopeOptions}
                    allowEmpty={false}
                    onChange={event => onChange({ ...state, scope: event.value as SequenceQueryScope })} />
            </div>

            <div className='query-editor__results'>
                <DataPage
                    key={runCount}
                    title={state.name}
                    query={QueryEvents}
                    queryArguments={runArguments}
                    selection={selectedEvent}
                    emptyMessage={sequenceStrings.empty}
                    dataKey='context.sequenceNumber'
                    detailsComponent={EventDetails}
                    onSelectionChange={event => setSelectedEvent(event.value as AppendedEvent | null)}>

                    <DataPage.MenuItems>
                        <MenuItem
                            id='run'
                            label={sequenceStrings.actions.run}
                            icon={faIcons.FaPlay}
                            command={run} />
                    </DataPage.MenuItems>

                    <DataPage.Columns>
                        <Column field='context.sequenceNumber' header={sequenceStrings.columns.sequenceNumber} />
                        <Column field='context.eventType.id' header={sequenceStrings.columns.eventType} />
                        <Column field='context.eventSourceId' header={sequenceStrings.columns.eventSourceId} />
                        <Column field='context.occurred' header={sequenceStrings.columns.occurred} body={occurred} />
                    </DataPage.Columns>
                </DataPage>
            </div>
        </div>
    );
};
