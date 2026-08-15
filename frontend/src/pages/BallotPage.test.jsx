import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import BallotPage from './BallotPage';
import axiosInstance from '../api/axiosInstance';

vi.mock('../api/axiosInstance', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn()
  }
}));

const electionId = 'election-1';

function renderBallotPage() {
  render(
    <MemoryRouter initialEntries={[`/elections/${electionId}/ballot`]}>
      <Routes>
        <Route path="/elections/:id/ballot" element={<BallotPage />} />
      </Routes>
    </MemoryRouter>
  );
}

function mockBallot(candidates = [{ candidateId: 'c1', name: 'Alice', party: 'Independent', photoUrl: null }]) {
  axiosInstance.get.mockResolvedValueOnce({
    data: {
      electionId,
      title: 'Student Union Election',
      description: 'Pick your rep.',
      candidates
    }
  });
}

async function selectCandidateAndRequestCode() {
  await screen.findByText('Alice');
  fireEvent.click(screen.getByRole('radio'));
  fireEvent.click(screen.getByRole('button', { name: /send verification code/i }));
  await waitFor(() => expect(axiosInstance.post).toHaveBeenCalledWith(`/elections/${electionId}/otp/request`));
}

describe('BallotPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the fetched candidates', async () => {
    mockBallot([
      { candidateId: 'c1', name: 'Alice', party: 'Independent', photoUrl: null },
      { candidateId: 'c2', name: 'Bob', party: 'Green', photoUrl: null }
    ]);

    renderBallotPage();

    expect(await screen.findByText('Alice')).toBeInTheDocument();
    expect(screen.getByText('Bob')).toBeInTheDocument();
  });

  it('does not request a code when no candidate is selected', async () => {
    mockBallot();

    renderBallotPage();

    await screen.findByText('Alice');
    fireEvent.click(screen.getByRole('button', { name: /send verification code/i }));

    expect(screen.getByText('Select a candidate before requesting a code.')).toBeInTheDocument();
    expect(axiosInstance.post).not.toHaveBeenCalled();
  });

  it('requests a verification code and reveals the code-entry step', async () => {
    mockBallot();
    axiosInstance.post.mockResolvedValueOnce({
      data: { expiresAt: '2030-01-01T00:05:00Z', maskedEmail: 'vo***@example.com' }
    });

    renderBallotPage();
    await selectCandidateAndRequestCode();

    expect(await screen.findByText(/enter your verification code/i)).toBeInTheDocument();
    expect(screen.getByText(/vo\*\*\*@example\.com/)).toBeInTheDocument();
  });

  it('shows an error and stays on the candidate step when the code request fails', async () => {
    mockBallot();
    axiosInstance.post.mockRejectedValueOnce({
      response: { data: { message: 'You have already voted in this election.' } }
    });

    renderBallotPage();
    await screen.findByText('Alice');
    fireEvent.click(screen.getByRole('radio'));
    fireEvent.click(screen.getByRole('button', { name: /send verification code/i }));

    expect(await screen.findByText('You have already voted in this election.')).toBeInTheDocument();
    expect(screen.queryByText(/enter your verification code/i)).not.toBeInTheDocument();
  });

  it('submits the candidate and code, and shows the confirmation panel', async () => {
    mockBallot();
    axiosInstance.post.mockResolvedValueOnce({
      data: { expiresAt: '2030-01-01T00:05:00Z', maskedEmail: 'vo***@example.com' }
    });
    axiosInstance.post.mockResolvedValueOnce({
      data: { voteId: 'vote-1', confirmationHash: 'hash-abc', votedAt: '2030-01-01T00:00:00Z' }
    });

    renderBallotPage();
    await selectCandidateAndRequestCode();

    await screen.findByText(/enter your verification code/i);
    fireEvent.change(screen.getByLabelText(/verification code/i), { target: { value: '123456' } });
    fireEvent.click(screen.getByRole('button', { name: /submit vote/i }));

    await waitFor(() => {
      expect(axiosInstance.post).toHaveBeenCalledWith(`/elections/${electionId}/vote`, {
        candidateId: 'c1',
        otpCode: '123456'
      });
    });

    expect(await screen.findByText('Vote recorded')).toBeInTheDocument();
    expect(screen.getByText('hash-abc')).toBeInTheDocument();
  });

  it('shows an error and stays on the code step when the code is rejected', async () => {
    mockBallot();
    axiosInstance.post.mockResolvedValueOnce({
      data: { expiresAt: '2030-01-01T00:05:00Z', maskedEmail: 'vo***@example.com' }
    });
    axiosInstance.post.mockRejectedValueOnce({
      response: { data: { message: 'Incorrect verification code.' } }
    });

    renderBallotPage();
    await selectCandidateAndRequestCode();

    await screen.findByText(/enter your verification code/i);
    fireEvent.change(screen.getByLabelText(/verification code/i), { target: { value: '000000' } });
    fireEvent.click(screen.getByRole('button', { name: /submit vote/i }));

    expect(await screen.findByText('Incorrect verification code.')).toBeInTheDocument();
    expect(screen.getByText(/enter your verification code/i)).toBeInTheDocument();
  });
});