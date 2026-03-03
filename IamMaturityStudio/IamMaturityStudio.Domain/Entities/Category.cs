namespace IamMaturityStudio.Domain.Entities;

public class Category
{
    public Guid Id { get; set; }
    public Guid DomainId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public string BusinessRisk { get; set; } = string.Empty;
}