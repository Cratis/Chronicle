// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/arc.react.mvvm';
import { NamespacesViewModel } from './NamespacesViewModel';
import { AllNamespaces, AllNamespacesParameters } from 'Api/Namespaces';
import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import { DataPage, MenuItem } from 'Components';
import { Column } from 'primereact/column';
import * as faIcons from 'react-icons/fa';
import { useParams } from 'react-router-dom';
import { useDialog } from '@cratis/arc.react.mvvm/dialogs';
import { AddNamespace, AddNamespaceRequest, AddNamespaceResponse } from './AddNamespace';

export const Namespaces = withViewModel(NamespacesViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [AddNamespaceDialog] = useDialog<AddNamespaceRequest, AddNamespaceResponse>(AddNamespaceRequest, AddNamespace);
    const queryArgs: AllNamespacesParameters = {
        eventStore: params.eventStore!
    };

    const nameColumn = (namespace: string) => {
        return <>{namespace}</>;
    };

    return (
        <>
            <DataPage
                title={strings.eventStore.general.namespaces.title}
                query={AllNamespaces}
                queryArguments={queryArgs}
                emptyMessage={strings.eventStore.general.namespaces.empty}>

                <DataPage.MenuItems>
                    <MenuItem
                        id='create'
                        label={strings.eventStore.general.eventTypes.actions.create} icon={faIcons.FaPlus}
                        command={() => viewModel.addNamespace()} />
                </DataPage.MenuItems>

                <DataPage.Columns>
                    <Column header={strings.eventStore.general.namespaces.columns.name} sortable body={nameColumn} />
                </DataPage.Columns>
            </DataPage>
            <AddNamespaceDialog/>
        </>
    );
});
