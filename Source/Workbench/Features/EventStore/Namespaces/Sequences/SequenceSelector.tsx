// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InputText } from 'primereact/inputtext';
import css from './Queries.module.css';
import { useState } from 'react';

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

    return (
        <>
            <div className={'mb-2'}>
                <InputText value={search}
                    placeholder={'Search for sequence'}
                    onChange={(e) => {
                        setSearch(e.target.value);
                    }} />
            </div>
            <ul className={css.sequenceList}>
                {sequences.filter((t) => t.name.toLowerCase().includes(search.toLowerCase())).map((namespace) => {
                    return <li className={`p-2 ${css.sequenceListItem}`}>{namespace.name}</li>
                })}
            </ul>
        </>
    );
};
