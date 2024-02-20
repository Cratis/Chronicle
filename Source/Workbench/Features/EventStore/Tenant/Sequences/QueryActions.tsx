/* Copyright (c) Aksio Insurtech. All rights reserved.
   Licensed under the MIT license. See LICENSE file in the project root for full license information. */

import { EventHistogram } from './Histogram/Histogram';
import { Menubar } from 'primereact/menubar';
import { useToggle } from 'usehooks-ts';
import { MenuItem } from 'primereact/menuitem';
import { useRef } from 'react';
import { OverlayPanel } from 'primereact/overlaypanel';
import { SequenceSelector } from './SequenceSelector';


export const QueryActions = () => {
    const [showTimeRange, toggleTimeRange] = useToggle(false);
    const selectSequencePanelRef = useRef<OverlayPanel>(null);

    const items: MenuItem[] = [
        {
            label: 'Event log',
            icon: 'pi pi-list',
            command: (e) => selectSequencePanelRef.current?.toggle(e.originalEvent),
        },
        {
            label: 'Run',
            icon: 'pi pi-play',
            command: () => { },
        },
        {
            label: 'Time range',
            icon: 'pi pi-chart-line',
            className: showTimeRange ? 'highlight' : '',
            command: () => toggleTimeRange(),
        },
        {
            label: 'Save',
            icon: 'pi pi-save',
            command: () => { }
        },
    ];

    return (
        <>
            <div className="px-4 py-2">
                <Menubar
                    model={items} />
                    <OverlayPanel ref={selectSequencePanelRef}>
                        <SequenceSelector />
                    </OverlayPanel>

                {showTimeRange && <EventHistogram eventLog={''} />}
            </div>
        </>
    );
};
