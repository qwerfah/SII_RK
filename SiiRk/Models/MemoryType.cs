using System.ComponentModel;

namespace SiiRk.Models
{
    public enum MemoryType
    {
        [Description("Оперативная память")]
        RAM,
        [Description("Графическая память")]
        GraphicMemory,
        [Description("Память микроконтроллеров")]
        MicrocontrollerRAM,
        [Description("Кэш-память")]
        Cache,
        [Description("Вторичная память")]
        SecondaryMemory,
    }
}
