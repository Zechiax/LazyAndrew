using LazyAndrew.Enums;
using LazyAndrew.Interfaces;

namespace LazyAndrew.Models;

public class UpdateStatus : IUpdateStatus<PluginDto>
{
    public string Name { get; set; } = null!;
    public bool SuccessfulCheck { get; set; }
    public CheckStatus Status { get; set; }
    public PluginDto? Payload { get; set; }
    public string? ErrorMessage { get; set; }
}