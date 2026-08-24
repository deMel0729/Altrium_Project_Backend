// written by the whole team 
using Microsoft.Data.SqlClient;

namespace Altrium_Project_Backend.Data
{
    // common helper methods for working with database values and SqlDataReader
    public static class DbHelpers
    {
        public static object Nullable(object? value) => value ?? DBNull.Value;
        public static string GetStringCol(this SqlDataReader r, string col) => r.GetString(r.GetOrdinal(col));
        public static int GetIntCol(this SqlDataReader r, string col) => r.GetInt32(r.GetOrdinal(col));
        public static bool GetBoolCol(this SqlDataReader r, string col) => r.GetBoolean(r.GetOrdinal(col));
        public static DateTime GetDateTimeCol(this SqlDataReader r, string col) => r.GetDateTime(r.GetOrdinal(col));
        public static string? GetNullableString(this SqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetString(r.GetOrdinal(col));
        public static int? GetNullableInt(this SqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? (int?)null : r.GetInt32(r.GetOrdinal(col));
        public static DateTime? GetNullableDateTime(this SqlDataReader r, string col) => r.IsDBNull(r.GetOrdinal(col)) ? (DateTime?)null : r.GetDateTime(r.GetOrdinal(col));

    }
}
