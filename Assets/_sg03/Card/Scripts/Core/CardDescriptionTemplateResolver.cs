using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SG03
{
    /// <summary>
    /// Expands card-description tokens in the form <c>[group:key]</c> or
    /// <c>[group.key]</c>.
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
            @"\[(?<group>[a-zA-Z_][a-zA-Z0-9_]*)(?:\:|\.)(?<key>[a-zA-Z_][a-zA-Z0-9_]*)\]",
            RegexOptions.Compiled);

        // The key after the colon is intentionally not enumerated here.  For
        // example, [stats:summon_count] reads the summon_count member directly
        // from base_stats, and any future stats field works without changing
        // this resolver.
        private static readonly Dictionary<string, Func<CardDefinitionData, object>> Sources =
            new Dictionary<string, Func<CardDefinitionData, object>>(StringComparer.Ordinal)
            {
                { "metadata", definition => definition.metadata },
                { "card", definition => definition }
            };

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
            if (group == "stats") return definition.TryGetBaseStat(key, out value);

            if (group == "char_code")
            {
                // A character code is supplied by the template itself, e.g.
                // [char_code:skeleton]. Keep this as a dedicated group so it
                // can later be replaced with a localized character-name lookup.
                value = ToDisplayName(key);
                return true;
            }

            Func<CardDefinitionData, object> getSource;
            if (!Sources.TryGetValue(group, out getSource))
            {
                value = null;
                return false;
            }

            return TryGetMemberValue(getSource(definition), key, out value);
        }

        private static bool TryGetMemberValue(object source, string key, out string value)
        {
            value = null;
            if (source == null) return false;

            Type sourceType = source.GetType();
            FieldInfo field = sourceType.GetField(key, BindingFlags.Instance | BindingFlags.Public);
            object rawValue;

            if (field != null)
            {
                rawValue = field.GetValue(source);
            }
            else
            {
                PropertyInfo property = sourceType.GetProperty(key, BindingFlags.Instance | BindingFlags.Public);
                if (property == null || !property.CanRead) return false;
                rawValue = property.GetValue(source, null);
            }

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
