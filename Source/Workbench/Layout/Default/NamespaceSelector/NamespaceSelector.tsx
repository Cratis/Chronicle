// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState, useEffect, type ChangeEvent } from "react";

import { Popover, type PopoverRootOpenChangeEvent } from "primereact/popover";
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

    const [isNamespacePanelOpen, setIsNamespacePanelOpen] = useState(false);

    const selectNamespace = (namespace: string) => {
        viewModel.onNamespaceSelected(namespace);
        setIsNamespacePanelOpen(false);
    };

    const filteredNamespaces = useMemo(() => viewModel.namespaces.filter((t) => t.toLowerCase().includes(search.toLowerCase())), [viewModel.namespaces, search]);

    return (
        <div>
            <Popover.Root
                open={isNamespacePanelOpen}
                onOpenChange={(event: PopoverRootOpenChangeEvent) => setIsNamespacePanelOpen(event.value ?? false)}>
                <Popover.Trigger as="div">
                    <CurrentNamespace compact={!layoutConfig.leftSidebarOpen}
                        namespace={viewModel.currentNamespace} />
                </Popover.Trigger>

                <Popover.Portal>
                    <Popover.Positioner>
                        <Popover.Popup
                            className={`${css.overlayPanel} ${layoutConfig.leftSidebarOpen ? css.openOverlayPanel : css.closedOverlayPanel}`}>
                            <Popover.Content>
                                <div>
                                    <div className={'mb-2'}>
                                        <InputText value={search}
                                            placeholder={'Search for namespace'}
                                            onChange={(event: ChangeEvent<HTMLInputElement>) => {
                                                setSearch(event.target.value);
                                            }} />
                                    </div>

                                    <ItemsList<string> items={filteredNamespaces} onItemClicked={selectNamespace} />
                                </div>
                            </Popover.Content>
                        </Popover.Popup>
                    </Popover.Positioner>
                </Popover.Portal>
            </Popover.Root>
        </div>);
});
