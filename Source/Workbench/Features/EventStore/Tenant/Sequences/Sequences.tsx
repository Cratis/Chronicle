// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SequencesViewModel } from './SequencesViewModel';
import { withViewModel } from 'MVVM/withViewModel';
import { Page } from 'Components/Common/Page';
import { Button } from 'primereact/button';
import { OverlayPanel } from 'primereact/overlaypanel';
import { Bookmark } from './Bookmark/Bookmark';
import { TabMenu } from 'primereact/tabmenu';
import { QueryActions } from './QueryActions';
import { EventList } from './EventList';
import { useRef } from 'react';
import { MenuItem } from 'primereact/menuitem';

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
                            icon='pi pi-book'
                            onClick={(e) => overlayPanelRef.current?.toggle(e)}
                        />
                        <OverlayPanel ref={overlayPanelRef}>
                            <Bookmark />
                        </OverlayPanel>
                    </div>
                    <TabMenu className="pb-1" model={viewModel.queries.map(_ => {
                        return {
                            label: _.name
                        } as MenuItem;
                    })} />
                </div>
            </div>

            <div className="flex flex-col h-full contentBackground">
                <QueryActions />
                <div className={'flex-1 overflow-hidden mt-4'}>
                    <EventList events={[]} />
                </div>
            </div>

        </Page>
    );
});
