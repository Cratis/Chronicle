// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OverlayPanel } from 'primereact/overlaypanel';
import { useRef } from 'react';
import { ImDatabase } from "react-icons/im";
import { ItemsList } from 'Components/ItemsList/ItemsList';
import { EventStoreViewModel } from './EventStoreViewModel';
import { withViewModel } from '@cratis/arc.react.mvvm';

export const EventStore = withViewModel(EventStoreViewModel, ({ viewModel }) => {
    const selectEventStorePanel = useRef<OverlayPanel>(null);

    return (<div className='flex-1 cursor-pointer' onClick={(e) => selectEventStorePanel.current?.toggle(e)}>
        <div className={'flex justify-end gap-3 '}>
            <ImDatabase size={25} />

            <OverlayPanel ref={selectEventStorePanel}>
                <ItemsList<string> items={viewModel.eventStores} />
            </OverlayPanel>
        </div>
    </div>);
});
