// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { AllServerInstances, ServerInstanceDetails } from 'Features/Servers';
import { Page } from 'Components/Common/Page';
import { Column } from '@cratis/components/DataTables';
import { DataTable } from 'Components/DataTable';
import strings from 'Strings';

const byteUnits = ['B', 'KB', 'MB', 'GB', 'TB'];

const formatBytes = (bytes: number) => {
    if (bytes <= 0) return `0 ${byteUnits[0]}`;

    const exponent = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), byteUnits.length - 1);
    const value = bytes / 1024 ** exponent;
    return `${value.toFixed(exponent === 0 ? 0 : 1)} ${byteUnits[exponent]}`;
};

const cpuColumn = (instance: ServerInstanceDetails) => <>{instance.cpuUsagePercentage.toFixed(1)}%</>;

const memoryColumn = (instance: ServerInstanceDetails) => <>{formatBytes(instance.memoryUsageBytes)}</>;

export const Servers = () => {
    const [result] = AllServerInstances.use();
    const instances = result.data ?? [];

    return (
        <Page title={strings.eventStore.system.servers.title}>
            <DataTable
                value={instances}
                dataKey='id'
                emptyMessage={strings.eventStore.system.servers.empty}
                scrollable
                scrollHeight='flex'>
                <Column field='address' header={strings.eventStore.system.servers.columns.address} sortable />
                <Column field='status' header={strings.eventStore.system.servers.columns.status} sortable />
                <Column field='cpuUsagePercentage' header={strings.eventStore.system.servers.columns.cpu} sortable body={cpuColumn} />
                <Column field='memoryUsageBytes' header={strings.eventStore.system.servers.columns.memory} sortable body={memoryColumn} />
            </DataTable>
        </Page>
    );
};
