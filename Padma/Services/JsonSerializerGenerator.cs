using System.Text.Json.Serialization;
using Padma.ViewModels;

namespace Padma.Services;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, WriteIndented = true)]
[JsonSerializable(typeof(AppSettingsJsonClass))]
public partial class JsonSerializerGenerator : JsonSerializerContext;