using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PropertyMaintenance.Api.DTOs;

// Check the original JSON scale before decimal parsing can round an extremely
// small or high precision number. Scientific notation is evaluated exactly.
public sealed class VisitCostJsonConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Cost must be a decimal number.");
        var raw = Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan);
        var exponentIndex = raw.IndexOfAny(['e', 'E']);
        var mantissa = exponentIndex >= 0 ? raw[..exponentIndex] : raw;
        var exponent = 0;
        if (exponentIndex >= 0 && !int.TryParse(raw[(exponentIndex + 1)..], NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out exponent)) throw new JsonException("Cost is outside the supported range.");
        var point = mantissa.IndexOf('.');
        var scale = (point < 0 ? 0L : mantissa.Length - point - 1L) - exponent;
        if (scale > 2 || !reader.TryGetDecimal(out var cost))
            throw new JsonException("Cost must fit decimal(18,2) without rounding.");
        return cost;
    }
    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue) writer.WriteNumberValue(value.Value); else writer.WriteNullValue();
    }
}
