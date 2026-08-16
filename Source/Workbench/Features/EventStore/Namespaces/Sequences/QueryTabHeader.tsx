// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef, useState } from 'react';
import { InputText } from 'primereact/inputtext';
import './QueryTabHeader.css';

/**
 * Props for {@link QueryTabHeader}.
 */
export interface QueryTabHeaderProps {
    /** The name shown on the tab. */
    name: string;

    /** Whether the query carries edits that have not been written back. */
    hasUnsavedChanges: boolean;

    /** Called with the new name once the user finishes renaming. */
    onRename: (name: string) => void;
}

/**
 * The name on a query's tab, renamed in place by double-clicking the tab.
 *
 * The name is edited where it is shown - here and on the node in the hierarchy - rather than
 * through a field in the query itself, which would take up room on every query to change something
 * that is changed rarely.
 * @param props The {@link QueryTabHeaderProps}.
 * @returns The rendered header.
 */
export const QueryTabHeader = ({ name, hasUnsavedChanges, onRename }: QueryTabHeaderProps) => {
    const [isRenaming, setIsRenaming] = useState(false);
    const [draft, setDraft] = useState(name);
    const inputRef = useRef<HTMLInputElement>(null);
    const labelRef = useRef<HTMLSpanElement>(null);

    // The tab is a padded element that this only fills a sliver of, so listening on the label alone
    // would leave most of the tab dead to a double-click. The listener goes on the tab itself,
    // which is PrimeReact's element rather than ours - hence reaching for it rather than binding
    // through JSX.
    useEffect(() => {
        const tab = labelRef.current?.closest('[role="tab"]');
        if (!tab) return;

        const startRenaming = () => setIsRenaming(true);
        tab.addEventListener('dblclick', startRenaming);

        return () => tab.removeEventListener('dblclick', startRenaming);
    }, [isRenaming]);

    useEffect(() => {
        if (isRenaming) {
            setDraft(name);
            inputRef.current?.focus();
            inputRef.current?.select();
        }
    }, [isRenaming, name]);

    const commit = () => {
        setIsRenaming(false);
        const trimmed = draft.trim();
        if (trimmed && trimmed !== name) onRename(trimmed);
    };

    if (isRenaming) {
        return (
            <InputText
                ref={inputRef}
                className='query-tab-header__rename'
                value={draft}
                onChange={(event: React.ChangeEvent<HTMLInputElement>) => setDraft(event.target.value)}
                onBlur={commit}
                onClick={(event: React.MouseEvent<HTMLInputElement>) => event.stopPropagation()}
                onDoubleClick={(event: React.MouseEvent<HTMLInputElement>) => event.stopPropagation()}
                onKeyDown={(event: React.KeyboardEvent<HTMLInputElement>) => {
                    // The box sits inside the tab, which acts on Space and Enter to select
                    // the tab - left to bubble, a space would move the tab rather than reach the name.
                    event.stopPropagation();
                    if (event.key === 'Enter') commit();
                    if (event.key === 'Escape') setIsRenaming(false);
                }} />
        );
    }

    return (
        <span ref={labelRef} className='query-tab-header'>
            {name}
            {hasUnsavedChanges && <span className='query-tab-header__unsaved' aria-hidden='true' />}
        </span>
    );
};
