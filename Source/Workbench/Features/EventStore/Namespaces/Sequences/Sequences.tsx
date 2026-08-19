// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useMemo, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { Allotment } from 'allotment';
import { DialogResult, useDialog } from '@cratis/arc.react/dialogs';
import { Tabs, type TabsRootChangeEvent } from 'primereact/tabs';
import { Button } from 'Components/Button';
import * as faIcons from 'react-icons/fa6';
import strings from 'Strings';
import { Page } from 'Components/Common/Page';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { AllEventSequences } from 'Features/Sequences';
import { AllEventTypes } from 'Features/EventTypes';
import { AllQueryFolders } from 'Features/SequenceQueries';
import { AllSequenceQueries } from 'Features/SequenceQueries';
import { SequenceQueryScope } from 'Features/Concepts/SequenceQueries';
import { getDistinctEventTypeOptions } from './getDistinctEventTypeOptions';
import { QueryEditor } from './QueryEditor/QueryEditor';
import { SaveQueryDialog, SaveQueryDialogResponse } from './QueryEditor/SaveQueryDialog';
import { SequenceQueryState } from './QueryEditor/SequenceQueryState';
import { OpenQuery, hasUnsavedChanges } from './QueryEditor/OpenQuery';
import { saveSequenceQuery } from './QueryEditor/saveSequenceQuery';
import { useOpenQueries } from './QueryEditor/useOpenQueries';
import { QueryHierarchy } from './QueryHierarchy/QueryHierarchy';
import { foldersInScope } from './QueryHierarchy/buildQueryTree';
import { useHierarchyWidth, minimumHierarchyWidth } from './QueryHierarchy/useHierarchyWidth';
import { useQueryHierarchyActions } from './QueryHierarchy/useQueryHierarchyActions';
import { QueryTabHeader } from './QueryTabHeader';
import './Sequences.css';

/**
 * The event sequence workspace: a hierarchy of the queries the user has saved on the left, and the
 * ones they currently have open as tabs on the right.
 * @returns The rendered page.
 */
export const Sequences = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const eventStore = params.eventStore!;
    const namespace = params.namespace!;
    const sequenceStrings = strings.eventStore.namespaces.sequences;

    const [eventTypes] = AllEventTypes.use({ eventStore });
    const eventTypeIds = useMemo(
        () => getDistinctEventTypeOptions(eventTypes.data).map(option => option.value),
        [eventTypes.data]
    );

    const [eventSequences] = AllEventSequences.use({ eventStore });

    const [savedQueries, performSavedQueries] = AllSequenceQueries.use({ eventStore });
    const queriesForNamespace = useMemo(
        () => savedQueries.data.filter(query => query.namespace === namespace),
        [savedQueries.data, namespace]
    );

    const [savedFolders, performSavedFolders] = AllQueryFolders.use({ eventStore });
    const foldersForNamespace = useMemo(
        () => savedFolders.data.filter(folder => folder.namespace === namespace),
        [savedFolders.data, namespace]
    );

    // Re-reading has to repeat the arguments - a query performed without them narrows nothing and
    // is rejected before it reaches the server, which would silently leave the hierarchy stale.
    const performRef = useRef({ queries: performSavedQueries, folders: performSavedFolders });
    performRef.current = { queries: performSavedQueries, folders: performSavedFolders };
    const refreshSavedQueries = useCallback(() => {
        performRef.current.queries({ eventStore });
        performRef.current.folders({ eventStore });
    }, [eventStore]);

    const { open, activeIndex, setActiveIndex, update, markSaved, applyPersisted, add, close, closeById, openSaved } =
        useOpenQueries(queriesForNamespace, namespace, sequenceStrings.newQuery, !savedQueries.isPerforming);

    const hierarchy = useQueryHierarchyActions(
        eventStore, namespace, queriesForNamespace, foldersForNamespace, refreshSavedQueries, closeById, applyPersisted);
    const [SaveQueryWrapper, showSaveQuery] = useDialog<SaveQueryDialogResponse>(SaveQueryDialog);

    const persist = useCallback(async (state: SequenceQueryState) => {
        if (await saveSequenceQuery(state, eventStore)) {
            markSaved(state);
            refreshSavedQueries();
        }
    }, [eventStore, markSaved, refreshSavedQueries]);

    // The first save is where a query gets its name and its place in the hierarchy; every save
    // after that just writes it back where it already lives.
    const save = useCallback(async (state: SequenceQueryState, isFirstSave: boolean) => {
        if (!isFirstSave) {
            await persist(state);
            return;
        }

        const [result, response] = await showSaveQuery({
            name: state.name,
            scope: state.scope,
            folder: state.folder,
            folders: foldersInScope(queriesForNamespace, state.scope, foldersForNamespace)
        });
        if (result !== DialogResult.Ok || !response) return;

        await persist({ ...state, ...response });
    }, [persist, showSaveQuery, queriesForNamespace, foldersForNamespace]);

    // Renaming from the tab writes the new name back there and then, the same as renaming the node
    // in the hierarchy does - a name is what a query is called rather than an edit to what it asks
    // for. Only the name is written, so anything else in flight stays unsaved; and a query that has
    // never been saved has nowhere to write it to yet, so it just carries the name until it is.
    const rename = useCallback(async (index: number, query: OpenQuery, name: string) => {
        if (query.saved === null) {
            update(index, { ...query.state, name });
            return;
        }

        if (!await saveSequenceQuery({ ...query.saved, name }, eventStore)) return;

        applyPersisted(state => (state.id === query.state.id ? { ...state, name } : state));
        refreshSavedQueries();
    }, [eventStore, update, applyPersisted, refreshSavedQueries]);

    const hierarchyWidth = useHierarchyWidth();
    const activeQuery = open[activeIndex];

    return (
        <Page title={sequenceStrings.title}>
            <div className='sequences'>
                <Allotment className='h-full' proportionalLayout={false} onChange={hierarchyWidth.onChange}>
                    <Allotment.Pane preferredSize={hierarchyWidth.width} minSize={minimumHierarchyWidth}>
                        <QueryHierarchy
                            queries={queriesForNamespace}
                            folders={foldersForNamespace}
                            selectedQueryId={activeQuery?.state.id ?? null}
                            renamingId={hierarchy.renamingId}
                            onRenamingIdChange={hierarchy.setRenamingId}
                            onOpen={openSaved}
                            onNewQuery={(scope, folder) => add(scope, folder)}
                            onNewFolder={hierarchy.newFolder}
                            onRenameQuery={hierarchy.renameQuery}
                            onRenameFolder={hierarchy.renameFolder}
                            onDeleteQuery={hierarchy.deleteQuery}
                            onDeleteFolder={hierarchy.deleteFolder} />
                    </Allotment.Pane>

                    <Allotment.Pane className='flex-grow'>
                        <div className='sequences__tabs'>
                            <Tabs.Root
                                className='sequences__tabview'
                                value={activeIndex}
                                onValueChange={(event: TabsRootChangeEvent) => setActiveIndex(Number(event.value))}>
                                <Tabs.List>
                                    {open.map((query, index) => (
                                        // Rendered as a div rather than the default button so the close
                                        // control can sit inside the tab without nesting one button in another.
                                        <Tabs.Tab key={query.state.id} as='div' value={index}>
                                            <QueryTabHeader
                                                name={query.state.name || sequenceStrings.newQuery}
                                                hasUnsavedChanges={hasUnsavedChanges(query)}
                                                onRename={name => rename(index, query, name)} />
                                            <button
                                                type='button'
                                                className='ml-2 opacity-60 hover:opacity-100'
                                                aria-label={sequenceStrings.actions.closeQuery}
                                                title={sequenceStrings.actions.closeQuery}
                                                onClick={event => { event.stopPropagation(); close(index); }}>
                                                <faIcons.FaXmark />
                                            </button>
                                        </Tabs.Tab>
                                    ))}
                                </Tabs.List>
                                <Tabs.Panels className='flex flex-col flex-1 min-h-0 p-0'>
                                    {open.map((query, index) => (
                                        <Tabs.Panel key={query.state.id} value={index} className='flex-1 min-h-0'>
                                            <QueryEditor
                                                state={query.state}
                                                eventStore={eventStore}
                                                eventTypeIds={eventTypeIds}
                                                eventSequenceIds={eventSequences.data.map(eventSequence => eventSequence.name)}
                                                hasUnsavedChanges={hasUnsavedChanges(query)}
                                                onChange={state => update(index, state)}
                                                onSave={() => save(query.state, query.saved === null)} />
                                        </Tabs.Panel>
                                    ))}
                                </Tabs.Panels>
                            </Tabs.Root>

                            <Button
                                className='sequences__add'
                                icon='pi pi-plus'
                                text
                                aria-label={sequenceStrings.actions.newQuery}
                                tooltip={sequenceStrings.actions.newQuery}
                                tooltipOptions={{ position: 'left', className: 'sequences__add-tooltip' }}
                                onClick={() => add(SequenceQueryScope.user, '')} />
                        </div>
                    </Allotment.Pane>
                </Allotment>
            </div>

            <SaveQueryWrapper />
        </Page>
    );
};
