// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    IDropdownStyles,
    Pivot,
    PivotItem,
    Stack
} from '@fluentui/react';

import { useState, useEffect } from 'react';
import { EventTypeSchema } from './EventTypeSchema';
import { AllEventTypes, AllEventTypesArguments } from 'API/events/store/types/AllEventTypes';
import { GenerationSchemasForType } from 'API/events/store/types/GenerationSchemasForType';
import { useRouteParams } from './RouteParams';
import { DataGrid, GridColDef, GridRowSelectionModel, GridCallbackDetails } from '@mui/x-data-grid';
import { Box } from '@mui/material';
import { EventTypeInformation } from '../API/events/store/types/EventTypeInformation';

const eventTypesColumns: GridColDef[] = [
    {
        headerName: 'Identifier',
        field: 'identifier',
        width: 300,
    },
    {
        headerName: 'Name',
        field: 'name',
        width: 300
    },
    {
        headerName: 'Generations',
        field: 'generations',
        width: 100
    }
];

const eventSchemaColumns: GridColDef[] = [
    {
        headerName: 'Property',
        field: 'name',
        width: 200
    },
    {
        headerName: 'Type',
        field: 'type',
        width: 100
    }
];

const commandBarDropdownStyles: Partial<IDropdownStyles> = { dropdown: { width: 200, marginLeft: 8, marginTop: 8 } };

export const EventTypes = () => {
    const { microserviceId } = useRouteParams();
    const [eventType, setEventType] = useState<string>();

    const getAllEventTypesArguments = () => {
        return {
            microserviceId
        } as AllEventTypesArguments;
    };

    const getGenerationalSchemasForTypeArguments = () => {
        return {
            microserviceId,
            eventTypeId: eventType!
        };
    };

    const [generationalSchemas, reloadGenerationalSchemas] = GenerationSchemasForType.use(getGenerationalSchemasForTypeArguments());
    const [eventTypes] = AllEventTypes.use(getAllEventTypesArguments());

    useEffect(() => {
        reloadGenerationalSchemas(getGenerationalSchemasForTypeArguments());
    }, [eventType]);

    const eventTypeSelected = (selectionModel: GridRowSelectionModel, details: GridCallbackDetails) => {
        const selectedItems = selectionModel.map((id => eventTypes.data.find(eventType => eventType.identifier == id))) as EventTypeInformation[];
        if (selectedItems.length > 0) {
            setEventType(selectedItems[0].identifier);
        }
    };

    return (
        <div>
            <div>
                <Stack>
                    <Box sx={{ height: 400 }}>
                        <DataGrid
                            columns={eventTypesColumns}
                            filterMode="client"
                            sortingMode="client"
                            getRowId={(row: EventTypeInformation) => row.identifier}
                            onRowSelectionModelChange={eventTypeSelected}
                            rows={eventTypes.data}
                        />
                    </Box>
                </Stack>
            </div>
            <div>
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
                                <Box sx={{ height: 400 }}>
                                    <DataGrid
                                        columns={eventSchemaColumns}
                                        filterMode="client"
                                        sortingMode="client"
                                        getRowId={(row) => row.name}
                                        onRowSelectionModelChange={eventTypeSelected}
                                        rows={properties}
                                    />
                                </Box>

                            </PivotItem>
                        );
                    })}
                </Pivot>
            </div>
        </div>
    );
};
