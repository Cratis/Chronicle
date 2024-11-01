// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/applications.react.mvvm';
import { NamespacesViewModel } from './NamespacesViewModel';
import { AllNamespaces, AllNamespacesArguments, Namespace } from 'Api/Namespaces';
import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import { DataPage, MenuItem } from 'Components';
import { Column } from 'primereact/column';
import * as faIcons from 'react-icons/fa';
import { useParams } from 'react-router-dom';
import { useDialogRequest } from '@cratis/applications.react.mvvm/dialogs';
import { AddNamespace, AddNamespaceRequest, AddNamespaceResponse } from './AddNamespace';

const renderId = (namespace: Namespace) => namespace.id.toString();

export const Namespaces = withViewModel(NamespacesViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [AddNamespaceDialogWrapper] = useDialogRequest<AddNamespaceRequest, AddNamespaceResponse>(AddNamespaceRequest);
    const queryArgs: AllNamespacesArguments = {
        eventStore: params.eventStore!
    };

    return (
        <>
            <DataPage
                title={strings.eventStore.general.types.title}
                query={AllNamespaces}
                queryArguments={queryArgs}
                dataKey='id'
                emptyMessage={strings.eventStore.general.namespaces.empty}>

                <DataPage.MenuItems>
                    <MenuItem
                        id='create'
                        label={strings.eventStore.general.types.actions.create} icon={faIcons.FaPlus}
                        command={() => viewModel.addNamespace()} />
                </DataPage.MenuItems>

                <DataPage.Columns>
                    <Column
                        field='id'
                        style={{ width: '400px' }}
                        header={strings.eventStore.general.namespaces.columns.id}
                        sortable
                        body={renderId}/>
                    <Column field='name' header={strings.eventStore.general.namespaces.columns.name} sortable />
                </DataPage.Columns>
            </DataPage>
            <AddNamespaceDialogWrapper>
                <AddNamespace />
            </AddNamespaceDialogWrapper>
        </>
    );
});
