// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { InputText } from 'primereact/inputtext';
import { useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { AllEventSequences } from 'Api/EventSequences/AllEventSequences';
import { type EventStoreAndNamespaceParams } from 'Shared';

export interface SequenceSelectorProps {
    value?: string;
    onChange?: (eventSequenceId: string) => void;
}

export const SequenceSelector = ({ value, onChange }: SequenceSelectorProps) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [search, setSearch] = useState<string>('');

    const [sequences] = AllEventSequences.use({
        eventStore: params.eventStore!,
        namespace: params.namespace!,
    });

    const filtered = useMemo(() => {
        const items = sequences.data ?? [];
        return [...items]
            .filter(sequence => sequence.name.toLowerCase().includes(search.toLowerCase()))
            .sort((a, b) => a.name.localeCompare(b.name));
    }, [sequences.data, search]);

    return (
        <div className="flex flex-col gap-2">
            <InputText
                value={search}
                placeholder="Search for sequence"
                className="p-inputtext-sm"
                onChange={(event) => setSearch(event.target.value)} />
            <ul className="flex flex-col gap-0.5 max-h-60 overflow-auto m-0 p-0 list-none">
                {filtered.map(sequence => {
                    const isSelected = sequence.id === value;
                    return (
                        <li key={sequence.id}>
                            <button
                                type="button"
                                onClick={() => onChange?.(sequence.id)}
                                className="w-full text-left px-2 py-1 rounded text-sm"
                                style={{
                                    background: isSelected ? 'var(--highlight-bg)' : 'transparent',
                                    color: isSelected ? 'var(--primary-color-text)' : 'var(--text-color)',
                                    border: 'none',
                                    cursor: 'pointer',
                                }}>
                                {sequence.name}
                            </button>
                        </li>
                    );
                })}
            </ul>
        </div>
    );
};
