// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/arc.react.mvvm';
import { NamespacesViewModel } from './NamespacesViewModel';
import { NamespaceNames, ObserveNamespaces, ObserveNamespacesParameters } from 'Features/Namespaces';
import { type EventStoreAndNamespaceParams } from 'Shared';
import strings from 'Strings';
import { Column, DataPage, MenuItem } from '@cratis/components/DataPage';
import { Page } from 'Components/Common/Page';
import * as faIcons from 'react-icons/fa';
import { useParams } from 'react-router-dom';
import { useDialog } from '@cratis/arc.react.mvvm/dialogs';
import { AddNamespace, AddNamespaceRequest, AddNamespaceResponse } from './AddNamespace';

export const Namespaces = withViewModel(NamespacesViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [AddNamespaceDialog] = useDialog<AddNamespaceRequest, AddNamespaceResponse>(AddNamespaceRequest, AddNamespace);
    const queryArgs: ObserveNamespacesParameters = {
        eventStore: params.eventStore!
    };

    const nameColumn = (namespace: NamespaceNames) => {
        return <>{namespace.name}</>;
    };

    return (
        <Page title={strings.eventStore.general.namespaces.title}>
            <DataPage
                title={strings.eventStore.general.namespaces.title}
                query={ObserveNamespaces}
                queryArguments={queryArgs}
                dataKey='id'
                emptyMessage={strings.eventStore.general.namespaces.empty}>

                <DataPage.MenuItems>
                    <MenuItem
                        label={strings.eventStore.general.eventTypes.actions.create} icon={faIcons.FaPlus}
                        command={() => viewModel.addNamespace()} />
                </DataPage.MenuItems>

                <DataPage.Columns>
                    <Column field='name' header={strings.eventStore.general.namespaces.columns.name} sortable body={nameColumn} />
                </DataPage.Columns>
            </DataPage>
            <AddNamespaceDialog/>
        </Page>
    );
});
