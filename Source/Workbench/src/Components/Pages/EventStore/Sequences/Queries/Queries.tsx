// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueriesViewModel } from './QueriesViewModel';
import { Button } from 'primereact/button';
import { QueryInput } from './QueryInput';
import { observer } from 'mobx-react';
import 'primeicons/primeicons.css';
import { Fragment } from 'react';

export interface QueriesProps {
    viewModal: QueriesViewModel;
}

export const Queries = observer((props: QueriesProps) => {
    const { viewModal } = props;
    return (
        <>
            <h1 className='text-3xl mb-3'>Queries</h1>
            <Button
                label='Add Query'
                icon='pi pi-plus'
                onClick={() => viewModal.addQuery()}
            />
            {viewModal?.queries?.map((query) => (
                <Fragment key={query.id}>
                    <QueryInput
                        query={query}
                        onRemove={() => viewModal.removeQuery(query.id)}
                        onUpdate={(newText: string) =>
                            viewModal.updateQuery(query.id, newText)
                        }
                        onFetchData={() => {}}
                    />
                    <h1>Data goes here</h1>
                </Fragment>
            ))}
        </>
    );
});
