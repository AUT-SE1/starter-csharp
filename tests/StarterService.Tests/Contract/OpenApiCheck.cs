using System.Text.Json;

namespace StarterService.Tests.Contract;

/// <summary>
/// Validates a live response against the shared OpenAPI document.
///
/// This understands the small slice of JSON Schema that openapi.yaml actually
/// uses: $ref, type, required, properties, enum and the numeric and string
/// bounds. It is deliberately dependency-free. If you would rather use a real
/// validator, NJsonSchema is the usual .NET choice — swap it in and keep the
/// tests as they are.
/// </summary>
public sealed class OpenApiCheck
{
    private readonly JsonElement _root;

    private OpenApiCheck(JsonElement root) => _root = root;

    /// <summary>Reads an OpenAPI document. Pass the .json file, not the .yaml.</summary>
    public static OpenApiCheck Load(string path)
    {
        var document = JsonDocument.Parse(File.ReadAllText(path));
        return new OpenApiCheck(document.RootElement.Clone());
    }

    private JsonElement Schema(string name)
    {
        if (_root.TryGetProperty("components", out var components)
            && components.TryGetProperty("schemas", out var schemas)
            && schemas.TryGetProperty(name, out var schema))
        {
            return schema;
        }

        throw new ArgumentException($"schema {name} is not defined in the document", nameof(name));
    }

    /// <summary>Returns every problem found, so one failure names all of them.</summary>
    public IReadOnlyList<string> Validate(string schemaName, JsonElement value)
    {
        var problems = new List<string>();
        Check(Schema(schemaName), value, schemaName, problems);
        return problems;
    }

    private JsonElement Resolve(JsonElement schema)
    {
        if (!schema.TryGetProperty("$ref", out var reference))
        {
            return schema;
        }

        // Only local refs of the form #/components/schemas/Name are used here.
        var text = reference.GetString() ?? string.Empty;
        return Schema(text[(text.LastIndexOf('/') + 1)..]);
    }

    private void Check(JsonElement schema, JsonElement value, string path, List<string> problems)
    {
        schema = Resolve(schema);
        var expectedType = schema.TryGetProperty("type", out var type) ? type.GetString() : null;

        if (expectedType is not null && !MatchesType(expectedType, value))
        {
            problems.Add($"{path}: expected type {expectedType}, got {Describe(value)}");
            return;
        }

        if (schema.TryGetProperty("enum", out var allowed) && !Contains(allowed, value))
        {
            problems.Add($"{path}: value {value} is not one of {allowed}");
        }

        switch (expectedType)
        {
            case "object":
                if (schema.TryGetProperty("required", out var required))
                {
                    foreach (var name in required.EnumerateArray())
                    {
                        if (!value.TryGetProperty(name.GetString()!, out _))
                        {
                            problems.Add($"{path}: required field \"{name.GetString()}\" is missing");
                        }
                    }
                }

                if (schema.TryGetProperty("properties", out var properties))
                {
                    foreach (var property in properties.EnumerateObject())
                    {
                        if (value.TryGetProperty(property.Name, out var fieldValue))
                        {
                            Check(property.Value, fieldValue, $"{path}.{property.Name}", problems);
                        }
                    }
                }

                break;

            case "string":
                var length = new System.Globalization.StringInfo(value.GetString() ?? string.Empty)
                    .LengthInTextElements;
                if (schema.TryGetProperty("minLength", out var min) && length < min.GetInt32())
                {
                    problems.Add($"{path}: shorter than minLength {min.GetInt32()}");
                }

                if (schema.TryGetProperty("maxLength", out var max) && length > max.GetInt32())
                {
                    problems.Add($"{path}: longer than maxLength {max.GetInt32()}");
                }

                break;

            case "integer":
            case "number":
                var number = value.GetDouble();
                if (schema.TryGetProperty("minimum", out var minimum) && number < minimum.GetDouble())
                {
                    problems.Add($"{path}: {number} is below minimum {minimum.GetDouble()}");
                }

                if (schema.TryGetProperty("maximum", out var maximum) && number > maximum.GetDouble())
                {
                    problems.Add($"{path}: {number} is above maximum {maximum.GetDouble()}");
                }

                break;
        }
    }

    private static bool MatchesType(string expected, JsonElement value) => expected switch
    {
        "object" => value.ValueKind == JsonValueKind.Object,
        "array" => value.ValueKind == JsonValueKind.Array,
        "string" => value.ValueKind == JsonValueKind.String,
        "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        "number" => value.ValueKind == JsonValueKind.Number,
        "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
        _ => true,
    };

    private static string Describe(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => "object",
        JsonValueKind.Array => "array",
        JsonValueKind.String => "string",
        JsonValueKind.True or JsonValueKind.False => "boolean",
        JsonValueKind.Number => value.TryGetInt64(out _) ? "integer" : "number",
        JsonValueKind.Null => "null",
        _ => value.ValueKind.ToString().ToLowerInvariant(),
    };

    private static bool Contains(JsonElement allowed, JsonElement value)
    {
        foreach (var candidate in allowed.EnumerateArray())
        {
            if (candidate.ValueKind == value.ValueKind
                && candidate.GetRawText() == value.GetRawText())
            {
                return true;
            }
        }

        return false;
    }
}
