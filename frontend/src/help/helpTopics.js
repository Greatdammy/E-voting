// Plain keyword-matched FAQ — no LLM, no ML, no network call. Deliberately
// NOT styled/branded as "AI" (see HelpWidget.jsx) so the app's real AI
// claims (Integrity Guard, Turnout Forecast) stay unambiguous.
const topics = [
  {
    keywords: ['register', 'sign up', 'signup', 'create account', 'create an account'],
    response:
      "To register, go to the Register page and enter your full name, email, and a password. You'll be signed up as a Voter — no separate verification step is needed before you can log in."
  },
  {
    keywords: ['log in', 'login', 'sign in', 'signin', "can't log in", 'cant log in'],
    response:
      'Log in with the email and password you registered with. If your session expires, you\'ll be redirected to the Login page automatically.'
  },
  {
    keywords: ['vote', 'voting', 'ballot', 'cast'],
    response:
      "To vote: go to Elections, find one marked Active, click Vote to open its ballot, select a candidate, and submit. You'll get a confirmation receipt afterward — you can only vote once per election."
  },
  {
    keywords: ['upcoming', 'active', 'closed', 'status'],
    response:
      "Upcoming means the election hasn't started yet. Active means voting is open right now. Closed means voting has ended and the tally is final."
  },
  {
    keywords: ['receipt', 'confirmation', 'hash', 'confirmation hash'],
    response:
      "After you vote, you get a confirmation hash — a code that proves your vote was recorded without revealing who you voted for. It's verifiable, but it can't be used by anyone (including admins) to look up your choice."
  },
  {
    keywords: ['results', 'live', 'tally', 'count'],
    response:
      "Results pages update live as votes come in while an election is Active, and show the final tally once it's Closed. You don't need to refresh the page."
  },
  {
    keywords: ['dark mode', 'theme', 'light mode'],
    response: 'Use the sun/moon icon in the navigation bar to switch between light and dark mode. Your choice is remembered on this device.'
  }
];

const fallback =
  "Sorry, I didn't understand that. I can help with: registering, logging in, voting, what election statuses mean, your confirmation receipt, live results, and dark mode.";

export function findAnswer(message) {
  const normalized = message.toLowerCase();
  const match = topics.find((topic) => topic.keywords.some((keyword) => normalized.includes(keyword)));
  return match ? match.response : fallback;
}
