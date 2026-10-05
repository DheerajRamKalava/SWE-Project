namespace Persistence
{
    // All CRUD operations are asynchronous
    // Applications module interacts with the Persistence module through this interface
    public interface IStorage
    {
        Task Create(string key, string data);

        Task<string> Read(string key);

        Task Update(string key, string data);

        Task Delete(string key);

    }

}