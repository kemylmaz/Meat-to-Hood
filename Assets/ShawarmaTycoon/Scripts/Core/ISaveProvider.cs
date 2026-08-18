namespace ShawarmaTycoon
{
    public interface ISaveProvider
    {
        bool TryLoad(out SaveData data);
        void Save(SaveData data);
        void Delete();
    }
}
