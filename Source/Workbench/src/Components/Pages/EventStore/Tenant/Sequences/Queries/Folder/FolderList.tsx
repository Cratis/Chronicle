import { getItemByPath, initialData } from './utils';
import { FolderItem } from './FolderItem';
import { EditState } from './types';
import { useState } from 'react';

export interface FolderListProps {
    val?: unknown;
}

export const FolderList = (props: FolderListProps) => {
    const [data, setData] = useState(initialData);
    const [editState, setEditState] = useState<EditState>({
        path: [],
        isEditing: false,
        isAdding: false,
    });

    const handleDoubleClick = (path: number[]) => {
        const item = getItemByPath(data, path);
        if (item) {
            if (item.isAddButton) {
                setEditState({ path, isEditing: false, isAdding: true, name: '' });
            } else if (item.isEditable) {
                setEditState({ path, isEditing: true, isAdding: false, name: item.name });
            }
        }
    };

    const handleInput = (event: React.ChangeEvent<HTMLInputElement>, path: number[]) => {
        const newValue = event.target.value;
        setEditState((prevState) => ({ ...prevState, name: newValue }));

        if (!editState.isAdding) {
            updateData(path, newValue);
        }
    };

    const handleAddItem = (path?: number[]) => {
        setData((prevData) => {
            const newData = JSON.parse(JSON.stringify(prevData));

            if (editState.isAdding) {
                const newFolder = {
                    name: editState.name,
                    isEditable: true,
                    children: [],
                };

                if (path && path.length > 0) {
                    let parent = getItemByPath(newData, path.slice(0, -1));
                    if (parent && parent.children) {
                        parent.children.push(newFolder);
                    }
                } else {
                    newData.push(newFolder);
                }
            } else if (editState.isEditing && path) {
                let currentItem = getItemByPath(newData, path);
                if (currentItem) {
                    currentItem.name = editState.name;
                }
            }
            setEditState({ path: [], isEditing: false, isAdding: false, name: '' });

            return newData;
        });
    };

    const updateData = (path: number[], newValue: string) => {
        setData((prevData) => {
            const newData = JSON.parse(JSON.stringify(prevData));
            let currentItem;
            if (path.length > 1) {
                const parentItem = getItemByPath(newData, path.slice(0, -1));
                currentItem = parentItem.children[path[path.length - 1]];
            } else {
                currentItem = newData[path[0]];
            }

            if (currentItem) {
                currentItem.name = newValue;
            }

            return newData;
        });
    };

    return (
        <div>
            {data.map((item, index) => (
                <FolderItem
                    key={index}
                    item={item}
                    path={[index]}
                    setData={setData}
                    onInput={handleInput}
                    editState={editState}
                    onAddItem={handleAddItem}
                    onDoubleClick={handleDoubleClick}
                />
            ))}
        </div>
    );
};
