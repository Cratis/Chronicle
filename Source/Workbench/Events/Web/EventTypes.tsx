// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    CommandBar,
    DetailsList,
    IColumn,
    ICommandBarItemProps,
    Pivot,
    PivotItem,
    SelectionMode
} from '@fluentui/react';

import { default as styles } from './EventTypes.module.scss';


const commandBarItems: ICommandBarItemProps[] = [
    {
        key: 'newItem',
        text: 'New',
        iconProps: { iconName: 'Add' }
    }
];

const eventTypesColumns: IColumn[] = [
    {
        key: 'name',
        name: 'Name',
        fieldName: 'name',
        minWidth: 200
    },
    {
        key: 'generations',
        name: 'Generations',
        fieldName: 'generations',
        minWidth: 100
    }
];

const eventSchemaColumns: IColumn[] = [
    {
        key: 'property',
        name: 'Property',
        fieldName: 'property',
        minWidth: 200
    },
    {
        key: 'type',
        name: 'Type',
        fieldName: 'type',
        minWidth: 100
    }
];


export const EventTypes = () => {
    const eventTypes: any[] = [
        {
            name: 'DebitAccountOpened',
            generations: 3
        }
    ];

    const eventSchemaGen1: any[] = [
        {
            property: 'Account',
            type: 'UID'
        },
        {
            property: 'Name',
            type: 'String'
        }
    ];

    const eventSchemaGen2: any[] = [
        {
            property: 'Account',
            type: 'UID'
        },
        {
            property: 'Name',
            type: 'String'
        },
        {
            property: 'Owner',
            type: 'UID'
        }
    ];
    const eventSchemaGen3: any[] = [
        {
            property: 'Account',
            type: 'UID'
        },
        {
            property: 'Name',
            type: 'String'
        },
        {
            property: 'Owner',
            type: 'UID'
        },
        {
            property: 'Parent',
            type: 'UID'
        }
    ];

    return (
        <div className={styles.container}>
            <div className={styles.commandBar}>
                <CommandBar items={commandBarItems} />
            </div>
            <div className={styles.eventList}>
                <DetailsList items={eventTypes} columns={eventTypesColumns} selectionMode={SelectionMode.single} />
            </div>
            <div className={styles.eventDetails}>
                <Pivot linkFormat="tabs" defaultSelectedKey="2">
                    <PivotItem headerText="1">
                        <DetailsList items={eventSchemaGen1} columns={eventSchemaColumns} selectionMode={SelectionMode.none} />
                    </PivotItem>
                    <PivotItem headerText="2">
                        <DetailsList items={eventSchemaGen2} columns={eventSchemaColumns} selectionMode={SelectionMode.none} />
                    </PivotItem>
                    <PivotItem headerText="3">
                        <DetailsList items={eventSchemaGen3} columns={eventSchemaColumns} selectionMode={SelectionMode.none} />
                    </PivotItem>
                </Pivot>
            </div>
        </div>
    );
}
