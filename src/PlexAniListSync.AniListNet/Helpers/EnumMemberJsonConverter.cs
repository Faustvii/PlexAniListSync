using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlexAniListSync.AniListNet.Helpers;

internal sealed class EnumMemberJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(EnumMemberJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

internal sealed class EnumMemberJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    private readonly Dictionary<string, TEnum> _fromString = new(StringComparer.Ordinal);
    private readonly Dictionary<TEnum, string> _toString = new();

    public EnumMemberJsonConverter()
    {
        foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var value = (TEnum)field.GetValue(null)!;
            var name = field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? field.Name;
            _fromString[name] = value;
            _toString[value] = name;
        }
    }

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (text is not null && _fromString.TryGetValue(text, out var value))
            return value;
        throw new JsonException($"Unable to convert \"{text}\" to enum {typeof(TEnum).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(_toString.TryGetValue(value, out var text) ? text : value.ToString());
    }
}
