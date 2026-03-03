namespace IamMaturityStudio.Domain.ValueObjects;

public readonly record struct EmailAddress(string Value)
{
    public override string ToString() => Value;
}