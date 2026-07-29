namespace Dentists.Infrastructure.Persistence;

/// <summary>
/// Splits a Cosmos connection string into the parts that need them separately.
/// <para>
/// EF takes the whole string, but MassTransit's Cosmos saga repository wants the account
/// endpoint and key as distinct values. Rather than configure the account twice and risk the
/// two drifting, both are derived from the one connection string.
/// </para>
/// </summary>
public readonly record struct CosmosConnectionString(string AccountEndpoint, string AccountKey)
{
    private const string EndpointKey = "AccountEndpoint";
    private const string AuthKey = "AccountKey";

    /// <exception cref="ArgumentException">
    /// The string is missing one of the two parts, which would otherwise surface much later as
    /// an opaque authentication failure.
    /// </exception>
    public static CosmosConnectionString Parse(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        string? endpoint = null;
        string? key = null;

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            // Only the first '=' separates name from value: an account key is base64 and
            // routinely ends in padding, so splitting on every '=' would truncate it.
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var name = part[..separator].Trim();
            var value = part[(separator + 1)..].Trim();

            if (name.Equals(EndpointKey, StringComparison.OrdinalIgnoreCase))
            {
                endpoint = value;
            }
            else if (name.Equals(AuthKey, StringComparison.OrdinalIgnoreCase))
            {
                key = value;
            }
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException(
                $"The Cosmos connection string has no {EndpointKey}.", nameof(connectionString));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException(
                $"The Cosmos connection string has no {AuthKey}.", nameof(connectionString));
        }

        return new CosmosConnectionString(endpoint, key);
    }
}
