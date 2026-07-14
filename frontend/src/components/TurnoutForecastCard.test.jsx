import { render, screen } from '@testing-library/react';
import TurnoutForecastCard from './TurnoutForecastCard';

beforeEach(() => {
  window.matchMedia =
    window.matchMedia ||
    function matchMedia() {
      return {
        matches: false,
        media: '',
        addListener: () => {},
        removeListener: () => {},
        addEventListener: () => {},
        removeEventListener: () => {},
        dispatchEvent: () => false
      };
    };
});

describe('TurnoutForecastCard', () => {
  it('shows a gathering-data placeholder when there is no forecast yet', () => {
    render(<TurnoutForecastCard history={[]} forecast={null} endDate="2030-01-10T00:00:00Z" />);

    expect(screen.getByText(/gathering data/i)).toBeInTheDocument();
    expect(screen.queryByText(/votes$/i)).not.toBeInTheDocument();
  });

  it('renders the projected total and confidence once a forecast is available', () => {
    const history = [
      { t: 1000, v: 10 },
      { t: 2000, v: 20 },
      { t: 3000, v: 30 }
    ];
    const forecast = { projectedVotes: 1234, confidence: 'medium', pointCount: 3 };

    render(<TurnoutForecastCard history={history} forecast={forecast} endDate="2030-01-10T00:00:00Z" />);

    expect(screen.getByText('~1,234 votes')).toBeInTheDocument();
    expect(screen.getByText('Medium confidence')).toBeInTheDocument();
    expect(screen.getByText(/estimated from 3 snapshots/i)).toBeInTheDocument();
    expect(screen.getByText(/not the/i)).toHaveTextContent('not the official count.');
  });
});
