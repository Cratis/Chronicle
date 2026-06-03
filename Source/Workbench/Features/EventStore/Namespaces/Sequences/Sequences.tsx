// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Page } from 'Components/Common/Page';
import { Bookmark } from './Bookmark/Bookmark';
import { Query } from './Query';
import { QueryDefinition } from './QueryDefinition';
import { Allotment } from 'allotment';
import { TabPanel, TabView } from 'primereact/tabview';
import { Guid } from '@cratis/fundamentals';
import { AllEventSequenceQueryFolders } from 'Api/SequenceQueries/Listing/AllEventSequenceQueryFolders';
import { AddEventSequenceQuery } from 'Api/SequenceQueries/Adding/AddEventSequenceQuery';
import { AddEventSequenceQueryFolder } from 'Api/SequenceQueries/Adding/AddEventSequenceQueryFolder';
import { AddEventSequenceQueryFolderForUser } from 'Api/SequenceQueries/Adding/AddEventSequenceQueryFolderForUser';
import { EventSequenceQueryFolder } from 'Api/SequenceQueries/Listing/EventSequenceQueryFolder';
import { SequenceQueryFilter } from 'Api/SequenceQueries/SequenceQueryFilter';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';

const flattenQueries = (folders: EventSequenceQueryFolder[]): QueryDefinition[] =>
    folders.flatMap(folder => (folder.queries ?? []).map(query => ({
        id: query.queryId?.toString(),
        name: query.name,
        eventSequenceId: query.eventSequenceId,
        eventSourceId: query.filter?.eventSourceId ?? undefined,
        eventTypes: query.filter?.eventTypes ?? [],
        startTime: query.filter?.startTime ? new Date(query.filter.startTime as unknown as string) : undefined,
        endTime: query.filter?.endTime ? new Date(query.filter.endTime as unknown as string) : undefined,
        folderId: folder.folderId?.toString(),
        isUnsaved: false,
    } as QueryDefinition)));

export const Sequences = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const eventStore = params.eventStore!;
    const namespace = params.namespace!;

    const [folders, refresh] = AllEventSequenceQueryFolders.use({ eventStore, namespace });

    const [queries, setQueries] = useState<QueryDefinition[]>([]);
    const [activeTabIndex, setActiveTabIndex] = useState<number>(0);

    const savedQueries = useMemo(() => flattenQueries(folders.data ?? []), [folders.data]);

    useEffect(() => {
        setQueries(previous => {
            const drafts = previous.filter(query => query.isUnsaved === true);
            return [...savedQueries, ...drafts];
        });
    }, [savedQueries]);

    const handleAddFolder = useCallback(async (group: 'myQueries' | 'sharedQueries', name: string) => {
        const folderId = Guid.create();
        if (group === 'sharedQueries') {
            const command = new AddEventSequenceQueryFolder();
            command.eventStore = eventStore;
            command.namespace = namespace;
            command.folderId = folderId;
            command.name = name;
            await command.execute();
        } else {
            const command = new AddEventSequenceQueryFolderForUser();
            command.eventStore = eventStore;
            command.namespace = namespace;
            command.folderId = folderId;
            command.name = name;
            await command.execute();
        }
        refresh();
    }, [eventStore, namespace, refresh]);

    const handleAddQueryForGroup = useCallback((group: 'myQueries' | 'sharedQueries') => {
        const allFolders = folders.data ?? [];
        const systemOwner = 'System';
        const groupFolder = group === 'sharedQueries'
            ? allFolders.find(folder => folder.owner === systemOwner)
            : allFolders.find(folder => folder.owner !== systemOwner);

        setQueries(previous => {
            const draft: QueryDefinition = {
                id: undefined,
                name: `Query ${previous.length + 1}`,
                eventSequenceId: 'event-log',
                folderId: groupFolder?.folderId?.toString(),
                isUnsaved: true,
            };
            const next = [...previous, draft];
            setActiveTabIndex(next.length - 1);
            return next;
        });
    }, [folders.data]);

    const handleSaveQuery = useCallback(async (updated: QueryDefinition) => {
        if (updated.folderId === undefined || updated.isUnsaved !== true) {
            return;
        }
        const queryId = Guid.create();
        const command = new AddEventSequenceQuery();
        command.eventStore = eventStore;
        command.namespace = namespace;
        command.queryId = queryId;
        command.folderId = Guid.parse(updated.folderId);
        command.name = updated.name;
        command.eventSequenceId = updated.eventSequenceId;

        const filter = new SequenceQueryFilter();
        filter.eventSourceId = updated.eventSourceId ?? '';
        filter.eventTypes = updated.eventTypes ?? [];
        filter.startTime = updated.startTime ?? null as unknown as Date;
        filter.endTime = updated.endTime ?? null as unknown as Date;
        command.filter = filter;

        await command.execute();
        refresh();
    }, [eventStore, namespace, refresh]);

    const tabHeader = (query: QueryDefinition) => (
        <span className="px-1">
            {query.name}
            {query.isUnsaved === true && <span className="ml-1">*</span>}
        </span>
    );

    return (
        <Page title='Sequences'>
            <Allotment className="h-full" proportionalLayout={false}>
                <Allotment.Pane preferredSize="270px">
                    <Bookmark
                        folders={folders.data ?? []}
                        onAddFolder={handleAddFolder}
                        onAddQuery={handleAddQueryForGroup}
                    />
                </Allotment.Pane>
                <Allotment.Pane className="h-full">
                    <TabView
                        activeIndex={activeTabIndex}
                        onTabChange={(event) => setActiveTabIndex(event.index)}
                        pt={{
                            root: { className: 'h-full flex flex-col' },
                            panelContainer: { className: 'flex-1 min-h-0 overflow-hidden p-0' },
                        }}>
                        {queries.map((query, index) => (
                            <TabPanel
                                key={query.id ?? `draft-${index}`}
                                header={tabHeader(query)}
                                contentClassName="h-full">
                                <Query query={query} onSave={handleSaveQuery} />
                            </TabPanel>
                        ))}
                    </TabView>
                </Allotment.Pane>
            </Allotment>
        </Page>
    );
};
