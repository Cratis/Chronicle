import { InputText } from 'primereact/inputtext';
import { ChangeEvent } from 'react';

export interface QueryType {
    title: string;
    id: string;
    isEditing?: boolean;
}

export interface QueryHeaderProps {
    idx: number;
    query: QueryType;
    onToggleEdit: (idx: number) => void;
    onQueryChange: (e: ChangeEvent<HTMLInputElement>, idx: number) => void;
}

export const QueryHeader = (props: QueryHeaderProps) => {
    const { query, idx, onToggleEdit, onQueryChange } = props;

    const handleDoubleClick = () => {
        onToggleEdit(idx);
    };

    return (
        <div onClick={handleDoubleClick}>
            {query.isEditing ? (
                <InputText
                    autoFocus
                    value={query.title}
                    onBlur={() => onToggleEdit(idx)}
                    onChange={(e) => onQueryChange(e, idx)}
                />
            ) : (
                <span>{query.title}</span>
            )}
        </div>
    );
};
