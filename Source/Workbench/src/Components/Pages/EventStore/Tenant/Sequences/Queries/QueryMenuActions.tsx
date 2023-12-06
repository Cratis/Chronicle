import { EChartExample } from './EChartExample';
import { Menubar } from 'primereact/menubar';
import { SaveQuery } from './SaveQuery';
import css from './Queries.module.css';
import { useState } from 'react';
export interface QueryMenuActionsProps {
    val?: unknown;
}

export const QueryMenuActions = (props: QueryMenuActionsProps) => {
    const { val } = props;
    const [showChart, setShowChart] = useState(false);

    const showChartHandler = () => {
        setShowChart((prev) => !prev);
    };

    const items = [
        {
            label: 'Run',
            icon: 'pi pi-play',
        },
        {
            label: 'Time range',
            icon: 'pi pi-chart-line',
            command: showChartHandler,
        },
        {
            label: 'Save',
            icon: 'pi pi-save',
        },
    ];

    return (
        <div className={css.actions}>
            <div>
                <Menubar model={items} />
            </div>
            {showChart && <EChartExample />}

            <SaveQuery />
        </div>
    );
};
