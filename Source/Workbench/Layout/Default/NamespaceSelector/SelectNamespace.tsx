// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { INamespace } from "./NamespaceSelector";
import { InputText } from "primereact/inputtext";
import { useState } from "react";
import { NamespaceListItem } from "./NamespaceListItem";
import css from "./NamespaceSelector.module.css";

interface ISelectNamespaceProps {
    onSelected: (namespace: INamespace) => void;
}

export const SelectNamespace = ({ onSelected }: ISelectNamespaceProps) => {
    const [search, setSearch] = useState<string>('');
    const allNamespaces: INamespace[] = [
        {
            id: '1',
            name: 'Drammen Kommunale Pensjonskasse'
        },
        {
            id: '2',
            name: 'Arendal Kommunale Pensjonskasse'
        },
        {
            id: '3',
            name: 'Viken Fylkeskommunale Pensjonskasse'
        },
        {
            id: '4',
            name: 'Nordland Fylkeskommunale Pensjonskasse'
        },
        {
            id: '5',
            name: 'Bergen Kommunale Pensjonskasse'
        },
        {
            id: '6',
            name: 'Oslo Kommunale Pensjonskasse'

        },
        {
            id: '7',
            name: 'Trondheim Kommunale Pensjonskasse'
        }
    ];

    return <div>
        <div className={'mb-2'}>
            <InputText value={search}
                placeholder={'Search for namespace'}
                onChange={(e) => {
                    setSearch(e.target.value)
                }} />
        </div>
        <ul className={css.namespaceList}>
            {allNamespaces.filter((t) => t.name.toLowerCase().includes(search.toLowerCase())).map((namespace) => {
                return <NamespaceListItem namespace={namespace} onClick={() => onSelected(namespace)} key={namespace.id} />;
            })}
        </ul>
    </div>;
}
