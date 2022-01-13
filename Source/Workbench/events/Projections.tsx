// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState } from 'react';
import { AllProjections } from 'API/events/projections/AllProjections';
import { Projection } from 'API/events/projections/Projection';


import {
    CommandBar,
    DetailsList,
    IColumn,
    ICommandBarItemProps,
    IDetailsListStyles,
    Pivot,
    PivotItem,
    Selection,
    SelectionMode,
    Stack
} from '@fluentui/react';
import { Collections } from './Collections';

const gridStyles: Partial<IDetailsListStyles> = {
    root: {
        height: '100%',
        overflowX: 'scroll',
        selectors: {
            '& [role=grid]': {
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'start'
            },
        },
    },
    headerWrapper: {
        flex: '0 0 auto',
    },
    contentWrapper: {
        flex: '1 1 auto',
        overflowX: 'hidden',
        overflowY: 'auto',
        height: '300px'
    },
};

const columns: IColumn[] = [
    {
        key: 'name',
        name: 'Name',
        fieldName: 'name',
        minWidth: 200,
        isResizable: true
    },
    {
        key: 'state',
        name: 'State',
        fieldName: 'state',
        minWidth: 200,
        isResizable: true
    },
    {
        key: 'jobInformation',
        name: 'Job Information',
        fieldName: 'jobInformation',
        minWidth: 200,
        isResizable: true
    },
    {
        key: 'position',
        name: 'Positions',
        fieldName: 'positions',
        minWidth: 200,
        isResizable: true
    }
];

const rewind = (id: string) => {
    (async () => {
        const response = await fetch(`/api/events/projections/${id}/rewind`, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });
    })();
};


export const Projections = () => {
    const [projections] = AllProjections.use();
    const commandBarItems: ICommandBarItemProps[] = [
        {
            key: 'add',
            name: 'Add',
            iconProps: { iconName: 'Add' }
        }
    ];
    const [selected, setSelected] = useState<Projection>();

    const selection = useMemo(
        () => new Selection({
            selectionMode: SelectionMode.single,
            onSelectionChanged: () => {
                const selected = selection.getSelection();
                if (selected.length === 1) {
                    setSelected(selected[0] as Projection);
                } else {
                    setSelected(undefined);
                }
            },
        }), [projections.data]);

    if (selected) {
        commandBarItems.push({
            key: 'rewind',
            name: 'Rewind',
            iconProps: { iconName: 'Rewind' },
            onClick: () => rewind(selected.id)
        });
    }

    return (
        <Stack style={{ height: '100%' }}>
            <Stack.Item>
                <CommandBar items={commandBarItems} />
            </Stack.Item>
            <Stack.Item grow={1}>
                <DetailsList
                    items={projections.data}
                    columns={columns}
                    selection={selection}
                    styles={gridStyles} />
            </Stack.Item>
            {selected &&
                <Stack.Item grow={1}>
                    <Pivot linkFormat="links">
                        <PivotItem headerText="Collections">
                            <Collections projectionId={selected.id}/>
                        </PivotItem>
                    </Pivot>
                </Stack.Item>
            }
        </Stack >
    );
};
