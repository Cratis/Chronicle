// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from 'primereact/button';
import * as faIcons from 'react-icons/fa6';
import strings from 'Strings';
import { SequenceQuery } from 'Api/SequenceQueries/SequenceQuery';
import { SequenceQueryScope } from 'Api/SequenceQueries/SequenceQueryScope';
import { DeleteSequenceQuery } from 'Api/SequenceQueries/DeleteSequenceQuery';
import './SavedQueries.css';

/**
 * Props for {@link SavedQueries}.
 */
export interface SavedQueriesProps {
    /** The saved queries visible to the user. */
    queries: SequenceQuery[];
    /** The event store the queries belong to. */
    eventStore: string;
    /** The identifiers of the queries already open, so they can be marked as such. */
    openIds: string[];
    /** Called when the user picks a query to open. */
    onOpen: (query: SequenceQuery) => void;
    /** Called after a query was deleted, so the list can be re-read. */
    onDeleted: () => void;
}

/**
 * The list of queries the user has saved, plus the ones other people shared with everyone.
 * @param props The {@link SavedQueriesProps}.
 * @returns The rendered list.
 */
export const SavedQueries = ({ queries, eventStore, openIds, onOpen, onDeleted }: SavedQueriesProps) => {
    const sequenceStrings = strings.eventStore.namespaces.sequences;

    const remove = async (query: SequenceQuery) => {
        const command = new DeleteSequenceQuery();
        command.eventStore = eventStore;
        command.id = query.id;

        const result = await command.execute();
        if (result.isSuccess) {
            onDeleted();
        }
    };

    return (
        <aside className='saved-queries'>
            <h2 className='saved-queries__title'>{sequenceStrings.savedQueries}</h2>

            {queries.length === 0 && (
                <p className='saved-queries__empty'>{sequenceStrings.noSavedQueries}</p>
            )}

            <ul className='saved-queries__list'>
                {queries.map(query => (
                    <li
                        key={query.id}
                        className={`saved-queries__item ${openIds.includes(query.id) ? 'is-open' : ''}`}>
                        <button
                            type='button'
                            className='saved-queries__open'
                            onClick={() => onOpen(query)}>
                            <span className='saved-queries__name'>{query.name}</span>
                            {query.scope === SequenceQueryScope.everyone && (
                                <faIcons.FaUsers
                                    className='saved-queries__shared'
                                    title={sequenceStrings.scope.everyone} />
                            )}
                        </button>
                        <Button
                            className='saved-queries__delete'
                            icon='pi pi-trash'
                            text
                            aria-label={sequenceStrings.actions.deleteQuery}
                            tooltip={sequenceStrings.actions.deleteQuery}
                            onClick={() => remove(query)} />
                    </li>
                ))}
            </ul>
        </aside>
    );
};
