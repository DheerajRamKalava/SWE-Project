using Microsoft.Azure.Cosmos;
using Persistence;

public class Program
{
    public static async Task Main()
    {
        try
        {
            IStorage storage =
                await StorageFactory.CreateAsync(ModuleType.Chat);

            string key = "message-004";

            await storage.Create(key, "Hello from Chat!");
            Console.WriteLine("Item created.");

            Console.WriteLine($"Read: {await storage.Read(key)}");

            await storage.Update(key, "Updated message");
            Console.WriteLine("Item updated.");

            Console.WriteLine(
                $"Read after update: {await storage.Read(key)}");

            await storage.Delete(key);
            Console.WriteLine("Item deleted.");

            string key2 = "message-006";
            await storage.Create(key2, "Hello from Chat2!");
            await storage.Update(key2, "Updated message from Chat2");
        }
        catch (CosmosException ex)
        {
            Console.WriteLine($"Cosmos error : {ex.Message}");
        }

    }
}