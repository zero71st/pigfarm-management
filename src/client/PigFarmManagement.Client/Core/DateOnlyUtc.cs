namespace PigFarmManagement.Client.Core;

public static class DateOnlyUtc
{
    public static DateTime AsUnspecifiedDate(DateTime value)
        => DateTime.SpecifyKind(value.Date, DateTimeKind.Unspecified);

    public static DateTime? AsUnspecifiedDate(DateTime? value)
        => value.HasValue ? AsUnspecifiedDate(value.Value) : null;
}
