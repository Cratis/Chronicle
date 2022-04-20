// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useRef, useEffect } from 'react';

import {
    IColumn,
    Selection,
    SelectionMode,
} from '@fluentui/react';

import { default as styles } from './EventList.module.scss';
import { ScrollableDetailsList } from '@aksio/cratis-fluentui';
import { AppendedEvent } from 'API/events/store/sequence/AppendedEvent';
import { EventType } from 'API/events/types/EventType';

export type EventSelected = (item: any) => void;

export interface EventListProps {
    items: any[];
    eventTypes: EventType[];
    onEventSelected?: EventSelected;
}

export const EventList = (props: EventListProps) => {
    const eventListColumns: IColumn[] = [

        {
            key: 'sequence',
            name: 'Sequence',
            minWidth: 100,
            maxWidth: 100,
            onRender: (item: AppendedEvent) => {
                return (
                    <span>{item.metadata.sequenceNumber}</span>
                );
            }
        },
        {
            key: 'name',
            name: 'Name',
            minWidth: 200,
            onRender: (item: AppendedEvent) => {
                const eventType = props.eventTypes.find(_ => _.identifier == item.metadata.type.id);
                return (
                    <span>{eventType?.name || '[n/a]'}</span>
                );
            }
        },
        {
            key: 'occurred',
            name: 'Occurred',
            minWidth: 300,
            onRender: (item: AppendedEvent) => {
                return (
                    <span>{new Date(item.context.occurred).toLocaleString()}</span>
                );
            }
        }
    ];

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
