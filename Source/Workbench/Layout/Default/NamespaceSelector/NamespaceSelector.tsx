// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useRef, useState, useEffect } from "react";

import { OverlayPanel } from "primereact/overlaypanel";
import { useLayoutContext } from "../context/LayoutContext";
import { CurrentNamespace } from "./CurrentNamespace";
import { InputText } from 'primereact/inputtext';
import { ItemsList } from 'Components/ItemsList/ItemsList';
import { INamespaceSelectorProps, NamespaceSelectorViewModel } from './NamespaceSelectorViewModel';
import { withViewModel } from '@cratis/arc.react.mvvm';
import css from './NamespaceSelector.module.css';
import { useParams } from 'react-router-dom';
import { type EventStoreAndNamespaceParams } from 'Shared';

export const NamespaceSelector = withViewModel<NamespaceSelectorViewModel, INamespaceSelectorProps>(NamespaceSelectorViewModel, ({ viewModel }) => {
    const { layoutConfig } = useLayoutContext();
    const [search, setSearch] = useState<string>('');
    const params = useParams<EventStoreAndNamespaceParams>();

    useEffect(() => {
        if (params.eventStore) {
            viewModel.setEventStore(params.eventStore);
        }
    }, [params.eventStore, viewModel]);

    const op = useRef<OverlayPanel>(null);

    const selectNamespace = (namespace: string) => {
        viewModel.onNamespaceSelected(namespace);
        op?.current?.hide();
    };

    const filteredNamespaces = useMemo(() => viewModel.namespaces.filter((t) => t.toLowerCase().includes(search.toLowerCase())), [viewModel.namespaces, search]);

    return (
        <div>
            <CurrentNamespace compact={!layoutConfig.leftSidebarOpen}
                namespace={viewModel.currentNamespace} onClick={(e) => {
                    op?.current?.toggle(e, null);
                }} />

            <OverlayPanel ref={op}
                className={`${css.overlayPanel} ${layoutConfig.leftSidebarOpen ? css.openOverlayPanel : css.closedOverlayPanel}`}>

                <div>
                    <div className={'mb-2'}>
                        <InputText value={search}
                            placeholder={'Search for namespace'}
                            onChange={(e) => {
                                setSearch(e.target.value);
                            }} />
                    </div>

                    <ItemsList<string> items={filteredNamespaces} onItemClicked={selectNamespace} />
                </div>
            </OverlayPanel>
        </div>);
});
