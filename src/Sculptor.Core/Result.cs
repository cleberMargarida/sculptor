using System.Collections.Concurrent;
using System.Text;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Sculptor.Core;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public interface IResult
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    bool IsFailed { get; }

    /// <summary>
    /// Gets the error message if the operation was not successful.
    /// </summary>
    string? Error { get; }
}

/// <summary>
/// Represents the result of an operation with a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public interface IResult<out T> : IResult
{
    /// <summary>
    /// Gets the value of the result if the operation was successful.
    /// </summary>
    T? Value { get; }
}

/// <summary>
/// Represents the result of an operation.
/// </summary>
public readonly struct Result : IResult
{
    internal readonly object? value;

    /// <inheritdoc/>
    public bool IsSuccess { get; }

    /// <inheritdoc/>
    public bool IsFailed => !IsSuccess;

    /// <inheritdoc/>
    public string? Error { get; }

    internal Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = isSuccess ? null : error;
        this.value = null;
    }

    internal Result(bool isSuccess, string? error, object? value)
    {
        IsSuccess = isSuccess;
        Error = isSuccess ? null : error;
        this.value = value;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Ok()
    {
        return new Result(true, null);
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A successful result with a value.</returns>
    public static Result<T> Ok<T>(T value)
    {
        return new Result<T>(true, null, value);
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result Fail(string error)
    {
        return new Result(false, error);
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static Result<T> Fail<T>(string error)
    {
        return new Result<T>(false, error, default);
    }

    internal static string? CombineErrors(string? existingError, string newError)
    {
        if (string.IsNullOrEmpty(existingError))
            return newError;

        var builder = StringBuilderPool.Rent();
        try
        {
            builder.Append(existingError);
            builder.AppendLine();
            builder.Append(newError);
            return builder.ToString();
        }
        finally
        {
            StringBuilderPool.Return(builder);
        }
    }
}

/// <summary>
/// Represents the result of an operation with a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public readonly struct Result<T> : IResult<T>
{
    /// <inheritdoc/>
    public bool IsSuccess { get; }

    /// <inheritdoc/>
    public bool IsFailed => !IsSuccess;

    /// <inheritdoc/>
    public string? Error { get; }

    /// <inheritdoc/>
    public T? Value { get; }

    internal Result(bool isSuccess, string? error, T? value)
    {
        IsSuccess = isSuccess;
        Error = isSuccess ? null : error;
        Value = value;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A successful result with a value.</returns>
    public static Result<T> Ok(T value)
    {
        return new Result<T>(true, null, value);
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result with a value.</returns>
    public static Result<T> Fail(string error)
    {
        return new Result<T>(false, error, default);
    }

    /// <summary>
    /// Implicitly converts a result of type <typeparamref name="T"/> to a non-generic result.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    public static implicit operator Result(Result<T> result)
    {
        return new(result.IsSuccess, result.Error, result.Value);
    }

    /// <summary>
    /// Implicitly converts a non-generic result to a result of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    public static implicit operator Result<T>(Result result)
    {
        return new(result.IsSuccess, result.Error, (T?)result.value);
    }

}

/// <summary>
/// Extension methods for results.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Tries to get the value of the result.
    /// </summary>
    /// <param name="result">The result to get the value from.</param>
    /// <param name="value">The value of the result if the operation was successful.</param>
    /// <returns>True if the operation was successful; otherwise, false.</returns>
    public static bool TryGetValue<T>(this IResult<T> result, [MaybeNullWhen(false)] out T? value)
    {
        value = result.IsSuccess ? result.Value : default;
        return result.IsSuccess;
    }

    /// <summary>
    /// Creates a new result with an additional error message.
    /// </summary>
    /// <param name="result">The original result.</param>
    /// <param name="error">The additional error message.</param>
    /// <returns>A new result with the combined error messages.</returns>
    public static IResult WithError(this IResult result, string error)
    {
        return new Result(false, Result.CombineErrors(result.Error, error), null);
    }

    /// <summary>
    /// Creates a new result with a new value.
    /// </summary>
    /// <param name="result">The original result.</param>
    /// <param name="newValue">The new value.</param>
    /// <returns>A new result with the new value.</returns>
    public static Result WithValue(this IResult result, object? newValue)
    {
        return new Result(result.IsSuccess, result.Error, newValue);
    }

    /// <summary>
    /// Creates a new result with an additional error message.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The original result.</param>
    /// <param name="error">The additional error message.</param>
    /// <returns>A new result with the combined error messages.</returns>
    public static Result<T> WithError<T>(this IResult<T> result, string error)
    {
        return new Result<T>(false, Result.CombineErrors(result.Error, error), default);
    }

    /// <summary>
    /// Creates a new result with a new value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The original result.</param>
    /// <param name="newValue">The new value.</param>
    /// <returns>A new result with the new value.</returns>
    public static Result<T> WithValue<T>(this IResult<T> result, T? newValue)
    {
        return new Result<T>(result.IsSuccess, result.Error, newValue);
    }
}

internal static class StringBuilderPool
{
    private static readonly ObjectPool<StringBuilder> _pool = new(static () => new StringBuilder(), static builder => builder.Clear());

    public static StringBuilder Rent() => _pool.Get();

    public static void Return(StringBuilder builder) => _pool.Return(builder);
}

internal class ObjectPool<T>(Func<T> createInstance, Action<T>? resetInstance) where T : class
{
    private readonly ConcurrentBag<T> _items = [];

    public T Get()
    {
        return _items.TryTake(out var item) ? item : createInstance();
    }

    public void Return(T item)
    {
        resetInstance?.Invoke(item);
        _items.Add(item);
    }
}