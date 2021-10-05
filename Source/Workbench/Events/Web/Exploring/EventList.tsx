// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    IColumn,
    IDetailsListStyles,
    DetailsList,
    SelectionMode,
    Stack
} from '@fluentui/react';
import {
    Pagination
} from '@fluentui/react-experiments';


export interface EventListProps {
    items: any[];
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

    return (
        <Stack>
            <Stack.Item grow>
                <DetailsList
                    styles={gridStyles}
                    columns={eventListColumns}
                    items={props.items} selectionMode={SelectionMode.single}
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
