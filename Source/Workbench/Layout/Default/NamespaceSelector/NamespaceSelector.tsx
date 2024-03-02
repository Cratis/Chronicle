// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useRef, useState } from "react";

import css from './NamespaceSelector.module.css';
import { OverlayPanel } from "primereact/overlaypanel";
import { useLayoutContext } from "../context/LayoutContext";
import { CurrentNamespace } from "./CurrentNamespace";
import { InputText } from 'primereact/inputtext';

export interface INamespaceSelectorProps extends React.HTMLAttributes<HTMLDivElement> {
    namespaces: string[];
    currentNamespace: string;
    onNamespaceSelected: (namespace: string) => void;
}

export const NamespaceSelector = (props: INamespaceSelectorProps) => {
    const { layoutConfig } = useLayoutContext();
    const [search, setSearch] = useState<string>('');

    const op = useRef<OverlayPanel>(null);

    const selectNamespace = (namespace: string) => {
        props.onNamespaceSelected(namespace);
        op?.current?.hide();
    }

    return (
        <div>
            <CurrentNamespace compact={!layoutConfig.leftSidebarOpen}
                namespace={props.currentNamespace} onClick={(e) => {
                    op?.current?.toggle(e, null)
                }} />

            <OverlayPanel ref={op}
                className={`${css.overlayPanel} ${layoutConfig.leftSidebarOpen ? css.openOverlayPanel : css.closedOverlayPanel}`}>

                <div>
                    <div className={'mb-2'}>
                        <InputText value={search}
                            placeholder={'Search for namespace'}
                            onChange={(e) => {
                                setSearch(e.target.value)
                            }} />
                    </div>
                    <ul className={css.namespaceList}>
                        {props.namespaces.filter((t) => t.toLowerCase().includes(search.toLowerCase())).map((namespace) => {
                            return (
                                <li onClick={() => selectNamespace(namespace)} className={`p-2 ${css.namespaceListItem}`}>
                                    {namespace}
                                </li>)
                        })}
                    </ul>
                </div>
            </OverlayPanel>
        </div>);
}
