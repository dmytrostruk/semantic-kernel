// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.AI.Abstract;
using Microsoft.SemanticKernel.AI.OpenAI.Services;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Reliability;

namespace Microsoft.SemanticKernel.Configuration;

/// <summary>
/// Semantic kernel configuration.
/// </summary>
public sealed class KernelConfig
{
    /// <summary>
    /// Factory for creating HTTP handlers.
    /// </summary>
    public IDelegatingHandlerFactory HttpHandlerFactory { get; private set; } = new DefaultHttpRetryHandlerFactory(new HttpRetryConfig());

    /// <summary>
    /// Default HTTP retry configuration for built-in HTTP handler factory.
    /// </summary>
    public HttpRetryConfig DefaultHttpRetryConfig { get; private set; } = new();

    /// <summary>
    /// Logger for backend clients.
    /// </summary>
    public ILogger Logger { get; private set; } = NullLogger.Instance;

    /// <summary>
    /// Adds an Azure OpenAI backend to the list.
    /// See https://learn.microsoft.com/azure/cognitive-services/openai for service details.
    /// </summary>
    /// <param name="label">An identifier used to map semantic functions to backend,
    /// decoupling prompts configurations from the actual model used</param>
    /// <param name="deploymentName">Azure OpenAI deployment name, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="endpoint">Azure OpenAI deployment URL, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="apiKey">Azure OpenAI API key, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="apiVersion">Azure OpenAI API version, see https://learn.microsoft.com/azure/cognitive-services/openai/reference</param>
    /// <param name="overwrite">Whether to overwrite an existing configuration if the same name exists</param>
    /// <returns>Self instance</returns>
    public KernelConfig AddAzureOpenAICompletionBackend(
        string label, string deploymentName, string endpoint, string apiKey, string apiVersion = "2022-12-01", bool overwrite = false)
    {
        Verify.NotEmpty(label, "The backend label is empty");

        if (!overwrite && this.Backends.ContainsKey(label))
        {
            throw new KernelException(
                KernelException.ErrorCodes.InvalidBackendConfiguration,
                $"A backend already exists for the label: {label}");
        }

        this.Backends[label] = new AzureTextCompletion(deploymentName, endpoint, apiKey, apiVersion, this.Logger, this.HttpHandlerFactory);

        if (this.Backends.Count == 1)
        {
            this._defaultBackend = label;
        }

        return this;
    }

    /// <summary>
    /// Adds the OpenAI completion backend to the list.
    /// See https://platform.openai.com/docs for service details.
    /// </summary>
    /// <param name="label">An identifier used to map semantic functions to backend,
    /// decoupling prompts configurations from the actual model used</param>
    /// <param name="modelId">OpenAI model name, see https://platform.openai.com/docs/models</param>
    /// <param name="apiKey">OpenAI API key, see https://platform.openai.com/account/api-keys</param>
    /// <param name="orgId">OpenAI organization id. This is usually optional unless your account belongs to multiple organizations.</param>
    /// <param name="overwrite">Whether to overwrite an existing configuration if the same name exists</param>
    /// <returns>Self instance</returns>
    public KernelConfig AddOpenAICompletionBackend(
        string label, string modelId, string apiKey, string? orgId = null, bool overwrite = false)
    {
        Verify.NotEmpty(label, "The backend label is empty");

        if (!overwrite && this.Backends.ContainsKey(label))
        {
            throw new KernelException(
                KernelException.ErrorCodes.InvalidBackendConfiguration,
                $"A backend already exists for the label: {label}");
        }

        this.Backends[label] = new OpenAITextCompletion(modelId, apiKey, orgId, this.Logger, this.HttpHandlerFactory);

        if (this.Backends.Count == 1)
        {
            this._defaultBackend = label;
        }

        return this;
    }

    /// <summary>
    /// Adds an Azure OpenAI embeddings backend to the list.
    /// See https://learn.microsoft.com/azure/cognitive-services/openai for service details.
    /// </summary>
    /// <param name="label">An identifier used to map semantic functions to backend,
    /// decoupling prompts configurations from the actual model used</param>
    /// <param name="deploymentName">Azure OpenAI deployment name, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="endpoint">Azure OpenAI deployment URL, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="apiKey">Azure OpenAI API key, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="apiVersion">Azure OpenAI API version, see https://learn.microsoft.com/azure/cognitive-services/openai/reference</param>
    /// <param name="overwrite">Whether to overwrite an existing configuration if the same name exists</param>
    /// <returns>Self instance</returns>
    public KernelConfig AddAzureOpenAIEmbeddingsBackend(
        string label, string deploymentName, string endpoint, string apiKey, string apiVersion = "2022-12-01", bool overwrite = false)
    {
        Verify.NotEmpty(label, "The backend label is empty");

        if (!overwrite && this.Backends.ContainsKey(label))
        {
            throw new KernelException(
                KernelException.ErrorCodes.InvalidBackendConfiguration,
                $"A backend already exists for the label: {label}");
        }

        this.Backends[label] = new AzureTextEmbeddings(deploymentName, endpoint, apiKey, apiVersion, this.Logger, this.HttpHandlerFactory);

        if (this.Backends.Count == 1)
        {
            this._defaultBackend = label;
        }

        return this;
    }

    /// <summary>
    /// Adds the OpenAI embeddings backend to the list.
    /// See https://platform.openai.com/docs for service details.
    /// </summary>
    /// <param name="label">An identifier used to map semantic functions to backend,
    /// decoupling prompts configurations from the actual model used</param>
    /// <param name="modelId">OpenAI model name, see https://platform.openai.com/docs/models</param>
    /// <param name="apiKey">OpenAI API key, see https://platform.openai.com/account/api-keys</param>
    /// <param name="orgId">OpenAI organization id. This is usually optional unless your account belongs to multiple organizations.</param>
    /// <param name="overwrite">Whether to overwrite an existing configuration if the same name exists</param>
    /// <returns>Self instance</returns>
    public KernelConfig AddOpenAIEmbeddingsBackend(
        string label, string modelId, string apiKey, string? orgId = null, bool overwrite = false)
    {
        Verify.NotEmpty(label, "The backend label is empty");

        if (!overwrite && this.Backends.ContainsKey(label))
        {
            throw new KernelException(
                KernelException.ErrorCodes.InvalidBackendConfiguration,
                $"An backend already exists for the label: {label}");
        }

        this.Backends[label] = new OpenAITextEmbeddings(modelId, apiKey, orgId, this.Logger, this.HttpHandlerFactory);

        if (this.Backends.Count == 1)
        {
            this._defaultBackend = label;
        }

        return this;
    }

    /// <summary>
    /// Check whether a given backend is in the configuration.
    /// </summary>
    /// <param name="label">Name of backend to look for.</param>
    /// <param name="condition">Optional condition that must be met for a backend to be deemed present.</param>
    /// <returns><c>true</c> when a backend matching the giving label is present, <c>false</c> otherwise.</returns>
    public bool HasBackend(string label, Func<ISKBackend, bool>? condition = null)
    {
        return condition == null
            ? this.Backends.ContainsKey(label)
            : this.Backends.Any(x => x.Key == label && condition(x.Value));
    }

    /// <summary>
    /// Check whether a given backend is in the configuration.
    /// </summary>
    /// <param name="condition">Condition that must be met for a backend to be deemed present.</param>
    /// <returns><c>true</c> when a backend matching the giving condition is present, <c>false</c> otherwise.</returns>
    public bool HasBackend(Func<ISKBackend, bool> condition)
    {
        return this.Backends.Any(x => condition(x.Value));
    }

    /// <summary>
    /// Set the http retry handler factory to use for the kernel.
    /// </summary>
    /// <param name="httpHandlerFactory">Http retry handler factory to use.</param>
    /// <returns>The updated kernel configuration.</returns>
    public KernelConfig SetHttpRetryHandlerFactory(IDelegatingHandlerFactory? httpHandlerFactory = null)
    {
        if (httpHandlerFactory != null)
        {
            this.HttpHandlerFactory = httpHandlerFactory;
        }

        return this;
    }

    /// <summary>
    /// Set the logger to use for the kernel.
    /// </summary>
    /// <param name="logger"></param>
    /// <returns></returns>
    public KernelConfig SetLogger(ILogger logger)
    {
        if (logger != null)
        {
            this.Logger = logger;
        }

        return this;
    }

    public KernelConfig SetDefaultHttpRetryConfig(HttpRetryConfig? httpRetryConfig)
    {
        if (httpRetryConfig != null)
        {
            this.DefaultHttpRetryConfig = httpRetryConfig;
            this.SetHttpRetryHandlerFactory(new DefaultHttpRetryHandlerFactory(httpRetryConfig));
        }

        return this;
    }

    /// <summary>
    /// Set the default backend to use for the kernel.
    /// </summary>
    /// <param name="label">Label of backend to use.</param>
    /// <returns>The updated kernel configuration.</returns>
    /// <exception cref="KernelException">Thrown if the requested backend doesn't exist.</exception>
    public KernelConfig SetDefaultBackend(string label)
    {
        if (!this.Backends.ContainsKey(label))
        {
            throw new KernelException(
                KernelException.ErrorCodes.BackendNotFound,
                $"The backend doesn't exist with label: {label}");
        }

        this._defaultBackend = label;
        return this;
    }

    /// <summary>
    /// Default backend.
    /// </summary>
    public string? DefaultBackend => this._defaultBackend;

    /// <summary>
    /// Get the backend matching the given label or the default if a label is not provided or not found.
    /// </summary>
    /// <param name="label">Optional label of the desired backend.</param>
    /// <returns>The backend matching the given label or the default.</returns>
    /// <exception cref="KernelException">Thrown when no suitable backend is found.</exception>
    public ISKBackend GetBackend(string? label = null)
    {
        if (string.IsNullOrEmpty(label))
        {
            if (this._defaultBackend == null)
            {
                throw new KernelException(
                    KernelException.ErrorCodes.BackendNotFound,
                    "A label was not provided and no default backend is available.");
            }

            return this.Backends[this._defaultBackend];
        }

        if (this.Backends.TryGetValue(label, out ISKBackend value))
        {
            return value;
        }

        if (this._defaultBackend != null)
        {
            return this.Backends[this._defaultBackend];
        }

        throw new KernelException(
            KernelException.ErrorCodes.BackendNotFound,
            $"Backend not found with label: {label} and no default backend is available.");
    }

    /// <summary>
    /// Get the backend matching the given label, the default if a label is not provided or first that matches requested type.
    /// </summary>
    /// <typeparam name="T">Specific type of backend to return.</typeparam>
    /// <param name="label">Optional label of the desired backend.</param>
    /// <returns>The backend matching the given label or the default.</returns>
    /// <exception cref="KernelException">Thrown when no suitable backend is found.</exception>
    public T GetBackend<T>(string? label = null)
    {
        var backend = this.GetBackend(label);

        if (backend is T specificBackend)
        {
            return specificBackend;
        }

        backend = this.GetAllBackends().FirstOrDefault(backend => backend is T);

        if (backend is not null)
        {
            return (T)backend;
        }

        throw new KernelException(
                KernelException.ErrorCodes.BackendNotFound,
                $"Backend not found with type {typeof(T).Name}.");
    }

    /// <summary>
    /// Get all backends.
    /// </summary>
    /// <returns>IEnumerable of all backends in the kernel configuration.</returns>
    public IEnumerable<ISKBackend> GetAllBackends()
    {
        return this.Backends.Select(x => x.Value);
    }

    /// <summary>
    /// Remove the backend with the given label.
    /// </summary>
    /// <param name="label">Label of backend to remove.</param>
    /// <returns>The updated kernel configuration.</returns>
    public KernelConfig RemoveBackend(string label)
    {
        this.Backends.Remove(label);
        if (this._defaultBackend == label)
        {
            this._defaultBackend = this.Backends.Keys.FirstOrDefault();
        }

        return this;
    }

    /// <summary>
    /// Remove all backends.
    /// </summary>
    /// <returns>The updated kernel configuration.</returns>
    public KernelConfig RemoveAllBackends()
    {
        this.Backends.Clear();
        this._defaultBackend = null;
        return this;
    }

    #region private

    private Dictionary<string, ISKBackend> Backends { get; } = new();

    private string? _defaultBackend;

    #endregion
}
