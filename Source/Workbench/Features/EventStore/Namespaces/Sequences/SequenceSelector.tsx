// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InputText } from 'primereact/inputtext';
import { useMemo, useState } from 'react';
import { ItemsList } from 'Components/ItemsList/ItemsList';

type Sequence = {
    id: string;
    name: string;
};

export const SequenceSelector = () => {
    const [search, setSearch] = useState<string>('');
    const sequences = [
        {
            id: '1',
            name: 'Event log'
        },
        {
            id: '2',
            name: 'Outbox'
        }
    ];

    const filteredSequences = useMemo(() => sequences.filter((t) => t.name.toLowerCase().includes(search.toLowerCase())), [search]);

    return (
        <>
            <div className={'mb-2'}>
                <InputText value={search}
                    placeholder={'Search for sequence'}
                    onChange={(e) => {
                        setSearch(e.target.value);
                    }} />
            </div>

            <ItemsList<Sequence> items={filteredSequences} idProperty='id' nameProperty='name' />
        </>
    );
};
