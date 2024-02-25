// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OverlayPanel } from 'primereact/overlaypanel';
import { useRef } from 'react';
import { ImDatabase } from "react-icons/im";

export const EventStore = () => {
    const selectEventStorePanel = useRef<OverlayPanel>(null);
    return (<div className='flex-1 cursor-pointer' onClick={(e) => selectEventStorePanel.current?.toggle(e)}>
        <div className={'flex justify-end gap-3 '}>
            <ImDatabase size={25} />

            <OverlayPanel ref={selectEventStorePanel}>
            </OverlayPanel>

        </div>
    </div>)
};
