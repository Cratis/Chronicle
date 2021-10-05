// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';

import {
    IColumn,
    IDetailsListStyles,
    DetailsList,
    Selection,
    SelectionMode,
    Stack
} from '@fluentui/react';
import {
    Pagination
} from '@fluentui/react-experiments';

export type EventSelected = (item: any) => void;

export interface EventListProps {
    items: any[];
    onEventSelected?: EventSelected;
}

const eventListColumns: IColumn[] = [

    {
        key: 'sequence',
        name: 'Sequence',
        fieldName: 'sequence',
        minWidth: 100,
        maxWidth: 100
    },
    {
        key: 'name',
        name: 'Name',
        fieldName: 'name',
        minWidth: 200
    },
    {
        key: 'occurred',
        name: 'Occurred',
        fieldName: 'occurred',
        minWidth: 300
    }
];

const gridStyles: Partial<IDetailsListStyles> = {
    root: {
        overflowX: 'scroll',
        selectors: {
            '& [role=grid]': {
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


export const EventList = (props: EventListProps) => {
    const selection = useMemo(
        () => new Selection({
            selectionMode: SelectionMode.single,
            onSelectionChanged: () => {
                const selected = selection.getSelection();
                if (selected.length === 1) {
                    props.onEventSelected?.(selected[0]);
                }
            },
            items: props.items
        }), [props.items]);

    return (
        <Stack verticalFill>
            <Stack.Item grow>
                <DetailsList
                    styles={gridStyles}
                    columns={eventListColumns}
                    selection={selection}
                    items={props.items}
                />
            </Stack.Item>
            <Stack.Item>
                {/* https://codepen.io/micahgodbolt/pen/jXNLvB */}
                <Pagination
                    format="buttons"
                    pageCount={4}
                    itemsPerPage={20} />
            </Stack.Item>

        </Stack>
    );
};
