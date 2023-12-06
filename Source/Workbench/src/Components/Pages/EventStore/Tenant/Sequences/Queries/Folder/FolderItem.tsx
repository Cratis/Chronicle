import { EditableField } from './EditableField';
import { Button } from 'primereact/button';
import { EditState, Item } from './types';
import { arraysEqual, getItemByPath } from './utils';
import React from 'react';

interface FolderItemProps {
    item: Item;
    path: number[];
    editState: EditState;
    onAddItem: (path?: number[]) => void;
    onDoubleClick: (path: number[]) => void;
    setData: React.Dispatch<React.SetStateAction<Item[]>>;
    onInput: (event: React.ChangeEvent<HTMLInputElement>, path: number[]) => void;
}

export const FolderItem = (props: FolderItemProps) => {
    const { item, path, editState, setData, onDoubleClick, onInput, onAddItem } = props;
    const isEditing = editState.isEditing && arraysEqual(editState.path, path);
    const isAdding = editState.isAdding && arraysEqual(editState.path, path);

    const renderFolderName = () => {
        if (isEditing) {
            return (
                <EditableField
                    editState={editState}
                    path={path}
                    onInput={onInput}
                    onAddItem={onAddItem}
                />
            );
        } else {
            return <div onDoubleClick={() => onDoubleClick(path)}>{item.name}</div>;
        }
    };

    return (
        <div key={path.join('-')} style={{ marginLeft: 10 }}>
            {item.isAddButton ? (
                <div>
                    <span>{item.name}</span>
                    <Button
                        rounded
                        icon='pi pi-plus'
                        onClick={() => onDoubleClick(path)}
                    />
                    {isAdding && (
                        <EditableField
                            editState={editState}
                            path={path}
                            onInput={onInput}
                            onAddItem={onAddItem}
                        />
                    )}
                </div>
            ) : (
                renderFolderName()
            )}

            {item && item.children && (
                <div className='children'>
                    {item.children.map((child, index) => (
                        <FolderItem
                            key={index}
                            item={child}
                            setData={setData}
                            onInput={onInput}
                            onAddItem={onAddItem}
                            editState={editState}
                            path={path.concat(index)}
                            onDoubleClick={onDoubleClick}
                        />
                    ))}
                </div>
            )}
        </div>
    );
};
