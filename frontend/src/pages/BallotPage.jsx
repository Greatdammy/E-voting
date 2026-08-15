import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { CheckCircle2, Mail, Vote } from 'lucide-react';
import axiosInstance from '../api/axiosInstance';
import { extractErrorMessage } from '../api/extractErrorMessage';
import Card from '../components/ui/Card';
import Button from '../components/ui/Button';
import Spinner from '../components/ui/Spinner';
import OtpInput from '../components/ui/OtpInput';

const RESEND_COOLDOWN_MS = 60_000;

export default function BallotPage() {
  const { id } = useParams();
  const [ballot, setBallot] = useState(null);
  const [selectedCandidateId, setSelectedCandidateId] = useState('');
  const [error, setError] = useState('');
  const [confirmation, setConfirmation] = useState(null);
  const [otpInfo, setOtpInfo] = useState(null); // { expiresAt, maskedEmail }
  const [otpCode, setOtpCode] = useState('');
  const [sendingCode, setSendingCode] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [resendAvailableAt, setResendAvailableAt] = useState(null);
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    axiosInstance
      .get(`/elections/${id}/ballot`)
      .then((response) => setBallot(response.data))
      .catch((err) => setError(extractErrorMessage(err, 'Could not load the ballot.')));
  }, [id]);

  useEffect(() => {
    if (!otpInfo) {
      return undefined;
    }

    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, [otpInfo]);

  const requestCode = async () => {
    if (!selectedCandidateId) {
      setError('Select a candidate before requesting a code.');
      return;
    }

    setSendingCode(true);
    setError('');

    try {
      const response = await axiosInstance.post(`/elections/${id}/otp/request`);
      setOtpInfo({ expiresAt: response.data.expiresAt, maskedEmail: response.data.maskedEmail });
      setResendAvailableAt(Date.now() + RESEND_COOLDOWN_MS);
      setOtpCode('');
    } catch (err) {
      setError(extractErrorMessage(err, 'Could not send a verification code.'));
    } finally {
      setSendingCode(false);
    }
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (otpCode.length !== 6) {
      setError('Enter the 6-digit code sent to your email.');
      return;
    }

    setSubmitting(true);
    setError('');

    try {
      const response = await axiosInstance.post(`/elections/${id}/vote`, {
        candidateId: selectedCandidateId,
        otpCode
      });
      setConfirmation(response.data);
    } catch (err) {
      setError(extractErrorMessage(err, 'Could not submit your vote.'));
    } finally {
      setSubmitting(false);
    }
  };

  if (confirmation) {
    return (
      <div className="mx-auto max-w-lg text-center">
        <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-emerald-100 text-emerald-600 dark:bg-emerald-500/10 dark:text-emerald-400">
          <CheckCircle2 className="h-7 w-7" />
        </div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Vote recorded</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Keep this confirmation for your records — it verifies your vote without revealing your choice.
        </p>

        <Card className="mt-6 space-y-3 p-5 text-left text-sm">
          <div className="flex justify-between gap-4">
            <span className="text-slate-500 dark:text-slate-400">Vote ID</span>
            <span className="font-mono text-slate-900 dark:text-slate-100">{confirmation.voteId}</span>
          </div>
          <div className="flex justify-between gap-4">
            <span className="text-slate-500 dark:text-slate-400">Confirmation hash</span>
            <span className="break-all font-mono text-slate-900 dark:text-slate-100">
              {confirmation.confirmationHash}
            </span>
          </div>
          <div className="flex justify-between gap-4">
            <span className="text-slate-500 dark:text-slate-400">Voted at</span>
            <span className="text-slate-900 dark:text-slate-100">
              {new Date(confirmation.votedAt).toLocaleString()}
            </span>
          </div>
        </Card>

        <Link to={`/elections/${id}/results`}>
          <Button variant="secondary" className="mt-6">
            View live results
          </Button>
        </Link>
      </div>
    );
  }

  if (error && !ballot) {
    return <p className="text-rose-600 dark:text-rose-400">{error}</p>;
  }

  if (!ballot) {
    return <Spinner label="Loading ballot..." />;
  }

  // Stage 2: a code has been requested - verify it and submit the vote.
  if (otpInfo) {
    const resendLocked = resendAvailableAt !== null && now < resendAvailableAt;
    const resendSecondsLeft = resendLocked ? Math.ceil((resendAvailableAt - now) / 1000) : 0;
    const expiresInSeconds = Math.max(0, Math.ceil((new Date(otpInfo.expiresAt).getTime() - now) / 1000));

    return (
      <form onSubmit={handleSubmit} className="mx-auto max-w-md space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Enter your verification code</h1>
          <p className="mt-1 flex items-center gap-1.5 text-sm text-slate-500 dark:text-slate-400">
            <Mail className="h-4 w-4" />
            Sent to {otpInfo.maskedEmail}
            {expiresInSeconds > 0 ? ` · expires in ${Math.ceil(expiresInSeconds / 60)} min` : ' · expired'}
          </p>
        </div>

        <OtpInput id="otp-code" value={otpCode} onChange={setOtpCode} disabled={submitting} />

        {error && <p className="text-sm text-rose-600 dark:text-rose-400">{error}</p>}

        <div className="flex flex-col gap-2">
          <Button type="submit" disabled={submitting}>
            <Vote className="h-4 w-4" />
            {submitting ? 'Submitting...' : 'Submit vote'}
          </Button>
          <Button
            type="button"
            variant="secondary"
            disabled={resendLocked || sendingCode}
            onClick={requestCode}
          >
            {sendingCode ? 'Sending...' : resendLocked ? `Resend code (${resendSecondsLeft}s)` : 'Resend code'}
          </Button>
          <Button type="button" variant="ghost" onClick={() => setOtpInfo(null)}>
            Change candidate
          </Button>
        </div>
      </form>
    );
  }

  // Stage 1: pick a candidate, then request a code.
  return (
    <div className="mx-auto max-w-xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">{ballot.title}</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{ballot.description}</p>
      </div>

      <div className="space-y-2">
        {ballot.candidates.map((candidate) => (
          <label
            key={candidate.candidateId}
            className="flex cursor-pointer items-center gap-3 rounded-xl border border-slate-200 bg-white p-4 [&:has(:checked)]:border-indigo-500 [&:has(:checked)]:ring-2 [&:has(:checked)]:ring-indigo-500/30 dark:border-slate-800 dark:bg-slate-900"
          >
            <input
              type="radio"
              name="candidate"
              value={candidate.candidateId}
              checked={selectedCandidateId === candidate.candidateId}
              onChange={() => setSelectedCandidateId(candidate.candidateId)}
              className="h-4 w-4 border-slate-300 text-indigo-600 focus:ring-indigo-500"
            />
            <span>
              <span className="block font-medium text-slate-900 dark:text-white">{candidate.name}</span>
              <span className="text-sm text-slate-500 dark:text-slate-400">{candidate.party}</span>
            </span>
          </label>
        ))}
      </div>

      {error && <p className="text-sm text-rose-600 dark:text-rose-400">{error}</p>}

      <Button type="button" onClick={requestCode} disabled={sendingCode}>
        <Mail className="h-4 w-4" />
        {sendingCode ? 'Sending code...' : 'Send verification code'}
      </Button>
    </div>
  );
}