// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useDataFrom } from '../useDataFrom';
import { useMemo, useState } from 'react';

import {
    CommandBar,
    DetailsList,
    IColumn,
    ICommandBarItemProps,
    IDetailsListStyles,
    Selection,
    SelectionMode,
    Stack
} from '@fluentui/react';

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
        minWidth: 200
    },
    {
        key: 'position',
        name: 'Position',
        fieldName: 'position',
        minWidth: 200
    }

];


export const Projections = () => {
    const [projections, refreshProjections] = useDataFrom('/api/events/projections');
    const commandBarItems: ICommandBarItemProps[] = [
        {
            key: 'add',
            name: 'Add',
            iconProps: { iconName: 'Add' }
        }
    ];
    const [selected, setSelected] = useState(undefined);


    const selection = useMemo(
        () => new Selection({
            selectionMode: SelectionMode.single,
            onSelectionChanged: () => {
                const selected = selection.getSelection();
                if (selected.length === 1) {
                    setSelected(selected[0]);
                } else {
                    setSelected(undefined);
                }
            },
            items: projections
        }), [projections]);

    if (selected) {
        commandBarItems.push({
            key: 'rewind',
            name: 'Rewind',
            iconProps: { iconName: 'Rewind' }
        });
    }

    return (
        <Stack style={{ height: '100%' }}>
            <Stack.Item>
                <CommandBar items={commandBarItems} />
            </Stack.Item>
            <Stack.Item grow={1}>
                <DetailsList
                    items={projections}
                    columns={columns}
                    selection={selection}
                    styles={gridStyles} />


            </Stack.Item>

        </Stack>
    );
};
