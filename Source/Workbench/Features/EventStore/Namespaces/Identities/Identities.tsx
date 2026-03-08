// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import { DataPage } from 'Components';
import { Column } from 'primereact/column';
import { useParams } from 'react-router-dom';
import { AllIdentities, AllIdentitiesParameters } from 'Api/Identities';

export const Identities = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const queryArgs: AllIdentitiesParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };

    return (
        <DataPage
            title={strings.eventStore.namespaces.identities.title}
            query={AllIdentities}
            queryArguments={queryArgs}
            dataKey='id'
            emptyMessage={strings.eventStore.namespaces.identities.empty}>

            <DataPage.Columns>
                <Column field='subject' header={strings.eventStore.namespaces.identities.columns.subject} sortable />
                <Column field='name' header={strings.eventStore.namespaces.identities.columns.name} sortable />
                <Column field='userName' header={strings.eventStore.namespaces.identities.columns.userName} sortable />
            </DataPage.Columns>
        </DataPage>);
};
