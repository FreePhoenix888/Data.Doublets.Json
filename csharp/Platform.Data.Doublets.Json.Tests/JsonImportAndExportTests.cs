using System.Text;
using System.Text.Json;
using System.Threading;
using System.IO;
using Xunit;
using TLink = System.UInt64;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.Memory;
using Platform.Data.Doublets.Memory;
using System.Text.RegularExpressions;
using Platform.Data.Doublets.Sequences.Converters;

namespace Platform.Data.Doublets.Json.Tests
{
    /// <summary>
    /// <para>
    /// Represents the json import and export tests.
    /// </para>
    /// <para></para>
    /// </summary>
    public class JsonImportAndExportTests
    {
        /// <summary>
        /// <para>
        /// The balanced variant converter.
        /// </para>
        /// <para></para>
        /// </summary>
        public static BalancedVariantConverter<TLink> BalancedVariantConverter;
        
        /// <summary>
        /// <para>
        /// Creates the links.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>
        /// <para>A links of t link</para>
        /// <para></para>
        /// </returns>
        public static ILinks<TLink> CreateLinks() => CreateLinks<TLink>(new IO.TemporaryFile());

        /// <summary>
        /// <para>
        /// Creates the links using the specified data db filename.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <typeparam name="TLink">
        /// <para>The link.</para>
        /// <para></para>
        /// </typeparam>
        /// <param name="dataDBFilename">
        /// <para>The data db filename.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A links of t link</para>
        /// <para></para>
        /// </returns>
        public static ILinks<TLink> CreateLinks<TLink>(string dataDBFilename)
        {
            var linksConstants = new LinksConstants<TLink>(enableExternalReferencesSupport: true);
            return new UnitedMemoryLinks<TLink>(new FileMappedResizableDirectMemory(dataDBFilename), UnitedMemoryLinks<TLink>.DefaultLinksSizeStep, linksConstants, IndexTreeType.Default);
        }

        /// <summary>
        /// <para>
        /// Creates the json storage using the specified links.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="links">
        /// <para>The links.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>A default json storage of t link</para>
        /// <para></para>
        /// </returns>
        public static DefaultJsonStorage<TLink> CreateJsonStorage(ILinks<TLink> links) => new (links, BalancedVariantConverter);
        
        /// <summary>
        /// <para>
        /// Imports the storage.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="storage">
        /// <para>The storage.</para>
        /// <para></para>
        /// </param>
        /// <param name="documentName">
        /// <para>The document name.</para>
        /// <para></para>
        /// </param>
        /// <param name="json">
        /// <para>The json.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The link</para>
        /// <para></para>
        /// </returns>
        public TLink Import(IJsonStorage<TLink> storage, string documentName, byte[] json)
        {
            Utf8JsonReader utf8JsonReader = new(json);
            JsonImporter<TLink> jsonImporter = new(storage);
            CancellationTokenSource importCancellationTokenSource = new();
            CancellationToken cancellationToken = importCancellationTokenSource.Token;
            return jsonImporter.Import(documentName, ref utf8JsonReader, in cancellationToken);
        }

        /// <summary>
        /// <para>
        /// Exports the document link.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="documentLink">
        /// <para>The document link.</para>
        /// <para></para>
        /// </param>
        /// <param name="storage">
        /// <para>The storage.</para>
        /// <para></para>
        /// </param>
        /// <param name="stream">
        /// <para>The stream.</para>
        /// <para></para>
        /// </param>
        public void Export(TLink documentLink, IJsonStorage<TLink> storage, in MemoryStream stream)
        {
            Utf8JsonWriter writer = new(stream);
            JsonExporter<TLink> jsonExporter = new(storage);
            CancellationTokenSource exportCancellationTokenSource = new();
            CancellationToken exportCancellationToken = exportCancellationTokenSource.Token;
            jsonExporter.Export(documentLink, ref writer, in exportCancellationToken);
            writer.Dispose();
        }
        
        /// <summary>
        /// <para>
        /// Tests that test.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="initialJson">
        /// <para>The initial json.</para>
        /// <para></para>
        /// </param>
        [Theory]
        [InlineData("{}")]
        [InlineData("\"stringValue\"")]
        [InlineData("228")]
        [InlineData("0.5")]
        [InlineData("[]")]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData("null")]
        [InlineData("{ \"string\": \"string\" }")]
        [InlineData("{ \"null\": null }")]
        [InlineData("{ \"boolean\": false }")]
        [InlineData("{ \"boolean\": true }")]
        [InlineData("{ \"array\": [] }")]
        [InlineData("{ \"array\": [1] }")]
        [InlineData("{ \"object\": {} }")]
        [InlineData("{ \"number\": 1 }")]
        [InlineData("{ \"decimal\": 0.5 }")]
        [InlineData("[null]")]
        [InlineData("[true]")]
        [InlineData("[false]")]
        [InlineData("[[]]")]
        [InlineData("[[1]]")]
        [InlineData("[[0.5]]")]
        [InlineData("[{}]")]
        [InlineData("[\"The Venus Project\"]")]
        [InlineData("[{ \"title\": \"The Venus Project\" }]")]
        [InlineData("[1,2,3,4]")]
        [InlineData("[-0.5, 0.5]")]
        public void Test(string initialJson)
        {
            var links = CreateLinks();
            BalancedVariantConverter = new(links);
            var storage = CreateJsonStorage(links);
            var json = Encoding.UTF8.GetBytes(initialJson);
            var documentLink = Import(storage, "documentName", json);
            MemoryStream stream = new();
            Export(documentLink, storage, in stream);
            string exportedJson = Encoding.UTF8.GetString(stream.ToArray());
            stream.Dispose();
            var minimizedInitialJson = Regex.Replace(initialJson, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
            Assert.Equal(minimizedInitialJson, exportedJson);
        }
    }
}
