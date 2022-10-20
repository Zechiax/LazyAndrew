using LazyAndrew.Enums;
using LazyAndrew.Interfaces;

namespace LazyAndrew.Models;

public class UpdateStatus : IUpdateStatus<PluginDto>
{
    public bool SuccessfulCheck { get; set; }
    public Update Status { get; set; }
    public PluginDto Payload { get; set; } = null!;
}