﻿using Platform.Numbers;
using Platform.Data.Doublets.Unicode;
using Platform.Data.Doublets.Sequences.Converters;
using Platform.Data.Doublets.CriterionMatchers;
using Platform.Data.Numbers.Raw;
using Platform.Converters;
using Platform.Data.Doublets.Sequences.Walkers;
using Platform.Collections.Stacks;
using System;
using System.Collections.Generic;
using Platform.Data.Doublets.Numbers.Rational;
using Platform.Data.Doublets.Numbers.Raw;
using Platform.Data.Doublets.Sequences.HeightProviders;
using Platform.Data.Doublets.Sequences;

namespace Platform.Data.Doublets.Json
{
    public class DefaultJsonStorage<TLink> : IJsonStorage<TLink>
        where TLink : struct
    {
        public readonly TLink Any;
        public static readonly TLink Zero = default;
        public static readonly TLink One = Arithmetic.Increment(Zero);
        public readonly BalancedVariantConverter<TLink> BalancedVariantConverter;
        public readonly IConverter<IList<TLink>, TLink> ListToSequenceConverter;
        public readonly TLink MeaningRoot;
        public readonly EqualityComparer<TLink> EqualityComparer = EqualityComparer<TLink>.Default;
        // Converters that are able to convert link's address (UInt64 value) to a raw number represented with another UInt64 value and back
        public readonly RawNumberToAddressConverter<TLink> NumberToAddressConverter = new();
        public readonly AddressToRawNumberConverter<TLink> AddressToNumberConverter = new();
        // Converters between BigInteger and raw number sequence
        public readonly BigIntegerToRawNumberSequenceConverter<TLink> BigIntegerToRawNumberSequenceConverter;
        public readonly RawNumberSequenceToBigIntegerConverter<TLink> RawNumberSequenceToBigIntegerConverter;
        // Converters between decimal and rational number sequence
        public readonly DecimalToRationalConverter<TLink> DecimalToRationalConverter;
        public readonly RationalToDecimalConverter<TLink> RationalToDecimalConverter;
        // Converters between string and unicode sequence
        public readonly IConverter<string, TLink> StringToUnicodeSequenceConverter;
        public readonly IConverter<TLink, string> UnicodeSequenceToStringConverter;
        // For sequences
        public readonly JsonArrayElementCriterionMatcher<TLink> JsonArrayElementCriterionMatcher;
        public readonly DefaultSequenceRightHeightProvider<TLink> DefaultSequenceRightHeightProvider;
        public readonly DefaultSequenceAppender<TLink> DefaultSequenceAppender;
        public ILinks<TLink> Links { get; }
        public TLink DocumentMarker { get; }
        public TLink ObjectMarker { get; }
        public TLink MemberMarker { get; }
        public TLink ValueMarker { get; }
        public TLink StringMarker { get; }
        public TLink EmptyStringMarker { get; }
        public TLink NumberMarker { get; }
        public TLink NegativeNumberMarker { get; }
        public TLink ArrayMarker { get; }
        public TLink EmptyArrayMarker { get; }
        public TLink TrueMarker { get; }
        public TLink FalseMarker { get; }
        public TLink NullMarker { get; }

        public DefaultJsonStorage(ILinks<TLink> links, IConverter<IList<TLink>, TLink> listToSequenceConverter)
        {
            Links = links;
            ListToSequenceConverter = listToSequenceConverter;
            // Initializes constants
            Any = Links.Constants.Any;
            var markerIndex = One;
            MeaningRoot = links.GetOrCreate(markerIndex, markerIndex);
            var unicodeSymbolMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            var unicodeSequenceMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            DocumentMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            ObjectMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            MemberMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            ValueMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            StringMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            EmptyStringMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            NumberMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            NegativeNumberMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            ArrayMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            EmptyArrayMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            TrueMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            FalseMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            NullMarker = links.GetOrCreate(MeaningRoot, Arithmetic.Increment(ref markerIndex));
            BalancedVariantConverter = new(links);
            TargetMatcher<TLink> unicodeSymbolCriterionMatcher = new(Links, unicodeSymbolMarker);
            TargetMatcher<TLink> unicodeSequenceCriterionMatcher = new(Links, unicodeSequenceMarker);
            CharToUnicodeSymbolConverter<TLink> charToUnicodeSymbolConverter =
                new(Links, AddressToNumberConverter, unicodeSymbolMarker);
            UnicodeSymbolToCharConverter<TLink> unicodeSymbolToCharConverter =
                new(Links, NumberToAddressConverter, unicodeSymbolCriterionMatcher);
            StringToUnicodeSequenceConverter = new CachingConverterDecorator<string, TLink>(
                new StringToUnicodeSequenceConverter<TLink>(Links, charToUnicodeSymbolConverter,
                    BalancedVariantConverter, unicodeSequenceMarker));
            RightSequenceWalker<TLink> sequenceWalker =
                new(Links, new DefaultStack<TLink>(), unicodeSymbolCriterionMatcher.IsMatched);
            UnicodeSequenceToStringConverter = new CachingConverterDecorator<TLink, string>(
                new UnicodeSequenceToStringConverter<TLink>(Links, unicodeSequenceCriterionMatcher, sequenceWalker,
                    unicodeSymbolToCharConverter));
            BigIntegerToRawNumberSequenceConverter =
                new(links, AddressToNumberConverter, ListToSequenceConverter, NegativeNumberMarker);
            RawNumberSequenceToBigIntegerConverter = new(links, NumberToAddressConverter, NegativeNumberMarker);
            DecimalToRationalConverter = new(links, BigIntegerToRawNumberSequenceConverter);
            RationalToDecimalConverter = new(links, RawNumberSequenceToBigIntegerConverter);
            JsonArrayElementCriterionMatcher = new(this);
            DefaultSequenceRightHeightProvider = new(Links, JsonArrayElementCriterionMatcher);
            DefaultSequenceAppender = new(Links, new DefaultStack<TLink>(), DefaultSequenceRightHeightProvider);
            
        }
        
        public TLink CreateString(string content)
        {
            var @string = GetStringSequence(content);
            return Links.GetOrCreate(StringMarker, @string);
        }

        public TLink CreateStringValue(string content)
        {
            var @string = CreateString(content);
            return CreateValue(@string);
        }

        public TLink CreateNumber(decimal number)
        {
            var numberLink = DecimalToRationalConverter.Convert(number);
            return Links.GetOrCreate(NumberMarker, numberLink);
        }

        public TLink CreateNumberValue(decimal number)
        {
            var numberLink = CreateNumber(number);
            return CreateValue(numberLink);
        }

        public TLink CreateBooleanValue(bool value) => CreateValue(value ? TrueMarker : FalseMarker);

        public TLink CreateNullValue() => CreateValue(NullMarker);

        public TLink CreateDocument(string name)
        {
            var documentName = CreateString(name);
            return Links.GetOrCreate(DocumentMarker, documentName);
        }

        public TLink CreateObject()
        {
            var objectInstance = Links.Create();
            return Links.Update(objectInstance, newSource: ObjectMarker, newTarget: objectInstance);
        }

        public TLink CreateObjectValue()
        {
            var @object = CreateObject();
            return CreateValue(@object);
        }

        public TLink CreateArray(IList<TLink> array)
        {
            var arraySequence = array.Count == 0 ? EmptyArrayMarker : BalancedVariantConverter.Convert(array);
            return CreateArray(arraySequence);
        }

        public TLink CreateArray(TLink sequence) => Links.GetOrCreate(ArrayMarker, sequence);

        public TLink CreateArrayValue(IList<TLink> array)
        {
            var arrayLink = CreateArray(array);
            return CreateValue(arrayLink);
        }

        public TLink CreateArrayValue(TLink sequence)
        {
            var array = CreateArray(sequence);
            return CreateValue(array);
        }

        public TLink CreateMember(string name) => Links.GetOrCreate(MemberMarker, CreateString(name));

        public TLink CreateValue(TLink key, TLink @object) => Links.GetOrCreate(key, CreateValue(@object));

        public TLink CreateValue(TLink @object) => Links.GetOrCreate(ValueMarker, @object);

        public TLink AttachObject(TLink parent) => Attach(parent, CreateObjectValue());
        
        public TLink AttachString(TLink parent, string content) => Attach(parent, CreateValue(CreateString(content)));

        public TLink AttachNumber(TLink parent, decimal number)
        {
            var numberLInk = CreateNumber(number);
            var numberValue = CreateValue(numberLInk);
            return Attach(parent, numberValue);
        }

        public TLink AttachBoolean(TLink parent, bool value)
        {
            var booleanValue = CreateBooleanValue(value);
            return Attach(parent, booleanValue);
        }

        public TLink AttachNull(TLink parent)
        {
            var nullValue = CreateNullValue();
            return Attach(parent, nullValue);
        }

        public TLink AttachArray(TLink parent, IList<TLink> array)
        {
            var arrayValue = CreateArrayValue(array);
            return Attach(parent, arrayValue);
        }

        public TLink AttachMemberToObject(TLink @object, string keyName)
        {
            var member = CreateMember(keyName); 
            return Attach(@object, member);
        }

        public TLink Attach(TLink parent, TLink child) => Links.GetOrCreate(parent, child);

        public TLink AppendArrayValue(TLink arrayValue, TLink appendant)
        {
            var array = GetArray(arrayValue);
            var arraySequence = Links.GetTarget(array);
            TLink newArrayValue;
            if (EqualityComparer.Equals(arraySequence, EmptyArrayMarker))
            {
                newArrayValue = CreateArrayValue(appendant);
            }
            else
            {
                arraySequence = DefaultSequenceAppender.Append(arraySequence, appendant);
                newArrayValue = CreateArrayValue(arraySequence);
            }
            return newArrayValue;
        }

        public TLink GetDocumentOrDefault(string name)
        {
            var stringSequence = GetStringSequence(name);
            var @string = Links.SearchOrDefault(StringMarker, stringSequence);
            if (EqualityComparer.Equals(@string, default))
            {
                return default;
            }
            return Links.SearchOrDefault(DocumentMarker, @string);
        }

        private TLink GetStringSequence(string content) => content == "" ? EmptyStringMarker : StringToUnicodeSequenceConverter.Convert(content);
        
        public string GetString(TLink stringValue)
        {
            var current = stringValue;
            TLink source;
            for (int i = 0; i < 3; i++)
            {
                source = Links.GetSource(current);
                if (EqualityComparer.Equals(source, StringMarker))
                {
                    var sequence = Links.GetTarget(current);
                    var isEmpty = EqualityComparer.Equals(sequence, EmptyStringMarker);
                    return isEmpty ? "" : UnicodeSequenceToStringConverter.Convert(sequence);
                }
                current = Links.GetTarget(current);
            }
            throw new Exception("The passed link does not contain a string.");
        }

        public decimal GetNumber(TLink valueLink)
        {
            var current = valueLink;
            TLink source;
            TLink target;
            for (int i = 0; i < 3; i++)
            {
                source = Links.GetSource(current);
                target = Links.GetTarget(current);
                if (EqualityComparer.Equals(source, NumberMarker))
                {
                    return RationalToDecimalConverter.Convert(target);
                }
                current = target;
            }
            throw new Exception("The passed link does not contain a number.");
        }


        public TLink GetObject(TLink objectValueLink)
        {
            var current = objectValueLink;
            TLink source;
            for (int i = 0; i < 3; i++)
            {
                source = Links.GetSource(current);
                if (EqualityComparer.Equals(source, ObjectMarker))
                {
                    return current;
                }
                current = Links.GetTarget(current);
            }
            throw new Exception("The passed link does not contain an object.");
        }

        public TLink GetArray(TLink arrayValueLink)
        {
            var current = arrayValueLink;
            TLink source;
            for (int i = 0; i < 3; i++)
            {
                source = Links.GetSource(current);
                if (EqualityComparer.Equals(source, ArrayMarker))
                {
                    return current;
                }
                current = Links.GetTarget(current);
            }
            throw new Exception("The passed link does not contain an array.");
        }

        public TLink GetArraySequence(TLink array) => Links.GetTarget(array);

        public TLink GetValueLink(TLink parent)
        {
            var query = new Link<TLink>(index: Any, source: parent, target: Any);
            var resultLinks = Links.All(query);
            switch (resultLinks.Count)
            {
                case 0:
                    return default;
                case 1:
                    var resultLinkTarget = Links.GetTarget(resultLinks[0]);
                    if (EqualityComparer.Equals(Links.GetSource(resultLinkTarget), ValueMarker))
                    {
                        return resultLinkTarget;
                    }
                    else
                    {
                        throw new InvalidOperationException("The passed link is not a value.");
                    }
                case > 1:
                    throw new InvalidOperationException("More than 1 value found.");
                default:
                    throw new InvalidOperationException("The list elements length is negative.");
            }
        }

        public TLink GetValueMarker(TLink value)
        {
            var target = Links.GetTarget(value);
            var targetSource = Links.GetSource(target);
            if (EqualityComparer.Equals(MeaningRoot, targetSource))
            {
                return target;
            }
            return targetSource;
        }

        public List<TLink> GetMembersLinks(TLink @object)
        {
            Link<TLink> query = new(index: Any, source: @object, target: Any);
            List<TLink> members = new();
            Links.Each(objectMemberLink =>
            {
                var memberLink = Links.GetTarget(objectMemberLink);
                var memberMarker = Links.GetSource(memberLink);
                if (EqualityComparer.Equals(memberMarker, MemberMarker))
                {
                    members.Add(Links.GetIndex(objectMemberLink));
                }
                return Links.Constants.Continue;
            }, query);
            return members;
        }
    }
}



