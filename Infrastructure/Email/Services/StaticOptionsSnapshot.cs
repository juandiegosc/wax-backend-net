using Microsoft.Extensions.Options;

namespace Infrastructure.Email.Services;
public sealed class StaticOptionsSnapshot<T>(IOptions<T> inner) : IOptionsSnapshot<T>
    where T : class
{
    public T Value => inner.Value;

    public T Get(string? name) => inner.Value;
}
