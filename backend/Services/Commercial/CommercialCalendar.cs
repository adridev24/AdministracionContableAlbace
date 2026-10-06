namespace BudgetControl.Api.Services.Commercial;

/// <summary>Calendar deadlines use the business day in Argentina, not UTC time of day.</summary>
public static class CommercialCalendar
{
    private static readonly TimeZoneInfo BusinessZone = FindBusinessZone();

    private static TimeZoneInfo FindBusinessZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires"); }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
        }
    }

    public static DateTime Today => BusinessDate(DateTimeOffset.UtcNow);

    public static DateTime BusinessDate(DateTimeOffset instant) =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(instant, BusinessZone).Date, DateTimeKind.Utc);

    public static DateTime Normalize(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    public static bool IsOverdue(DateTime deadline) => deadline.Date < Today;
}
