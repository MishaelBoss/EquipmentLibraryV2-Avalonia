namespace EquipmentLibraryV2_Avalonia.Messages;

public enum PageType
{
    WorkArea,
    Library,
    AdminPanel,
    MeasurementRegister,
    RegisterOfTestingEquipment
}

public record PageChangedMessage(PageType Page);