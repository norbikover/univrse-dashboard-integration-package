using System;

namespace UniVRseDashboardIntegration
{
    public static class Extensions
    {
        /// <summary>
        /// Converts a DateTimw value to its long string representation.
        /// </summary>
        public static string ToLongString(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd-HH-mm-ss");
        }

        /// <summary>
        /// Converts an enum value to its string representation.
        /// </summary>
        public static string ToEnumString<T>(this T enumValue) where T : Enum
        {
            return enumValue.ToString();
        }

        /// <summary>
        /// Parses a string into an enum value of type T.
        /// Throws an exception if the string is invalid.
        /// </summary>
        public static T ToEnum<T>(this string value) where T : Enum
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase: true);
        }
    }
}
