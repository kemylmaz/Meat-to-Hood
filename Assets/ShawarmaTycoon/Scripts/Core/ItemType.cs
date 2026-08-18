namespace ShawarmaTycoon
{
    public enum ItemType
    {
        None,
        RawMeat,
        CookedMeat,
        SlicedMeat,
        Wrap,
        Trash,
        // Appended rather than slotted in beside the food they are sold with:
        // stations and pads are built at runtime, but a saved level that stored a
        // type by its number would read as something else if these shifted.
        Drink,
        Dessert
    }

    public enum StationMode
    {
        Source,
        Processor,
        Service
    }
}
