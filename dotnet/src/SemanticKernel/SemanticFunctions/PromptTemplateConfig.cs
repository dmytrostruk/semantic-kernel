// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Text;

namespace Microsoft.SemanticKernel.SemanticFunctions;

/// <summary>
/// Prompt template configuration.
/// </summary>
public class PromptTemplateConfig
{
    /// <summary>
    /// Input parameter for semantic functions.
    /// </summary>
    public class InputParameter
    {
        /// <summary>
        /// Name of the parameter to pass to the function.
        /// e.g. when using "{{$input}}" the name is "input", when using "{{$style}}" the name is "style", etc.
        /// </summary>
        [JsonPropertyName("name")]
        [JsonPropertyOrder(1)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Parameter description for UI apps and planner. Localization is not supported here.
        /// </summary>
        [JsonPropertyName("description")]
        [JsonPropertyOrder(2)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Default value when nothing is provided.
        /// </summary>
        [JsonPropertyName("defaultValue")]
        [JsonPropertyOrder(3)]
        public string DefaultValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Input configuration (list of all input parameters for a semantic function).
    /// </summary>
    public class InputConfig
    {
        [JsonPropertyName("parameters")]
        [JsonPropertyOrder(1)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<InputParameter> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Schema - Not currently used.
    /// </summary>
    [JsonPropertyName("schema")]
    [JsonPropertyOrder(1)]
    public int Schema { get; set; } = 1;

    /// <summary>
    /// Type, such as "completion", "embeddings", etc.
    /// </summary>
    /// <remarks>TODO: use enum</remarks>
    [JsonPropertyName("type")]
    [JsonPropertyOrder(2)]
    public string Type { get; set; } = "completion";

    /// <summary>
    /// Description
    /// </summary>
    [JsonPropertyName("description")]
    [JsonPropertyOrder(3)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Backend settings.
    /// </summary>
    [JsonPropertyName("backend_settings")]
    [JsonPropertyOrder(4)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, object> BackendSettings { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Default backends to use.
    /// </summary>
    [JsonPropertyName("default_backends")]
    [JsonPropertyOrder(5)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string> DefaultBackends { get; set; } = new();

    /// <summary>
    /// Input configuration (that is, list of all input parameters).
    /// </summary>
    [JsonPropertyName("input")]
    [JsonPropertyOrder(6)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InputConfig Input { get; set; } = new();

    /// <summary>
    /// Remove some default properties to reduce the JSON complexity.
    /// </summary>
    /// <returns>Compacted prompt template configuration.</returns>
    public PromptTemplateConfig Compact()
    {
        if (this.DefaultBackends.Count == 0)
        {
            this.DefaultBackends = null!;
        }

        return this;
    }

    /// <summary>
    /// Creates a prompt template configuration from JSON.
    /// </summary>
    /// <param name="json">JSON of the prompt template configuration.</param>
    /// <returns>Prompt template configuration.</returns>
    public static PromptTemplateConfig FromJson(string json)
    {
        var result = Json.Deserialize<PromptTemplateConfig>(json);
        Verify.NotNull(result, "Unable to deserialize prompt template config. The deserialized returned NULL.");
        return result;
    }
}
