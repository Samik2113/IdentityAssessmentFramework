using System.Text.Json;
using System.Text.Json.Serialization;

namespace IamMaturityStudio.Infrastructure.Seeding.Contracts;

public sealed record QuestionnaireSeedRoot(
    [property: JsonPropertyName("questionnaire")] SeedQuestionnaire Questionnaire,
    [property: JsonPropertyName("scoring")] JsonElement? Scoring,
    [property: JsonPropertyName("metadata")] JsonElement? Metadata);

public sealed record SeedQuestionnaire(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("domains")] IReadOnlyList<SeedDomain> Domains);

public sealed record SeedDomain(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("categories")] IReadOnlyList<SeedCategory> Categories);

public sealed record SeedCategory(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("weight")] decimal Weight,
    [property: JsonPropertyName("business_risk")] string BusinessRisk,
    [property: JsonPropertyName("questions")] IReadOnlyList<SeedQuestion> Questions);

public sealed record SeedQuestion(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("default_weight")] decimal DefaultWeight,
    [property: JsonPropertyName("evidence_required")] bool EvidenceRequired,
    [property: JsonPropertyName("help_text")] string HelpText);