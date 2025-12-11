using System.Diagnostics.CodeAnalysis;

namespace Domain.Common.Models;

public abstract class Entity<TId> : IEquatable<Entity<TId>>, IEqualityComparer<TId>
{
    public TId Id { get; protected init; }

    // Protected constructor for ORM
    protected Entity()
    {
        Id = default!;

    }
    protected Entity(TId id) => Id = id;

    // Equality based on ID, not reference
    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        return Id!.Equals(other.Id);
    }

    public bool Equals(TId? x, TId? y)
    {
        if (x is null && y is null)
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.Equals(y);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Id!.Equals(entity.Id);

    public int GetHashCode([DisallowNull] TId obj)
    {
        return Id!.GetHashCode();
    }

    public override int GetHashCode() => Id!.GetHashCode();
}
