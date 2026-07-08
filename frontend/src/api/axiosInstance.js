import axios from 'axios';
import { store } from '../store/store';
import { logout } from '../store/authSlice';
import { API_BASE_URL } from '../config';

const axiosInstance = axios.create({
  baseURL: API_BASE_URL
});

axiosInstance.interceptors.request.use((config) => {
  const { token } = store.getState().auth;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      store.dispatch(logout());
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

export default axiosInstance;
