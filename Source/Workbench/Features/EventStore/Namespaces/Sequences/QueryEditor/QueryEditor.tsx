// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Allotment } from 'allotment';
import { Dropdown } from '@cratis/components/Dropdown';
import { ActionMenubar, type ActionMenuItem } from '@cratis/components/Common';
import * as faIcons from 'react-icons/fa6';
import strings from 'Strings';
import { AppendedEvent } from 'Api/Events';
import { EventDetails } from '../EventDetails';
import { EventsTable } from './EventsTable';
import { QueryFilterBar } from './QueryFilterBar';
import { SequenceQueryState } from './SequenceQueryState';
import { exportQueryToFile } from './exportEvents';
import { toQueryArguments } from './toQueryArguments';
import { useEventActions } from './useEventActions';
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
    /** The event sequences the query can be pointed at. */
    eventSequenceIds: string[];
    /** Whether the query has edits that have not been written back. */
    hasUnsavedChanges: boolean;
    /** Called with the query state whenever the user changes it. */
    onChange: (state: SequenceQueryState) => void;
    /** Called when the user asks for the query to be saved. */
    onSave: () => void;
}

/**
 * One open query: its filters, how it is sorted, and the events it currently matches.
 *
 * Changing what the results *are* - a filter, the order, the sequence - re-runs the query as soon
 * as the user is done choosing. Editing a filter mid-thought does not, which is what the Run action
 * and the filter panel closing are for: a query over a large sequence is worth running once the
 * user has settled rather than on every keystroke.
 * @param props The {@link QueryEditorProps}.
 * @returns The rendered editor.
 */
export const QueryEditor = ({
    state,
    eventStore,
    eventTypeIds,
    eventSequenceIds,
    hasUnsavedChanges,
    onChange,
    onSave }: QueryEditorProps) => {
    const sequenceStrings = strings.eventStore.namespaces.sequences;

    // The arguments the results are currently showing, which only move when the query is run.
    const [runArguments, setRunArguments] = useState(() => toQueryArguments(state, eventStore));
    const [runCount, setRunCount] = useState(0);
    const [selectedEvent, setSelectedEvent] = useState<AppendedEvent | null>(null);

    const runWith = (next: SequenceQueryState) => {
        setRunArguments(toQueryArguments(next, eventStore));
        setRunCount(count => count + 1);
    };

    const run = () => runWith(state);

    const applyAndRun = (next: SequenceQueryState) => {
        onChange(next);
        runWith(next);
    };

    const { AppendEventWrapper, RedactEventWrapper, ReviseWrapper, append, redact, revise } =
        useEventActions(eventStore, state.namespace, state.eventSequenceId, selectedEvent, run);

    const menuItems: ActionMenuItem[] = [
        {
            label: sequenceStrings.actions.save,
            icon: <faIcons.FaFloppyDisk className='mr-2' />,
            disabled: !hasUnsavedChanges,
            command: onSave
        },
        {
            label: sequenceStrings.actions.run,
            icon: <faIcons.FaPlay className='mr-2' />,
            command: run
        },
        {
            label: sequenceStrings.actions.appendEvent,
            icon: <faIcons.FaPlus className='mr-2' />,
            command: append
        },
        {
            label: sequenceStrings.actions.redact,
            icon: <faIcons.FaEraser className='mr-2' />,
            disabled: !selectedEvent,
            command: redact
        },
        {
            label: sequenceStrings.actions.revise,
            icon: <faIcons.FaArrowsRotate className='mr-2' />,
            disabled: !selectedEvent,
            command: revise
        },
        {
            label: sequenceStrings.actions.export,
            icon: <faIcons.FaFileExport className='mr-2' />,
            command: () => exportQueryToFile(runArguments, eventStore, state.namespace)
        }
    ];

    // The sequence the query already points at is offered even when it is not in the list, so a
    // saved query never silently re-points itself at another sequence.
    const sequenceOptions = [...new Set([state.eventSequenceId, ...eventSequenceIds])];

    // The menubar has no end slot of its own, so what used to be handed to it sits beside it in the
    // toolbar row instead.
    const toolbarEnd = (
        <div className='query-editor__toolbar-end'>
            <Dropdown
                className='query-editor__sequence'
                value={state.eventSequenceId}
                options={sequenceOptions}
                aria-label={sequenceStrings.eventSequence}
                onChange={value => applyAndRun({ ...state, eventSequenceId: value as string })} />

            <QueryFilterBar
                state={state}
                eventStore={eventStore}
                eventTypeIds={eventTypeIds}
                onChange={onChange}
                onFiltersSettled={applyAndRun} />
        </div>
    );

    return (
        <div className='query-editor'>
            <div className='query-editor__toolbar flex items-center justify-between gap-3'>
                <ActionMenubar aria-label={sequenceStrings.title} model={menuItems} />
                {toolbarEnd}
            </div>

            <div className='query-editor__results'>
                <Allotment className='h-full' proportionalLayout={false}>
                    <Allotment.Pane className='flex-grow'>
                        <EventsTable
                            key={runCount}
                            queryArguments={runArguments}
                            sortBy={state.sortBy}
                            descending={state.descending}
                            selection={selectedEvent}
                            onSelectionChange={setSelectedEvent}
                            onSort={(sortBy, descending) => applyAndRun({ ...state, sortBy, descending })} />
                    </Allotment.Pane>

                    {selectedEvent &&
                        <Allotment.Pane preferredSize='450px'>
                            <EventDetails item={selectedEvent} onRefresh={run} />
                        </Allotment.Pane>}
                </Allotment>
            </div>

            <AppendEventWrapper />
            <RedactEventWrapper />
            <ReviseWrapper />
        </div>
    );
};
