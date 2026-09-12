using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PlexAniListSync.AniListNet.Helpers;

internal static class GqlParser
{
    public static IList<GqlSelection> ParseToSelections<TObject>()
    {
        return ParseToSelections(typeof(TObject));
    }

    public static IList<GqlSelection> ParseToSelections(Type type)
    {
        var elementType = type.GetElementType();
        if (elementType is not null)
            type = elementType;
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var properties = type.GetProperties(flags).Cast<MemberInfo>();
        var fields = type.GetFields(flags).Cast<MemberInfo>();
        var variables = properties.Concat(fields);
        var selections = new List<GqlSelection>();
        foreach (var variable in variables)
        {
            var selectionAttribute = variable.GetCustomAttribute<GqlSelectionAttribute>();
            if (selectionAttribute is null)
                continue;
            var subSelections = ParseToSelections(variable.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)variable).FieldType,
                MemberTypes.Property => ((PropertyInfo)variable).PropertyType,
                _ => throw new NotSupportedException($"Unsupported member type {variable.MemberType}")
            });
            var parameters = variable.GetCustomAttributes<GqlParameterAttribute>()
                .Select(attribute => new GqlParameter(attribute));
            var selection = new GqlSelection(selectionAttribute)
            {
                Parameters = parameters.ToList(),
                Selections = subSelections
            };
            if (!string.IsNullOrEmpty(selectionAttribute.Alias))
                selection.Alias = selectionAttribute.Alias;
            selections.Add(selection);
        }

        return selections;
    }

    public static string ParseToString(GqlSelection selection)
    {
        return ParseToString(new[] { selection });
    }

    public static string ParseToString(IEnumerable<GqlSelection> selections)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append('{');
        stringBuilder.Append(BuildSelections(selections));
        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }

    public static TObject? ParseFromJson<TObject>(JsonNode? node)
    {
        return node is null ? default : node.Deserialize<TObject>(GqlJson.Options);
    }

    private static string BuildSelections(IEnumerable<GqlSelection> selections)
    {
        var stringBuilder = new StringBuilder();
        foreach (var selection in selections)
        {
            stringBuilder.Append(stringBuilder.Length > 0 ? ',' : string.Empty);
            if (!string.IsNullOrEmpty(selection.Alias))
                stringBuilder.Append(selection.Alias + ":");
            stringBuilder.Append(selection.Name);
            if (selection.Parameters is { Count: > 0 })
                stringBuilder.Append($"({BuildParameters(selection.Parameters)})");
            if (selection.Selections is { Count: > 0 })
                stringBuilder.Append($"{{{BuildSelections(selection.Selections)}}}");
        }

        return stringBuilder.ToString();
    }

    private static string BuildParameters(IEnumerable<GqlParameter> parameters)
    {
        var stringBuilder = new StringBuilder();
        foreach (var parameter in parameters)
            stringBuilder.Append((stringBuilder.Length > 1 ? "," : string.Empty) + parameter.Name + ":" +
                                 ParseObjectString(parameter.Value));
        return stringBuilder.ToString();
    }

    private static string ParseObjectString(object? value)
    {
        return value switch
        {
            null => "null",
            string @string => ((Func<string>)(() =>
            {
                if (@string.Contains('\n'))
                    return $"\"\"\"{@string}\"\"\"";
                return @string.StartsWith('$') ? @string.TrimStart('$') : $"\"{@string}\"";
            }))(),
            bool @bool => @bool ? "true" : "false",
            Enum @enum => HelperUtilities.GetEnumMemberValue(@enum),
            IEnumerable<GqlParameter> parameters => ((Func<string>)(() =>
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append('{');
                foreach (var parameter in parameters)
                {
                    stringBuilder.Append(stringBuilder.Length > 1 ? "," : string.Empty);
                    stringBuilder.Append(parameter.Name);
                    stringBuilder.Append(':');
                    stringBuilder.Append(ParseObjectString(parameter.Value));
                }

                stringBuilder.Append('}');
                return stringBuilder.ToString();
            }))(),
            IEnumerable enumerable => ((Func<string>)(() =>
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append('[');
                foreach (var item in enumerable)
                {
                    stringBuilder.Append(stringBuilder.Length > 1 ? "," : string.Empty);
                    stringBuilder.Append(ParseObjectString(item));
                }

                stringBuilder.Append(']');
                return stringBuilder.ToString();
            }))(),
            _ => value.ToString() ?? "null"
        };
    }
}
