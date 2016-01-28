﻿// Enums.NET
// Copyright 2016 Tyler Brinkley. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

#if !(NET20 || NET35)
using System.Collections.Concurrent;
#endif

namespace EnumsNET
{
    internal sealed class EnumsCache<TInt>
    {
        #region Static
        internal static Func<TInt, TInt, bool> Equal;

        internal static Func<TInt, TInt, bool> GreaterThan;

        internal static Func<TInt, TInt, TInt> And;

        internal static Func<TInt, TInt, TInt> Or;

        internal static Func<TInt, TInt, TInt> Xor;

        internal static Func<TInt, int, TInt> LeftShift;

        //internal static Func<TInt, int, TInt> RightShift;

        internal static Func<TInt, TInt, TInt> Add;

        internal static Func<TInt, TInt, TInt> Subtract;

        internal static bool IsPowerOfTwo(TInt x) => Equal(And(x, Subtract(x, One)), Zero);

        internal static Func<long, TInt> FromInt64;

        internal static Func<ulong, TInt> FromUInt64;

        internal static Func<long, bool> Int64IsInValueRange;

        internal static Func<ulong, bool> UInt64IsInValueRange;

        internal static Func<TInt, sbyte> ToSByte;

        internal static Func<TInt, byte> ToByte;

        internal static Func<TInt, short> ToInt16;

        internal static Func<TInt, ushort> ToUInt16;

        internal static Func<TInt, int> ToInt32;

        internal static Func<TInt, uint> ToUInt32;

        internal static Func<TInt, long> ToInt64;

        internal static Func<TInt, ulong> ToUInt64;

        internal static Func<TInt, string, string> ToStringFormat;

        internal static IntegralTryParseDelegate<TInt> TryParseMethod;

        internal static string HexFormatString;

        internal static TInt Zero;

        internal static TInt One;

        internal static TInt MaxValue;

        internal static TInt MinValue;

        static EnumsCache()
        {
            EnumsCache.Populate(Type.GetTypeCode(typeof(TInt)));
        }
        #endregion

        #region Fields
        // The main collection of values, names, and attributes with ~O(1) retrieval on name or value
        // If constant contains a DescriptionAttribute it will be the first in the attribute array
        private OrderedBiDirectionalDictionary<TInt, NameAndAttributes> _valueMap;

        // Duplicate values are stored here with a key of the constant's name, is null if no duplicates
        private Dictionary<string, ValueAndAttributes<TInt>> _duplicateValues;

        private Dictionary<string, string> _ignoreCaseSet;

        private int _lastCustomEnumFormatIndex = -1;

        private List<Func<IEnumMemberInfo<TInt>, string>> _customEnumFormatters;

#if NET20 || NET35
        private Dictionary<EnumFormat, EnumParser> _customEnumFormatParsers;
#else
        private ConcurrentDictionary<EnumFormat, EnumParser> _customEnumFormatParsers;
#endif
        #endregion

        #region Properties
        public TInt AllFlags { get; }

        public bool IsFlagEnum { get; }

        public bool IsContiguous { get; }

        public Type UnderlyingType => typeof(TInt);

        private TInt MaxDefined => _valueMap.GetFirstAt(_valueMap.Count - 1);

        private TInt MinDefined => _valueMap.GetFirstAt(0);

        // Enables case insensitive parsing, lazily instantiated to reduce memory usage if not going to use this feature, is thread-safe as it's only used for retrieval
        private Dictionary<string, string> IgnoreCaseSet
        {
            get
            {
                if (_ignoreCaseSet == null)
                {
                    var ignoreCaseSet = new Dictionary<string, string>(_valueMap.Count + (_duplicateValues?.Count ?? 0), StringComparer.OrdinalIgnoreCase);
                    foreach (var nameAndAttributes in _valueMap.SecondItems)
                    {
                        ignoreCaseSet.Add(nameAndAttributes.Name, nameAndAttributes.Name);
                    }
                    if (_duplicateValues != null)
                    {
                        foreach (var name in _duplicateValues.Keys)
                        {
                            ignoreCaseSet.Add(name, name);
                        }
                    }
                    // Doesn't matter if it gets overwritten
                    _ignoreCaseSet = ignoreCaseSet;
                }
                return _ignoreCaseSet;
            }
        }
        #endregion

        public EnumsCache(Type enumType)
        {
            Debug.Assert(enumType != null);
            Debug.Assert(enumType.IsEnum);
            IsFlagEnum = enumType.IsDefined(typeof(FlagsAttribute), false);

            var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            _valueMap = new OrderedBiDirectionalDictionary<TInt, NameAndAttributes>(fields.Length);
            if (fields.Length == 0)
            {
                return;
            }
            var duplicateValues = new Dictionary<string, ValueAndAttributes<TInt>>();
            var greaterThan = GreaterThan;
            var or = Or;
            foreach (var field in fields)
            {
                var value = (TInt)field.GetValue(null);
                var name = field.Name;
                var attributes = Attribute.GetCustomAttributes(field, false);
                var isMainDupe = false;
                if (attributes.Length > 0)
                {
                    var descriptionFound = false;
                    for (var i = 0; i < attributes.Length; ++i)
                    {
                        var attr = attributes[i];
                        if (!descriptionFound)
                        {
                            var descAttr = attr as DescriptionAttribute;
                            if (descAttr != null)
                            {
                                for (var j = i; j > 0; --j)
                                {
                                    attributes[j] = attributes[j - 1];
                                }
                                attributes[0] = descAttr;
                                if (descAttr.GetType() == typeof(DescriptionAttribute))
                                {
                                    descriptionFound = true;
                                    if (isMainDupe)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        if (!isMainDupe && (attr as MainDuplicateAttribute) != null)
                        {
                            isMainDupe = true;
                            if (descriptionFound)
                            {
                                break;
                            }
                        }
                    }
                }
                var index = _valueMap.IndexOfFirst(value);
                if (index < 0)
                {
                    for (index = _valueMap.Count; index > 0; --index)
                    {
                        var mapValue = _valueMap.GetFirstAt(index - 1);
                        if (!greaterThan(mapValue, value))
                        {
                            break;
                        }
                    }
                    _valueMap.Insert(index, value, new NameAndAttributes(name, attributes));
                    if (IsPowerOfTwo(value))
                    {
                        AllFlags = or(AllFlags, value);
                    }
                }
                else
                {
                    if (isMainDupe)
                    {
                        var nameAndAttributes = _valueMap.GetSecondAt(index);
                        _valueMap.ReplaceSecondAt(index, new NameAndAttributes(name, attributes));
                        name = nameAndAttributes.Name;
                        attributes = nameAndAttributes.Attributes;
                    }
                    duplicateValues.Add(name, new ValueAndAttributes<TInt>(value, attributes));
                }
            }
            IsContiguous = ToInt64(Add(Subtract(MaxDefined, MinDefined), One)) == _valueMap.Count;

            _valueMap.TrimExcess();
            if (duplicateValues.Count > 0)
            {
                // Makes sure is in increasing order, due to no removals
#if NET20
                var dupes = duplicateValues.ToArray();
                Array.Sort(dupes, (first, second) => InternalCompare(first.Value.Value, second.Value.Value));
#else
                var dupes = duplicateValues.OrderBy(pair => pair.Value.Value).ToList();
#endif
                _duplicateValues = new Dictionary<string, ValueAndAttributes<TInt>>(duplicateValues.Count);
                foreach (var pair in dupes)
                {
                    _duplicateValues.Add(pair.Key, pair.Value);
                }
            }
        }

        #region Standard Enum Operations
        #region Type Methods
        public int GetDefinedCount(bool uniqueValued) => _valueMap.Count + (uniqueValued ? 0 : _duplicateValues?.Count ?? 0);

        public IEnumerable<EnumMemberInfo<TInt>> GetEnumMemberInfos(bool uniqueValued) => GetEnumMembersInValueOrder(uniqueValued).Select(info => (EnumMemberInfo<TInt>)info);

        public IEnumerable<string> GetNames(bool uniqueValued) => GetEnumMembersInValueOrder(uniqueValued).Select(info => info.Name);

        public IEnumerable<TInt> GetValues(bool uniqueValued) => GetEnumMembersInValueOrder(uniqueValued).Select(info => info.Value);

        public IEnumerable<string> GetDescriptions(bool uniqueValued) => GetEnumMembersInValueOrder(uniqueValued).Select(info => info.Description);

        public IEnumerable<string> GetDescriptionsOrNames(bool uniqueValued) => GetEnumMembersInValueOrder(uniqueValued).Select(info => info.GetDescriptionOrName());

        public IEnumerable<string> GetDescriptionsOrNames(Func<string, string> nameFormatter, bool uniqueValued) => GetEnumMembersInValueOrder(uniqueValued).Select(info => info.GetDescriptionOrName(nameFormatter));

        public IEnumerable<string> GetFormattedValues(EnumFormat[] formats, bool uniqueValued) => GetEnumMembersInValueOrder(uniqueValued).Select(info => info.Format(formats));

        public IEnumerable<Attribute[]> GetAllAttributes(bool uniqueValued) => GetEnumMembersInValueOrder(uniqueValued).Select(info => info.Attributes);

        public IEnumerable<TAttribute> GetAttributes<TAttribute>(bool uniqueValued)
            where TAttribute : Attribute => GetEnumMembersInValueOrder(uniqueValued).Select(info => info.GetAttribute<TAttribute>());

        internal static int Compare(TInt x, TInt y)
        {
            if (GreaterThan(x, y))
            {
                return 1;
            }
            if (GreaterThan(y, x))
            {
                return -1;
            }
            return 0;
        }

        public EnumFormat RegisterCustomEnumFormat(Func<IEnumMemberInfo<TInt>, string> formatter)
        {
            Preconditions.NotNull(formatter, nameof(formatter));

            var index = Interlocked.Increment(ref _lastCustomEnumFormatIndex);
            if (index == 0)
            {
                _customEnumFormatters = new List<Func<IEnumMemberInfo<TInt>, string>>();
            }
            else
            {
                while (_customEnumFormatters == null || _customEnumFormatters.Count < index)
                {
                }
            }
            _customEnumFormatters.Add(formatter);
            return Enums.ToObject<EnumFormat>(index + Enums.StartingGenericCustomEnumFormatValue, false);
        }

        private IEnumerable<InternalEnumMemberInfo<TInt>> GetEnumMembersInValueOrder(bool uniqueValued)
        {
            if (uniqueValued)
            {
                return _valueMap.Select(pair => new InternalEnumMemberInfo<TInt>(pair.First, pair.Second.Name, pair.Second.Attributes));
            }
            else
            {
                return GetAllEnumMembersInValueOrder();
            }
        }

        private IEnumerable<InternalEnumMemberInfo<TInt>> GetAllEnumMembersInValueOrder()
        {
            using (var mainEnumerator = _valueMap.GetEnumerator())
            {
                var mainIsActive = mainEnumerator.MoveNext();
                var mainPair = mainIsActive ? mainEnumerator.Current : new Pair<TInt, NameAndAttributes>();
                using (IEnumerator<KeyValuePair<string, ValueAndAttributes<TInt>>> dupeEnumerator = _duplicateValues?.GetEnumerator())
                {
                    var dupeIsActive = dupeEnumerator?.MoveNext() ?? false;
                    var greaterThan = GreaterThan;
                    var dupePair = dupeIsActive ? dupeEnumerator.Current : new KeyValuePair<string, ValueAndAttributes<TInt>>();
                    var count = _valueMap.Count + (_duplicateValues?.Count ?? 0);
                    for (var i = 0; i < count; ++i)
                    {
                        TInt value;
                        string name;
                        Attribute[] attributes;
                        if (dupeIsActive && (!mainIsActive || greaterThan(mainPair.First, dupePair.Value.Value)))
                        {
                            value = dupePair.Value.Value;
                            name = dupePair.Key;
                            attributes = dupePair.Value.Attributes;
                            if (dupeEnumerator.MoveNext())
                            {
                                dupePair = dupeEnumerator.Current;
                            }
                            else
                            {
                                dupeIsActive = false;
                            }
                        }
                        else
                        {
                            value = mainPair.First;
                            name = mainPair.Second.Name;
                            attributes = mainPair.Second.Attributes;
                            if (mainEnumerator.MoveNext())
                            {
                                mainPair = mainEnumerator.Current;
                            }
                            else
                            {
                                mainIsActive = false;
                            }
                        }
                        yield return new InternalEnumMemberInfo<TInt>(value, name, attributes);
                    }
                }
            }
        }
        #endregion

        #region IsValid
        public bool IsValid(object value)
        {
            TInt result;
            return TryToObject(value, out result);
        }

        public bool IsValid(TInt value) => IsFlagEnum ? IsValidFlagCombination(value) : IsDefined(value);

        public bool IsValid(long value) => Int64IsInValueRange(value) && IsValid(FromInt64(value));

        public bool IsValid(ulong value) => UInt64IsInValueRange(value) && IsValid(FromUInt64(value));
        #endregion

        #region IsDefined
        public bool IsDefined(object value)
        {
            TInt result;
            return TryToObject(value, out result, false) && IsDefined(result);
        }

        public bool IsDefined(TInt value)
        {
            if (IsContiguous)
            {
                return !(GreaterThan(MinDefined, value) || GreaterThan(value, MaxDefined));
            }
            else
            {
                return _valueMap.ContainsFirst(value);
            }
        }

        public bool IsDefined(string name, bool ignoreCase = false)
        {
            Preconditions.NotNull(name, nameof(name));

            return _valueMap.ContainsSecond(new NameAndAttributes(name)) || (_duplicateValues?.ContainsKey(name) ?? false) || (ignoreCase && IgnoreCaseSet.ContainsKey(name));
        }

        public bool IsDefined(long value) => Int64IsInValueRange(value) && IsDefined(FromInt64(value));

        public bool IsDefined(ulong value) => UInt64IsInValueRange(value) && IsDefined(FromUInt64(value));
        #endregion

        #region ToObject
        public TInt ToObject(object value, bool validate = true)
        {
            Preconditions.NotNull(value, nameof(value));

            if (value is TInt)
            {
                var result = (TInt)value;
                if (validate)
                {
                    Validate(result, nameof(value));
                }
                return result;
            }

            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.SByte:
                    return ToObject((sbyte)value, validate);
                case TypeCode.Byte:
                    return ToObject((byte)value, validate);
                case TypeCode.Int16:
                    return ToObject((short)value, validate);
                case TypeCode.UInt16:
                    return ToObject((ushort)value, validate);
                case TypeCode.Int32:
                    return ToObject((int)value, validate);
                case TypeCode.UInt32:
                    return ToObject((uint)value, validate);
                case TypeCode.Int64:
                    return ToObject((long)value, validate);
                case TypeCode.UInt64:
                    return ToObject((ulong)value, validate);
                case TypeCode.String:
                    var result = Parse((string)value);
                    if (validate)
                    {
                        Validate(result, nameof(value));
                    }
                    return result;
            }
            throw new ArgumentException($"value is not type {typeof(TInt).Name}, SByte, Int16, Int32, Int64, Byte, UInt16, UInt32, UInt64, or String.");
        }

        public TInt ToObject(long value, bool validate = true)
        {
            if (!Int64IsInValueRange(value))
            {
                throw Enums.GetOverflowException();
            }

            var result = FromInt64(value);
            if (validate)
            {
                Validate(result, nameof(value));
            }
            return result;
        }

        public TInt ToObject(ulong value, bool validate = true)
        {
            if (!UInt64IsInValueRange(value))
            {
                throw Enums.GetOverflowException();
            }

            var result = FromUInt64(value);
            if (validate)
            {
                Validate(result, nameof(value));
            }
            return result;
        }

        public bool TryToObject(object value, out TInt result, bool validate = true)
        {
            Preconditions.NotNull(value, nameof(value));

            if (value is TInt)
            {
                result = (TInt)value;
                return true;
            }

            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.SByte:
                    return TryToObject((sbyte)value, out result, validate);
                case TypeCode.Byte:
                    return TryToObject((byte)value, out result, validate);
                case TypeCode.Int16:
                    return TryToObject((short)value, out result, validate);
                case TypeCode.UInt16:
                    return TryToObject((ushort)value, out result, validate);
                case TypeCode.Int32:
                    return TryToObject((int)value, out result, validate);
                case TypeCode.UInt32:
                    return TryToObject((uint)value, out result, validate);
                case TypeCode.Int64:
                    return TryToObject((long)value, out result, validate);
                case TypeCode.UInt64:
                    return TryToObject((ulong)value, out result, validate);
                case TypeCode.String:
                    if (TryParse((string)value, out result))
                    {
                        if (!validate || IsValid(result))
                        {
                            return true;
                        }
                    }
                    break;
            }
            result = Zero;
            return false;
        }

        public bool TryToObject(long value, out TInt result, bool validate = true)
        {
            if (Int64IsInValueRange(value))
            {
                result = FromInt64(value);
                if (!validate || IsValid(result))
                {
                    return true;
                }
            }
            result = Zero;
            return false;
        }

        public bool TryToObject(ulong value, out TInt result, bool validate = true)
        {
            if (UInt64IsInValueRange(value))
            {
                result = FromUInt64(value);
                if (!validate || IsValid(result))
                {
                    return true;
                }
            }
            result = Zero;
            return false;
        }
        #endregion

        #region All Values Main Methods
        public void Validate(TInt value, string paramName)
        {
            if (!IsValid(value))
            {
                throw new ArgumentException($"invalid value of {AsString(value)} for {typeof(TInt).Name}", paramName);
            }
        }

        public string AsString(TInt value) => InternalAsString(GetInternalEnumMemberInfo(value));

        private string InternalAsString(InternalEnumMemberInfo<TInt> info)
        {
            if (IsFlagEnum)
            {
                var str = InternalFormatAsFlags(info);
                if (str != null)
                {
                    return str;
                }
            }
            return InternalFormat(info, Enums.DefaultFormatOrder);
        }

        public string AsString(TInt value, EnumFormat[] formats) => formats?.Length > 0 ? Format(value, formats) : AsString(value);

        public string AsString(TInt value, string format) => string.IsNullOrEmpty(format) ? AsString(value) : Format(value, format);

        public string Format(TInt value, EnumFormat format) => InternalFormat(GetInternalEnumMemberInfo(value), format);

        public string Format(TInt value, EnumFormat format0, EnumFormat format1) => InternalFormat(GetInternalEnumMemberInfo(value), format0, format1);

        public string Format(TInt value, EnumFormat format0, EnumFormat format1, EnumFormat format2) => InternalFormat(GetInternalEnumMemberInfo(value), format0, format1, format2);

        public string Format(TInt value, EnumFormat format0, EnumFormat format1, EnumFormat format2, EnumFormat format3) => InternalFormat(GetInternalEnumMemberInfo(value), format0, format1, format2, format3);

        public string Format(TInt value, EnumFormat format0, EnumFormat format1, EnumFormat format2, EnumFormat format3, EnumFormat format4) => InternalFormat(GetInternalEnumMemberInfo(value), format0, format1, format2, format3, format4);

        public string Format(TInt value, EnumFormat[] formats) => InternalFormat(GetInternalEnumMemberInfo(value), formats);

        public string Format(TInt value, string format) => InternalFormat(GetInternalEnumMemberInfo(value), format);

        internal string InternalFormat(InternalEnumMemberInfo<TInt> info, string format)
        {
            Preconditions.NotNull(format, nameof(format));

            switch (format)
            {
                case "G":
                case "g":
                    return InternalAsString(info);
                case "F":
                case "f":
                    return InternalFormatAsFlags(info) ?? InternalFormat(info, Enums.DefaultFormatOrder);
                case "D":
                case "d":
                    return ToDecimalString(info.Value);
                case "X":
                case "x":
                    return ToHexadecimalString(info.Value);
            }
            throw new FormatException("format string can be only \"G\", \"g\", \"X\", \"x\", \"F\", \"f\", \"D\" or \"d\".");
        }

        internal string InternalFormat(InternalEnumMemberInfo<TInt> info, EnumFormat format)
        {
            switch (format)
            {
                case EnumFormat.DecimalValue:
                    return ToDecimalString(info.Value);
                case EnumFormat.HexadecimalValue:
                    return ToHexadecimalString(info.Value);
                case EnumFormat.Name:
                    return info.Name;
                case EnumFormat.Description:
                    return info.Description;
                default:
                    var index = Enums.ToInt32(format) - Enums.StartingCustomEnumFormatValue;
                    if (index >= 0 && index < Enums.CustomEnumFormatters?.Count)
                    {
                        return Enums.CustomEnumFormatters[index](info);
                    }
                    else
                    {
                        index -= Enums.StartingGenericCustomEnumFormatValue - Enums.StartingCustomEnumFormatValue;
                        if (index >= 0 && index < _customEnumFormatters?.Count)
                        {
                            return _customEnumFormatters[index](info);
                        }
                    }
                    return null;
            }
        }

        internal string InternalFormat(InternalEnumMemberInfo<TInt> info, EnumFormat format0, EnumFormat format1)
        {
            return InternalFormat(info, format0) ?? InternalFormat(info, format1);
        }

        internal string InternalFormat(InternalEnumMemberInfo<TInt> info, EnumFormat format0, EnumFormat format1, EnumFormat format2)
        {
            return InternalFormat(info, format0) ?? InternalFormat(info, format1) ?? InternalFormat(info, format2);
        }

        internal string InternalFormat(InternalEnumMemberInfo<TInt> info, EnumFormat format0, EnumFormat format1, EnumFormat format2, EnumFormat format3)
        {
            return InternalFormat(info, format0) ?? InternalFormat(info, format1) ?? InternalFormat(info, format2) ?? InternalFormat(info, format3);
        }

        internal string InternalFormat(InternalEnumMemberInfo<TInt> info, EnumFormat format0, EnumFormat format1, EnumFormat format2, EnumFormat format3, EnumFormat format4)
        {
            return InternalFormat(info, format0) ?? InternalFormat(info, format1) ?? InternalFormat(info, format2) ?? InternalFormat(info, format3) ?? InternalFormat(info, format4);
        }

        internal string InternalFormat(InternalEnumMemberInfo<TInt> info, EnumFormat[] formats)
        {
            Preconditions.NotNull(formats, nameof(formats));

            foreach (var format in formats)
            {
                var formattedValue = InternalFormat(info, format);
                if (formattedValue != null)
                {
                    return formattedValue;
                }
            }
            return null;
        }

        private string ToDecimalString(TInt value) => ToStringFormat(value, "D");

        private string ToHexadecimalString(TInt value) => ToStringFormat(value, HexFormatString);
        #endregion

        #region Defined Values Main Methods
        public EnumMemberInfo<TInt> GetEnumMemberInfo(TInt value) => GetInternalEnumMemberInfo(value);

        public EnumMemberInfo<TInt> GetEnumMemberInfo(string name, bool ignoreCase = false) => GetInternalEnumMemberInfo(name, ignoreCase);

        public string GetName(TInt value) => GetInternalEnumMemberInfo(value).Name;

        public string GetDescription(TInt value) => GetInternalEnumMemberInfo(value).Description;

        public string GetDescriptionOrName(TInt value) => GetInternalEnumMemberInfo(value).GetDescriptionOrName();

        public string GetDescriptionOrName(TInt value, Func<string, string> nameFormatter) => GetInternalEnumMemberInfo(value).GetDescriptionOrName(nameFormatter);

        private InternalEnumMemberInfo<TInt> GetInternalEnumMemberInfo(TInt value)
        {
            var index = _valueMap.IndexOfFirst(value);
            if (index >= 0)
            {
                var nameAndAttributes = _valueMap.GetSecondAt(index);
                return new InternalEnumMemberInfo<TInt>(value, nameAndAttributes.Name, nameAndAttributes.Attributes);
            }
            else
            {
                return new InternalEnumMemberInfo<TInt>(value, null, null);
            }
        }

        private InternalEnumMemberInfo<TInt> GetInternalEnumMemberInfo(string name, bool ignoreCase)
        {
            Preconditions.NotNull(name, nameof(name));

            var index = _valueMap.IndexOfSecond(new NameAndAttributes(name));
            if (index >= 0)
            {
                var pair = _valueMap.GetAt(index);
                return new InternalEnumMemberInfo<TInt>(pair.First, name, pair.Second.Attributes);
            }
            ValueAndAttributes<TInt> valueAndAttributes;
            if (_duplicateValues != null && _duplicateValues.TryGetValue(name, out valueAndAttributes))
            {
                return new InternalEnumMemberInfo<TInt>(valueAndAttributes.Value, name, valueAndAttributes.Attributes);
            }
            if (ignoreCase)
            {
                string actualName;
                if (IgnoreCaseSet.TryGetValue(name, out actualName))
                {
                    index = _valueMap.IndexOfSecond(new NameAndAttributes(actualName));
                    if (index >= 0)
                    {
                        var pair = _valueMap.GetAt(index);
                        return new InternalEnumMemberInfo<TInt>(pair.First, actualName, pair.Second.Attributes);
                    }
                    valueAndAttributes = _duplicateValues[actualName];
                    return new InternalEnumMemberInfo<TInt>(valueAndAttributes.Value, actualName, valueAndAttributes.Attributes);
                }
            }
            return new InternalEnumMemberInfo<TInt>();
        }
        #endregion

        #region Attributes
        public bool HasAttribute<TAttribute>(TInt value)
            where TAttribute : Attribute => GetInternalEnumMemberInfo(value).HasAttribute<TAttribute>();

        public TAttribute GetAttribute<TAttribute>(TInt value)
            where TAttribute : Attribute => GetInternalEnumMemberInfo(value).GetAttribute<TAttribute>();

        public TResult GetAttributeSelect<TAttribute, TResult>(TInt value, Func<TAttribute, TResult> selector, TResult defaultValue)
            where TAttribute : Attribute => GetInternalEnumMemberInfo(value).GetAttributeSelect(selector, defaultValue);

        public bool TryGetAttributeSelect<TAttribute, TResult>(TInt value, Func<TAttribute, TResult> selector, out TResult result)
            where TAttribute : Attribute => GetInternalEnumMemberInfo(value).TryGetAttributeSelect(selector, out result);

        public IEnumerable<TAttribute> GetAttributes<TAttribute>(TInt value)
            where TAttribute : Attribute => GetInternalEnumMemberInfo(value).GetAttributes<TAttribute>();

        public Attribute[] GetAllAttributes(TInt value) => GetInternalEnumMemberInfo(value).Attributes;
        #endregion

        #region Parsing
        public TInt Parse(string value) => Parse(value, false, null);

        public TInt Parse(string value, EnumFormat[] parseFormatOrder) => Parse(value, false, parseFormatOrder);

        public TInt Parse(string value, bool ignoreCase) => Parse(value, ignoreCase, null);

        public TInt Parse(string value, bool ignoreCase, EnumFormat[] parseFormatOrder)
        {
            Preconditions.NotNull(value, nameof(value));

            value = value.Trim();
            TInt result;
            if (IsFlagEnum)
            {
                return TryParseMethod(value, NumberStyles.AllowLeadingSign, null, out result) ? result : ParseFlags(value, ignoreCase, FlagEnums.DefaultDelimiter, parseFormatOrder);
            }

            if (!(parseFormatOrder?.Length > 0))
            {
                parseFormatOrder = Enums.DefaultFormatOrder;
            }

            if (InternalTryParse(value, ignoreCase, out result, parseFormatOrder))
            {
                return result;
            }
            if (Enums.IsNumeric(value))
            {
                throw Enums.GetOverflowException();
            }
            throw new ArgumentException($"string was not recognized as being a member of {typeof(TInt).Name}", nameof(value));
        }

        public bool TryParse(string value, out TInt result) => TryParse(value, false, out result, null);

        public bool TryParse(string value, out TInt result, EnumFormat[] parseFormatOrder) => TryParse(value, false, out result, parseFormatOrder);

        public bool TryParse(string value, bool ignoreCase, out TInt result) => TryParse(value, ignoreCase, out result, null);

        public bool TryParse(string value, bool ignoreCase, out TInt result, EnumFormat[] parseFormatOrder)
        {
            if (value != null)
            {
                value = value.Trim();
                if (IsFlagEnum)
                {
                    if (TryParseMethod(value, NumberStyles.AllowLeadingSign, null, out result) || TryParseFlags(value, ignoreCase, FlagEnums.DefaultDelimiter, out result, parseFormatOrder))
                    {
                        return true;
                    }
                    return false;
                }

                if (!(parseFormatOrder?.Length > 0))
                {
                    parseFormatOrder = Enums.DefaultFormatOrder;
                }

                if (InternalTryParse(value, ignoreCase, out result, parseFormatOrder))
                {
                    return true;
                }
            }
            result = Zero;
            return false;
        }

        private bool InternalTryParse(string value, bool ignoreCase, out TInt result, EnumFormat[] parseFormatOrder)
        {
            foreach (var format in parseFormatOrder)
            {
                switch (format)
                {
                    case EnumFormat.DecimalValue:
                    case EnumFormat.HexadecimalValue:
                        if (TryParseMethod(value, format == EnumFormat.DecimalValue ? NumberStyles.AllowLeadingSign : NumberStyles.AllowHexSpecifier, null, out result))
                        {
                            return true;
                        }
                        break;
                    case EnumFormat.Name:
                        if (TryParseName(value, ignoreCase, out result))
                        {
                            return true;
                        }
                        break;
                    default:
                        EnumParser parser = null;
#if NET20
                        lock (_valueMap)
                        {
#endif
                            if (_customEnumFormatParsers == null || !_customEnumFormatParsers.TryGetValue(format, out parser))
                            {
                                switch (format)
                                {
                                    case EnumFormat.Description:
                                        parser = new EnumParser(Enums.DescriptionEnumFormatter, this);
                                        break;
                                    default:
                                        var index = Enums.ToInt32(format) - Enums.StartingCustomEnumFormatValue;
                                        if (index >= 0 && index < Enums.CustomEnumFormatters?.Count)
                                        {
                                            parser = new EnumParser(Enums.CustomEnumFormatters[index], this);
                                        }
                                        else
                                        {
                                            index -= Enums.StartingGenericCustomEnumFormatValue - Enums.StartingCustomEnumFormatValue;
                                            if (index >= 0 && index < _customEnumFormatters?.Count)
                                            {
                                                parser = new EnumParser(_customEnumFormatters[index], this);
                                            }
                                        }
                                        break;
                                }
                                if (parser != null)
                                {
                                    if (_customEnumFormatParsers == null)
                                    {
#if NET20
                                        _customEnumFormatParsers = new Dictionary<EnumFormat, EnumParser>();
#else
                                        Interlocked.CompareExchange(ref _customEnumFormatParsers, new ConcurrentDictionary<EnumFormat, EnumParser>(), null);
#endif
                                    }
#if NET20
                                    _customEnumFormatParsers.Add(format, parser);
#else
                                    _customEnumFormatParsers.TryAdd(format, parser);
#endif
                                }
                            }
                            if (parser != null && parser.TryParse(value, ignoreCase, out result))
                            {
                                return true;
                            }
#if NET20
                        }
#endif
                        break;
                }
            }
            result = Zero;
            return false;
        }

        private bool TryParseName(string value, bool ignoreCase, out TInt result)
        {
            if (_valueMap.TryGetFirst(new NameAndAttributes(value), out result))
            {
                return true;
            }
            ValueAndAttributes<TInt> valueAndAttributes;
            if (_duplicateValues != null && _duplicateValues.TryGetValue(value, out valueAndAttributes))
            {
                result = valueAndAttributes.Value;
                return true;
            }
            if (ignoreCase)
            {
                string name;
                if (IgnoreCaseSet.TryGetValue(value, out name))
                {
                    if (!_valueMap.TryGetFirst(new NameAndAttributes(name), out result))
                    {
                        result = _duplicateValues[name].Value;
                    }
                    return true;
                }
            }
            result = Zero;
            return false;
        }
        #endregion
        #endregion

        #region Flag Enum Operations
        #region Main Methods
        private bool IsValidFlagCombination(TInt value) => Equal(And(AllFlags, value), value);

        public string FormatAsFlags(TInt value) => InternalFormatAsFlags(GetInternalEnumMemberInfo(value), FlagEnums.DefaultDelimiter, Enums.DefaultFormatOrder);

        public string FormatAsFlags(TInt value, string delimiter) => InternalFormatAsFlags(GetInternalEnumMemberInfo(value), delimiter, Enums.DefaultFormatOrder);

        public string FormatAsFlags(TInt value, EnumFormat[] formats) => InternalFormatAsFlags(GetInternalEnumMemberInfo(value), FlagEnums.DefaultDelimiter, formats);

        public string FormatAsFlags(TInt value, string delimiter, EnumFormat[] formats) => InternalFormatAsFlags(GetInternalEnumMemberInfo(value), delimiter, formats);

        private string InternalFormatAsFlags(InternalEnumMemberInfo<TInt> info, string delimiter = FlagEnums.DefaultDelimiter, EnumFormat[] formats = null)
        {
            Preconditions.NotNullOrEmpty(delimiter, nameof(delimiter));

            if (!IsValidFlagCombination(info.Value))
            {
                return null;
            }

            if (!(formats?.Length > 0))
            {
                formats = Enums.DefaultFormatOrder;
            }

            IEnumerable<TInt> flags;
            if (info.IsDefined || !(flags = InternalGetFlags(info.Value)).Any())
            {
                return InternalFormat(info, formats);
            }

            return string.Join(delimiter,
                flags.Select(flag => InternalFormat(GetInternalEnumMemberInfo(flag), formats))
#if NET20 || NET35
                .ToArray()
#endif
                );
        }

        public TInt[] GetFlags(TInt value)
        {
            return IsValidFlagCombination(value) ? InternalGetFlags(value).ToArray() : null;
        }

        public bool HasAnyFlags(TInt value)
        {
            ValidateIsValidFlagCombination(value, nameof(value));
            return !Equal(value, Zero);
        }

        public bool HasAnyFlags(TInt value, TInt flagMask)
        {
            ValidateIsValidFlagCombination(value, nameof(value));
            ValidateIsValidFlagCombination(flagMask, nameof(flagMask));
            return InternalHasAnyFlags(value, flagMask);
        }

        public bool HasAllFlags(TInt value)
        {
            ValidateIsValidFlagCombination(value, nameof(value));
            return Equal(value, AllFlags);
        }

        public bool HasAllFlags(TInt value, TInt flagMask)
        {
            ValidateIsValidFlagCombination(value, nameof(value));
            ValidateIsValidFlagCombination(flagMask, nameof(flagMask));
            return Equal(And(value, flagMask), flagMask);
        }

        public TInt InvertFlags(TInt value)
        {
            ValidateIsValidFlagCombination(value, nameof(value));
            return Xor(value, AllFlags);
        }

        public TInt InvertFlags(TInt value, TInt flagMask)
        {
            ValidateIsValidFlagCombination(value, nameof(value));
            ValidateIsValidFlagCombination(flagMask, nameof(flagMask));
            return Xor(value, flagMask);
        }

        public TInt CommonFlags(TInt value, TInt flagMask)
        {
            ValidateIsValidFlagCombination(value, nameof(value));
            ValidateIsValidFlagCombination(flagMask, nameof(flagMask));
            return And(value, flagMask);
        }

        public TInt SetFlags(TInt flag0, TInt flag1)
        {
            ValidateIsValidFlagCombination(flag0, nameof(flag0));
            ValidateIsValidFlagCombination(flag1, nameof(flag1));
            return Or(flag0, flag1);
        }

        public TInt SetFlags(TInt flag0, TInt flag1, TInt flag2)
        {
            ValidateIsValidFlagCombination(flag0, nameof(flag0));
            ValidateIsValidFlagCombination(flag1, nameof(flag1));
            ValidateIsValidFlagCombination(flag2, nameof(flag2));

            return Or(Or(flag0, flag1), flag2);
        }

        public TInt SetFlags(TInt flag0, TInt flag1, TInt flag2, TInt flag3)
        {
            ValidateIsValidFlagCombination(flag0, nameof(flag0));
            ValidateIsValidFlagCombination(flag1, nameof(flag1));
            ValidateIsValidFlagCombination(flag2, nameof(flag2));
            ValidateIsValidFlagCombination(flag3, nameof(flag3));

            return Or(Or(Or(flag0, flag1), flag2), flag3);
        }

        public TInt SetFlags(TInt flag0, TInt flag1, TInt flag2, TInt flag3, TInt flag4)
        {
            ValidateIsValidFlagCombination(flag0, nameof(flag0));
            ValidateIsValidFlagCombination(flag1, nameof(flag1));
            ValidateIsValidFlagCombination(flag2, nameof(flag2));
            ValidateIsValidFlagCombination(flag3, nameof(flag3));
            ValidateIsValidFlagCombination(flag4, nameof(flag4));

            return Or(Or(Or(Or(flag0, flag1), flag2), flag3), flag4);
        }

        public TInt SetFlags(TInt[] flags)
        {
            var flag = Zero;
            var flagsLength = flags?.Length ?? 0;
            for (var i = 0; i < flagsLength; ++i)
            {
                var nextFlag = flags[i];
                ValidateIsValidFlagCombination(nextFlag, nameof(flags) + " must contain all valid flag combinations");
                flag = Or(flag, nextFlag);
            }
            return flag;
        }

        public TInt ClearFlags(TInt value, TInt flagMask)
        {
            ValidateIsValidFlagCombination(value, nameof(value));
            ValidateIsValidFlagCombination(flagMask, nameof(flagMask));
            return And(value, Xor(flagMask, AllFlags));
        }
        #endregion

        #region Parsing
        public TInt ParseFlags(string value) => ParseFlags(value, false, FlagEnums.DefaultDelimiter, null);

        public TInt ParseFlags(string value, EnumFormat[] parseFormatOrder) => ParseFlags(value, false, FlagEnums.DefaultDelimiter, parseFormatOrder);

        public TInt ParseFlags(string value, bool ignoreCase) => ParseFlags(value, ignoreCase, FlagEnums.DefaultDelimiter, null);

        public TInt ParseFlags(string value, bool ignoreCase, EnumFormat[] parseFormatOrder) => ParseFlags(value, ignoreCase, FlagEnums.DefaultDelimiter, parseFormatOrder);

        public TInt ParseFlags(string value, string delimiter) => ParseFlags(value, false, delimiter, null);

        public TInt ParseFlags(string value, string delimiter, EnumFormat[] parseFormatOrder) => ParseFlags(value, false, delimiter, parseFormatOrder);

        public TInt ParseFlags(string value, bool ignoreCase, string delimiter) => ParseFlags(value, ignoreCase, delimiter, null);

        public TInt ParseFlags(string value, bool ignoreCase, string delimiter, EnumFormat[] parseFormatOrder)
        {
            Preconditions.NotNull(value, nameof(value));
            Preconditions.NotNullOrEmpty(delimiter, nameof(delimiter));

            var effectiveDelimiter = delimiter.Trim();
            if (effectiveDelimiter.Length == 0)
            {
                effectiveDelimiter = delimiter;
            }
            var split = value.Split(new[] { effectiveDelimiter }, StringSplitOptions.None);

            if (!(parseFormatOrder?.Length > 0))
            {
                parseFormatOrder = Enums.DefaultFormatOrder;
            }

            var result = Zero;
            foreach (var indValue in split)
            {
                var trimmedIndValue = indValue.Trim();
                TInt indValueAsInt;
                if (InternalTryParse(trimmedIndValue, ignoreCase, out indValueAsInt, parseFormatOrder))
                {
                    if (!IsValidFlagCombination(indValueAsInt))
                    {
                        throw new ArgumentException("All individual enum values within value must be valid");
                    }
                    result = Or(result, indValueAsInt);
                }
                else
                {
                    if (Enums.IsNumeric(indValue))
                    {
                        throw Enums.GetOverflowException();
                    }
                    throw new ArgumentException("value is not a valid combination of flag enum values");
                }
            }
            return result;
        }

        public bool TryParseFlags(string value, out TInt result) => TryParseFlags(value, false, FlagEnums.DefaultDelimiter, out result, null);

        public bool TryParseFlags(string value, out TInt result, EnumFormat[] parseFormatOrder) => TryParseFlags(value, false, FlagEnums.DefaultDelimiter, out result, parseFormatOrder);

        public bool TryParseFlags(string value, bool ignoreCase, out TInt result) => TryParseFlags(value, ignoreCase, FlagEnums.DefaultDelimiter, out result, null);

        public bool TryParseFlags(string value, bool ignoreCase, out TInt result, EnumFormat[] parseFormatOrder) => TryParseFlags(value, ignoreCase, FlagEnums.DefaultDelimiter, out result, parseFormatOrder);

        public bool TryParseFlags(string value, string delimiter, out TInt result) => TryParseFlags(value, false, delimiter, out result, null);

        public bool TryParseFlags(string value, string delimiter, out TInt result, EnumFormat[] parseFormatOrder) => TryParseFlags(value, false, delimiter, out result, parseFormatOrder);

        public bool TryParseFlags(string value, bool ignoreCase, string delimiter, out TInt result) => TryParseFlags(value, ignoreCase, delimiter, out result, null);

        public bool TryParseFlags(string value, bool ignoreCase, string delimiter, out TInt result, EnumFormat[] parseFormatOrder)
        {
            Preconditions.NotNullOrEmpty(delimiter, nameof(delimiter));

            if (value == null)
            {
                result = Zero;
                return false;
            }

            var effectiveDelimiter = delimiter.Trim();
            if (effectiveDelimiter.Length == 0)
            {
                effectiveDelimiter = delimiter;
            }
            var split = value.Split(new[] { effectiveDelimiter }, StringSplitOptions.None);

            if (!(parseFormatOrder?.Length > 0))
            {
                parseFormatOrder = Enums.DefaultFormatOrder;
            }

            var resultAsInt = Zero;
            foreach (var indValue in split)
            {
                var trimmedIndValue = indValue.Trim();
                TInt indValueAsInt;
                if (!InternalTryParse(trimmedIndValue, ignoreCase, out indValueAsInt, parseFormatOrder) || !IsValidFlagCombination(indValueAsInt))
                {
                    result = Zero;
                    return false;
                }
                resultAsInt = Or(resultAsInt, indValueAsInt);
            }
            result = resultAsInt;
            return true;
        }
        #endregion

        #region Private Methods
        private IEnumerable<TInt> InternalGetFlags(TInt value)
        {
            var greaterThan = GreaterThan;
            var leftShift = LeftShift;
            var equal = Equal;
            var zero = Zero;
            var isGreaterThanOrEqualToZero = !greaterThan(zero, value);
            for (var currentValue = One; (isGreaterThanOrEqualToZero && !greaterThan(currentValue, value)) && !equal(currentValue, zero); currentValue = leftShift(currentValue, 1))
            {
                if (IsValidFlagCombination(currentValue) && InternalHasAnyFlags(value, currentValue))
                {
                    yield return currentValue;
                }
            }
        }

        private bool InternalHasAnyFlags(TInt value, TInt flagMask)
        {
            return !Equal(And(value, flagMask), Zero);
        }

        private void ValidateIsValidFlagCombination(TInt value, string paramName)
        {
            if (!IsValidFlagCombination(value))
            {
                throw new ArgumentException("must be valid flag combination", paramName);
            }
        }
        #endregion
        #endregion

        #region Nested Types
        internal sealed class EnumParser
        {
            private readonly Dictionary<string, string> _formatNameMap;
            private Dictionary<string, string> _formatIgnoreCase;
            private EnumsCache<TInt> _cache;

            private Dictionary<string, string> FormatIgnoreCase
            {
                get
                {
                    if (_formatIgnoreCase == null)
                    {
                        var formatIgnoreCase = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var pair in _formatNameMap)
                        {
                            if (!formatIgnoreCase.ContainsKey(pair.Key))
                            {
                                formatIgnoreCase.Add(pair.Key, pair.Value);
                            }
                        }

                        // Reduces memory usage
                        _formatIgnoreCase = new Dictionary<string, string>(formatIgnoreCase, StringComparer.OrdinalIgnoreCase);
                    }
                    return _formatIgnoreCase;
                }
            }

            public EnumParser(Func<IEnumMemberInfo<TInt>, string> enumFormatter, EnumsCache<TInt> cache)
            {
                _cache = cache;
                var formatNameMap = new Dictionary<string, string>();
                foreach (var pair in _cache._valueMap)
                {
                    var format = enumFormatter(new InternalEnumMemberInfo<TInt>(pair.First, pair.Second.Name, pair.Second.Attributes));
                    if (format != null && !formatNameMap.ContainsKey(format))
                    {
                        formatNameMap.Add(format, pair.Second.Name);
                    }
                }
                if (_cache._duplicateValues != null)
                {
                    foreach (var pair in _cache._duplicateValues)
                    {
                        var format = enumFormatter(new InternalEnumMemberInfo<TInt>(pair.Value.Value, pair.Key, pair.Value.Attributes));
                        if (format != null && !formatNameMap.ContainsKey(format))
                        {
                            formatNameMap.Add(format, pair.Key);
                        }
                    }
                }

                // Reduces memory usage
                _formatNameMap = new Dictionary<string, string>(formatNameMap);
            }

            internal bool TryParse(string format, bool ignoreCase, out TInt result)
            {
                string name;
                if (_formatNameMap.TryGetValue(format, out name) || (ignoreCase && FormatIgnoreCase.TryGetValue(format, out name)))
                {
                    if (!_cache._valueMap.TryGetFirst(new NameAndAttributes(name), out result))
                    {
                        result = _cache._duplicateValues[name].Value;
                    }
                    return true;
                }
                result = Zero;
                return false;
            }
        }
        #endregion
    }

    internal static class EnumsCache
    {
        internal static void Populate(TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Int32:
                    EnumsCache<int>.Equal = (x, y) => x == y;
                    EnumsCache<int>.GreaterThan = (x, y) => x > y;
                    EnumsCache<int>.And = (x, y) => x & y;
                    EnumsCache<int>.Or = (x, y) => x | y;
                    EnumsCache<int>.Xor = (x, y) => x ^ y;
                    EnumsCache<int>.LeftShift = (x, n) => x << n;
                    //IntegralOperators<int>.RightShift = (x, n) => x >> n;
                    EnumsCache<int>.Add = (x, y) => x + y;
                    EnumsCache<int>.Subtract = (x, y) => x - y;
                    EnumsCache<int>.FromInt64 = x => (int)x;
                    EnumsCache<int>.FromUInt64 = x => (int)x;
                    EnumsCache<int>.Int64IsInValueRange = x => x >= int.MinValue && x <= int.MaxValue;
                    EnumsCache<int>.UInt64IsInValueRange = x => x <= int.MaxValue;
                    EnumsCache<int>.ToSByte = Convert.ToSByte;
                    EnumsCache<int>.ToByte = Convert.ToByte;
                    EnumsCache<int>.ToInt16 = Convert.ToInt16;
                    EnumsCache<int>.ToUInt16 = Convert.ToUInt16;
                    EnumsCache<int>.ToInt32 = Convert.ToInt32;
                    EnumsCache<int>.ToUInt32 = Convert.ToUInt32;
                    EnumsCache<int>.ToInt64 = Convert.ToInt64;
                    EnumsCache<int>.ToUInt64 = Convert.ToUInt64;
                    EnumsCache<int>.ToStringFormat = (x, format) => x.ToString(format);
                    EnumsCache<int>.TryParseMethod = int.TryParse;
                    EnumsCache<int>.HexFormatString = "X8";
                    EnumsCache<int>.Zero = 0;
                    EnumsCache<int>.One = 1;
                    break;
                case TypeCode.UInt32:
                    EnumsCache<uint>.Equal = (x, y) => x == y;
                    EnumsCache<uint>.GreaterThan = (x, y) => x > y;
                    EnumsCache<uint>.And = (x, y) => x & y;
                    EnumsCache<uint>.Or = (x, y) => x | y;
                    EnumsCache<uint>.Xor = (x, y) => x ^ y;
                    EnumsCache<uint>.LeftShift = (x, n) => x << n;
                    //IntegralOperators<uint>.RightShift = (x, n) => x >> n;
                    EnumsCache<uint>.Add = (x, y) => x + y;
                    EnumsCache<uint>.Subtract = (x, y) => x - y;
                    EnumsCache<uint>.FromInt64 = x => (uint)x;
                    EnumsCache<uint>.FromUInt64 = x => (uint)x;
                    EnumsCache<uint>.Int64IsInValueRange = x => x >= uint.MinValue && x <= uint.MaxValue;
                    EnumsCache<uint>.UInt64IsInValueRange = x => x <= uint.MaxValue;
                    EnumsCache<uint>.ToSByte = Convert.ToSByte;
                    EnumsCache<uint>.ToByte = Convert.ToByte;
                    EnumsCache<uint>.ToInt16 = Convert.ToInt16;
                    EnumsCache<uint>.ToUInt16 = Convert.ToUInt16;
                    EnumsCache<uint>.ToInt32 = Convert.ToInt32;
                    EnumsCache<uint>.ToUInt32 = Convert.ToUInt32;
                    EnumsCache<uint>.ToInt64 = Convert.ToInt64;
                    EnumsCache<uint>.ToUInt64 = Convert.ToUInt64;
                    EnumsCache<uint>.ToStringFormat = (x, format) => x.ToString(format);
                    EnumsCache<uint>.TryParseMethod = uint.TryParse;
                    EnumsCache<uint>.HexFormatString = "X8";
                    EnumsCache<uint>.Zero = 0U;
                    EnumsCache<uint>.One = 1U;
                    break;
                case TypeCode.Int64:
                    EnumsCache<long>.Equal = (x, y) => x == y;
                    EnumsCache<long>.GreaterThan = (x, y) => x > y;
                    EnumsCache<long>.And = (x, y) => x & y;
                    EnumsCache<long>.Or = (x, y) => x | y;
                    EnumsCache<long>.Xor = (x, y) => x ^ y;
                    EnumsCache<long>.LeftShift = (x, n) => x << n;
                    //IntegralOperators<long>.RightShift = (x, n) => x >> n;
                    EnumsCache<long>.Add = (x, y) => x + y;
                    EnumsCache<long>.Subtract = (x, y) => x - y;
                    EnumsCache<long>.FromInt64 = x => x;
                    EnumsCache<long>.FromUInt64 = x => (long)x;
                    EnumsCache<long>.Int64IsInValueRange = x => true;
                    EnumsCache<long>.UInt64IsInValueRange = x => x <= long.MaxValue;
                    EnumsCache<long>.ToSByte = Convert.ToSByte;
                    EnumsCache<long>.ToByte = Convert.ToByte;
                    EnumsCache<long>.ToInt16 = Convert.ToInt16;
                    EnumsCache<long>.ToUInt16 = Convert.ToUInt16;
                    EnumsCache<long>.ToInt32 = Convert.ToInt32;
                    EnumsCache<long>.ToUInt32 = Convert.ToUInt32;
                    EnumsCache<long>.ToInt64 = Convert.ToInt64;
                    EnumsCache<long>.ToUInt64 = Convert.ToUInt64;
                    EnumsCache<long>.ToStringFormat = (x, format) => x.ToString(format);
                    EnumsCache<long>.TryParseMethod = long.TryParse;
                    EnumsCache<long>.HexFormatString = "X16";
                    EnumsCache<long>.Zero = 0L;
                    EnumsCache<long>.One = 1L;
                    break;
                case TypeCode.UInt64:
                    EnumsCache<ulong>.Equal = (x, y) => x == y;
                    EnumsCache<ulong>.GreaterThan = (x, y) => x > y;
                    EnumsCache<ulong>.And = (x, y) => x & y;
                    EnumsCache<ulong>.Or = (x, y) => x | y;
                    EnumsCache<ulong>.Xor = (x, y) => x ^ y;
                    EnumsCache<ulong>.LeftShift = (x, n) => x << n;
                    //IntegralOperators<ulong>.RightShift = (x, n) => x >> n;
                    EnumsCache<ulong>.Add = (x, y) => x + y;
                    EnumsCache<ulong>.Subtract = (x, y) => x - y;
                    EnumsCache<ulong>.FromInt64 = x => (ulong)x;
                    EnumsCache<ulong>.FromUInt64 = x => x;
                    EnumsCache<ulong>.Int64IsInValueRange = x => x >= 0L;
                    EnumsCache<ulong>.UInt64IsInValueRange = x => true;
                    EnumsCache<ulong>.ToSByte = Convert.ToSByte;
                    EnumsCache<ulong>.ToByte = Convert.ToByte;
                    EnumsCache<ulong>.ToInt16 = Convert.ToInt16;
                    EnumsCache<ulong>.ToUInt16 = Convert.ToUInt16;
                    EnumsCache<ulong>.ToInt32 = Convert.ToInt32;
                    EnumsCache<ulong>.ToUInt32 = Convert.ToUInt32;
                    EnumsCache<ulong>.ToInt64 = Convert.ToInt64;
                    EnumsCache<ulong>.ToUInt64 = Convert.ToUInt64;
                    EnumsCache<ulong>.ToStringFormat = (x, format) => x.ToString(format);
                    EnumsCache<ulong>.TryParseMethod = ulong.TryParse;
                    EnumsCache<ulong>.HexFormatString = "X16";
                    EnumsCache<ulong>.Zero = 0UL;
                    EnumsCache<ulong>.One = 1UL;
                    break;
                case TypeCode.SByte:
                    EnumsCache<sbyte>.Equal = (x, y) => x == y;
                    EnumsCache<sbyte>.GreaterThan = (x, y) => x > y;
                    EnumsCache<sbyte>.And = (x, y) => (sbyte)(x & y);
                    EnumsCache<sbyte>.Or = (x, y) => (sbyte)(x | y);
                    EnumsCache<sbyte>.Xor = (x, y) => (sbyte)(x ^ y);
                    EnumsCache<sbyte>.LeftShift = (x, n) => (sbyte)(x << n);
                    //IntegralOperators<sbyte>.RightShift = (x, n) => (sbyte)(x >> n);
                    EnumsCache<sbyte>.Add = (x, y) => (sbyte)(x + y);
                    EnumsCache<sbyte>.Subtract = (x, y) => (sbyte)(x - y);
                    EnumsCache<sbyte>.FromInt64 = x => (sbyte)x;
                    EnumsCache<sbyte>.FromUInt64 = x => (sbyte)x;
                    EnumsCache<sbyte>.Int64IsInValueRange = x => x >= sbyte.MinValue && x <= sbyte.MaxValue;
                    EnumsCache<sbyte>.UInt64IsInValueRange = x => x <= (ulong)sbyte.MaxValue;
                    EnumsCache<sbyte>.ToSByte = Convert.ToSByte;
                    EnumsCache<sbyte>.ToByte = Convert.ToByte;
                    EnumsCache<sbyte>.ToInt16 = Convert.ToInt16;
                    EnumsCache<sbyte>.ToUInt16 = Convert.ToUInt16;
                    EnumsCache<sbyte>.ToInt32 = Convert.ToInt32;
                    EnumsCache<sbyte>.ToUInt32 = Convert.ToUInt32;
                    EnumsCache<sbyte>.ToInt64 = Convert.ToInt64;
                    EnumsCache<sbyte>.ToUInt64 = Convert.ToUInt64;
                    EnumsCache<sbyte>.ToStringFormat = (x, format) => x.ToString(format);
                    EnumsCache<sbyte>.TryParseMethod = sbyte.TryParse;
                    EnumsCache<sbyte>.HexFormatString = "X2";
                    EnumsCache<sbyte>.Zero = 0;
                    EnumsCache<sbyte>.One = 1;
                    break;
                case TypeCode.Byte:
                    EnumsCache<byte>.Equal = (x, y) => x == y;
                    EnumsCache<byte>.GreaterThan = (x, y) => x > y;
                    EnumsCache<byte>.And = (x, y) => (byte)(x & y);
                    EnumsCache<byte>.Or = (x, y) => (byte)(x | y);
                    EnumsCache<byte>.Xor = (x, y) => (byte)(x ^ y);
                    EnumsCache<byte>.LeftShift = (x, n) => (byte)(x << n);
                    //IntegralOperators<byte>.RightShift = (x, n) => (byte)(x >> n);
                    EnumsCache<byte>.Add = (x, y) => (byte)(x + y);
                    EnumsCache<byte>.Subtract = (x, y) => (byte)(x - y);
                    EnumsCache<byte>.FromInt64 = x => (byte)x;
                    EnumsCache<byte>.FromUInt64 = x => (byte)x;
                    EnumsCache<byte>.Int64IsInValueRange = x => x >= byte.MinValue && x <= byte.MaxValue;
                    EnumsCache<byte>.UInt64IsInValueRange = x => x <= byte.MaxValue;
                    EnumsCache<byte>.ToSByte = Convert.ToSByte;
                    EnumsCache<byte>.ToByte = Convert.ToByte;
                    EnumsCache<byte>.ToInt16 = Convert.ToInt16;
                    EnumsCache<byte>.ToUInt16 = Convert.ToUInt16;
                    EnumsCache<byte>.ToInt32 = Convert.ToInt32;
                    EnumsCache<byte>.ToUInt32 = Convert.ToUInt32;
                    EnumsCache<byte>.ToInt64 = Convert.ToInt64;
                    EnumsCache<byte>.ToUInt64 = Convert.ToUInt64;
                    EnumsCache<byte>.ToStringFormat = (x, format) => x.ToString(format);
                    EnumsCache<byte>.TryParseMethod = byte.TryParse;
                    EnumsCache<byte>.HexFormatString = "X2";
                    EnumsCache<byte>.Zero = 0;
                    EnumsCache<byte>.One = 1;
                    break;
                case TypeCode.Int16:
                    EnumsCache<short>.Equal = (x, y) => x == y;
                    EnumsCache<short>.GreaterThan = (x, y) => x > y;
                    EnumsCache<short>.And = (x, y) => (short)(x & y);
                    EnumsCache<short>.Or = (x, y) => (short)(x | y);
                    EnumsCache<short>.Xor = (x, y) => (short)(x ^ y);
                    EnumsCache<short>.LeftShift = (x, n) => (short)(x << n);
                    //IntegralOperators<short>.RightShift = (x, n) => (short)(x >> n);
                    EnumsCache<short>.Add = (x, y) => (short)(x + y);
                    EnumsCache<short>.Subtract = (x, y) => (short)(x - y);
                    EnumsCache<short>.FromInt64 = x => (short)x;
                    EnumsCache<short>.FromUInt64 = x => (short)x;
                    EnumsCache<short>.Int64IsInValueRange = x => x >= short.MinValue && x <= short.MaxValue;
                    EnumsCache<short>.UInt64IsInValueRange = x => x <= (ulong)short.MaxValue;
                    EnumsCache<short>.ToSByte = Convert.ToSByte;
                    EnumsCache<short>.ToByte = Convert.ToByte;
                    EnumsCache<short>.ToInt16 = Convert.ToInt16;
                    EnumsCache<short>.ToUInt16 = Convert.ToUInt16;
                    EnumsCache<short>.ToInt32 = Convert.ToInt32;
                    EnumsCache<short>.ToUInt32 = Convert.ToUInt32;
                    EnumsCache<short>.ToInt64 = Convert.ToInt64;
                    EnumsCache<short>.ToUInt64 = Convert.ToUInt64;
                    EnumsCache<short>.ToStringFormat = (x, format) => x.ToString(format);
                    EnumsCache<short>.TryParseMethod = short.TryParse;
                    EnumsCache<short>.HexFormatString = "X4";
                    EnumsCache<short>.Zero = 0;
                    EnumsCache<short>.One = 1;
                    break;
                case TypeCode.UInt16:
                    EnumsCache<ushort>.Equal = (x, y) => x == y;
                    EnumsCache<ushort>.GreaterThan = (x, y) => x > y;
                    EnumsCache<ushort>.And = (x, y) => (ushort)(x & y);
                    EnumsCache<ushort>.Or = (x, y) => (ushort)(x | y);
                    EnumsCache<ushort>.Xor = (x, y) => (ushort)(x ^ y);
                    EnumsCache<ushort>.LeftShift = (x, n) => (ushort)(x << n);
                    //IntegralOperators<ushort>.RightShift = (x, n) => (ushort)(x >> n);
                    EnumsCache<ushort>.Add = (x, y) => (ushort)(x + y);
                    EnumsCache<ushort>.Subtract = (x, y) => (ushort)(x - y);
                    EnumsCache<ushort>.FromInt64 = x => (ushort)x;
                    EnumsCache<ushort>.FromUInt64 = x => (ushort)x;
                    EnumsCache<ushort>.Int64IsInValueRange = x => x >= ushort.MinValue && x <= ushort.MaxValue;
                    EnumsCache<ushort>.UInt64IsInValueRange = x => x <= ushort.MaxValue;
                    EnumsCache<ushort>.ToSByte = Convert.ToSByte;
                    EnumsCache<ushort>.ToByte = Convert.ToByte;
                    EnumsCache<ushort>.ToInt16 = Convert.ToInt16;
                    EnumsCache<ushort>.ToUInt16 = Convert.ToUInt16;
                    EnumsCache<ushort>.ToInt32 = Convert.ToInt32;
                    EnumsCache<ushort>.ToUInt32 = Convert.ToUInt32;
                    EnumsCache<ushort>.ToInt64 = Convert.ToInt64;
                    EnumsCache<ushort>.ToUInt64 = Convert.ToUInt64;
                    EnumsCache<ushort>.ToStringFormat = (x, format) => x.ToString(format);
                    EnumsCache<ushort>.TryParseMethod = ushort.TryParse;
                    EnumsCache<ushort>.HexFormatString = "X4";
                    EnumsCache<ushort>.Zero = 0;
                    EnumsCache<ushort>.One = 1;
                    break;
            }
        }
    }
}