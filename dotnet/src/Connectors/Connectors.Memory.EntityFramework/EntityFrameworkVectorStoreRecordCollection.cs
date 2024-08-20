// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Data;

namespace Microsoft.SemanticKernel.Connectors.EntityFramework;

/// <summary>
/// Service for storing and retrieving vector records, that uses Entity Framework as the underlying storage.
/// </summary>
/// <typeparam name="TRecord">The data model to use for adding, updating and retrieving data from storage.</typeparam>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class EntityFrameworkVectorStoreRecordCollection<TRecord> : IVectorStoreRecordCollection<string, TRecord> where TRecord : class
#pragma warning restore CA1711 // Identifiers should not have incorrect
{
    /// <summary>A set of types that a key on the provided model may have.</summary>
    private static readonly HashSet<Type> s_supportedKeyTypes =
    [
        typeof(string)
    ];

    /// <summary>A set of types that data properties on the provided model may have.</summary>
    private static readonly HashSet<Type> s_supportedDataTypes =
    [
        typeof(string),
        typeof(int),
        typeof(long),
        typeof(double),
        typeof(float),
        typeof(bool),
        typeof(DateTimeOffset),
        typeof(int?),
        typeof(long?),
        typeof(double?),
        typeof(float?),
        typeof(bool?),
        typeof(DateTimeOffset?),
    ];

    /// <summary>A set of types that vector properties on the provided model may have.</summary>
    private static readonly HashSet<Type> s_supportedVectorTypes =
    [
        typeof(byte[]),
    ];

    /// <summary><see cref="DbContext"/> that can be used to manage tables in Entity Framework.</summary>
    private readonly DbContext _dbContext;

    /// <summary>Optional configuration options for this class.</summary>
    private readonly EntityFrameworkVectorStoreRecordCollectionOptions<TRecord> _options;

    /// <summary>A definition of the current storage model.</summary>
    private readonly VectorStoreRecordDefinition _vectorStoreRecordDefinition;

    /// <summary>The key property of the current storage model.</summary>
    private readonly VectorStoreRecordKeyProperty _keyProperty;

    /// <inheritdoc />
    public string CollectionName => string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkVectorStoreRecordCollection{TRecord}"/> class.
    /// </summary>
    /// <param name="dbContext"><see cref="DbContext"/> that can be used to manage tables in Entity Framework.</param>
    /// <param name="options">Optional configuration options for this class.</param>
    public EntityFrameworkVectorStoreRecordCollection(
        DbContext dbContext,
        EntityFrameworkVectorStoreRecordCollectionOptions<TRecord>? options = default)
    {
        Verify.NotNull(dbContext);

        this._dbContext = dbContext;
        this._options = options ?? new();
        this._vectorStoreRecordDefinition = this._options.VectorStoreRecordDefinition ?? VectorStoreRecordPropertyReader.CreateVectorStoreRecordDefinitionFromType(typeof(TRecord), true);

        var (keyProperty, dataProperties, vectorProperties) = VectorStoreRecordPropertyReader.SplitDefinitionAndVerify(typeof(TRecord).Name, this._vectorStoreRecordDefinition, supportsMultipleVectors: true, requiresAtLeastOneVector: false);
        VectorStoreRecordPropertyReader.VerifyPropertyTypes([keyProperty], s_supportedKeyTypes, "Key");
        VectorStoreRecordPropertyReader.VerifyPropertyTypes(dataProperties, s_supportedDataTypes, "Data", supportEnumerable: true);
        VectorStoreRecordPropertyReader.VerifyPropertyTypes(vectorProperties, s_supportedVectorTypes, "Vector");

        this._keyProperty = keyProperty;
    }

    /// <inheritdoc />
    public Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public Task CreateCollectionAsync(CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public Task CreateCollectionIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public Task DeleteCollectionAsync(CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string key, DeleteRecordOptions? options = null, CancellationToken cancellationToken = default)
    {
        var dbSet = this._dbContext.Set<TRecord>();

        var entity = await dbSet.FindAsync([key], cancellationToken).ConfigureAwait(false);

        if (entity != null)
        {
            dbSet.Remove(entity);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteBatchAsync(IEnumerable<string> keys, DeleteRecordOptions? options = null, CancellationToken cancellationToken = default)
    {
        var dbSet = this._dbContext.Set<TRecord>();

        var entities = await dbSet
            .FilterByIds(keys.ToList(), this._keyProperty.DataModelPropertyName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        dbSet.RemoveRange(entities);

        await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TRecord?> GetAsync(string key, GetRecordOptions? options = null, CancellationToken cancellationToken = default)
    {
        var dbSet = this._dbContext.Set<TRecord>();
        return await dbSet.FindAsync([key], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TRecord> GetBatchAsync(
        IEnumerable<string> keys,
        GetRecordOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var dbSet = this._dbContext.Set<TRecord>();

        var query = dbSet
            .FilterByIds(keys.ToList(), this._keyProperty.DataModelPropertyName);

        await foreach (var item in query.AsAsyncEnumerable().ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(TRecord record, UpsertRecordOptions? options = null, CancellationToken cancellationToken = default)
    {
        var dbSet = this._dbContext.Set<TRecord>();

        var id = this.GetEntityId(record);
        var existingEntry = await dbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);

        if (existingEntry != null)
        {
            this._dbContext.Entry(existingEntry).CurrentValues.SetValues(record);
        }
        else
        {
            dbSet.Add(record);
        }

        await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return id;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> UpsertBatchAsync(
        IEnumerable<TRecord> records,
        UpsertRecordOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var dbSet = this._dbContext.Set<TRecord>();

        var entityDictionary = records.ToDictionary(this.GetEntityId);
        var ids = entityDictionary.Keys.ToList();

        var existingEntities = await dbSet
            .FilterByIds(ids, this._keyProperty.DataModelPropertyName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Update existing entities.
        foreach (var existingEntity in existingEntities)
        {
            var entityId = this.GetEntityId(existingEntity);

            if (entityDictionary.TryGetValue(entityId, out var newEntity))
            {
                this._dbContext.Entry(existingEntity).CurrentValues.SetValues(newEntity);

                // Remove updated entity from dictionary to insert new entities later.
                entityDictionary.Remove(entityId);
            }
        }

        // Insert new entities.
        dbSet.AddRange(entityDictionary.Values);

        await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var id in ids)
        {
            yield return id;
        }
    }

    #region private

    private string GetEntityId(TRecord entity)
    {
        var keyPropertyName = this._keyProperty.DataModelPropertyName;
        var keyProperty = typeof(TRecord).GetProperty(keyPropertyName)!;

        var id = keyProperty.GetValue(entity) as string;

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new VectorStoreOperationException($"Key property {keyPropertyName} is not initialized.");
        }

        return id;
    }

    #endregion
}
