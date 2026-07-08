import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import { configureStore } from '@reduxjs/toolkit';
import authReducer from '../store/authSlice';
import LoginPage from './LoginPage';
import axiosInstance from '../api/axiosInstance';

vi.mock('../api/axiosInstance', () => ({
  default: {
    post: vi.fn()
  }
}));

function renderLoginPage(initialEntries = ['/login']) {
  const store = configureStore({ reducer: { auth: authReducer } });
  render(
    <Provider store={store}>
      <MemoryRouter initialEntries={initialEntries}>
        <LoginPage />
      </MemoryRouter>
    </Provider>
  );
  return store;
}

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('submits credentials and stores the session on success', async () => {
    axiosInstance.post.mockResolvedValueOnce({
      data: { token: 'abc123', expiresAt: '2030-01-01T00:00:00Z', userId: 'user-1', role: 'Voter' }
    });

    const store = renderLoginPage();

    fireEvent.change(screen.getByPlaceholderText('Email'), { target: { value: 'voter@example.com' } });
    fireEvent.change(screen.getByPlaceholderText('Password'), { target: { value: 'Password1' } });
    fireEvent.click(screen.getByRole('button', { name: /login/i }));

    await waitFor(() => {
      expect(axiosInstance.post).toHaveBeenCalledWith('/auth/login', {
        email: 'voter@example.com',
        password: 'Password1'
      });
    });

    await waitFor(() => {
      expect(store.getState().auth.token).toBe('abc123');
    });
  });

  it('shows an error message when login fails', async () => {
    axiosInstance.post.mockRejectedValueOnce({
      response: { data: { message: 'Invalid email or password.' } }
    });

    renderLoginPage();

    fireEvent.change(screen.getByPlaceholderText('Email'), { target: { value: 'voter@example.com' } });
    fireEvent.change(screen.getByPlaceholderText('Password'), { target: { value: 'WrongPassword' } });
    fireEvent.click(screen.getByRole('button', { name: /login/i }));

    expect(await screen.findByText('Invalid email or password.')).toBeInTheDocument();
  });

  it('shows the registration success banner when navigated with registered state', () => {
    renderLoginPage([{ pathname: '/login', state: { email: 'new@example.com', registered: true } }]);

    expect(screen.getByText(/registration successful/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Email').value).toBe('new@example.com');
  });
});
