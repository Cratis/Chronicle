// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequencesViewModel } from './SequencesViewModel';
import { withViewModel } from 'MVVM/withViewModel';
import { Page } from 'Components/Common/Page';
import { Button } from 'primereact/button';
import { OverlayPanel } from 'primereact/overlaypanel';
import { Bookmark } from './Bookmark/Bookmark';
import { TabMenu } from 'primereact/tabmenu';
import { useRef } from 'react';
import { MenuItem } from 'primereact/menuitem';
import { Query } from './Query';

export const Sequences = withViewModel(SequencesViewModel, ({ viewModel }) => {
    const overlayPanelRef = useRef<OverlayPanel>(null);

    return (
        <Page
            title='Event Sequences'
            mainClassName={'overflow-hidden flex flex-col h-full'}>

            <div className="flex flex-col">
                <div className="flex">
                    <div className="p-1">
                        <Button
                            text
                            icon='pi pi-book'
                            onClick={(e) => overlayPanelRef.current?.toggle(e)}
                        />
                        <Button
                            text
                            icon='pi pi-plus'
                            onClick={() => viewModel.addQuery()}
                        />
                        <OverlayPanel ref={overlayPanelRef}>
                            <Bookmark />
                        </OverlayPanel>
                    </div>
                    <TabMenu
                        className="pb-1"

                        onTabChange={(e) => viewModel.currentQuery = viewModel.queries[e.index]}
                        model={viewModel.queries.map(_ => {
                            return {
                                label: _.name
                            } as MenuItem;
                        })} />
                </div>
            </div>

            <div className="flex flex-col h-full contentBackground">
                {viewModel.currentQuery &&
                    <Query query={viewModel.currentQuery} />
                }
            </div>
        </Page>
    );
});
