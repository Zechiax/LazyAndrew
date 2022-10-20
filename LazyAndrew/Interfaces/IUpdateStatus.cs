using LazyAndrew.Enums;

namespace LazyAndrew.Interfaces;

public interface IUpdateStatus<T>
{
    /// <summary>
    /// Name of entity to which does this update status belong
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// True if check was successful, otherwise false 
    /// </summary>
    bool SuccessfulCheck { get; set; }
    /// <summary>
    /// Status of the update, only relevant if the check was successful 
    /// </summary>
    CheckStatus Status { get; set; }
    T? Payload { get; set; }
    /// <summary>
    /// Filled only when the check is not successful
    /// </summary>
    string? ErrorMessage { get; set; }
}