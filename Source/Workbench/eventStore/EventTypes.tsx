// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState, useEffect } from 'react';
import { EventTypeSchema } from './EventTypeSchema';
import { AllEventTypes, AllEventTypesArguments } from 'API/events/store/types/AllEventTypes';
import { GenerationSchemasForType } from 'API/events/store/types/GenerationSchemasForType';
import { useRouteParams } from './RouteParams';
import { DataGrid, GridColDef, GridRowSelectionModel, GridCallbackDetails } from '@mui/x-data-grid';
import { Box, Divider, Grid, Stack, Tab, Tabs, Typography  } from '@mui/material';
import { EventTypeInformation } from '../API/events/store/types/EventTypeInformation';

interface TabPanelProps {
    children?: React.ReactNode;
    index: number;
    value: number;
}

const TabPanel = (props: TabPanelProps) => {
    const { children, value, index, ...other } = props;

    return (
        <div
            role="tabpanel"
            hidden={value !== index}
            id={`simple-tabpanel-${index}`}
            aria-labelledby={`simple-tab-${index}`}
            style={{ height: '100%' }}
            {...other}>
            {value === index && (
                <>
                    {children}
                </>
            )}
        </div>
    );
};

const eventTypesColumns: GridColDef[] = [
    {
        headerName: 'Identifier',
        field: 'identifier',
        width: 300,
    },
    {
        headerName: 'Name',
        field: 'name',
        width: 500
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

export const EventTypes = () => {
    const { microserviceId } = useRouteParams();
    const [eventType, setEventType] = useState<string>();
    const [selectedGeneration, setSelectedGeneration] = useState(0);

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

        <Stack direction="column" style={{ height: '100%' }}>
            <Typography variant='h4'>Event types</Typography>
            <Divider sx={{ mt: 1, mb: 3 }} />
            <Grid container spacing={2} sx={{ height: '100%' }}>
                <Grid item xs={8}>
                    <DataGrid
                        columns={eventTypesColumns}
                        filterMode="client"
                        sortingMode="client"
                        getRowId={(row: EventTypeInformation) => row.identifier}
                        onRowSelectionModelChange={eventTypeSelected}
                        rows={eventTypes.data}
                    />

                </Grid>

                <Grid item xs={4} >
                    <Tabs value={selectedGeneration} onChange={(e, newValue) => setSelectedGeneration(newValue)}>
                        {generationalSchemas.data.map((schema: EventTypeSchema) => {
                            return (
                                <Tab key={schema.type} label={schema.generation.toString()} />
                            );
                        })}
                    </Tabs>

                    <Box sx={{ height: '100%', flex: 1 }}>

                        {generationalSchemas.data.map((schema: EventTypeSchema, index: number) => {
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

                            console.log(properties);

                            return (
                                <TabPanel key={schema.type} value={selectedGeneration} index={index}>

                                    <DataGrid
                                        columns={eventSchemaColumns}
                                        filterMode="client"
                                        sortingMode="client"
                                        getRowId={(row) => row.name}
                                        onRowSelectionModelChange={eventTypeSelected}
                                        rows={properties}
                                    />

                                </TabPanel>
                            );
                        })}
                    </Box>


                    {/* <Pivot linkFormat="tabs" defaultSelectedKey="2">
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
                    </Pivot> */}

                </Grid>
            </Grid>
        </Stack>);
};
