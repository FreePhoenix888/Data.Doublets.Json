using System;
using System.IO;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.IO;
using System.Text.Json;

namespace Platform.Data.Doublets.Json
{
    public class JsonImporterCLI<TLink>
    {
        public void Run(params string[] args)
        {
            var jsonFilePath = ConsoleHelpers.GetOrReadArgument(0, "JSON file path", args);
            var linksFilePath = ConsoleHelpers.GetOrReadArgument(1, "Links file path", args);
            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine("Entered JSON file does not exist.");
            }
            var json = FileHelpers.ReadAll<byte>(jsonFilePath);
            Utf8JsonReader utf8JsonReader = new(json);
            using var cancellation = new ConsoleCancellation();
            using var memoryAdapter = new UnitedMemoryLinks<TLink>(linksFilePath);
            Console.WriteLine("Press CTRL+C to stop.");
            var links = memoryAdapter.DecorateWithAutomaticUniquenessAndUsagesResolution();
            var storage = new DefaultJsonStorage<TLink>(links);
            var importer = new JsonImporter<TLink>(storage);
            var cancallationToken = cancellation.Token;
            var documentName = Path.GetFileName(jsonFilePath);
            importer.Import(documentName, ref utf8JsonReader, ref cancallationToken);
            Console.WriteLine(cancellation.IsRequested ? "Import aborted." : "Import completed successfully.");
        }
    }
}