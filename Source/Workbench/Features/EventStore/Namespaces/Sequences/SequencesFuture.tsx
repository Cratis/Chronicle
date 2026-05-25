// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequencesViewModel } from './SequencesViewModel';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { Page } from 'Components/Common/Page';
import { Bookmark } from './Bookmark/Bookmark';
import { Query } from './Query';
import { Allotment } from 'allotment';
import { TabPanel, TabView } from 'primereact/tabview';
import { AllSequenceQueries } from 'Api/SequenceQueries/Listing';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { Button } from 'primereact/button';
import { useEffect } from 'react';
import { observer } from 'mobx-react';

export const SequencesFuture = withViewModel(SequencesViewModel, observer(({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();

    const [savedQueries] = AllSequenceQueries.use({
        eventStore: params.eventStore!,
        namespace: params.namespace!,
    });

    useEffect(() => {
        if (savedQueries.hasData) {
            viewModel.setQueriesFromApi(savedQueries.data);
        }
    }, [savedQueries.hasData, savedQueries.data, viewModel]);

    const handleAddQuery = async () => {
        await viewModel.addQuery(params.eventStore!, params.namespace!);
    };

    const tabHeader = (query: { name: string }) => (
        <span className="px-1">{query.name}</span>
    );

    return (
        <Page title='Sequences'>
            <Allotment className="h-full" proportionalLayout={false}>
                <Allotment.Pane preferredSize="270px">
                    <Bookmark />
                </Allotment.Pane>
                <Allotment.Pane className="h-full">
                    <div className="flex flex-col h-full w-full">
                        <div className="flex items-center">
                            <TabView
                                className="flex-1"
                                activeIndex={viewModel.activeTabIndex}
                                onTabChange={(e) => { viewModel.activeTabIndex = e.index; }}>
                                {viewModel.queries.map((query, i) => (
                                    <TabPanel
                                        key={query.id ?? i}
                                        header={tabHeader(query)}>
                                        <Query query={query} />
                                    </TabPanel>
                                ))}
                            </TabView>
                            <Button
                                icon="pi pi-plus"
                                className="p-button-text p-button-sm ml-1 mr-2 flex-shrink-0"
                                tooltip="New query"
                                tooltipOptions={{ position: 'bottom' }}
                                onClick={handleAddQuery} />
                        </div>
                    </div>
                </Allotment.Pane>
            </Allotment>
        </Page>
    );
}));

