// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    DetailsList,
    IColumn,
    Pivot,
    PivotItem,
    SelectionMode
} from '@fluentui/react';
import { useDataFrom } from '../useDataFrom';

import { default as styles } from './EventTypes.module.scss';
import { useState } from 'react';
import { EventTypeSchema } from './EventTypeSchema';

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
        fieldName: 'name',
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
    const [eventType, setEventType] = useState();
    const [generationalSchemas, reloadGenerationalSchemas] = useDataFrom(
        {
            template: '/api/events/types/schemas/{{eventTypeId}}',
            arguments: {
                eventTypeId: eventType
            }
        });
    const [eventTypes, reloadEventTypes] = useDataFrom('/api/events/types');
    return (
        <div className={styles.container}>
            <div className={styles.eventList}>
                <DetailsList
                    items={eventTypes}
                    columns={eventTypesColumns}
                    selectionMode={SelectionMode.single}
                    onActiveItemChanged={(item: any) => {
                        setEventType(item.identifier);
                    }}
                />
            </div>
            <div className={styles.eventDetails}>
                <Pivot linkFormat="tabs" defaultSelectedKey="2">
                    {generationalSchemas.map((schema: EventTypeSchema) => {
                        const properties = Object.keys(schema.properties).map(_ => {
                            let type = schema.properties[_].type;
                            if (Array.isArray(type)) {
                                type = type[0];
                            }

                            return {
                                name: _,
                                type
                            };
                        });
                        return (

                            <PivotItem key={schema.generation} headerText={schema.generation.toString()}>
                                <DetailsList items={properties}
                                    columns={eventSchemaColumns}
                                    selectionMode={SelectionMode.none} />
                            </PivotItem>
                        );
                    })}
                </Pivot>
            </div>
        </div>
    );
}
