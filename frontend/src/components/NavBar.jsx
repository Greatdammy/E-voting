import { Link, useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { logout } from '../store/authSlice';

export default function NavBar() {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const { token, role } = useSelector((state) => state.auth);

  const handleLogout = () => {
    dispatch(logout());
    navigate('/login');
  };

  return (
    <nav className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
      <Link to="/" className="font-semibold text-lg">
        E-Voting
      </Link>
      <div className="flex items-center gap-4">
        {!token && (
          <>
            <Link to="/login">Login</Link>
            <Link to="/register">Register</Link>
          </>
        )}
        {token && role === 'Voter' && <Link to="/elections">Elections</Link>}
        {token && (role === 'Administrator' || role === 'ElectionOfficer') && (
          <Link to="/admin/elections">Admin</Link>
        )}
        {token && role === 'Administrator' && <Link to="/admin/users">Create User</Link>}
        {token && (
          <button type="button" onClick={handleLogout} className="text-red-600">
            Logout
          </button>
        )}
      </div>
    </nav>
  );
}
