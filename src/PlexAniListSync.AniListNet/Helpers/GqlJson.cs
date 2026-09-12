using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace PlexAniListSync.AniListNet.Helpers;

internal static class GqlJson
{
    private const BindingFlags FieldFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(ApplyGqlSelections);
        return new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            Converters = { new EnumMemberJsonConverterFactory() },
        };
    }

    private static void ApplyGqlSelections(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        for (var index = typeInfo.Properties.Count - 1; index >= 0; index--)
        {
            var property = typeInfo.Properties[index];
            var member = property.AttributeProvider as MemberInfo;
            var attribute = member?.GetCustomAttribute<GqlSelectionAttribute>();
            if (attribute is null)
            {
                typeInfo.Properties.RemoveAt(index);
                continue;
            }

            property.Name = string.IsNullOrEmpty(attribute.Alias) ? attribute.Name : attribute.Alias;
            if (property.Set is null && member is PropertyInfo { CanWrite: true } writableProperty)
                property.Set = writableProperty.SetValue;
        }

        foreach (var field in EnumerateGqlFields(typeInfo.Type))
        {
            var attribute = field.GetCustomAttribute<GqlSelectionAttribute>()!;
            var name = string.IsNullOrEmpty(attribute.Alias) ? attribute.Name : attribute.Alias;
            var jsonProperty = typeInfo.CreateJsonPropertyInfo(field.FieldType, name);
            jsonProperty.Get = field.GetValue;
            jsonProperty.Set = field.SetValue;
            typeInfo.Properties.Add(jsonProperty);
        }
    }

    private static IEnumerable<FieldInfo> EnumerateGqlFields(Type type)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (var field in current.GetFields(FieldFlags))
            {
                if (field.IsPublic)
                    continue; // public fields are already surfaced by STJ
                if (field.GetCustomAttribute<GqlSelectionAttribute>() is not null)
                    yield return field;
            }
        }
    }
}
