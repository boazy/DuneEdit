namespace DuneEdit.Core;

public static class Regions
{
    public static string GetRegion(byte id) => id switch
    {
        0x01 => "Arrakeen",
        0x02 => "Carthag",
        0x03 => "Tuono",
        0x04 => "Habbanya",
        0x05 => "Oxtyn",
        0x06 => "Tsympo",
        0x07 => "Bledan",
        0x08 => "Ergsun",
        0x09 => "Haga",
        0x0A => "Cielago",
        0x0B => "Sihaya",
        0x0C => "Celimyn",
        _ => "Unknown",
    };

    public static string GetSubregion(byte id) => id switch
    {
        0x01 => "(Atreides Palace)",
        0x02 => "(Harkonnen Palace)",
        0x03 => "Tabr",
        0x04 => "Timin",
        0x05 => "Tuek",
        0x06 => "Harg",
        0x07 => "Clam",
        0x08 => "Tsymyn",
        0x09 => "Siet",
        0x0A => "Pyons",
        0x0B => "Pyort",
        _ => "Unknown",
    };
}
