using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VirtoCommerce.Platform.Core.Common
{
    public static partial class StringExtensions
    {
        [GeneratedRegex(@"([A-Z]+)([A-Z][a-z])")]
        private static partial Regex FirstUpperCaseRegex();

        [GeneratedRegex(@"([a-z\d])([A-Z])")]
        private static partial Regex FirstLowerCaseRegex();

        [GeneratedRegex(@"[\[, \]]")]
        private static partial Regex IllegalRegex();

        private static readonly Regex _emailRegex = new(@"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-||_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+([a-z]+|\d|-|\.{0,1}|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])?([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))$", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        private static readonly string[] _allowedUriSchemes = [Uri.UriSchemeFile, Uri.UriSchemeFtp, Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto, Uri.UriSchemeNetPipe, Uri.UriSchemeNetTcp];

        public static bool IsAbsoluteUrl(this string url)
        {
            ArgumentNullException.ThrowIfNull(url);

            var result = Uri.IsWellFormedUriString(url, UriKind.Absolute) && _allowedUriSchemes.ContainsIgnoreCase(new Uri(url).Scheme);

            return result;
        }

        public static decimal TryParse(this string u, decimal defaultValue)
        {
            var result = defaultValue;

            if (!string.IsNullOrEmpty(u))
            {
                decimal.TryParse(u, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            }

            return result;

        }

        public static bool TryParse(this string u, bool defaultValue)
        {
            var result = defaultValue;

            if (!string.IsNullOrEmpty(u))
            {
                bool.TryParse(u, out result);
            }

            return result;

        }

        public static int TryParse(this string u, int defaultValue)
        {
            var result = defaultValue;

            if (!string.IsNullOrEmpty(u))
            {
                int.TryParse(u, out result);
            }

            return result;

        }

        public static int TryParse(this string u)
        {
            return TryParse(u, 0);
        }

        /// <summary>
        /// Like null coalescing operator (??) but including empty strings
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string IfNullOrEmpty(this string a, string b)
        {
            return string.IsNullOrEmpty(a) ? b : a;
        }

        /// <summary>
        /// If <paramref name="a"/> is empty, returns null
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static string EmptyToNull(this string a)
        {
            return string.IsNullOrEmpty(a) ? null : a;
        }

        public static bool EqualsOrNullEmpty(this string str1, string str2, StringComparison comparisonType)
        {
            return string.Compare(str1 ?? "", str2 ?? "", comparisonType) == 0;
        }

        [Obsolete("Use EqualsIgnoreCase()", DiagnosticId = "VC0010", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        public static bool EqualsInvariant(this string str1, string str2)
        {
            return string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EqualsIgnoreCase(this string str1, string str2)
        {
            return string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ContainsIgnoreCase(this string value, string substring)
        {
            return value.Contains(substring, StringComparison.OrdinalIgnoreCase);
        }

        public static bool StartsWithIgnoreCase(this string value, string substring)
        {
            return value.StartsWith(substring, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EndsWithIgnoreCase(this string value, string substring)
        {
            return value.EndsWith(substring, StringComparison.OrdinalIgnoreCase);
        }


        public static string GetCurrencyName(this string isoCurrencySymbol)
        {
            return CultureInfo
                .GetCultures(CultureTypes.AllCultures)
                .Where(c => !c.IsNeutralCulture && c.LCID != 127)
                .Select(culture =>
                {
                    try
                    {
                        return new RegionInfo(culture.LCID);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(ri => ri != null && ri.ISOCurrencySymbol == isoCurrencySymbol)
                .Select(ri => ri.CurrencyNativeName)
                .FirstOrDefault() ?? isoCurrencySymbol;
        }

        public static string ToSpecificLangCode(this string lang)
        {
            try
            {
                var culture = CultureInfo.CreateSpecificCulture(lang);
                return culture.Name;
            }
            catch
            {
                return "";
            }
        }

        public static string Truncate(this string value, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + suffix;
        }

        public static string EscapeSearchTerm(this string term)
        {
            char[] specialCharacters = ['+', '-', '!', '(', ')', '{', '}', '[', ']', '^', '"', '~', '*', '?', ':', '\\'];
            var result = new StringBuilder("");
            //'&&', '||',
            foreach (var ch in term)
            {
                if (specialCharacters.Any(x => x == ch))
                {
                    result.Append('\\');
                }
                result.Append(ch);
            }
            result = result.Replace("&&", @"\&&");
            result = result.Replace("||", @"\||");

            return result.ToString().Trim();
        }

        /// <summary>
        /// Escapes the selector. Query requires special characters to be escaped in query
        /// http://api.jquery.com/category/selectors/
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns></returns>
        public static string EscapeSelector(this string attribute)
        {
            return Regex.Replace(attribute, string.Format("([{0}])", "/[!\"#$%&'()*+,./:;<=>?@^`{|}~\\]"), @"\\$1");
        }

        public static string GenerateSlug(this string phrase)
        {
            var str = phrase.RemoveAccent().ToLower();

            str = Regex.Replace(str, @"[^a-z0-9\s-]", ""); // invalid chars           
            str = Regex.Replace(str, @"\s+", " ").Trim(); // convert multiple spaces into one space   
            str = str.Substring(0, str.Length <= 240 ? str.Length : 240).Trim(); // cut and trim it   
            str = Regex.Replace(str, @"\s", "-"); // hyphens   

            return str;
        }

        /// <summary>
        /// Only english characters,
        /// Numbers are allowed,  
        /// Dashes are allowed, 
        /// Spaces are replaced by dashes, 
        /// Nothing else is allowed,
        /// Possibly you could replace ;amp by "-and-"
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string MakeFileNameWebSafe(this string fileName)
        {
            var str = fileName.RemoveAccent().ToLower();
            str = str.Replace("&", "-and-");
            str = Regex.Replace(str, @"[^A-Za-z0-9_\-. ]", ""); // invalid chars           
            str = Regex.Replace(str, @"\s+", "-").Trim(); // convert multiple spaces into one dash
            str = str.Substring(0, str.Length <= 240 ? str.Length : 240).Trim(); // cut and trim it   
            return str;
        }

        public static string RemoveAccent(this string txt)
        {
            var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(txt);
            return Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int ComputeLevenshteinDistance(this string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (var i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (var j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (var i = 1; i <= n; i++)
            {
                //Step 4
                for (var j = 1; j <= m; j++)
                {
                    // Step 5
                    var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public static T? ToNullable<T>(this string s) where T : struct
        {
            var result = new T?();
            try
            {
                if (!string.IsNullOrEmpty(s) && s.Trim().Length > 0)
                {
                    var conv = TypeDescriptor.GetConverter(typeof(T));
                    result = (T)conv?.ConvertFromInvariantString(s);
                }
            }
            catch
            {
                // Return default value in case of exception.
            }

            return result;
        }

        public static string[] LeftJoin(this IEnumerable<string> left, IEnumerable<string> right, string delimiter)
        {
            right ??= [];

            return left.Join(right.DefaultIfEmpty(string.Empty), _ => true, _ => true, (x, y) => string.Join(delimiter, new[] { x, y }.Where(z => !string.IsNullOrEmpty(z)))).ToArray();
        }

        public static string FirstCharToUpper(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return input.First().ToString().ToUpper() + input.Substring(1);
        }

        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static bool IsValidEmail(this string input)
        {
            return input != null && _emailRegex.IsMatch(input);
        }

        public static string ToSnakeCase(this string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            name = IllegalRegex().Replace(name, "_").TrimEnd('_');
            // Replace any capital letters, apart from the first character, with _x, the same way Ruby does
            return FirstLowerCaseRegex().Replace(FirstUpperCaseRegex().Replace(name, "$1_$2"), "$1_$2").ToLower();
        }
    }
}
