// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequencesViewModel } from './SequencesViewModel';
import { withViewModel } from 'MVVM/withViewModel';
import { Page } from 'Components/Common/Page';
import { Button } from 'primereact/button';
import { Bookmark } from './Bookmark/Bookmark';
import { TabMenu } from 'primereact/tabmenu';
import { MenuItem } from 'primereact/menuitem';
import { Query } from './Query';
import { Splitter, SplitterPanel } from 'primereact/splitter';

export const Sequences = withViewModel(SequencesViewModel, ({ viewModel }) => {
    const openQueries = viewModel.queries.map(_ => {
        return {
            label: _.name
        } as MenuItem;
    });

    const menuItems: MenuItem[] = [...openQueries, {
        template: () =>
            <>
                <Button
                    text
                    icon='pi pi-plus'
                    onClick={() => viewModel.addQuery()}
                />

            </>
    }]

    return (
        <Page
            title='Event Sequences'
            mainClassName={'overflow-hidden h-full'}>

            <Splitter className="h-full flex">
                <SplitterPanel className="flex shrink" size={1}>
                    <Bookmark />
                </SplitterPanel>
                <SplitterPanel className="h-full w-full grow">
                    <div className="flex flex-col w-full">


                        <TabMenu
                            className="pb-1"
                            onTabChange={(e) => viewModel.currentQuery = viewModel.queries[e.index]}
                            model={menuItems} />

                        <div className="flex flex-col h-full contentBackground">
                            {viewModel.currentQuery &&
                                <Query query={viewModel.currentQuery} />
                            }
                        </div>
                    </div>
                </SplitterPanel>
            </Splitter>
        </Page>
    );
});
