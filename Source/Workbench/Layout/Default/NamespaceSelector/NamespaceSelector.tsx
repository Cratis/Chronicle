// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo, useState, type ChangeEvent } from "react";

import { Popover, type PopoverRootOpenChangeEvent } from "primereact/popover";
import { useLayoutContext } from "../context/LayoutContext";
import { CurrentNamespace } from "./CurrentNamespace";
import { InputText } from 'primereact/inputtext';
import { ItemsList } from 'Components/ItemsList/ItemsList';
import { useWorkbenchContext } from '../context/WorkbenchContext';
import css from './NamespaceSelector.module.css';

export const NamespaceSelector = () => {
    const { layoutConfig } = useLayoutContext();
    const { namespace, namespaces, selectNamespace: select } = useWorkbenchContext();
    const [search, setSearch] = useState<string>('');
    const [isNamespacePanelOpen, setIsNamespacePanelOpen] = useState(false);

    const selectNamespace = (selected: string) => {
        select(selected);
        setIsNamespacePanelOpen(false);
    };

    const filteredNamespaces = useMemo(() => namespaces.filter((t) => t.toLowerCase().includes(search.toLowerCase())), [namespaces, search]);

    return (
        <div>
            <Popover.Root
                open={isNamespacePanelOpen}
                onOpenChange={(event: PopoverRootOpenChangeEvent) => setIsNamespacePanelOpen(event.value ?? false)}>
                <Popover.Trigger as="div">
                    <CurrentNamespace compact={!layoutConfig.leftSidebarOpen}
                        namespace={namespace} />
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
};
