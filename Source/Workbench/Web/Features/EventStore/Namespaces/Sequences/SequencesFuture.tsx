// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequencesViewModel } from './SequencesViewModel';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { Page } from 'Components/Common/Page';
import { Bookmark } from './Bookmark/Bookmark';
import { Query } from './Query';
import { Allotment } from 'allotment';
import { TabPanel, TabView } from 'primereact/tabview';

export const SequencesFuture = withViewModel(SequencesViewModel, ({ viewModel }) => {
    return (
        <Page title='Sequences'>
            <Allotment className="h-full" proportionalLayout={false}>
                <Allotment.Pane preferredSize="270px">
                    <Bookmark />
                </Allotment.Pane>
                <Allotment.Pane className="h-full">
                    <div className="flex flex-col h-full w-full">
                        <TabView>
                            <TabPanel header="Query 1" closable>
                                <Query query={viewModel.currentQuery!} />
                            </TabPanel>
                            <TabPanel header="Finding Nemo" closable>
                                <Query query={viewModel.currentQuery!} />
                            </TabPanel>
                        </TabView>
                    </div>
                </Allotment.Pane>
            </Allotment>
        </Page>
    );
});
