﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Threading;
using System.IO;
using Platform.Converters;
using System.Collections;
using Platform.Data.Doublets.Sequences;
using Platform.Data.Doublets.Sequences.HeightProviders;
using Platform.Data.Doublets.Sequences.CriterionMatchers;
using Platform.Collections.Stacks;

namespace Platform.Data.Doublets.Json
{
    public class JsonImporter<TLink>
    {
        private readonly IJsonStorage<TLink> _storage;
        public JsonImporter(IJsonStorage<TLink> storage) => _storage = storage;

        public TLink Import(string documentName, ref Utf8JsonReader utf8JsonReader, CancellationToken cancellationToken)
        {
            TLink document = _storage.CreateDocument(documentName);
            Stack<TLink> parents = new();
            Stack<JsonTokenType> parentsMarkers = new();
            parents.Push(document);
            DefaultStack<TLink> stack = new();
            while (utf8JsonReader.Read())
            {
                var tokenType = utf8JsonReader.TokenType;
                if (utf8JsonReader.TokenType == JsonTokenType.PropertyName)
                {
                    parents.Push(_storage.AttachMemberToObject(_storage.GetObject(parents.First()), utf8JsonReader.GetString()));
                    parentsMarkers.Push(tokenType);
                }
                if (tokenType == JsonTokenType.StartObject)
                {
                    var parent = parents.First();
                    if (parentsMarkers.First() == JsonTokenType.PropertyName)
                    {
                        parentsMarkers.Pop();
                        parent = parents.Pop();
                    }
                    parents.Push(_storage.AttachObject(parent));
                    parentsMarkers.Push(tokenType);
                }
                else if (tokenType == JsonTokenType.EndObject)
                {
                    parents.Pop();
                    parentsMarkers.Pop();
                }
                else if (tokenType == JsonTokenType.String)
                {
                    _storage.AttachString(parents.First(), utf8JsonReader.GetString());
                }
                else if (tokenType == JsonTokenType.Number)
                {
                    _storage.AttachNumber(parents.First(), UncheckedConverter<int, TLink>.Default.Convert(utf8JsonReader.GetInt32()));
                }
                else if (tokenType == JsonTokenType.StartArray)
                {
                    JsonArrayElementCriterionMatcher<TLink> jsonArrayElementCriterionMatcher = new(_storage);
                    DefaultSequenceRightHeightProvider<TLink> defaultSequenceRightHeightProvider = new(_storage.Links, jsonArrayElementCriterionMatcher);
                    //CachedSequenceHeightProvider<TLink> cachedSequenceHeightProvider = new(defaultSequenceRightHeightProvider);
                    DefaultSequenceAppender<TLink> defaultSequenceAppender = new(_storage.Links, stack, defaultSequenceRightHeightProvider);
                    parentsMarkers.Push(tokenType);
                }
                else if (tokenType == JsonTokenType.EndArray)
                {
                    parents.Pop();
                    parentsMarkers.Pop();
                }
                else if (tokenType == JsonTokenType.True)
                {
                    _storage.AttachBoolean(parents.First(), true);
                }
                else if (tokenType == JsonTokenType.False)
                {
                    _storage.AttachBoolean(parents.First(), false);
                }
                else if (tokenType == JsonTokenType.Null)
                {
                    _storage.AttachNull(parents.First());
                }
                if (parentsMarkers.First() == JsonTokenType.PropertyName && tokenType != JsonTokenType.PropertyName)
                {
                    parentsMarkers.Pop();
                    parents.Pop();
                }
            }

            return document;
        }
    }
}
