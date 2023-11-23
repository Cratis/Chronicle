import { FaBell, FaUserCircle } from "react-icons/fa";

export const Profile = () => {
    return (<div className='flex-1'>
        <div className={'flex justify-end gap-3 '}>
            <FaBell size={25}/>
            <FaUserCircle size={25}/>
        </div>
    </div>)
}