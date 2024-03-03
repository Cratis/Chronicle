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
import { Allotment } from 'allotment';

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
            title='Sequences'
            mainClassName={'overflow-hidden h-full'}>

            <Allotment className="h-full" proportionalLayout={false}>
                <Allotment.Pane preferredSize="270px">
                    <Bookmark />
                </Allotment.Pane>
                <Allotment.Pane className="h-full">
                    <div className="flex flex-col h-full w-full">
                        <TabMenu
                            className="pb-1"
                            onTabChange={(e) => viewModel.currentQuery = viewModel.queries[e.index]}
                            model={menuItems} />

                        <div className="flex flex-col h-full">
                            {viewModel.currentQuery &&
                                <Query query={viewModel.currentQuery} />
                            }
                        </div>
                    </div>
                </Allotment.Pane>
            </Allotment>
        </Page>
    );
});
