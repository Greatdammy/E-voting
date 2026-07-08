using EVoting.Domain.Enums;

namespace EVoting.Application.Common;

public static class ElectionStatusCalculator
{
    public static ElectionStatus Compute(DateTime startDate, DateTime endDate, DateTime nowUtc)
    {
        if (nowUtc < startDate)
        {
            return ElectionStatus.Upcoming;
        }

        if (nowUtc < endDate)
        {
            return ElectionStatus.Active;
        }

        return ElectionStatus.Closed;
    }
}
