using Avalonia.Data.Converters;

namespace WaterElectricityAutoClient;

public static class AppConverters
{
    public static readonly FuncValueConverter<bool, double> BoolToDouble =
        new(x => x ? 1.0 : 0.0);
}
