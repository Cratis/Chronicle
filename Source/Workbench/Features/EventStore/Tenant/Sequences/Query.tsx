import { withViewModel } from 'MVVM/withViewModel';
import { QueryViewModel } from './QueryViewModel';
import { EventList } from './EventList';
import { QueryDefinition } from './QueryDefinition';
import { Menubar } from 'primereact/menubar';
import { OverlayPanel } from 'primereact/overlaypanel';
import { EventHistogram } from './Histogram/Histogram';
import { SequenceSelector } from './SequenceSelector';
import { MenuItem } from 'primereact/menuitem';
import { useRef } from 'react';
import { useToggle } from 'usehooks-ts';

export interface QueryProps {
    query: QueryDefinition;
}

export const Query = withViewModel<QueryViewModel, QueryProps>(QueryViewModel, ({ viewModel }) => {
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
            disabled: !viewModel.hasChanges,
            command: () => viewModel.save()
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

            <div className={'flex-1 overflow-hidden mt-4'}>
                <EventList events={[]} />
            </div>
        </>
    )
});
