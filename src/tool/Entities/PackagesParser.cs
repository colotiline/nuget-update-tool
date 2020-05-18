using System.Linq;
using System.Text.RegularExpressions;

namespace nut.Entities
{
    public static class PackagesParser
    {
        public static string[] Parse(string csprojContent)
        {
            var regex = new Regex
            (
                @"PackageReference Include=""([^""]*)""",
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );

            var matches = regex.Matches(csprojContent);

            return matches
                .Select
                (
                    _ => 
                        _
                        .Value
                        .Replace
                        (
                            "PackageReference Include=",
                            string.Empty
                        )
                        .Trim('"')
                )
                .ToArray();
        }
    }
}