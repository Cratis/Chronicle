// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/applications.react.mvvm';
import { WebhooksViewModel } from './WebhooksViewModel';
import { AllWebhooks, AllWebhooksParameters } from 'Api/Observation/Webhooks/';
import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import { DataPage, MenuItem } from 'Components';
import { Column } from 'primereact/column';
import * as faIcons from 'react-icons/fa';
import { useParams } from 'react-router-dom';
import { useDialog } from '@cratis/applications.react.mvvm/dialogs';
import { AddWebhook, AddWebhookRequest, AddWebhookResponse } from './AddWebhook';

export const Webhooks = withViewModel(WebhooksViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [AddWebhookDialog] = useDialog<AddWebhookRequest, AddWebhookResponse>(AddWebhookRequest, AddWebhook);
    const queryArgs: AllWebhooksParameters = {
        eventStore: params.eventStore!
    };

    return (
        <>
            <DataPage
                title={strings.eventStore.general.webhooks.title}
                query={AllWebhooks}
                queryArguments={queryArgs}
                emptyMessage={strings.eventStore.general.webhooks.empty}>

                <DataPage.MenuItems>
                    <MenuItem
                        id='create'
                        label={strings.eventStore.general.types.actions.create} icon={faIcons.FaPlus}
                        command={() => viewModel.addWebhook()} />
                </DataPage.MenuItems>

                <DataPage.Columns>
                    <Column header='Id' field='identifier' sortable />
                    <Column header='Event Sequence' field='eventSequenceId' sortable />
                    <Column header='Url' field='target.url' sortable />
                </DataPage.Columns>
            </DataPage>
            <AddWebhookDialog eventTypes={[]}/>
        </>
    );
});
