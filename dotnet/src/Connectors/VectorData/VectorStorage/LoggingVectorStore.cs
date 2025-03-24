﻿// Copyright (c) Microsoft. All rights reserved.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;

namespace Microsoft.Extensions.VectorData;

/// <summary>
/// A vector store that logs operations to an <see cref="ILogger"/>
/// </summary>
[Experimental("SKEXP0020")]
public class LoggingVectorStore : DelegatingVectorStore
{
    /// <summary>An <see cref="ILogger"/> instance used for all logging.</summary>
    private readonly ILogger _logger;

    /// <summary>The underlying <see cref="IVectorStore"/>.</summary>
    private readonly IVectorStore _innerStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingVectorStore"/> class.
    /// </summary>
    /// <param name="innerStore">The underlying <see cref="IVectorStore"/>.</param>
    /// <param name="logger">An <see cref="ILogger"/> instance that will be used for all logging.</param>
    public LoggingVectorStore(IVectorStore innerStore, ILogger logger) : base(innerStore)
    {
        Verify.NotNull(innerStore);
        Verify.NotNull(logger);

        this._innerStore = innerStore;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public override IVectorStoreRecordCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreRecordDefinition? vectorStoreRecordDefinition = null)
        => new LoggingVectorStoreRecordCollection<TKey, TRecord>(
            base.GetCollection<TKey, TRecord>(name, vectorStoreRecordDefinition),
            this._logger);

    /// <inheritdoc/>
    public override IAsyncEnumerable<string> ListCollectionNamesAsync(CancellationToken cancellationToken = default)
    {
        return LoggingExtensions.RunWithLoggingAsync(
            this._logger,
            nameof(ListCollectionNamesAsync),
            () => base.ListCollectionNamesAsync(cancellationToken),
            cancellationToken);
    }
}
