using System;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SG03
{
    /// <summary>
    /// Expands card-description tokens in the form <c>[group:key]</c>.
    ///
    /// Current groups are <c>stats</c> (CardDefinitionData.base_stats),
    /// <c>metadata</c> (CardDefinitionData.metadata), <c>card</c>
    /// (CardDefinitionData), and <c>char_code</c>. Add a group in
    /// <see cref="TryResolve"/> when a new kind of description value is introduced.
    /// Unknown tokens deliberately remain visible instead of silently becoming an
    /// incorrect value.
    /// </summary>
    public static class CardDescriptionTemplateResolver
    {
        private static readonly Regex TokenPattern = new Regex(
            @"\[(?<group>[a-zA-Z_][a-zA-Z0-9_]*):(?<key>[a-zA-Z_][a-zA-Z0-9_]*)\]",
            RegexOptions.Compiled);

        public static string Resolve(string template, CardDefinitionData definition)
        {
            if (string.IsNullOrEmpty(template) || definition == null) return template;

            return TokenPattern.Replace(template, match =>
            {
                string value;
                return TryResolve(match.Groups["group"].Value, match.Groups["key"].Value, definition, out value)
                    ? value
                    : match.Value;
            });
        }

        private static bool TryResolve(string group, string key, CardDefinitionData definition, out string value)
        {
            switch (group)
            {
                case "stats":
                    return TryGetFieldValue(definition.base_stats, key, out value);
                case "metadata":
                    return TryGetFieldValue(definition.metadata, key, out value);
                case "card":
                    return TryGetFieldValue(definition, key, out value);
                case "char_code":
                    // A character code is supplied by the template itself, e.g.
                    // [char_code:skeleton]. Keep this as a dedicated group so it
                    // can later be replaced with a localized character-name lookup.
                    value = ToDisplayName(key);
                    return true;
                default:
                    value = null;
                    return false;
            }
        }

        private static bool TryGetFieldValue(object source, string key, out string value)
        {
            value = null;
            if (source == null) return false;

            FieldInfo field = source.GetType().GetField(key, BindingFlags.Instance | BindingFlags.Public);
            if (field == null) return false;

            object rawValue = field.GetValue(source);
            if (rawValue == null) return false;

            value = Convert.ToString(rawValue, CultureInfo.InvariantCulture);
            return true;
        }

        private static string ToDisplayName(string code)
        {
            return code.Replace('_', ' ');
        }
    }
}
