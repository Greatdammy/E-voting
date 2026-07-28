import { fireEvent, render, screen } from '@testing-library/react';
import HelpWidget from './HelpWidget';

function openWidget() {
  fireEvent.click(screen.getByRole('button', { name: /open help/i }));
}

function ask(question) {
  fireEvent.change(screen.getByPlaceholderText(/ask a question/i), { target: { value: question } });
  fireEvent.click(screen.getByRole('button', { name: /send/i }));
}

describe('HelpWidget', () => {
  it('is closed by default', () => {
    render(<HelpWidget />);

    expect(screen.queryByPlaceholderText(/ask a question/i)).not.toBeInTheDocument();
  });

  it('opens the panel when the toggle button is clicked', () => {
    render(<HelpWidget />);

    openWidget();

    expect(screen.getByPlaceholderText(/ask a question/i)).toBeInTheDocument();
  });

  it('shows the matching canned response for a recognized keyword', () => {
    render(<HelpWidget />);

    openWidget();
    ask('how do I vote');

    expect(screen.getByText(/find one marked Active/i)).toBeInTheDocument();
  });

  it('shows the fallback message for an unrecognized question', () => {
    render(<HelpWidget />);

    openWidget();
    ask('asdkjhasd');

    expect(screen.getByText(/didn't understand/i)).toBeInTheDocument();
  });

  it('matches a natural phrasing that is not an exact keyword, via scoring', () => {
    render(<HelpWidget />);

    openWidget();
    ask("what's the confirmation hash for?");

    expect(screen.getByText(/proves your vote was recorded/i)).toBeInTheDocument();
  });

  it('responds to a greeting instead of falling back', () => {
    render(<HelpWidget />);

    openWidget();
    ask('hello, what can you do?');

    expect(screen.getByText(/ask me about registering/i)).toBeInTheDocument();
  });
});
