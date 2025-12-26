// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Column } from 'primereact/column';
import strings from 'Strings';
import { AllReadModels } from 'Api/ReadModels';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { useDialog } from '@cratis/arc.react/dialogs';
import { AddReadModel, AddReadModelRequest, AddReadModelResponse } from './AddReadModel';
import { DataPage } from 'Components';
import { ReadModelDetails } from './ReadModelDetails';

export const ReadModels = () => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [AddReadModelDialog] = useDialog<AddReadModelRequest, AddReadModelResponse>(AddReadModelRequest, AddReadModel);

    return (
        <>
            <DataPage
                title={strings.eventStore.general.readModels.title}
                query={AllReadModels}
                queryArguments={{ eventStore: params.eventStore! }}
                dataKey='identifier'
                emptyMessage={strings.eventStore.general.readModels.empty}
                detailsComponent={ReadModelDetails}>

                <DataPage.Columns>
                    <Column field='name' header={strings.eventStore.general.readModels.columns.name} />
                    <Column
                        field='generation'
                        style={{ width: '100px' }}
                        header={strings.eventStore.general.readModels.columns.generation} />
                </DataPage.Columns>
            </DataPage>
            <AddReadModelDialog/>
        </>
    );
};
