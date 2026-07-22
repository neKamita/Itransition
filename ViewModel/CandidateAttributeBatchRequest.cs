namespace Itransition.ViewModel;

public sealed class CandidateAttributeBatchRequest
{
    public Guid ProfileId { get; init; }
    public List<CandidateAttributeValueInput> Items { get; init; } = [];
}

public sealed class CandidateAttributeValueInput
{
    public Guid Id { get; init; }
    public string? Value { get; init; }
    public uint Version { get; init; }
}

public sealed class CandidateAttributeDeleteRequest
{
    public Guid ProfileId { get; init; }
    public List<CandidateAttributeVersionInput> Items { get; init; } = [];
}

public sealed class CandidateAttributeVersionInput
{
    public Guid Id { get; init; }
    public uint Version { get; init; }
}
