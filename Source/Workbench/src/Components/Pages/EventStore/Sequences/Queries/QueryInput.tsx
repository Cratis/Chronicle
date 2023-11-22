// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InputText } from 'primereact/inputtext';
import { Button } from 'primereact/button';
import 'primeicons/primeicons.css';

export interface QueryInputProps {
    query: {
        id: number;
        queryText: string;
    };
    onUpdate: (newVal: string) => void;
    onRemove: () => void;
    onFetchData: () => void;
}

export const QueryInput = (props: QueryInputProps) => {
    const { query, onUpdate, onRemove, onFetchData } = props;

    return (
        <div className='flex space-x-2 m-2'>
            <span className='p-input-icon-right'>
                {query.id !== 0 && <i className='pi pi-times' onClick={onRemove} />}
                <InputText
                    type='text'
                    value={query.queryText}
                    onChange={(e) => onUpdate(e.target.value)}
                />
            </span>
            <Button label='Run query' icon='pi pi-caret-right' onClick={onFetchData} />
        </div>
    );
};
