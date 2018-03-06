using System.IO;
using System.Linq;

namespace kissproxy.Lib {
    /// <summary>
    /// Extension class
    /// </summary>
    public static class Extensions {
        /// <summary>
        /// Replaces every forbidden char (forbidden for a filename) in the text
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ToValidFileName(this string text) {
            return Path.GetInvalidFileNameChars().Aggregate(text, (current, c) => current.Replace(c, '~'));
        }
    }
}