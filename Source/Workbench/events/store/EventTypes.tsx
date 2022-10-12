// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    DetailsList,
    Dropdown,
    IColumn,
    IDropdownOption,
    IDropdownStyles,
    Pivot,
    PivotItem,
    SelectionMode,
    Stack
} from '@fluentui/react';

import { default as styles } from './EventTypes.module.scss';
import { useState, useEffect } from 'react';
import { EventTypeSchema } from './EventTypeSchema';
import { Microservices } from 'API/configuration/Microservices';
import { Microservice } from 'API/configuration/Microservice';
import { AllEventTypes, AllEventTypesArguments } from 'API/events/store/types/AllEventTypes';
import { GenerationSchemasForType } from 'API/events/store/types/GenerationSchemasForType';

const eventTypesColumns: IColumn[] = [
    {
        key: 'identifier',
        name: 'Identifier',
        fieldName: 'identifier',
        minWidth: 100,
        maxWidth: 200,
    },
    {
        key: 'name',
        name: 'Name',
        fieldName: 'name',
        minWidth: 300
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

const commandBarDropdownStyles: Partial<IDropdownStyles> = { dropdown: { width: 200, marginLeft: 8, marginTop: 8 } };

export const EventTypes = () => {
    const [microservices] = Microservices.use();
    const [selectedMicroservice, setSelectedMicroservice] = useState<Microservice>();
    const [eventType, setEventType] = useState();

    const getAllEventTypesArguments = () => {
        return {
            microserviceId: selectedMicroservice?.id || undefined!
        } as AllEventTypesArguments;
    };

    const getGenerationalSchemasForTypeArguments = () => {
        return {
            microserviceId: selectedMicroservice?.id || undefined!,
            eventTypeId: eventType!
        };
    };

    const [generationalSchemas, reloadGenerationalSchemas] = GenerationSchemasForType.use(getGenerationalSchemasForTypeArguments());
    const [eventTypes, refreshEventTypes] = AllEventTypes.use(getAllEventTypesArguments());

    const microserviceOptions = microservices.data.map(_ => {
        return {
            key: _.id,
            text: _.name
        } as IDropdownOption;
    });

    useEffect(() => {
        if (microservices.data.length > 0) {
            setSelectedMicroservice(microservices.data[0]);
        }
    }, [microservices.data]);

    useEffect(() => {
        if (selectedMicroservice) {
            refreshEventTypes(getAllEventTypesArguments());
        }
    }, [selectedMicroservice]);

    useEffect(() => {
        reloadGenerationalSchemas(getGenerationalSchemasForTypeArguments());
    }, [eventType]);

    return (
        <div className={styles.container}>
            <div className={styles.eventList}>
                <Stack>
                    <Dropdown
                        styles={commandBarDropdownStyles}
                        options={microserviceOptions}
                        selectedKey={selectedMicroservice?.id}
                        onChange={(e, option) => {
                            setSelectedMicroservice(microservices.data.find(_ => _.id == option!.key));
                        }} />

                    <DetailsList
                        items={eventTypes.data}
                        columns={eventTypesColumns}
                        selectionMode={SelectionMode.single}
                        onActiveItemChanged={(item: any) => {
                            setEventType(item.identifier);
                        }}
                    />
                </Stack>
            </div>
            <div className={styles.eventDetails}>
                <Pivot linkFormat="tabs" defaultSelectedKey="2">
                    {generationalSchemas.data.map((schema: EventTypeSchema) => {
                        const properties = Object.keys(schema.properties || []).map(_ => {
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
};
