using System;
using System.Collections.Concurrent;
using System.Text;

namespace Sculptor.Core
{
    /// <summary>
    /// Represents the result of an operation.
    /// </summary>
    public readonly struct Result
    {
        internal readonly object? value;

        /// <summary>
        /// Gets a value indicating whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the error message if the operation was not successful.
        /// </summary>
        public string? Error { get; }

        internal Result(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            Error = isSuccess ? null : error;
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
            return new(true, null);
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
            return new(false, error);
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

        /// <summary>
        /// Wraps a result of type <typeparamref name="T"/> into a non-generic result.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="result">The result to wrap.</param>
        /// <returns>A non-generic result.</returns>
        public static Result Wrap<T>(Result<T> result)
        {
            return new(result.IsSuccess, result.Error, result.Value);
        }

        /// <summary>
        /// Creates a new result with an additional error message.
        /// </summary>
        /// <param name="error">The additional error message.</param>
        /// <returns>A new result with the combined error messages.</returns>
        public Result WithError(string error)
        {
            return new Result(false, CombineErrors(Error, error), value);
        }

        /// <summary>
        /// Creates a new result with a new value.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        /// <returns>A new result with the new value.</returns>
        public Result WithValue(object? newValue)
        {
            return new Result(IsSuccess, Error, newValue);
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
    public readonly struct Result<T>
    {
        /// <summary>
        /// Gets a value indicating whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the error message if the operation was not successful.
        /// </summary>
        public string? Error { get; }

        /// <summary>
        /// Gets the value of the result.
        /// </summary>
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
            return new(true, null, value);
        }

        /// <summary>
        /// Creates a failed result with an error message.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <returns>A failed result with a value.</returns>
        public static Result<T> Fail(string error)
        {
            return new(false, error, default);
        }

        /// <summary>
        /// Tries to get the value of the result.
        /// </summary>
        /// <param name="value">The value of the result if the operation was successful.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        public bool TryGetValue(out T? value)
        {
            if (IsSuccess)
            {
                value = Value!;
                return true;
            }

            value = default;
            return false;
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

        /// <summary>
        /// Wraps a non-generic result into a result of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="result">The result to wrap.</param>
        /// <returns>A result of type <typeparamref name="T"/>.</returns>
        public static Result<T> Wrap(Result result)
        {
            return new(result.IsSuccess, result.Error, (T?)result.value);
        }

        /// <summary>
        /// Creates a new result with an additional error message.
        /// </summary>
        /// <param name="error">The additional error message.</param>
        /// <returns>A new result with the combined error messages.</returns>
        public Result<T> WithError(string error)
        {
            return new Result<T>(false, Result.CombineErrors(Error, error), default);
        }

        /// <summary>
        /// Creates a new result with a new value.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        /// <returns>A new result with the new value.</returns>
        public Result<T> WithValue(T? newValue)
        {
            return new Result<T>(IsSuccess, Error, newValue);
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
        private readonly ConcurrentBag<T> _items = new();

        public T Get()
        {
            if (_items.TryTake(out var item))
            {
                return item;
            }

            return createInstance();
        }

        public void Return(T item)
        {
            resetInstance?.Invoke(item);
            _items.Add(item);
        }
    }
}
