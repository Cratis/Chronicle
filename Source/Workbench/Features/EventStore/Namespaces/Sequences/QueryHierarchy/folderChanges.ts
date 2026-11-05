// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequenceQueryScope } from 'Features/Concepts/SequenceQueries';
import { SequenceQueryState } from '../QueryEditor/SequenceQueryState';
import { rewriteFolderPath } from './folderNaming';

/**
 * Something a folder action asks the event store to do to one item, answering whether it was accepted.
 */
export type Change<TItem> = (item: TItem) => Promise<boolean>;

/**
 * What came of applying a change to a number of items - the ones the event store accepted and the
 * ones it turned down, which are left exactly as they were.
 */
export interface ChangeOutcome<TItem> {
    accepted: TItem[];
    refused: TItem[];
}

/**
 * Apply a change to every item and sort them by whether the event store accepted it.
 *
 * A folder can hold queries that belong to other users, so a refusal is an ordinary result for one
 * item and says nothing about the rest.
 * @param items The items to change.
 * @param change The change to apply to each of them.
 * @returns The accepted and the refused items, each in their original order.
 */
export const applyChanges = async <TItem>(items: TItem[], change: Change<TItem>): Promise<ChangeOutcome<TItem>> => {
    const results = await Promise.all(items.map(change));

    return {
        accepted: items.filter((_, index) => results[index]),
        refused: items.filter((_, index) => !results[index])
    };
};

/**
 * Bring the state of an open query along with a folder that was renamed.
 * @param state The state of the open query.
 * @param scope The scope of the folder that was renamed.
 * @param folder The path the folder had.
 * @param renamed The path the folder has now.
 * @param refused The identifiers of queries the event store refused to move; they stay where they were.
 * @returns The state to carry on with.
 */
export const followRenamedFolder = (
    state: SequenceQueryState,
    scope: SequenceQueryScope,
    folder: string,
    renamed: string,
    refused: ReadonlySet<string>): SequenceQueryState =>
    (state.scope === scope && !refused.has(state.id)
        ? { ...state, folder: rewriteFolderPath(state.folder, folder, renamed) }
        : state);
