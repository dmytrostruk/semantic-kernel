// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.AI.Abstract;
using Microsoft.SemanticKernel.AI.OpenAI.Clients;
using Microsoft.SemanticKernel.AI.OpenAI.HttpSchema;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Reliability;
using Microsoft.SemanticKernel.Text;

namespace Microsoft.SemanticKernel.AI.OpenAI.Services;

/// <summary>
/// Azure OpenAI text completion client.
/// </summary>
public sealed class AzureTextCompletion : AzureOpenAIClientAbstract, ISKBackend
{
    /// <summary>
    /// Creates a new AzureTextCompletion client instance
    /// </summary>
    /// <param name="modelId">Azure OpenAI model ID or deployment name, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="endpoint">Azure OpenAI deployment URL, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="apiKey">Azure OpenAI API key, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="apiVersion">Azure OpenAI API version, see https://learn.microsoft.com/azure/cognitive-services/openai/reference</param>
    /// <param name="log">Application logger</param>
    /// <param name="handlerFactory">Retry handler factory for HTTP requests.</param>
    public AzureTextCompletion(
        string modelId,
        string endpoint,
        string apiKey,
        string apiVersion,
        ILogger? log = null,
        IDelegatingHandlerFactory? handlerFactory = null)
        : base(log, handlerFactory)
    {
        Verify.NotEmpty(modelId, "The ID cannot be empty, you must provide a Model ID or a Deployment name.");
        this._modelId = modelId;

        Verify.NotEmpty(endpoint, "The Azure endpoint cannot be empty");
        Verify.StartsWith(endpoint, "https://", "The Azure OpenAI endpoint must start with 'https://'");
        this.Endpoint = endpoint.TrimEnd('/');

        Verify.NotEmpty(apiKey, "The Azure API key cannot be empty");
        this.HTTPClient.DefaultRequestHeaders.Add("api-key", apiKey);

        this.AzureOpenAIApiVersion = apiVersion;
    }

    /// <summary>
    /// Creates a completion for the provided prompt and parameters
    /// </summary>
    /// <param name="input">Text to complete</param>
    /// <param name="settings">Request settings for the completion API</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The completed text.</returns>
    /// <exception cref="AIException">AIException thrown during the request</exception>
    public async Task<string> InvokeAsync(string input, ISKBackendSettings settings, CancellationToken cancellationToken = default)
    {
        var completionSettings = settings as CompleteRequestSettings;

        Verify.NotNull(completionSettings, "Completion settings cannot be empty");

        var deploymentName = await this.GetDeploymentNameAsync(this._modelId);
        var url = $"{this.Endpoint}/openai/deployments/{deploymentName}/completions?api-version={this.AzureOpenAIApiVersion}";

        this.Log.LogDebug("Sending Azure OpenAI completion request to {0}", url);

        if (completionSettings.MaxTokens < 1)
        {
            throw new AIException(
                AIException.ErrorCodes.InvalidRequest,
                $"MaxTokens {completionSettings.MaxTokens} is not valid, the value must be greater than zero");
        }

        var requestBody = Json.Serialize(new AzureCompletionRequest
        {
            Prompt = input,
            Temperature = completionSettings.Temperature,
            TopP = completionSettings.TopP,
            PresencePenalty = completionSettings.PresencePenalty,
            FrequencyPenalty = completionSettings.FrequencyPenalty,
            MaxTokens = completionSettings.MaxTokens,
            Stop = completionSettings.StopSequences is { Count: > 0 } ? completionSettings.StopSequences : null,
        });

        return await this.ExecuteCompleteRequestAsync(url, requestBody, cancellationToken);
    }

    #region private ================================================================================

    private readonly string _modelId;

    #endregion
}
