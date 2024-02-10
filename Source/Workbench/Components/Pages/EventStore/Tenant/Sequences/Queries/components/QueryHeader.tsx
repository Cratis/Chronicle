/* Copyright (c) Aksio Insurtech. All rights reserved.
   Licensed under the MIT license. See LICENSE file in the project root for full license information. */

import { Button } from 'primereact/button';
import css from '../Queries.module.css';
import { ChangeEvent } from 'react';
export interface QueryType {
    id: string;
    title: string;
    isEditing?: boolean;
}

export interface QueryHeaderProps {
    idx: number;
    query: QueryType;
    onToggleEdit: (idx: number) => void;
    onQueryChange: (evt: ChangeEvent<HTMLInputElement>, idx: number) => void;
}

export const QueryHeader = (props: QueryHeaderProps) => {
    const { query, idx, onToggleEdit, onQueryChange } = props;

    const handleEditClick = (evt: React.MouseEvent<HTMLButtonElement>) => {
        evt.stopPropagation();
        onToggleEdit(idx);
    };

    const handleKeyDown = (evt: React.KeyboardEvent<HTMLInputElement>) => {
        if (evt.key === 'Enter') {
            onToggleEdit(idx);
        }
    };
    return (
        <div>
            {query.isEditing ? (
                <input
                    autoFocus
                    value={query.title}
                    onKeyDown={handleKeyDown}
                    onBlur={() => onToggleEdit(idx)}
                    onChange={(e) => onQueryChange(e, idx)}
                />
            ) : (
                <span>{query.title}</span>
            )}
            <Button
                unstyled
                icon='pi pi-pencil'
                onClick={handleEditClick}
                className={css.editButton}
            />
        </div>
    );
};
