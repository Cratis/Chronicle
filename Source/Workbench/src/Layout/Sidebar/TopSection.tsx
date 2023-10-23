import { FaUserCircle, FaBell } from 'react-icons/fa';

export const TopSection = () => {
    return (
        <div className='flex justify-evenly pt-5 px-2'>
            <div className='flex-1'>Logo</div>
            <div className='flex-1'>
                <div className='flex justify-end gap-3'>
                    <FaBell size={25} />
                    <FaUserCircle size={25} />
                </div>
            </div>
        </div>
    );
};
