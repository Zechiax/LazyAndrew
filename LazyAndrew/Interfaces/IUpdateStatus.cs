using LazyAndrew.Enums;

namespace LazyAndrew.Interfaces;

public interface IUpdateStatus<T>
{
    /// <summary>
    /// True if check was successful, otherwise false 
    /// </summary>
    bool SuccessfulCheck { get; set; }
    /// <summary>
    /// Status of the update, only relevant if the check was successful 
    /// </summary>
    Update Status { get; set; }
    T Payload { get; set; }
}