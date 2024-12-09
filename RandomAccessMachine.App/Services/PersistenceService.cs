using WinSharp.FlagInterfaces;

namespace RandomAccessMachine.App.Services;
public sealed class PersistenceService : IService
{
    public double Speed { get; set; } = 1.0;
    public bool IsRealTime { get; set; } = false;
}
