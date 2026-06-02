// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequencesViewModel } from './SequencesViewModel';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { Page } from 'Components/Common/Page';
import { Bookmark } from './Bookmark/Bookmark';
import { Query } from './Query';
import { Allotment } from 'allotment';
import { TabPanel, TabView } from 'primereact/tabview';
import { AllEventSequenceQueryFolders } from 'Api/SequenceQueries/Listing/AllEventSequenceQueryFolders';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { Button } from 'primereact/button';
import { useCallback, useEffect } from 'react';

export const Sequences = withViewModel(SequencesViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const eventStore = params.eventStore!;
    const namespace = params.namespace!;

    const [folders, refresh] = AllEventSequenceQueryFolders.use({ eventStore, namespace });

    useEffect(() => {
        if (folders.hasData) {
            viewModel.setFoldersFromApi(folders.data);
        }
    }, [folders.hasData, folders.data, viewModel]);

    const handleSaveFolder = useCallback(async (name: string, shared: boolean) => {
        await viewModel.addFolder(eventStore, namespace, name, shared);
        refresh();
    }, [viewModel, eventStore, namespace, refresh]);

    const handleAddQuery = () => {
        const firstFolder = viewModel.folders[0];
        viewModel.addUnsavedQuery(firstFolder?.folderId?.toString());
    };

    const handleSaveCurrentQuery = useCallback(async () => {
        await viewModel.saveCurrentQuery(eventStore, namespace);
        refresh();
    }, [viewModel, eventStore, namespace, refresh]);

    const tabHeader = (query: { name: string; isUnsaved?: boolean }) => (
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
                        onSaveFolder={handleSaveFolder}
                    />
                </Allotment.Pane>
                <Allotment.Pane className="h-full">
                    <div className="flex flex-col h-full w-full">
                        <div className="flex items-start">
                            <Button
                                icon="pi pi-plus"
                                className="p-button-text p-button-sm flex-shrink-0 mt-1 ml-2"
                                tooltip="New query"
                                tooltipOptions={{ position: 'bottom' }}
                                onClick={handleAddQuery} />
                            <TabView
                                className="flex-1"
                                activeIndex={viewModel.activeTabIndex}
                                onTabChange={(event) => { viewModel.activeTabIndex = event.index; }}>
                                {viewModel.queries.map((query, index) => (
                                    <TabPanel
                                        key={query.id ?? `draft-${index}`}
                                        header={tabHeader(query)}>
                                        <Query query={query} onSave={handleSaveCurrentQuery} />
                                    </TabPanel>
                                ))}
                            </TabView>
                        </div>
                    </div>
                </Allotment.Pane>
            </Allotment>
        </Page>
    );
});
