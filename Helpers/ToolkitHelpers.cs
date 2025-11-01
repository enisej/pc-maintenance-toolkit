
namespace PcMaintenanceToolkit.Helpers
{
    public static class ToolkitHelpers
    {
        // UTC shortcut – safe for PostgreSQL timestamptz
        public static DateTime UtcNow => DateTime.UtcNow;

        // Remove 0x00 chars that break PostgreSQL text columns
        public static string RemoveNullChars(string? input) =>
            input?.Replace("\0", "", StringComparison.Ordinal) ?? "";
    }
}
