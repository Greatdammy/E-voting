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

describe('BallotPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the fetched candidates', async () => {
    axiosInstance.get.mockResolvedValueOnce({
      data: {
        electionId,
        title: 'Student Union Election',
        description: 'Pick your rep.',
        candidates: [
          { candidateId: 'c1', name: 'Alice', party: 'Independent', photoUrl: null },
          { candidateId: 'c2', name: 'Bob', party: 'Green', photoUrl: null }
        ]
      }
    });

    renderBallotPage();

    expect(await screen.findByText('Alice')).toBeInTheDocument();
    expect(screen.getByText('Bob')).toBeInTheDocument();
  });

  it('does not submit when no candidate is selected', async () => {
    axiosInstance.get.mockResolvedValueOnce({
      data: {
        electionId,
        title: 'Student Union Election',
        description: 'Pick your rep.',
        candidates: [{ candidateId: 'c1', name: 'Alice', party: 'Independent', photoUrl: null }]
      }
    });

    renderBallotPage();

    await screen.findByText('Alice');
    fireEvent.click(screen.getByRole('button', { name: /submit vote/i }));

    expect(screen.getByText('Select a candidate before submitting.')).toBeInTheDocument();
    expect(axiosInstance.post).not.toHaveBeenCalled();
  });

  it('submits the selected candidate and shows the confirmation panel', async () => {
    axiosInstance.get.mockResolvedValueOnce({
      data: {
        electionId,
        title: 'Student Union Election',
        description: 'Pick your rep.',
        candidates: [{ candidateId: 'c1', name: 'Alice', party: 'Independent', photoUrl: null }]
      }
    });
    axiosInstance.post.mockResolvedValueOnce({
      data: { voteId: 'vote-1', confirmationHash: 'hash-abc', votedAt: '2030-01-01T00:00:00Z' }
    });

    renderBallotPage();

    await screen.findByText('Alice');
    fireEvent.click(screen.getByRole('radio'));
    fireEvent.click(screen.getByRole('button', { name: /submit vote/i }));

    await waitFor(() => {
      expect(axiosInstance.post).toHaveBeenCalledWith(`/elections/${electionId}/vote`, { candidateId: 'c1' });
    });

    expect(await screen.findByText('Vote recorded')).toBeInTheDocument();
    expect(screen.getByText('hash-abc')).toBeInTheDocument();
  });
});
