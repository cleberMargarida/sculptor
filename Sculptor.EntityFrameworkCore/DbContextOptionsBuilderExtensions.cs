using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Sculptor.EntityFrameworkCore
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder __<TContext>(this DbContextOptionsBuilder options)
        {
            options.AddInterceptors(new Interceptor());
            return options;
        }
    }

    internal class Interceptor : IDbCommandInterceptor
    {
    }
}
