namespace Galaxy2.SaveData.Utils;

public static class OsTime
{
    // The Wii system bus runs at 243 MHz.
    // The Time Base (TB) register updates at 1/4 of the bus speed.
    private const long TickFrequency = 60750000; // 60.75 MHz

    // Wii Epoch is Jan 1, 2000
    private static readonly DateTime WiiEpoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Converts a Wii OSTime tick value to a DateTime object.
    /// </summary>
    /// <remarks>
    /// .NET DateTime uses ticks of 100 nanoseconds, while Wii OSTime uses ticks of approx. 16ns.
    /// Converting between the two will result in a very minimal loss of precision,
    /// causing slight changes in roundtrip unit tests.
    /// </remarks>
    public static DateTime WiiTicksToUnix(long ticks)
    {
        var seconds = (double)ticks / TickFrequency * 1000;
        return WiiEpoch.AddMilliseconds(seconds);
    }

    /// <summary>
    /// Converts a DateTime object to a Wii OSTime tick value.
    /// </summary>
    public static long UnixToWiiTicks(DateTime date)
    {
        var utcDate = date.ToUniversalTime();
        var difference = utcDate - WiiEpoch;
        return (long)(difference.TotalMilliseconds / 1000.0 * TickFrequency);
    }
}