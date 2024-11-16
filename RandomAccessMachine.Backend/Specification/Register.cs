using System.ComponentModel;

namespace RandomAccessMachine.Backend.Specification;
public class Register(string name, uint value) : INotifyPropertyChanged
{
    public string Name
    {
        get; set
        {
            field = value;
            PropertyChanged?.Invoke(this, new(nameof(Name)));
        }
    } = name;
    public uint Value
    {
        get; set
        {
            field = value;
            PropertyChanged?.Invoke(this, new(nameof(Value)));
        }
    } = value;

    public event PropertyChangedEventHandler? PropertyChanged;


    public override string ToString() => $"{Name}: {Value}";
}
