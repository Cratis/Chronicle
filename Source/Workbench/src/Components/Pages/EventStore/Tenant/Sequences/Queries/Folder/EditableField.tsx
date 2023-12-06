import React from 'react';
import { InputText } from 'primereact/inputtext';
import { EditState } from './types';

interface EditableFieldProps {
    path: number[];
    editState: EditState;
    onAddItem: (path?: number[]) => void;
    onInput: (event: React.ChangeEvent<HTMLInputElement>, path: number[]) => void;
}

export const EditableField = (props: EditableFieldProps) => {
    const { editState, path, onAddItem, onInput } = props;
    return (
        <InputText
            style={{ marginLeft: 20 }}
            value={editState.name}
            onChange={(e) => onInput(e, path)}
            onBlur={() => onAddItem(path)}
            onKeyDown={(e) => {
                if (e.key === 'Enter') {
                    onAddItem(path);
                }
            }}
            autoFocus
        />
    );
};
