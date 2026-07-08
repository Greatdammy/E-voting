import { useEffect } from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { logout } from '../store/authSlice';

function isTokenValid(token, expiresAt) {
  if (!token || !expiresAt) {
    return false;
  }
  return new Date(expiresAt) > new Date();
}

export default function ProtectedRoute({ allowedRoles }) {
  const dispatch = useDispatch();
  const { token, role, expiresAt } = useSelector((state) => state.auth);
  const valid = isTokenValid(token, expiresAt);

  useEffect(() => {
    if (!valid) {
      dispatch(logout());
    }
  }, [valid, dispatch]);

  if (!valid) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(role)) {
    return <Navigate to="/elections" replace />;
  }

  return <Outlet />;
}
