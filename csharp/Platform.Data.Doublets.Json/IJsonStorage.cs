﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Platform.Data.Doublets.Json
{
    public interface IJsonStorage<TLink>
    {
        public ILinks<TLink> Links { get; }
        public TLink DocumentMarker { get; }
        public TLink ObjectMarker { get; }
        public TLink StringMarker { get; }
        public TLink MemberMarker { get; }
        public TLink ValueMarker { get; }
        public TLink NumberMarker { get; }
        public TLink ArrayMarker { get; }
        public TLink EmptyArrayMarker { get; }
        public TLink TrueMarker { get; }
        public TLink FalseMarker { get; }
        public TLink NullMarker { get; }
        TLink CreateString(string content);
        TLink CreateNumber(TLink number);
        TLink CreateBooleanValue(bool value);
        TLink CreateNullValue();
        TLink CreateDocument(string name);
        TLink GetDocumentOrDefault(string name);
        TLink CreateObject();
        TLink CreateObjectValue();
        TLink CreateArray(IList<TLink> array);
        TLink CreateMember(string name);
        TLink CreateValue(TLink keyLink, string @string);
        TLink CreateValue(TLink keyLink, TLink @object);
        TLink CreateValue(TLink @object);
        TLink AttachObject(TLink parent);
        TLink AttachObjectValue(TLink parent, TLink objectValueLink);
        TLink AttachString(TLink parent, string content);
        TLink AttachStringValue(TLink parent, TLink stringValueLink);
        TLink AttachNumber(TLink parent, TLink number);
        TLink AttachNumberValue(TLink parent, TLink numberValueLink);
        TLink AttachBoolean(TLink parent, bool value);
        TLink AttachBooleanValue(TLink parent, TLink booleanValueLink);
        TLink AttachNull(TLink parent);
        TLink AttachNullValue(TLink parent, TLink nullValueLink);
        TLink AttachArray(TLink parent, TLink arrayLink);
        TLink AttachArray(TLink parent, IList<TLink> array);
        TLink AttachArrayValue(TLink parent, TLink arrayValueLink);
        TLink AttachMemberToObject(TLink @object, string keyName);
        TLink Attach(TLink parent, TLink child);
        string GetString(TLink stringValue);
        TLink GetNumber(TLink value);
        TLink GetObject(TLink objectValue);
        TLink GetArray(TLink arrayValueLink);
        TLink GetArrayLink(TLink link);
        TLink GetValueLink(TLink parent);
        TLink GetValueMarker(TLink link);
        List<TLink> GetMembersLinks(TLink @object);
        bool IsMember(TLink link);
    }
}