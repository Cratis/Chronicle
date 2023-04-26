// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import { Divider, FormControl, Grid, Stack, Typography } from '@mui/material';
import { DataGrid, GridCallbackDetails, GridColDef, GridRowSelectionModel } from '@mui/x-data-grid';
import { AllMetrics } from 'API/metrics/AllMetrics';
import { MetricMeasurement } from 'API/metrics/MetricMeasurement';
import { MetricMeasurementPoint } from 'API/metrics/MetricMeasurementPoint';
import { MetricMeasurementPointTag } from 'API/metrics/MetricMeasurementPointTag';

const metricColumns: GridColDef[] = [
    {
        headerName: 'Name',
        field: 'name',
        width: 600,
    },
    {
        headerName: 'Value',
        field: 'aggregated',
        width: 100
    }
];


const metricPointColumns: GridColDef[] = [
    {
        headerName: 'Tags',
        field: 'tags',
        width: 600,
        renderCell: (params) => {
            const tags = params.value as MetricMeasurementPointTag[];
            return (
                <>
                    <FormControl>
                        {tags.map(tag => (
                            <div key={tag.tag}>{`${tag.tag}=${tag.value}`}</div>
                        ))}
                    </FormControl>
                </>
            );
        }
    },
    {
        headerName: 'Value',
        field: 'value',
        width: 100
    }
];

type TagValueSelection = {
    tag: string;
    values: string[];
}

export const Metrics = () => {
    const [metrics] = AllMetrics.use();
    const [selectedMetric, setSelectedMetric] = useState<MetricMeasurement | null>(null);

    const metricSelected = (selectionModel: GridRowSelectionModel, details: GridCallbackDetails) => {
        const selectedItems = selectionModel.map((id => metrics.data.find(metric => metric.name == id))) as MetricMeasurement[];
        if (selectedItems.length > 0) {
            const item = selectedItems[0];
            setSelectedMetric(item);
        }
    };

    return (
        <Stack direction="column" style={{ height: '100%', padding: '24px' }}>
            <Typography variant='h4'>Metrics</Typography>
            <Divider sx={{ mt: 1, mb: 3 }} />

            <Grid container spacing={2} sx={{ height: '100%' }}>
                <Grid item xs={6}>
                    <DataGrid
                        columns={metricColumns}
                        filterMode="client"
                        sortingMode="client"
                        getRowId={(row: MetricMeasurement) => row.name}
                        onRowSelectionModelChange={metricSelected}
                        rows={metrics.data}
                    />
                </Grid>

                <Grid item xs={4} >
                    {selectedMetric &&
                        <>
                            <DataGrid
                                rowHeight={200}
                                columns={metricPointColumns}
                                filterMode="client"
                                sortingMode="client"
                                getRowId={(row: MetricMeasurementPoint) => row.tags.map(tag => `${tag.tag}=${tag.value}`).join(', ')}
                                rows={selectedMetric.points}
                            />
                        </>
                    }
                </Grid>
            </Grid>
        </Stack>
    );
};
