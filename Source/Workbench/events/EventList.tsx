// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useRef, useEffect } from 'react';

import {
    IColumn,
    Selection,
    SelectionMode,
} from '@fluentui/react';

import { default as styles } from './EventList.module.scss';
import { ScrollableDetailsList } from '@aksio/fluentui';

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

    useEffect(() => {
        const detailsList = document.querySelector('.ms-DetailsList.eventList');
        if (detailsList) {
            detailsList.parentElement!.style!.height = '100%';
        }
    }, []);

    return (
        <ScrollableDetailsList
            columns={eventListColumns}
            selection={selection}
            items={props.items}
        />
    );
};
