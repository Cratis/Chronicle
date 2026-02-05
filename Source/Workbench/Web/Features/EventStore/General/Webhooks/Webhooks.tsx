// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataTable } from 'primereact/datatable';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { GetWebhooks, type WebhookDefinition } from 'Api/Webhooks';
import { Column } from 'primereact/column';
import { Button } from 'primereact/button';
import { useDialog } from '@cratis/arc.react/dialogs';
import { AddWebhookDialog } from './Add/AddWebhookDialog';

export const Webhooks = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [webhooks, performQuery] = GetWebhooks.use({ eventStore: params.eventStore! });
    const [AddWebhookWrapper, showAddWebhook] = useDialog(AddWebhookDialog);

    const handleAddWebhook = async () => {
        const [result] = await showAddWebhook();
        if (result) {
            performQuery({ eventStore: params.eventStore! });
        }
    };

    return (
        <>
            <div className="flex justify-content-between align-items-center mb-3">
                <h2>Webhooks</h2>
                <Button
                    label="Add Webhook"
                    icon="pi pi-plus"
                    onClick={handleAddWebhook} />
            </div>

            <DataTable
                value={webhooks.data}
                loading={webhooks.isPerforming}
                emptyMessage="No webhooks configured">
                <Column field="name" header="Name" />
                <Column field="url" header="URL" />
                <Column
                    field="authorizationType"
                    header="Authorization"
                    body={(rowData: WebhookDefinition) => rowData.authorizationType || 'None'} />
                <Column
                    field="isActive"
                    header="Active"
                    body={(rowData: WebhookDefinition) => (rowData.isActive ? 'Yes' : 'No')} />
                <Column
                    field="isReplayable"
                    header="Replayable"
                    body={(rowData: WebhookDefinition) => (rowData.isReplayable ? 'Yes' : 'No')} />
            </DataTable>

            <AddWebhookWrapper />
        </>
    );
};
