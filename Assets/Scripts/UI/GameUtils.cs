/// <summary>
/// Shared utilities used by multiple systems.
/// </summary>
public static class GameUtils
{
    /// Formats a large number into a readable abbreviation.
    /// e.g. 1234567 -> "1.23M"
    public static string FormatNumber(double n)
    {
        if (n >= 1e18) return $"{n / 1e18:F2}Qi";
        if (n >= 1e15) return $"{n / 1e15:F2}Qa";
        if (n >= 1e12) return $"{n / 1e12:F2}T";
        if (n >= 1e9)  return $"{n / 1e9:F2}B";
        if (n >= 1e6)  return $"{n / 1e6:F2}M";
        if (n >= 1e3)  return $"{n / 1e3:F2}K";
        return $"{n:F0}";
    }
}
