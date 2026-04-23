using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Sts.Admin.Utils
{
    public static class SlugUtils
    {
        public static string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            // 1. minuscules
            var str = input.ToLowerInvariant();

            // 2. enlever accents
            var normalized = str.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var unicodeCategory = Char.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            str = sb.ToString().Normalize(NormalizationForm.FormC);

            // 3. remplacer tout ce qui n’est pas alphanumérique par "-"
            str = Regex.Replace(str, @"[^a-z0-9]+", "-");

            // 4. trim des "-"
            return str.Trim('-');
        }
    }
}
