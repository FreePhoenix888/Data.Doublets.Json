﻿using System.Text;
using System.Text.Json;
using System.Threading;
using System.IO;
using Xunit;
using TLink = System.UInt64;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.Memory;
using Platform.Data.Doublets.Memory;
using System.Text.RegularExpressions;

namespace Platform.Data.Doublets.Json.Tests
{
    public class JsonImportAndExportTests
    {
        public static ILinks<TLink> CreateLinks() => CreateLinks<TLink>(new IO.TemporaryFile());

        public static ILinks<TLink> CreateLinks<TLink>(string dataDBFilename)
        {
            var linksConstants = new LinksConstants<TLink>(enableExternalReferencesSupport: true);
            return new UnitedMemoryLinks<TLink>(new FileMappedResizableDirectMemory(dataDBFilename), UnitedMemoryLinks<TLink>.DefaultLinksSizeStep, linksConstants, IndexTreeType.Default);
        }

        public static DefaultJsonStorage<TLink> CreateJsonStorage() => new DefaultJsonStorage<TLink>(CreateLinks());
        
        public static DefaultJsonStorage<TLink> CreateJsonStorage(ILinks<TLink> links) => new DefaultJsonStorage<TLink>(links);
        
        public TLink Import(IJsonStorage<TLink> storage, string documentName, byte[] json)
        {
            Utf8JsonReader utf8JsonReader = new(json);
            JsonImporter<TLink> jsonImporter = new(storage);
            CancellationTokenSource importCancellationTokenSource = new();
            CancellationToken cancellationToken = importCancellationTokenSource.Token;
            return jsonImporter.Import(documentName, ref utf8JsonReader, ref cancellationToken);
        }

        public void Export(TLink documentLink, IJsonStorage<TLink> storage, ref MemoryStream stream)
        {
            Utf8JsonWriter writer = new(stream);
            JsonExporter<TLink> jsonExporter = new(storage);
            CancellationTokenSource exportCancellationTokenSource = new();
            CancellationToken exportCancellationToken = exportCancellationTokenSource.Token;
            jsonExporter.Export(documentLink, ref writer, ref exportCancellationToken);
            writer.Dispose();
        }
        
        [Theory]
        [InlineData("{}")]
        [InlineData("\"stringValue\"")]
        [InlineData("228")]
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
        [InlineData("[null]")]
        [InlineData("[true]")]
        [InlineData("[false]")]
        [InlineData("[[]]")]
        [InlineData("[[1]]")]
        [InlineData("[{}]")]
        [InlineData("[\"The Venus Project\"]")]
        [InlineData("[{ \"title\": \"The Venus Project\" }]")]
        [InlineData("[1,2,3,4]")]
        public void Test(string initialJson)
        {
            var storage = CreateJsonStorage();
            var json = Encoding.UTF8.GetBytes(initialJson);
            var documentLink = Import(storage, "documentName", json);
            MemoryStream stream = new();
            Export(documentLink, storage, ref stream);
            string exportedJson = Encoding.UTF8.GetString(stream.ToArray());
            stream.Dispose();
            var minimizedInitialJson = Regex.Replace(initialJson, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
            Assert.Equal(minimizedInitialJson, exportedJson);
        }
    }
}
