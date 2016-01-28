﻿// Enums.NET
// Copyright 2016 Tyler Brinkley. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//	  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#define USE_EMIT

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using ExtraConstraints;
using System.Linq;

#if NET20 || USE_EMIT
using System.Reflection.Emit;
#else
using System.Linq.Expressions;
#endif

namespace EnumsNET
{
    /// <summary>
    /// Static class that provides efficient type-safe enum operations through the use of cached enum names, values, and attributes.
    /// Many operations are exposed as extension methods for convenience.
    /// </summary>
    public static class Enums
    {
        internal const int StartingCustomEnumFormatValue = 100;

        internal const int StartingGenericCustomEnumFormatValue = 200;

        private static int _lastCustomEnumFormatIndex = -1;

        internal static List<Func<IEnumMemberInfo, string>> CustomEnumFormatters;

        internal static readonly EnumFormat[] DefaultFormatOrder = { EnumFormat.Name, EnumFormat.DecimalValue };

        internal static readonly Attribute[] ZeroLengthAttributes = { };

        public static EnumFormat RegisterCustomEnumFormat(Func<IClsEnumMemberInfo, string> formatter) => RegisterCustomEnumFormat((Func<IEnumMemberInfo, string>)formatter);

        [CLSCompliant(false)]
        public static EnumFormat RegisterCustomEnumFormat(Func<IEnumMemberInfo, string> formatter)
        {
            Preconditions.NotNull(formatter, nameof(formatter));

            var index = Interlocked.Increment(ref _lastCustomEnumFormatIndex);
            if (index == 0)
            {
                CustomEnumFormatters = new List<Func<IEnumMemberInfo, string>>();
            }
            else
            {
                while (CustomEnumFormatters == null || CustomEnumFormatters.Count < index)
                {
                }
            }
            CustomEnumFormatters.Add(formatter);
            return ToObject<EnumFormat>(index + StartingCustomEnumFormatValue, false);
        }

        #region "Properties"
        /// <summary>
        /// Indicates if <typeparamref name="TEnum"/>'s defined values are contiguous.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>Indication if <typeparamref name="TEnum"/>'s defined values are contiguous.</returns>
        [Pure]
        public static bool IsContiguous<[EnumConstraint] TEnum>()
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsContiguous;
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsContiguous;
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsContiguous;
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsContiguous;
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsContiguous;
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsContiguous;
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsContiguous;
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsContiguous;
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Retrieves the underlying type of <typeparamref name="TEnum"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>The underlying type of <typeparamref name="TEnum"/>.</returns>
        [Pure]
        public static Type GetUnderlyingType<[EnumConstraint] TEnum>()
            where TEnum : struct
        {
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return typeof(int);
                case TypeCode.UInt32:
                    return typeof(uint);
                case TypeCode.Int64:
                    return typeof(long);
                case TypeCode.UInt64:
                    return typeof(ulong);
                case TypeCode.SByte:
                    return typeof(sbyte);
                case TypeCode.Byte:
                    return typeof(byte);
                case TypeCode.Int16:
                    return typeof(short);
                case TypeCode.UInt16:
                    return typeof(ushort);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        [Pure]
        public static TypeCode GetTypeCode<[EnumConstraint] TEnum>() where TEnum : struct => Enums<TEnum>.TypeCode;
        #endregion

        #region Type Methods
        /// <summary>
        /// Retrieves <typeparamref name="TEnum"/>'s members count.
        /// The optional parameter <paramref name="uniqueValued"/> indicates whether to exclude duplicate values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="uniqueValued"></param>
        /// <returns><typeparamref name="TEnum"/>'s members count.</returns>
        [Pure]
        public static int GetDefinedCount<[EnumConstraint] TEnum>(bool uniqueValued = false)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).GetDefinedCount(uniqueValued);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).GetDefinedCount(uniqueValued);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).GetDefinedCount(uniqueValued);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).GetDefinedCount(uniqueValued);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).GetDefinedCount(uniqueValued);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).GetDefinedCount(uniqueValued);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).GetDefinedCount(uniqueValued);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).GetDefinedCount(uniqueValued);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return 0;
        }

        /// <summary>
        /// Retrieves in value order an array of info on <typeparamref name="TEnum"/>'s members.
        /// The optional parameter <paramref name="uniqueValued"/> indicates whether to exclude duplicate values.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="uniqueValued"></param>
        /// <returns></returns>
        [Pure]
        public static IEnumerable<EnumMemberInfo<TEnum>> GetEnumMemberInfos<[EnumConstraint] TEnum>(bool uniqueValued = false) where TEnum : struct => Enums<TEnum>.Cache.GetEnumMemberInfos(uniqueValued);

        /// <summary>
        /// Retrieves in value order an array of <typeparamref name="TEnum"/>'s members' names.
        /// The optional parameter <paramref name="uniqueValued"/> indicates whether to exclude duplicate values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="uniqueValued"></param>
        /// <returns>Array of <typeparamref name="TEnum"/>'s members' names in value order.</returns>
        [Pure]
        public static IEnumerable<string> GetNames<[EnumConstraint] TEnum>(bool uniqueValued = false)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).GetNames(uniqueValued);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).GetNames(uniqueValued);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).GetNames(uniqueValued);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).GetNames(uniqueValued);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).GetNames(uniqueValued);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).GetNames(uniqueValued);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).GetNames(uniqueValued);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).GetNames(uniqueValued);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Retrieves in value order an array of <typeparamref name="TEnum"/>'s members' values.
        /// The optional parameter <paramref name="uniqueValued"/> indicates whether to exclude duplicate values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="uniqueValued"></param>
        /// <returns>Array of <typeparamref name="TEnum"/>'s members' values in value order.</returns>
        [Pure]
        public static IEnumerable<TEnum> GetValues<[EnumConstraint] TEnum>(bool uniqueValued = false)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    var int32ToEnum = (Func<int, TEnum>)toEnum;
                    return ((EnumsCache<int>)cache).GetValues(uniqueValued).Select(value => int32ToEnum(value));
                case TypeCode.UInt32:
                    var uint32ToEnum = (Func<uint, TEnum>)toEnum;
                    return ((EnumsCache<uint>)cache).GetValues(uniqueValued).Select(value => uint32ToEnum(value));
                case TypeCode.Int64:
                    var int64ToEnum = (Func<long, TEnum>)toEnum;
                    return ((EnumsCache<long>)cache).GetValues(uniqueValued).Select(value => int64ToEnum(value));
                case TypeCode.UInt64:
                    var uint64ToEnum = (Func<ulong, TEnum>)toEnum;
                    return ((EnumsCache<ulong>)cache).GetValues(uniqueValued).Select(value => uint64ToEnum(value));
                case TypeCode.SByte:
                    var sbyteToEnum = (Func<sbyte, TEnum>)toEnum;
                    return ((EnumsCache<sbyte>)cache).GetValues(uniqueValued).Select(value => sbyteToEnum(value));
                case TypeCode.Byte:
                    var byteToEnum = (Func<byte, TEnum>)toEnum;
                    return ((EnumsCache<byte>)cache).GetValues(uniqueValued).Select(value => byteToEnum(value));
                case TypeCode.Int16:
                    var int16ToEnum = (Func<short, TEnum>)toEnum;
                    return ((EnumsCache<short>)cache).GetValues(uniqueValued).Select(value => int16ToEnum(value));
                case TypeCode.UInt16:
                    var uint16ToEnum = (Func<ushort, TEnum>)toEnum;
                    return ((EnumsCache<ushort>)cache).GetValues(uniqueValued).Select(value => uint16ToEnum(value));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Retrieves in value order an array of <typeparamref name="TEnum"/>'s members' <see cref="DescriptionAttribute.Description"/>s.
        /// The optional parameter <paramref name="uniqueValued"/> indicates whether to exclude duplicate values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="uniqueValued"></param>
        /// <returns>Array of <typeparamref name="TEnum"/>'s members' <see cref="DescriptionAttribute.Description"/>s in value order.</returns>
        [Pure]
        public static IEnumerable<string> GetDescriptions<[EnumConstraint] TEnum>(bool uniqueValued = false)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).GetDescriptions(uniqueValued);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).GetDescriptions(uniqueValued);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).GetDescriptions(uniqueValued);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).GetDescriptions(uniqueValued);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).GetDescriptions(uniqueValued);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).GetDescriptions(uniqueValued);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).GetDescriptions(uniqueValued);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).GetDescriptions(uniqueValued);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Retrieves in value order an array of <typeparamref name="TEnum"/>'s members' descriptions else names.
        /// The optional parameter <paramref name="uniqueValued"/> indicates whether to exclude duplicate values.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="uniqueValued"></param>
        /// <returns></returns>
        [Pure]
        public static IEnumerable<string> GetDescriptionsOrNames<[EnumConstraint] TEnum>(bool uniqueValued = false)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).GetDescriptionsOrNames(uniqueValued);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).GetDescriptionsOrNames(uniqueValued);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).GetDescriptionsOrNames(uniqueValued);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).GetDescriptionsOrNames(uniqueValued);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).GetDescriptionsOrNames(uniqueValued);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).GetDescriptionsOrNames(uniqueValued);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).GetDescriptionsOrNames(uniqueValued);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).GetDescriptionsOrNames(uniqueValued);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Retrieves in value order an array of <typeparamref name="TEnum"/>'s members' descriptions else names formatted
        /// with <paramref name="nameFormatter"/>.
        /// The optional parameter <paramref name="uniqueValued"/> indicates whether to exclude duplicate values.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="nameFormatter"></param>
        /// <param name="uniqueValued"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetDescriptionsOrNames<[EnumConstraint] TEnum>(Func<string, string> nameFormatter, bool uniqueValued = false)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).GetDescriptionsOrNames(nameFormatter, uniqueValued);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).GetDescriptionsOrNames(nameFormatter, uniqueValued);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).GetDescriptionsOrNames(nameFormatter, uniqueValued);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).GetDescriptionsOrNames(nameFormatter, uniqueValued);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).GetDescriptionsOrNames(nameFormatter, uniqueValued);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).GetDescriptionsOrNames(nameFormatter, uniqueValued);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).GetDescriptionsOrNames(nameFormatter, uniqueValued);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).GetDescriptionsOrNames(nameFormatter, uniqueValued);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Retrieves in value order an array of all of <typeparamref name="TEnum"/>'s members' attributes.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="uniqueValued"></param>
        /// <returns>Array of all of <typeparamref name="TEnum"/>'s members' attributes in value order.</returns>
        [Pure]
        public static IEnumerable<Attribute[]> GetAllAttributes<[EnumConstraint] TEnum>(bool uniqueValued = false)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).GetAllAttributes(uniqueValued);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).GetAllAttributes(uniqueValued);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).GetAllAttributes(uniqueValued);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).GetAllAttributes(uniqueValued);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).GetAllAttributes(uniqueValued);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).GetAllAttributes(uniqueValued);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).GetAllAttributes(uniqueValued);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).GetAllAttributes(uniqueValued);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Retrieves in value order an array of <typeparamref name="TEnum"/>'s members' <typeparamref name="TAttribute"/>s.
        /// The optional parameter <paramref name="uniqueValued"/> indicates whether to exclude duplicate values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="uniqueValued"></param>
        /// <returns>Array of <typeparamref name="TEnum"/>'s members' <typeparamref name="TAttribute"/> in value order.</returns>
        [Pure]
        public static IEnumerable<TAttribute> GetAttributes<[EnumConstraint] TEnum, TAttribute>(bool uniqueValued = false)
            where TAttribute : Attribute
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).GetAttributes<TAttribute>(uniqueValued);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).GetAttributes<TAttribute>(uniqueValued);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).GetAttributes<TAttribute>(uniqueValued);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).GetAttributes<TAttribute>(uniqueValued);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).GetAttributes<TAttribute>(uniqueValued);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).GetAttributes<TAttribute>(uniqueValued);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).GetAttributes<TAttribute>(uniqueValued);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).GetAttributes<TAttribute>(uniqueValued);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Compares two <typeparamref name="TEnum"/>'s for ordering.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>1 if <paramref name="x"/> is greater than <paramref name="y"/>, 0 if <paramref name="x"/> equals <paramref name="y"/>,
        /// and -1 if <paramref name="x"/> is less than <paramref name="y"/>.</returns>
        [Pure]
        public static int Compare<[EnumConstraint] TEnum>(TEnum x, TEnum y)
            where TEnum : struct
        {
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    var enumToInt32 = (Func<TEnum, int>)toInt;
                    return EnumsCache<int>.Compare(enumToInt32(x), enumToInt32(y));
                case TypeCode.UInt32:
                    var enumToUInt32 = (Func<TEnum, uint>)toInt;
                    return EnumsCache<uint>.Compare(enumToUInt32(x), enumToUInt32(y));
                case TypeCode.Int64:
                    var enumToInt64 = (Func<TEnum, long>)toInt;
                    return EnumsCache<long>.Compare(enumToInt64(x), enumToInt64(y));
                case TypeCode.UInt64:
                    var enumToUInt64 = (Func<TEnum, ulong>)toInt;
                    return EnumsCache<ulong>.Compare(enumToUInt64(x), enumToUInt64(y));
                case TypeCode.SByte:
                    var enumToSByte = (Func<TEnum, sbyte>)toInt;
                    return EnumsCache<sbyte>.Compare(enumToSByte(x), enumToSByte(y));
                case TypeCode.Byte:
                    var enumToByte = (Func<TEnum, byte>)toInt;
                    return EnumsCache<byte>.Compare(enumToByte(x), enumToByte(y));
                case TypeCode.Int16:
                    var enumToInt16 = (Func<TEnum, short>)toInt;
                    return EnumsCache<short>.Compare(enumToInt16(x), enumToInt16(y));
                case TypeCode.UInt16:
                    var enumToUInt16 = (Func<TEnum, ushort>)toInt;
                    return EnumsCache<ushort>.Compare(enumToUInt16(x), enumToUInt16(y));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return 0;
        }

        [Pure]
        public static bool Equals<[EnumConstraint] TEnum>(TEnum x, TEnum y)
            where TEnum : struct
        {
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    var enumToInt32 = (Func<TEnum, int>)toInt;
                    return EnumsCache<int>.Equal(enumToInt32(x), enumToInt32(y));
                case TypeCode.UInt32:
                    var enumToUInt32 = (Func<TEnum, uint>)toInt;
                    return EnumsCache<uint>.Equal(enumToUInt32(x), enumToUInt32(y));
                case TypeCode.Int64:
                    var enumToInt64 = (Func<TEnum, long>)toInt;
                    return EnumsCache<long>.Equal(enumToInt64(x), enumToInt64(y));
                case TypeCode.UInt64:
                    var enumToUInt64 = (Func<TEnum, ulong>)toInt;
                    return EnumsCache<ulong>.Equal(enumToUInt64(x), enumToUInt64(y));
                case TypeCode.SByte:
                    var enumToSByte = (Func<TEnum, sbyte>)toInt;
                    return EnumsCache<sbyte>.Equal(enumToSByte(x), enumToSByte(y));
                case TypeCode.Byte:
                    var enumToByte = (Func<TEnum, byte>)toInt;
                    return EnumsCache<byte>.Equal(enumToByte(x), enumToByte(y));
                case TypeCode.Int16:
                    var enumToInt16 = (Func<TEnum, short>)toInt;
                    return EnumsCache<short>.Equal(enumToInt16(x), enumToInt16(y));
                case TypeCode.UInt16:
                    var enumToUInt16 = (Func<TEnum, ushort>)toInt;
                    return EnumsCache<ushort>.Equal(enumToUInt16(x), enumToUInt16(y));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        public static EnumFormat RegisterCustomEnumFormat<[EnumConstraint] TEnum>(Func<IClsEnumMemberInfo<TEnum>, string> formatter)
            where TEnum : struct
        {
            return Enums<TEnum>.Cache.RegisterCustomEnumFormat(formatter);
        }

        [CLSCompliant(false)]
        public static EnumFormat RegisterCustomEnumFormat<[EnumConstraint] TEnum>(Func<IEnumMemberInfo<TEnum>, string> formatter)
            where TEnum : struct
        {
            return Enums<TEnum>.Cache.RegisterCustomEnumFormat(formatter);
        }
        #endregion

        #region IsValid
        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">must be <see cref="sbyte"/>, <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>,
        /// <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, or <see cref="string"/></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.</returns>
        [Pure]
        public static bool IsValid<[EnumConstraint] TEnum>(object value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsValid(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsValid(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsValid(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsValid(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsValid(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsValid(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsValid(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsValid(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">The enum value.</param>
        /// <returns>Indication whether <paramref name="value"/> is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.</returns>
        [Pure]
        public static bool IsValid<[EnumConstraint] TEnum>(this TEnum value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsValid(((Func<TEnum, int>)toInt)(value));
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsValid(((Func<TEnum, uint>)toInt)(value));
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsValid(((Func<TEnum, long>)toInt)(value));
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsValid(((Func<TEnum, ulong>)toInt)(value));
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsValid(((Func<TEnum, sbyte>)toInt)(value));
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsValid(((Func<TEnum, byte>)toInt)(value));
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsValid(((Func<TEnum, short>)toInt)(value));
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsValid(((Func<TEnum, ushort>)toInt)(value));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.</returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool IsValid<[EnumConstraint] TEnum>(sbyte value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsValid(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsValid(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsValid(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsValid(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsValid(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsValid(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsValid(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsValid(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.</returns>
        [Pure]
        public static bool IsValid<[EnumConstraint] TEnum>(byte value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsValid(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsValid(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsValid(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsValid(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsValid(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsValid(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsValid(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsValid(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.</returns>
        [Pure]
        public static bool IsValid<[EnumConstraint] TEnum>(short value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsValid(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsValid(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsValid(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsValid(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsValid(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsValid(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsValid(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsValid(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.</returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool IsValid<[EnumConstraint] TEnum>(ushort value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsValid(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsValid(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsValid(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsValid(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsValid(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsValid(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsValid(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsValid(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.</returns>
        [Pure]
        public static bool IsValid<[EnumConstraint] TEnum>(int value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsValid(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsValid(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsValid(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsValid(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsValid(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsValid(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsValid(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsValid(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.</returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool IsValid<[EnumConstraint] TEnum>(uint value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsValid(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsValid(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsValid(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsValid(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsValid(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsValid(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsValid(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsValid(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.</returns>
        [Pure]
        public static bool IsValid<[EnumConstraint] TEnum>(long value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsValid(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsValid(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsValid(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsValid(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsValid(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsValid(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsValid(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsValid(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined or if <typeparamref name="TEnum"/> is marked with <see cref="FlagsAttribute"/>
        /// whether it's a valid flag combination of <typeparamref name="TEnum"/>'s defined values.</returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool IsValid<[EnumConstraint] TEnum>(ulong value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsValid(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsValid(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsValid(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsValid(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsValid(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsValid(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsValid(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsValid(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }
        #endregion

        #region IsDefined
        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to <typeparamref name="TEnum"/> and is defined.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">must be <see cref="sbyte"/>, <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>,
        /// <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, or <see cref="string"/></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to <typeparamref name="TEnum"/> and is defined.</returns>
        [Pure]
        public static bool IsDefined<[EnumConstraint] TEnum>(object value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsDefined(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsDefined(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsDefined(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsDefined(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsDefined(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsDefined(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsDefined(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsDefined(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> is defined.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">The enum value.</param>
        /// <returns>Indication whether <paramref name="value"/> is defined.</returns>
        [Pure]
        public static bool IsDefined<[EnumConstraint] TEnum>(this TEnum value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsDefined(((Func<TEnum, int>)toInt)(value));
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsDefined(((Func<TEnum, uint>)toInt)(value));
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsDefined(((Func<TEnum, long>)toInt)(value));
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsDefined(((Func<TEnum, ulong>)toInt)(value));
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsDefined(((Func<TEnum, sbyte>)toInt)(value));
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsDefined(((Func<TEnum, byte>)toInt)(value));
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsDefined(((Func<TEnum, short>)toInt)(value));
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsDefined(((Func<TEnum, ushort>)toInt)(value));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether a constant with the specified <paramref name="name"/> exists in <typeparamref name="TEnum"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="name">The name to check existence.</param>
        /// <returns>Indication whether a constant with the specified <paramref name="name"/> exists in <typeparamref name="TEnum"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is null</exception>
        [Pure]
        public static bool IsDefined<[EnumConstraint] TEnum>(string name)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsDefined(name);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsDefined(name);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsDefined(name);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsDefined(name);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsDefined(name);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsDefined(name);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsDefined(name);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsDefined(name);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether a constant with the specified <paramref name="name"/> exists in <typeparamref name="TEnum"/>.
        /// <paramref name="ignoreCase"/> specifies whether the operation is case-insensitive.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="name"></param>
        /// <param name="ignoreCase">Specifies whether the operation is case-insensitive.</param>
        /// <returns>Indication whether a constant with the specified <paramref name="name"/> exists in <typeparamref name="TEnum"/>.
        /// <paramref name="ignoreCase"/> specifies whether the operation is case-insensitive.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is null</exception>
        [Pure]
        public static bool IsDefined<[EnumConstraint] TEnum>(string name, bool ignoreCase)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsDefined(name, ignoreCase);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsDefined(name, ignoreCase);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsDefined(name, ignoreCase);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsDefined(name, ignoreCase);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsDefined(name, ignoreCase);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsDefined(name, ignoreCase);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsDefined(name, ignoreCase);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsDefined(name, ignoreCase);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.</returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool IsDefined<[EnumConstraint] TEnum>(sbyte value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsDefined(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsDefined(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsDefined(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsDefined(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsDefined(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsDefined(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsDefined(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsDefined(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.</returns>
        [Pure]
        public static bool IsDefined<[EnumConstraint] TEnum>(byte value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsDefined(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsDefined(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsDefined(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsDefined(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsDefined(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsDefined(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsDefined(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsDefined(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.</returns>
        [Pure]
        public static bool IsDefined<[EnumConstraint] TEnum>(short value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsDefined(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsDefined(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsDefined(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsDefined(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsDefined(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsDefined(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsDefined(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsDefined(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.</returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool IsDefined<[EnumConstraint] TEnum>(ushort value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsDefined(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsDefined(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsDefined(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsDefined(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsDefined(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsDefined(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsDefined(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsDefined(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.</returns>
        [Pure]
        public static bool IsDefined<[EnumConstraint] TEnum>(int value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsDefined(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsDefined(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsDefined(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsDefined(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsDefined(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsDefined(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsDefined(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsDefined(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.</returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool IsDefined<[EnumConstraint] TEnum>(uint value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsDefined(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsDefined(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsDefined(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsDefined(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsDefined(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsDefined(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsDefined(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsDefined(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.</returns>
        [Pure]
        public static bool IsDefined<[EnumConstraint] TEnum>(long value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsDefined(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsDefined(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsDefined(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsDefined(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsDefined(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsDefined(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsDefined(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsDefined(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether <paramref name="value"/> can be converted to a <typeparamref name="TEnum"/>
        /// and that that value is defined.</returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool IsDefined<[EnumConstraint] TEnum>(ulong value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).IsDefined(value);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).IsDefined(value);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).IsDefined(value);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).IsDefined(value);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).IsDefined(value);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).IsDefined(value);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).IsDefined(value);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).IsDefined(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }
        #endregion

        #region IsInValueRange
        /// <summary>
        /// Indicates whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.</returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool IsInValueRange<[EnumConstraint] TEnum>(sbyte value)
            where TEnum : struct
        {
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.Int64IsInValueRange(value);
                case TypeCode.UInt32:
                    return EnumsCache<uint>.Int64IsInValueRange(value);
                case TypeCode.Int64:
                    return EnumsCache<long>.Int64IsInValueRange(value);
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.Int64IsInValueRange(value);
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.Int64IsInValueRange(value);
                case TypeCode.Byte:
                    return EnumsCache<byte>.Int64IsInValueRange(value);
                case TypeCode.Int16:
                    return EnumsCache<short>.Int64IsInValueRange(value);
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.Int64IsInValueRange(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.</returns>
        [Pure]
        public static bool IsInValueRange<[EnumConstraint] TEnum>(byte value)
            where TEnum : struct
        {
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.Int64IsInValueRange(value);
                case TypeCode.UInt32:
                    return EnumsCache<uint>.Int64IsInValueRange(value);
                case TypeCode.Int64:
                    return EnumsCache<long>.Int64IsInValueRange(value);
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.Int64IsInValueRange(value);
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.Int64IsInValueRange(value);
                case TypeCode.Byte:
                    return EnumsCache<byte>.Int64IsInValueRange(value);
                case TypeCode.Int16:
                    return EnumsCache<short>.Int64IsInValueRange(value);
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.Int64IsInValueRange(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.</returns>
        [Pure]
        public static bool IsInValueRange<[EnumConstraint] TEnum>(short value)
            where TEnum : struct
        {
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.Int64IsInValueRange(value);
                case TypeCode.UInt32:
                    return EnumsCache<uint>.Int64IsInValueRange(value);
                case TypeCode.Int64:
                    return EnumsCache<long>.Int64IsInValueRange(value);
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.Int64IsInValueRange(value);
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.Int64IsInValueRange(value);
                case TypeCode.Byte:
                    return EnumsCache<byte>.Int64IsInValueRange(value);
                case TypeCode.Int16:
                    return EnumsCache<short>.Int64IsInValueRange(value);
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.Int64IsInValueRange(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.</returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool IsInValueRange<[EnumConstraint] TEnum>(ushort value)
            where TEnum : struct
        {
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.Int64IsInValueRange(value);
                case TypeCode.UInt32:
                    return EnumsCache<uint>.Int64IsInValueRange(value);
                case TypeCode.Int64:
                    return EnumsCache<long>.Int64IsInValueRange(value);
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.Int64IsInValueRange(value);
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.Int64IsInValueRange(value);
                case TypeCode.Byte:
                    return EnumsCache<byte>.Int64IsInValueRange(value);
                case TypeCode.Int16:
                    return EnumsCache<short>.Int64IsInValueRange(value);
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.Int64IsInValueRange(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.</returns>
        [Pure]
        public static bool IsInValueRange<[EnumConstraint] TEnum>(int value)
            where TEnum : struct
        {
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.Int64IsInValueRange(value);
                case TypeCode.UInt32:
                    return EnumsCache<uint>.Int64IsInValueRange(value);
                case TypeCode.Int64:
                    return EnumsCache<long>.Int64IsInValueRange(value);
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.Int64IsInValueRange(value);
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.Int64IsInValueRange(value);
                case TypeCode.Byte:
                    return EnumsCache<byte>.Int64IsInValueRange(value);
                case TypeCode.Int16:
                    return EnumsCache<short>.Int64IsInValueRange(value);
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.Int64IsInValueRange(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.</returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool IsInValueRange<[EnumConstraint] TEnum>(uint value)
            where TEnum : struct
        {
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.Int64IsInValueRange(value);
                case TypeCode.UInt32:
                    return EnumsCache<uint>.Int64IsInValueRange(value);
                case TypeCode.Int64:
                    return EnumsCache<long>.Int64IsInValueRange(value);
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.Int64IsInValueRange(value);
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.Int64IsInValueRange(value);
                case TypeCode.Byte:
                    return EnumsCache<byte>.Int64IsInValueRange(value);
                case TypeCode.Int16:
                    return EnumsCache<short>.Int64IsInValueRange(value);
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.Int64IsInValueRange(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.</returns>
        [Pure]
        public static bool IsInValueRange<[EnumConstraint] TEnum>(long value)
            where TEnum : struct
        {
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.Int64IsInValueRange(value);
                case TypeCode.UInt32:
                    return EnumsCache<uint>.Int64IsInValueRange(value);
                case TypeCode.Int64:
                    return EnumsCache<long>.Int64IsInValueRange(value);
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.Int64IsInValueRange(value);
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.Int64IsInValueRange(value);
                case TypeCode.Byte:
                    return EnumsCache<byte>.Int64IsInValueRange(value);
                case TypeCode.Int16:
                    return EnumsCache<short>.Int64IsInValueRange(value);
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.Int64IsInValueRange(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }

        /// <summary>
        /// Indicates whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Indication whether the specified <paramref name="value"/> is within <typeparamref name="TEnum"/>'s underlying type's value range.</returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool IsInValueRange<[EnumConstraint] TEnum>(ulong value)
            where TEnum : struct
        {
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.UInt64IsInValueRange(value);
                case TypeCode.UInt32:
                    return EnumsCache<uint>.UInt64IsInValueRange(value);
                case TypeCode.Int64:
                    return EnumsCache<long>.UInt64IsInValueRange(value);
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.UInt64IsInValueRange(value);
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.UInt64IsInValueRange(value);
                case TypeCode.Byte:
                    return EnumsCache<byte>.UInt64IsInValueRange(value);
                case TypeCode.Int16:
                    return EnumsCache<short>.UInt64IsInValueRange(value);
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.UInt64IsInValueRange(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return false;
        }
        #endregion

        #region ToObject
        /// <summary>
        /// Converts the specified <paramref name="value"/> to a <typeparamref name="TEnum"/> while checking that the result is within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to convert. Must be an <see cref="sbyte"/>, <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>,
        /// <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, or <see cref="string"/>.</param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns>The specified <paramref name="value"/> converted to a <typeparamref name="TEnum"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid type
        /// -or-
        /// <paramref name="validate"/> is true and <paramref name="value"/> is not a valid value.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the underlying type's value range.</exception>
        [Pure]
        public static TEnum ToObject<[EnumConstraint] TEnum>(object value, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((Func<int, TEnum>)toEnum)(((EnumsCache<int>)cache).ToObject(value, validate));
                case TypeCode.UInt32:
                    return ((Func<uint, TEnum>)toEnum)(((EnumsCache<uint>)cache).ToObject(value, validate));
                case TypeCode.Int64:
                    return ((Func<long, TEnum>)toEnum)(((EnumsCache<long>)cache).ToObject(value, validate));
                case TypeCode.UInt64:
                    return ((Func<ulong, TEnum>)toEnum)(((EnumsCache<ulong>)cache).ToObject(value, validate));
                case TypeCode.SByte:
                    return ((Func<sbyte, TEnum>)toEnum)(((EnumsCache<sbyte>)cache).ToObject(value, validate));
                case TypeCode.Byte:
                    return ((Func<byte, TEnum>)toEnum)(((EnumsCache<byte>)cache).ToObject(value, validate));
                case TypeCode.Int16:
                    return ((Func<short, TEnum>)toEnum)(((EnumsCache<short>)cache).ToObject(value, validate));
                case TypeCode.UInt16:
                    return ((Func<ushort, TEnum>)toEnum)(((EnumsCache<ushort>)cache).ToObject(value, validate));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return default(TEnum);
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to a <typeparamref name="TEnum"/> while checking that it's within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to convert.</param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns>The specified <paramref name="value"/> converted to a <typeparamref name="TEnum"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="validate"/> is true and <paramref name="value"/> is not a valid value.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the underlying type's value range.</exception>
        [Pure]
        [CLSCompliant(false)]
        public static TEnum ToObject<[EnumConstraint] TEnum>(sbyte value, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((Func<int, TEnum>)toEnum)(((EnumsCache<int>)cache).ToObject(value, validate));
                case TypeCode.UInt32:
                    return ((Func<uint, TEnum>)toEnum)(((EnumsCache<uint>)cache).ToObject(value, validate));
                case TypeCode.Int64:
                    return ((Func<long, TEnum>)toEnum)(((EnumsCache<long>)cache).ToObject(value, validate));
                case TypeCode.UInt64:
                    return ((Func<ulong, TEnum>)toEnum)(((EnumsCache<ulong>)cache).ToObject(value, validate));
                case TypeCode.SByte:
                    return ((Func<sbyte, TEnum>)toEnum)(((EnumsCache<sbyte>)cache).ToObject(value, validate));
                case TypeCode.Byte:
                    return ((Func<byte, TEnum>)toEnum)(((EnumsCache<byte>)cache).ToObject(value, validate));
                case TypeCode.Int16:
                    return ((Func<short, TEnum>)toEnum)(((EnumsCache<short>)cache).ToObject(value, validate));
                case TypeCode.UInt16:
                    return ((Func<ushort, TEnum>)toEnum)(((EnumsCache<ushort>)cache).ToObject(value, validate));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return default(TEnum);
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to a <typeparamref name="TEnum"/> while checking that it's within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to convert.</param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns>The specified <paramref name="value"/> converted to a <typeparamref name="TEnum"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="validate"/> is true and <paramref name="value"/> is not a valid value.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the underlying type's value range.</exception>
        [Pure]
        public static TEnum ToObject<[EnumConstraint] TEnum>(byte value, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((Func<int, TEnum>)toEnum)(((EnumsCache<int>)cache).ToObject(value, validate));
                case TypeCode.UInt32:
                    return ((Func<uint, TEnum>)toEnum)(((EnumsCache<uint>)cache).ToObject(value, validate));
                case TypeCode.Int64:
                    return ((Func<long, TEnum>)toEnum)(((EnumsCache<long>)cache).ToObject(value, validate));
                case TypeCode.UInt64:
                    return ((Func<ulong, TEnum>)toEnum)(((EnumsCache<ulong>)cache).ToObject(value, validate));
                case TypeCode.SByte:
                    return ((Func<sbyte, TEnum>)toEnum)(((EnumsCache<sbyte>)cache).ToObject(value, validate));
                case TypeCode.Byte:
                    return ((Func<byte, TEnum>)toEnum)(((EnumsCache<byte>)cache).ToObject(value, validate));
                case TypeCode.Int16:
                    return ((Func<short, TEnum>)toEnum)(((EnumsCache<short>)cache).ToObject(value, validate));
                case TypeCode.UInt16:
                    return ((Func<ushort, TEnum>)toEnum)(((EnumsCache<ushort>)cache).ToObject(value, validate));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return default(TEnum);
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to a <typeparamref name="TEnum"/> while checking that it's within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to convert.</param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns>The specified <paramref name="value"/> converted to a <typeparamref name="TEnum"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="validate"/> is true and <paramref name="value"/> is not a valid value.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the underlying type's value range.</exception>
        [Pure]
        public static TEnum ToObject<[EnumConstraint] TEnum>(short value, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((Func<int, TEnum>)toEnum)(((EnumsCache<int>)cache).ToObject(value, validate));
                case TypeCode.UInt32:
                    return ((Func<uint, TEnum>)toEnum)(((EnumsCache<uint>)cache).ToObject(value, validate));
                case TypeCode.Int64:
                    return ((Func<long, TEnum>)toEnum)(((EnumsCache<long>)cache).ToObject(value, validate));
                case TypeCode.UInt64:
                    return ((Func<ulong, TEnum>)toEnum)(((EnumsCache<ulong>)cache).ToObject(value, validate));
                case TypeCode.SByte:
                    return ((Func<sbyte, TEnum>)toEnum)(((EnumsCache<sbyte>)cache).ToObject(value, validate));
                case TypeCode.Byte:
                    return ((Func<byte, TEnum>)toEnum)(((EnumsCache<byte>)cache).ToObject(value, validate));
                case TypeCode.Int16:
                    return ((Func<short, TEnum>)toEnum)(((EnumsCache<short>)cache).ToObject(value, validate));
                case TypeCode.UInt16:
                    return ((Func<ushort, TEnum>)toEnum)(((EnumsCache<ushort>)cache).ToObject(value, validate));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return default(TEnum);
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to a <typeparamref name="TEnum"/> while checking that it's within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to convert.</param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns>The specified <paramref name="value"/> converted to a <typeparamref name="TEnum"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="validate"/> is true and <paramref name="value"/> is not a valid value.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the underlying type's value range.</exception>
        [Pure]
        [CLSCompliant(false)]
        public static TEnum ToObject<[EnumConstraint] TEnum>(ushort value, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((Func<int, TEnum>)toEnum)(((EnumsCache<int>)cache).ToObject(value, validate));
                case TypeCode.UInt32:
                    return ((Func<uint, TEnum>)toEnum)(((EnumsCache<uint>)cache).ToObject(value, validate));
                case TypeCode.Int64:
                    return ((Func<long, TEnum>)toEnum)(((EnumsCache<long>)cache).ToObject(value, validate));
                case TypeCode.UInt64:
                    return ((Func<ulong, TEnum>)toEnum)(((EnumsCache<ulong>)cache).ToObject(value, validate));
                case TypeCode.SByte:
                    return ((Func<sbyte, TEnum>)toEnum)(((EnumsCache<sbyte>)cache).ToObject(value, validate));
                case TypeCode.Byte:
                    return ((Func<byte, TEnum>)toEnum)(((EnumsCache<byte>)cache).ToObject(value, validate));
                case TypeCode.Int16:
                    return ((Func<short, TEnum>)toEnum)(((EnumsCache<short>)cache).ToObject(value, validate));
                case TypeCode.UInt16:
                    return ((Func<ushort, TEnum>)toEnum)(((EnumsCache<ushort>)cache).ToObject(value, validate));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return default(TEnum);
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to a <typeparamref name="TEnum"/> while checking that it's within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to convert.</param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns>The specified <paramref name="value"/> converted to a <typeparamref name="TEnum"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="validate"/> is true and <paramref name="value"/> is not a valid value.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the underlying type's value range.</exception>
        [Pure]
        public static TEnum ToObject<[EnumConstraint] TEnum>(int value, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((Func<int, TEnum>)toEnum)(((EnumsCache<int>)cache).ToObject(value, validate));
                case TypeCode.UInt32:
                    return ((Func<uint, TEnum>)toEnum)(((EnumsCache<uint>)cache).ToObject(value, validate));
                case TypeCode.Int64:
                    return ((Func<long, TEnum>)toEnum)(((EnumsCache<long>)cache).ToObject(value, validate));
                case TypeCode.UInt64:
                    return ((Func<ulong, TEnum>)toEnum)(((EnumsCache<ulong>)cache).ToObject(value, validate));
                case TypeCode.SByte:
                    return ((Func<sbyte, TEnum>)toEnum)(((EnumsCache<sbyte>)cache).ToObject(value, validate));
                case TypeCode.Byte:
                    return ((Func<byte, TEnum>)toEnum)(((EnumsCache<byte>)cache).ToObject(value, validate));
                case TypeCode.Int16:
                    return ((Func<short, TEnum>)toEnum)(((EnumsCache<short>)cache).ToObject(value, validate));
                case TypeCode.UInt16:
                    return ((Func<ushort, TEnum>)toEnum)(((EnumsCache<ushort>)cache).ToObject(value, validate));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return default(TEnum);
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to a <typeparamref name="TEnum"/> while checking that it's within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to convert.</param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns>The specified <paramref name="value"/> converted to a <typeparamref name="TEnum"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="validate"/> is true and <paramref name="value"/> is not a valid value.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the underlying type's value range.</exception>
        [Pure]
        [CLSCompliant(false)]
        public static TEnum ToObject<[EnumConstraint] TEnum>(uint value, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((Func<int, TEnum>)toEnum)(((EnumsCache<int>)cache).ToObject(value, validate));
                case TypeCode.UInt32:
                    return ((Func<uint, TEnum>)toEnum)(((EnumsCache<uint>)cache).ToObject(value, validate));
                case TypeCode.Int64:
                    return ((Func<long, TEnum>)toEnum)(((EnumsCache<long>)cache).ToObject(value, validate));
                case TypeCode.UInt64:
                    return ((Func<ulong, TEnum>)toEnum)(((EnumsCache<ulong>)cache).ToObject(value, validate));
                case TypeCode.SByte:
                    return ((Func<sbyte, TEnum>)toEnum)(((EnumsCache<sbyte>)cache).ToObject(value, validate));
                case TypeCode.Byte:
                    return ((Func<byte, TEnum>)toEnum)(((EnumsCache<byte>)cache).ToObject(value, validate));
                case TypeCode.Int16:
                    return ((Func<short, TEnum>)toEnum)(((EnumsCache<short>)cache).ToObject(value, validate));
                case TypeCode.UInt16:
                    return ((Func<ushort, TEnum>)toEnum)(((EnumsCache<ushort>)cache).ToObject(value, validate));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return default(TEnum);
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to a <typeparamref name="TEnum"/> while checking that it's within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to convert.</param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns>The specified <paramref name="value"/> converted to a <typeparamref name="TEnum"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="validate"/> is true and <paramref name="value"/> is not a valid value.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the underlying type's value range.</exception>
        [Pure]
        public static TEnum ToObject<[EnumConstraint] TEnum>(long value, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((Func<int, TEnum>)toEnum)(((EnumsCache<int>)cache).ToObject(value, validate));
                case TypeCode.UInt32:
                    return ((Func<uint, TEnum>)toEnum)(((EnumsCache<uint>)cache).ToObject(value, validate));
                case TypeCode.Int64:
                    return ((Func<long, TEnum>)toEnum)(((EnumsCache<long>)cache).ToObject(value, validate));
                case TypeCode.UInt64:
                    return ((Func<ulong, TEnum>)toEnum)(((EnumsCache<ulong>)cache).ToObject(value, validate));
                case TypeCode.SByte:
                    return ((Func<sbyte, TEnum>)toEnum)(((EnumsCache<sbyte>)cache).ToObject(value, validate));
                case TypeCode.Byte:
                    return ((Func<byte, TEnum>)toEnum)(((EnumsCache<byte>)cache).ToObject(value, validate));
                case TypeCode.Int16:
                    return ((Func<short, TEnum>)toEnum)(((EnumsCache<short>)cache).ToObject(value, validate));
                case TypeCode.UInt16:
                    return ((Func<ushort, TEnum>)toEnum)(((EnumsCache<ushort>)cache).ToObject(value, validate));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return default(TEnum);
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to a <typeparamref name="TEnum"/> while checking that it's within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to convert.</param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns>The specified <paramref name="value"/> converted to a <typeparamref name="TEnum"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="validate"/> is true and <paramref name="value"/> is not a valid value.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the underlying type's value range.</exception>
        [Pure]
        [CLSCompliant(false)]
        public static TEnum ToObject<[EnumConstraint] TEnum>(ulong value, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((Func<int, TEnum>)toEnum)(((EnumsCache<int>)cache).ToObject(value, validate));
                case TypeCode.UInt32:
                    return ((Func<uint, TEnum>)toEnum)(((EnumsCache<uint>)cache).ToObject(value, validate));
                case TypeCode.Int64:
                    return ((Func<long, TEnum>)toEnum)(((EnumsCache<long>)cache).ToObject(value, validate));
                case TypeCode.UInt64:
                    return ((Func<ulong, TEnum>)toEnum)(((EnumsCache<ulong>)cache).ToObject(value, validate));
                case TypeCode.SByte:
                    return ((Func<sbyte, TEnum>)toEnum)(((EnumsCache<sbyte>)cache).ToObject(value, validate));
                case TypeCode.Byte:
                    return ((Func<byte, TEnum>)toEnum)(((EnumsCache<byte>)cache).ToObject(value, validate));
                case TypeCode.Int16:
                    return ((Func<short, TEnum>)toEnum)(((EnumsCache<short>)cache).ToObject(value, validate));
                case TypeCode.UInt16:
                    return ((Func<ushort, TEnum>)toEnum)(((EnumsCache<ushort>)cache).ToObject(value, validate));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return default(TEnum);
        }

        /// <summary>
        /// Tries to converts the specified <paramref name="value"/> to a <typeparamref name="TEnum"/> while checking that it's within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid. An indication
        /// if the operation succeeded is returned and the result of the operation or if it fails the default enumeration value is returned in the output parameter <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to try to convert. Must be an <see cref="sbyte"/>, <see cref="byte"/>, <see cref="short"/>, <see cref="ushort"/>,
        /// <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, or <see cref="string"/></param>
        /// <param name="result"></param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns></returns>
        [Pure]
        public static bool TryToObject<[EnumConstraint] TEnum>(object value, out TEnum result, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            bool success;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    int resultAsInt32;
                    success = ((EnumsCache<int>)cache).TryToObject(value, out resultAsInt32, validate);
                    result = ((Func<int, TEnum>)toEnum)(resultAsInt32);
                    return success;
                case TypeCode.UInt32:
                    uint resultAsUInt32;
                    success = ((EnumsCache<uint>)cache).TryToObject(value, out resultAsUInt32, validate);
                    result = ((Func<uint, TEnum>)toEnum)(resultAsUInt32);
                    return success;
                case TypeCode.Int64:
                    long resultAsInt64;
                    success = ((EnumsCache<long>)cache).TryToObject(value, out resultAsInt64, validate);
                    result = ((Func<long, TEnum>)toEnum)(resultAsInt64);
                    return success;
                case TypeCode.UInt64:
                    ulong resultAsUInt64;
                    success = ((EnumsCache<ulong>)cache).TryToObject(value, out resultAsUInt64, validate);
                    result = ((Func<ulong, TEnum>)toEnum)(resultAsUInt64);
                    return success;
                case TypeCode.SByte:
                    sbyte resultAsSByte;
                    success = ((EnumsCache<sbyte>)cache).TryToObject(value, out resultAsSByte, validate);
                    result = ((Func<sbyte, TEnum>)toEnum)(resultAsSByte);
                    return success;
                case TypeCode.Byte:
                    byte resultAsByte;
                    success = ((EnumsCache<byte>)cache).TryToObject(value, out resultAsByte, validate);
                    result = ((Func<byte, TEnum>)toEnum)(resultAsByte);
                    return success;
                case TypeCode.Int16:
                    short resultAsInt16;
                    success = ((EnumsCache<short>)cache).TryToObject(value, out resultAsInt16, validate);
                    result = ((Func<short, TEnum>)toEnum)(resultAsInt16);
                    return success;
                case TypeCode.UInt16:
                    ushort resultAsUInt16;
                    success = ((EnumsCache<ushort>)cache).TryToObject(value, out resultAsUInt16, validate);
                    result = ((Func<ushort, TEnum>)toEnum)(resultAsUInt16);
                    return success;
            }
            Debug.Fail("Unknown Enum TypeCode");
            result = default(TEnum);
            return false;
        }

        /// <summary>
        /// Tries to converts the specified 8-bit signed integer <paramref name="value"/> to an enumeration member while checking that the <paramref name="value"/> is within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid. An indication
        /// if the operation succeeded is returned and the result of the operation or if it fails the default enumeration value is stored in the output parameter <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to try to convert.</param>
        /// <param name="result"></param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns></returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool TryToObject<[EnumConstraint] TEnum>(sbyte value, out TEnum result, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            bool success;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    int resultAsInt32;
                    success = ((EnumsCache<int>)cache).TryToObject(value, out resultAsInt32, validate);
                    result = ((Func<int, TEnum>)toEnum)(resultAsInt32);
                    return success;
                case TypeCode.UInt32:
                    uint resultAsUInt32;
                    success = ((EnumsCache<uint>)cache).TryToObject(value, out resultAsUInt32, validate);
                    result = ((Func<uint, TEnum>)toEnum)(resultAsUInt32);
                    return success;
                case TypeCode.Int64:
                    long resultAsInt64;
                    success = ((EnumsCache<long>)cache).TryToObject(value, out resultAsInt64, validate);
                    result = ((Func<long, TEnum>)toEnum)(resultAsInt64);
                    return success;
                case TypeCode.UInt64:
                    ulong resultAsUInt64;
                    success = ((EnumsCache<ulong>)cache).TryToObject(value, out resultAsUInt64, validate);
                    result = ((Func<ulong, TEnum>)toEnum)(resultAsUInt64);
                    return success;
                case TypeCode.SByte:
                    sbyte resultAsSByte;
                    success = ((EnumsCache<sbyte>)cache).TryToObject(value, out resultAsSByte, validate);
                    result = ((Func<sbyte, TEnum>)toEnum)(resultAsSByte);
                    return success;
                case TypeCode.Byte:
                    byte resultAsByte;
                    success = ((EnumsCache<byte>)cache).TryToObject(value, out resultAsByte, validate);
                    result = ((Func<byte, TEnum>)toEnum)(resultAsByte);
                    return success;
                case TypeCode.Int16:
                    short resultAsInt16;
                    success = ((EnumsCache<short>)cache).TryToObject(value, out resultAsInt16, validate);
                    result = ((Func<short, TEnum>)toEnum)(resultAsInt16);
                    return success;
                case TypeCode.UInt16:
                    ushort resultAsUInt16;
                    success = ((EnumsCache<ushort>)cache).TryToObject(value, out resultAsUInt16, validate);
                    result = ((Func<ushort, TEnum>)toEnum)(resultAsUInt16);
                    return success;
            }
            Debug.Fail("Unknown Enum TypeCode");
            result = default(TEnum);
            return false;
        }

        /// <summary>
        /// Tries to converts the specified 8-bit unsigned integer <paramref name="value"/> to an enumeration member while checking that the <paramref name="value"/> is within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid. An indication
        /// if the operation succeeded is returned and the result of the operation or if it fails the default enumeration value is stored in the output parameter <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to try to convert.</param>
        /// <param name="result"></param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns></returns>
        [Pure]
        public static bool TryToObject<[EnumConstraint] TEnum>(byte value, out TEnum result, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            bool success;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    int resultAsInt32;
                    success = ((EnumsCache<int>)cache).TryToObject(value, out resultAsInt32, validate);
                    result = ((Func<int, TEnum>)toEnum)(resultAsInt32);
                    return success;
                case TypeCode.UInt32:
                    uint resultAsUInt32;
                    success = ((EnumsCache<uint>)cache).TryToObject(value, out resultAsUInt32, validate);
                    result = ((Func<uint, TEnum>)toEnum)(resultAsUInt32);
                    return success;
                case TypeCode.Int64:
                    long resultAsInt64;
                    success = ((EnumsCache<long>)cache).TryToObject(value, out resultAsInt64, validate);
                    result = ((Func<long, TEnum>)toEnum)(resultAsInt64);
                    return success;
                case TypeCode.UInt64:
                    ulong resultAsUInt64;
                    success = ((EnumsCache<ulong>)cache).TryToObject(value, out resultAsUInt64, validate);
                    result = ((Func<ulong, TEnum>)toEnum)(resultAsUInt64);
                    return success;
                case TypeCode.SByte:
                    sbyte resultAsSByte;
                    success = ((EnumsCache<sbyte>)cache).TryToObject(value, out resultAsSByte, validate);
                    result = ((Func<sbyte, TEnum>)toEnum)(resultAsSByte);
                    return success;
                case TypeCode.Byte:
                    byte resultAsByte;
                    success = ((EnumsCache<byte>)cache).TryToObject(value, out resultAsByte, validate);
                    result = ((Func<byte, TEnum>)toEnum)(resultAsByte);
                    return success;
                case TypeCode.Int16:
                    short resultAsInt16;
                    success = ((EnumsCache<short>)cache).TryToObject(value, out resultAsInt16, validate);
                    result = ((Func<short, TEnum>)toEnum)(resultAsInt16);
                    return success;
                case TypeCode.UInt16:
                    ushort resultAsUInt16;
                    success = ((EnumsCache<ushort>)cache).TryToObject(value, out resultAsUInt16, validate);
                    result = ((Func<ushort, TEnum>)toEnum)(resultAsUInt16);
                    return success;
            }
            Debug.Fail("Unknown Enum TypeCode");
            result = default(TEnum);
            return false;
        }

        /// <summary>
        /// Tries to converts the specified 16-bit signed integer <paramref name="value"/> to an enumeration member while checking that the <paramref name="value"/> is within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid. An indication
        /// if the operation succeeded is returned and the result of the operation or if it fails the default enumeration value is stored in the output parameter <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to try to convert.</param>
        /// <param name="result"></param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns></returns>
        [Pure]
        public static bool TryToObject<[EnumConstraint] TEnum>(short value, out TEnum result, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            bool success;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    int resultAsInt32;
                    success = ((EnumsCache<int>)cache).TryToObject(value, out resultAsInt32, validate);
                    result = ((Func<int, TEnum>)toEnum)(resultAsInt32);
                    return success;
                case TypeCode.UInt32:
                    uint resultAsUInt32;
                    success = ((EnumsCache<uint>)cache).TryToObject(value, out resultAsUInt32, validate);
                    result = ((Func<uint, TEnum>)toEnum)(resultAsUInt32);
                    return success;
                case TypeCode.Int64:
                    long resultAsInt64;
                    success = ((EnumsCache<long>)cache).TryToObject(value, out resultAsInt64, validate);
                    result = ((Func<long, TEnum>)toEnum)(resultAsInt64);
                    return success;
                case TypeCode.UInt64:
                    ulong resultAsUInt64;
                    success = ((EnumsCache<ulong>)cache).TryToObject(value, out resultAsUInt64, validate);
                    result = ((Func<ulong, TEnum>)toEnum)(resultAsUInt64);
                    return success;
                case TypeCode.SByte:
                    sbyte resultAsSByte;
                    success = ((EnumsCache<sbyte>)cache).TryToObject(value, out resultAsSByte, validate);
                    result = ((Func<sbyte, TEnum>)toEnum)(resultAsSByte);
                    return success;
                case TypeCode.Byte:
                    byte resultAsByte;
                    success = ((EnumsCache<byte>)cache).TryToObject(value, out resultAsByte, validate);
                    result = ((Func<byte, TEnum>)toEnum)(resultAsByte);
                    return success;
                case TypeCode.Int16:
                    short resultAsInt16;
                    success = ((EnumsCache<short>)cache).TryToObject(value, out resultAsInt16, validate);
                    result = ((Func<short, TEnum>)toEnum)(resultAsInt16);
                    return success;
                case TypeCode.UInt16:
                    ushort resultAsUInt16;
                    success = ((EnumsCache<ushort>)cache).TryToObject(value, out resultAsUInt16, validate);
                    result = ((Func<ushort, TEnum>)toEnum)(resultAsUInt16);
                    return success;
            }
            Debug.Fail("Unknown Enum TypeCode");
            result = default(TEnum);
            return false;
        }

        /// <summary>
        /// Tries to converts the specified 16-bit unsigned integer <paramref name="value"/> to an enumeration member while checking that the <paramref name="value"/> is within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid. An indication
        /// if the operation succeeded is returned and the result of the operation or if it fails the default enumeration value is stored in the output parameter <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to try to convert.</param>
        /// <param name="result"></param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns></returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool TryToObject<[EnumConstraint] TEnum>(ushort value, out TEnum result, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            bool success;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    int resultAsInt32;
                    success = ((EnumsCache<int>)cache).TryToObject(value, out resultAsInt32, validate);
                    result = ((Func<int, TEnum>)toEnum)(resultAsInt32);
                    return success;
                case TypeCode.UInt32:
                    uint resultAsUInt32;
                    success = ((EnumsCache<uint>)cache).TryToObject(value, out resultAsUInt32, validate);
                    result = ((Func<uint, TEnum>)toEnum)(resultAsUInt32);
                    return success;
                case TypeCode.Int64:
                    long resultAsInt64;
                    success = ((EnumsCache<long>)cache).TryToObject(value, out resultAsInt64, validate);
                    result = ((Func<long, TEnum>)toEnum)(resultAsInt64);
                    return success;
                case TypeCode.UInt64:
                    ulong resultAsUInt64;
                    success = ((EnumsCache<ulong>)cache).TryToObject(value, out resultAsUInt64, validate);
                    result = ((Func<ulong, TEnum>)toEnum)(resultAsUInt64);
                    return success;
                case TypeCode.SByte:
                    sbyte resultAsSByte;
                    success = ((EnumsCache<sbyte>)cache).TryToObject(value, out resultAsSByte, validate);
                    result = ((Func<sbyte, TEnum>)toEnum)(resultAsSByte);
                    return success;
                case TypeCode.Byte:
                    byte resultAsByte;
                    success = ((EnumsCache<byte>)cache).TryToObject(value, out resultAsByte, validate);
                    result = ((Func<byte, TEnum>)toEnum)(resultAsByte);
                    return success;
                case TypeCode.Int16:
                    short resultAsInt16;
                    success = ((EnumsCache<short>)cache).TryToObject(value, out resultAsInt16, validate);
                    result = ((Func<short, TEnum>)toEnum)(resultAsInt16);
                    return success;
                case TypeCode.UInt16:
                    ushort resultAsUInt16;
                    success = ((EnumsCache<ushort>)cache).TryToObject(value, out resultAsUInt16, validate);
                    result = ((Func<ushort, TEnum>)toEnum)(resultAsUInt16);
                    return success;
            }
            Debug.Fail("Unknown Enum TypeCode");
            result = default(TEnum);
            return false;
        }

        /// <summary>
        /// Tries to converts the specified 32-bit signed integer <paramref name="value"/> to an enumeration member while checking that the <paramref name="value"/> is within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid. An indication
        /// if the operation succeeded is returned and the result of the operation or if it fails the default enumeration value is stored in the output parameter <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to try to convert.</param>
        /// <param name="result"></param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns></returns>
        [Pure]
        public static bool TryToObject<[EnumConstraint] TEnum>(int value, out TEnum result, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            bool success;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    int resultAsInt32;
                    success = ((EnumsCache<int>)cache).TryToObject(value, out resultAsInt32, validate);
                    result = ((Func<int, TEnum>)toEnum)(resultAsInt32);
                    return success;
                case TypeCode.UInt32:
                    uint resultAsUInt32;
                    success = ((EnumsCache<uint>)cache).TryToObject(value, out resultAsUInt32, validate);
                    result = ((Func<uint, TEnum>)toEnum)(resultAsUInt32);
                    return success;
                case TypeCode.Int64:
                    long resultAsInt64;
                    success = ((EnumsCache<long>)cache).TryToObject(value, out resultAsInt64, validate);
                    result = ((Func<long, TEnum>)toEnum)(resultAsInt64);
                    return success;
                case TypeCode.UInt64:
                    ulong resultAsUInt64;
                    success = ((EnumsCache<ulong>)cache).TryToObject(value, out resultAsUInt64, validate);
                    result = ((Func<ulong, TEnum>)toEnum)(resultAsUInt64);
                    return success;
                case TypeCode.SByte:
                    sbyte resultAsSByte;
                    success = ((EnumsCache<sbyte>)cache).TryToObject(value, out resultAsSByte, validate);
                    result = ((Func<sbyte, TEnum>)toEnum)(resultAsSByte);
                    return success;
                case TypeCode.Byte:
                    byte resultAsByte;
                    success = ((EnumsCache<byte>)cache).TryToObject(value, out resultAsByte, validate);
                    result = ((Func<byte, TEnum>)toEnum)(resultAsByte);
                    return success;
                case TypeCode.Int16:
                    short resultAsInt16;
                    success = ((EnumsCache<short>)cache).TryToObject(value, out resultAsInt16, validate);
                    result = ((Func<short, TEnum>)toEnum)(resultAsInt16);
                    return success;
                case TypeCode.UInt16:
                    ushort resultAsUInt16;
                    success = ((EnumsCache<ushort>)cache).TryToObject(value, out resultAsUInt16, validate);
                    result = ((Func<ushort, TEnum>)toEnum)(resultAsUInt16);
                    return success;
            }
            Debug.Fail("Unknown Enum TypeCode");
            result = default(TEnum);
            return false;
        }

        /// <summary>
        /// Tries to converts the specified 32-bit unsigned integer <paramref name="value"/> to an enumeration member while checking that the <paramref name="value"/> is within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid. An indication
        /// if the operation succeeded is returned and the result of the operation or if it fails the default enumeration value is stored in the output parameter <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to try to convert.</param>
        /// <param name="result"></param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns></returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool TryToObject<[EnumConstraint] TEnum>(uint value, out TEnum result, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            bool success;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    int resultAsInt32;
                    success = ((EnumsCache<int>)cache).TryToObject(value, out resultAsInt32, validate);
                    result = ((Func<int, TEnum>)toEnum)(resultAsInt32);
                    return success;
                case TypeCode.UInt32:
                    uint resultAsUInt32;
                    success = ((EnumsCache<uint>)cache).TryToObject(value, out resultAsUInt32, validate);
                    result = ((Func<uint, TEnum>)toEnum)(resultAsUInt32);
                    return success;
                case TypeCode.Int64:
                    long resultAsInt64;
                    success = ((EnumsCache<long>)cache).TryToObject(value, out resultAsInt64, validate);
                    result = ((Func<long, TEnum>)toEnum)(resultAsInt64);
                    return success;
                case TypeCode.UInt64:
                    ulong resultAsUInt64;
                    success = ((EnumsCache<ulong>)cache).TryToObject(value, out resultAsUInt64, validate);
                    result = ((Func<ulong, TEnum>)toEnum)(resultAsUInt64);
                    return success;
                case TypeCode.SByte:
                    sbyte resultAsSByte;
                    success = ((EnumsCache<sbyte>)cache).TryToObject(value, out resultAsSByte, validate);
                    result = ((Func<sbyte, TEnum>)toEnum)(resultAsSByte);
                    return success;
                case TypeCode.Byte:
                    byte resultAsByte;
                    success = ((EnumsCache<byte>)cache).TryToObject(value, out resultAsByte, validate);
                    result = ((Func<byte, TEnum>)toEnum)(resultAsByte);
                    return success;
                case TypeCode.Int16:
                    short resultAsInt16;
                    success = ((EnumsCache<short>)cache).TryToObject(value, out resultAsInt16, validate);
                    result = ((Func<short, TEnum>)toEnum)(resultAsInt16);
                    return success;
                case TypeCode.UInt16:
                    ushort resultAsUInt16;
                    success = ((EnumsCache<ushort>)cache).TryToObject(value, out resultAsUInt16, validate);
                    result = ((Func<ushort, TEnum>)toEnum)(resultAsUInt16);
                    return success;
            }
            Debug.Fail("Unknown Enum TypeCode");
            result = default(TEnum);
            return false;
        }

        /// <summary>
        /// Tries to converts the specified 64-bit signed integer <paramref name="value"/> to an enumeration member while checking that the <paramref name="value"/> is within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid. An indication
        /// if the operation succeeded is returned and the result of the operation or if it fails the default enumeration value is stored in the output parameter <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to try to convert.</param>
        /// <param name="result"></param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns></returns>
        [Pure]
        public static bool TryToObject<[EnumConstraint] TEnum>(long value, out TEnum result, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            bool success;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    int resultAsInt32;
                    success = ((EnumsCache<int>)cache).TryToObject(value, out resultAsInt32, validate);
                    result = ((Func<int, TEnum>)toEnum)(resultAsInt32);
                    return success;
                case TypeCode.UInt32:
                    uint resultAsUInt32;
                    success = ((EnumsCache<uint>)cache).TryToObject(value, out resultAsUInt32, validate);
                    result = ((Func<uint, TEnum>)toEnum)(resultAsUInt32);
                    return success;
                case TypeCode.Int64:
                    long resultAsInt64;
                    success = ((EnumsCache<long>)cache).TryToObject(value, out resultAsInt64, validate);
                    result = ((Func<long, TEnum>)toEnum)(resultAsInt64);
                    return success;
                case TypeCode.UInt64:
                    ulong resultAsUInt64;
                    success = ((EnumsCache<ulong>)cache).TryToObject(value, out resultAsUInt64, validate);
                    result = ((Func<ulong, TEnum>)toEnum)(resultAsUInt64);
                    return success;
                case TypeCode.SByte:
                    sbyte resultAsSByte;
                    success = ((EnumsCache<sbyte>)cache).TryToObject(value, out resultAsSByte, validate);
                    result = ((Func<sbyte, TEnum>)toEnum)(resultAsSByte);
                    return success;
                case TypeCode.Byte:
                    byte resultAsByte;
                    success = ((EnumsCache<byte>)cache).TryToObject(value, out resultAsByte, validate);
                    result = ((Func<byte, TEnum>)toEnum)(resultAsByte);
                    return success;
                case TypeCode.Int16:
                    short resultAsInt16;
                    success = ((EnumsCache<short>)cache).TryToObject(value, out resultAsInt16, validate);
                    result = ((Func<short, TEnum>)toEnum)(resultAsInt16);
                    return success;
                case TypeCode.UInt16:
                    ushort resultAsUInt16;
                    success = ((EnumsCache<ushort>)cache).TryToObject(value, out resultAsUInt16, validate);
                    result = ((Func<ushort, TEnum>)toEnum)(resultAsUInt16);
                    return success;
            }
            Debug.Fail("Unknown Enum TypeCode");
            result = default(TEnum);
            return false;
        }

        /// <summary>
        /// Tries to converts the specified 64-bit unsigned integer <paramref name="value"/> to an enumeration member while checking that the <paramref name="value"/> is within the
        /// underlying types value range. The optional parameter <paramref name="validate"/> indicates whether to check that the result is valid. An indication
        /// if the operation succeeded is returned and the result of the operation or if it fails the default enumeration value is stored in the output parameter <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">Value to try to convert.</param>
        /// <param name="result"></param>
        /// <param name="validate">(Optional) Indicates whether to check that the result is valid.</param>
        /// <returns></returns>
        [Pure]
        [CLSCompliant(false)]
        public static bool TryToObject<[EnumConstraint] TEnum>(ulong value, out TEnum result, bool validate = true)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toEnum = Enums<TEnum>.ToEnum;
            bool success;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    int resultAsInt32;
                    success = ((EnumsCache<int>)cache).TryToObject(value, out resultAsInt32, validate);
                    result = ((Func<int, TEnum>)toEnum)(resultAsInt32);
                    return success;
                case TypeCode.UInt32:
                    uint resultAsUInt32;
                    success = ((EnumsCache<uint>)cache).TryToObject(value, out resultAsUInt32, validate);
                    result = ((Func<uint, TEnum>)toEnum)(resultAsUInt32);
                    return success;
                case TypeCode.Int64:
                    long resultAsInt64;
                    success = ((EnumsCache<long>)cache).TryToObject(value, out resultAsInt64, validate);
                    result = ((Func<long, TEnum>)toEnum)(resultAsInt64);
                    return success;
                case TypeCode.UInt64:
                    ulong resultAsUInt64;
                    success = ((EnumsCache<ulong>)cache).TryToObject(value, out resultAsUInt64, validate);
                    result = ((Func<ulong, TEnum>)toEnum)(resultAsUInt64);
                    return success;
                case TypeCode.SByte:
                    sbyte resultAsSByte;
                    success = ((EnumsCache<sbyte>)cache).TryToObject(value, out resultAsSByte, validate);
                    result = ((Func<sbyte, TEnum>)toEnum)(resultAsSByte);
                    return success;
                case TypeCode.Byte:
                    byte resultAsByte;
                    success = ((EnumsCache<byte>)cache).TryToObject(value, out resultAsByte, validate);
                    result = ((Func<byte, TEnum>)toEnum)(resultAsByte);
                    return success;
                case TypeCode.Int16:
                    short resultAsInt16;
                    success = ((EnumsCache<short>)cache).TryToObject(value, out resultAsInt16, validate);
                    result = ((Func<short, TEnum>)toEnum)(resultAsInt16);
                    return success;
                case TypeCode.UInt16:
                    ushort resultAsUInt16;
                    success = ((EnumsCache<ushort>)cache).TryToObject(value, out resultAsUInt16, validate);
                    result = ((Func<ushort, TEnum>)toEnum)(resultAsUInt16);
                    return success;
            }
            Debug.Fail("Unknown Enum TypeCode");
            result = default(TEnum);
            return false;
        }
        #endregion

        #region All Values Main Methods
        /// <summary>
        /// Validates that <paramref name="value"/> is valid. If it's not it throws an <see cref="ArgumentException"/> with the given <paramref name="paramName"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="paramName"></param>
        /// <returns><paramref name="value"/> for use in constructor initializers and fluent API's</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> is invalid</exception>
        [Pure]
        public static TEnum Validate<[EnumConstraint] TEnum>(this TEnum value, string paramName)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    ((EnumsCache<int>)cache).Validate(((Func<TEnum, int>)toInt)(value), paramName);
                    return value;
                case TypeCode.UInt32:
                    ((EnumsCache<uint>)cache).Validate(((Func<TEnum, uint>)toInt)(value), paramName);
                    return value;
                case TypeCode.Int64:
                    ((EnumsCache<long>)cache).Validate(((Func<TEnum, long>)toInt)(value), paramName);
                    return value;
                case TypeCode.UInt64:
                    ((EnumsCache<ulong>)cache).Validate(((Func<TEnum, ulong>)toInt)(value), paramName);
                    return value;
                case TypeCode.SByte:
                    ((EnumsCache<sbyte>)cache).Validate(((Func<TEnum, sbyte>)toInt)(value), paramName);
                    return value;
                case TypeCode.Byte:
                    ((EnumsCache<byte>)cache).Validate(((Func<TEnum, byte>)toInt)(value), paramName);
                    return value;
                case TypeCode.Int16:
                    ((EnumsCache<short>)cache).Validate(((Func<TEnum, short>)toInt)(value), paramName);
                    return value;
                case TypeCode.UInt16:
                    ((EnumsCache<ushort>)cache).Validate(((Func<TEnum, ushort>)toInt)(value), paramName);
                    return value;
            }
            Debug.Fail("Unknown Enum TypeCode");
            return default(TEnum);
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to its equivalent string representation.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        [Pure]
        public static string AsString<[EnumConstraint] TEnum>(this TEnum value)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).AsString(((Func<TEnum, int>)toInt)(value));
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).AsString(((Func<TEnum, uint>)toInt)(value));
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).AsString(((Func<TEnum, long>)toInt)(value));
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).AsString(((Func<TEnum, ulong>)toInt)(value));
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).AsString(((Func<TEnum, sbyte>)toInt)(value));
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).AsString(((Func<TEnum, byte>)toInt)(value));
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).AsString(((Func<TEnum, short>)toInt)(value));
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).AsString(((Func<TEnum, ushort>)toInt)(value));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to its equivalent string representation according to the specified <paramref name="format"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"><paramref name="format"/> is an invalid value</exception>
        [Pure]
        public static string AsString<[EnumConstraint] TEnum>(this TEnum value, string format)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).AsString(((Func<TEnum, int>)toInt)(value), format);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).AsString(((Func<TEnum, uint>)toInt)(value), format);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).AsString(((Func<TEnum, long>)toInt)(value), format);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).AsString(((Func<TEnum, ulong>)toInt)(value), format);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).AsString(((Func<TEnum, sbyte>)toInt)(value), format);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).AsString(((Func<TEnum, byte>)toInt)(value), format);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).AsString(((Func<TEnum, short>)toInt)(value), format);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).AsString(((Func<TEnum, ushort>)toInt)(value), format);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to its equivalent string representation according to the specified <paramref name="formats"/>.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value"></param>
        /// <param name="formats"></param>
        /// <returns></returns>
        [Pure]
        public static string AsString<[EnumConstraint] TEnum>(this TEnum value, params EnumFormat[] formats)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).AsString(((Func<TEnum, int>)toInt)(value), formats);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).AsString(((Func<TEnum, uint>)toInt)(value), formats);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).AsString(((Func<TEnum, long>)toInt)(value), formats);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).AsString(((Func<TEnum, ulong>)toInt)(value), formats);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).AsString(((Func<TEnum, sbyte>)toInt)(value), formats);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).AsString(((Func<TEnum, byte>)toInt)(value), formats);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).AsString(((Func<TEnum, short>)toInt)(value), formats);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).AsString(((Func<TEnum, ushort>)toInt)(value), formats);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to its equivalent string representation according to the specified <paramref name="format"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="format"/> is null.</exception>
        /// <exception cref="FormatException"><paramref name="format"/> is an invalid value.</exception>
        [Pure]
        public static string Format<[EnumConstraint] TEnum>(this TEnum value, string format)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).Format(((Func<TEnum, int>)toInt)(value), format);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).Format(((Func<TEnum, uint>)toInt)(value), format);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).Format(((Func<TEnum, long>)toInt)(value), format);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).Format(((Func<TEnum, ulong>)toInt)(value), format);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).Format(((Func<TEnum, sbyte>)toInt)(value), format);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).Format(((Func<TEnum, byte>)toInt)(value), format);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).Format(((Func<TEnum, short>)toInt)(value), format);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).Format(((Func<TEnum, ushort>)toInt)(value), format);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        [Pure]
        public static string Format<[EnumConstraint] TEnum>(this TEnum value, EnumFormat format)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).Format(((Func<TEnum, int>)toInt)(value), format);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).Format(((Func<TEnum, uint>)toInt)(value), format);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).Format(((Func<TEnum, long>)toInt)(value), format);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).Format(((Func<TEnum, ulong>)toInt)(value), format);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).Format(((Func<TEnum, sbyte>)toInt)(value), format);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).Format(((Func<TEnum, byte>)toInt)(value), format);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).Format(((Func<TEnum, short>)toInt)(value), format);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).Format(((Func<TEnum, ushort>)toInt)(value), format);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        [Pure]
        public static string Format<[EnumConstraint] TEnum>(this TEnum value, EnumFormat format0, EnumFormat format1)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).Format(((Func<TEnum, int>)toInt)(value), format0, format1);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).Format(((Func<TEnum, uint>)toInt)(value), format0, format1);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).Format(((Func<TEnum, long>)toInt)(value), format0, format1);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).Format(((Func<TEnum, ulong>)toInt)(value), format0, format1);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).Format(((Func<TEnum, sbyte>)toInt)(value), format0, format1);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).Format(((Func<TEnum, byte>)toInt)(value), format0, format1);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).Format(((Func<TEnum, short>)toInt)(value), format0, format1);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).Format(((Func<TEnum, ushort>)toInt)(value), format0, format1);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        [Pure]
        public static string Format<[EnumConstraint] TEnum>(this TEnum value, EnumFormat format0, EnumFormat format1, EnumFormat format2)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).Format(((Func<TEnum, int>)toInt)(value), format0, format1, format2);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).Format(((Func<TEnum, uint>)toInt)(value), format0, format1, format2);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).Format(((Func<TEnum, long>)toInt)(value), format0, format1, format2);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).Format(((Func<TEnum, ulong>)toInt)(value), format0, format1, format2);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).Format(((Func<TEnum, sbyte>)toInt)(value), format0, format1, format2);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).Format(((Func<TEnum, byte>)toInt)(value), format0, format1, format2);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).Format(((Func<TEnum, short>)toInt)(value), format0, format1, format2);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).Format(((Func<TEnum, ushort>)toInt)(value), format0, format1, format2);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        [Pure]
        public static string Format<[EnumConstraint] TEnum>(this TEnum value, EnumFormat format0, EnumFormat format1, EnumFormat format2, EnumFormat format3)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).Format(((Func<TEnum, int>)toInt)(value), format0, format1, format2, format3);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).Format(((Func<TEnum, uint>)toInt)(value), format0, format1, format2, format3);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).Format(((Func<TEnum, long>)toInt)(value), format0, format1, format2, format3);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).Format(((Func<TEnum, ulong>)toInt)(value), format0, format1, format2, format3);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).Format(((Func<TEnum, sbyte>)toInt)(value), format0, format1, format2, format3);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).Format(((Func<TEnum, byte>)toInt)(value), format0, format1, format2, format3);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).Format(((Func<TEnum, short>)toInt)(value), format0, format1, format2, format3);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).Format(((Func<TEnum, ushort>)toInt)(value), format0, format1, format2, format3);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        [Pure]
        public static string Format<[EnumConstraint] TEnum>(this TEnum value, EnumFormat format0, EnumFormat format1, EnumFormat format2, EnumFormat format3, EnumFormat format4)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).Format(((Func<TEnum, int>)toInt)(value), format0, format1, format2, format3, format4);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).Format(((Func<TEnum, uint>)toInt)(value), format0, format1, format2, format3, format4);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).Format(((Func<TEnum, long>)toInt)(value), format0, format1, format2, format3, format4);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).Format(((Func<TEnum, ulong>)toInt)(value), format0, format1, format2, format3, format4);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).Format(((Func<TEnum, sbyte>)toInt)(value), format0, format1, format2, format3, format4);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).Format(((Func<TEnum, byte>)toInt)(value), format0, format1, format2, format3, format4);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).Format(((Func<TEnum, short>)toInt)(value), format0, format1, format2, format3, format4);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).Format(((Func<TEnum, ushort>)toInt)(value), format0, format1, format2, format3, format4);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Converts the specified <paramref name="value"/> to its equivalent string representation according to the specified <paramref name="formats"/>.
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="value"></param>
        /// <param name="formats"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="formats"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="formats"/> is empty.</exception>
        [Pure]
        public static string Format<[EnumConstraint] TEnum>(this TEnum value, params EnumFormat[] formats)
            where TEnum : struct
        {
            var cache = Enums<TEnum>.Cache;
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((EnumsCache<int>)cache).Format(((Func<TEnum, int>)toInt)(value), formats);
                case TypeCode.UInt32:
                    return ((EnumsCache<uint>)cache).Format(((Func<TEnum, uint>)toInt)(value), formats);
                case TypeCode.Int64:
                    return ((EnumsCache<long>)cache).Format(((Func<TEnum, long>)toInt)(value), formats);
                case TypeCode.UInt64:
                    return ((EnumsCache<ulong>)cache).Format(((Func<TEnum, ulong>)toInt)(value), formats);
                case TypeCode.SByte:
                    return ((EnumsCache<sbyte>)cache).Format(((Func<TEnum, sbyte>)toInt)(value), formats);
                case TypeCode.Byte:
                    return ((EnumsCache<byte>)cache).Format(((Func<TEnum, byte>)toInt)(value), formats);
                case TypeCode.Int16:
                    return ((EnumsCache<short>)cache).Format(((Func<TEnum, short>)toInt)(value), formats);
                case TypeCode.UInt16:
                    return ((EnumsCache<ushort>)cache).Format(((Func<TEnum, ushort>)toInt)(value), formats);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Returns an object with the enum's underlying value.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        [Pure]
        public static object GetUnderlyingValue<[EnumConstraint] TEnum>(this TEnum value)
            where TEnum : struct
        {
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((Func<TEnum, int>)toInt)(value);
                case TypeCode.UInt32:
                    return ((Func<TEnum, uint>)toInt)(value);
                case TypeCode.Int64:
                    return ((Func<TEnum, long>)toInt)(value);
                case TypeCode.UInt64:
                    return ((Func<TEnum, ulong>)toInt)(value);
                case TypeCode.SByte:
                    return ((Func<TEnum, sbyte>)toInt)(value);
                case TypeCode.Byte:
                    return ((Func<TEnum, byte>)toInt)(value);
                case TypeCode.Int16:
                    return ((Func<TEnum, short>)toInt)(value);
                case TypeCode.UInt16:
                    return ((Func<TEnum, ushort>)toInt)(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return null;
        }

        /// <summary>
        /// Converts <paramref name="value"/> to an <see cref="sbyte"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="OverflowException"><paramref name="value"/> cannot fit within <see cref="sbyte"/>'s value range without overflowing</exception>
        [Pure]
        [CLSCompliant(false)]
        public static sbyte ToSByte<[EnumConstraint] TEnum>(this TEnum value)
            where TEnum : struct
        {
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.ToSByte(((Func<TEnum, int>)toInt)(value));
                case TypeCode.UInt32:
                    return EnumsCache<uint>.ToSByte(((Func<TEnum, uint>)toInt)(value));
                case TypeCode.Int64:
                    return EnumsCache<long>.ToSByte(((Func<TEnum, long>)toInt)(value));
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.ToSByte(((Func<TEnum, ulong>)toInt)(value));
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.ToSByte(((Func<TEnum, sbyte>)toInt)(value));
                case TypeCode.Byte:
                    return EnumsCache<byte>.ToSByte(((Func<TEnum, byte>)toInt)(value));
                case TypeCode.Int16:
                    return EnumsCache<short>.ToSByte(((Func<TEnum, short>)toInt)(value));
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.ToSByte(((Func<TEnum, ushort>)toInt)(value));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return 0;
        }

        /// <summary>
        /// Converts <paramref name="value"/> to a <see cref="byte"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="OverflowException"><paramref name="value"/> cannot fit within <see cref="byte"/>'s value range without overflowing</exception>
        [Pure]
        public static byte ToByte<[EnumConstraint] TEnum>(this TEnum value)
            where TEnum : struct
        {
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.ToByte(((Func<TEnum, int>)toInt)(value));
                case TypeCode.UInt32:
                    return EnumsCache<uint>.ToByte(((Func<TEnum, uint>)toInt)(value));
                case TypeCode.Int64:
                    return EnumsCache<long>.ToByte(((Func<TEnum, long>)toInt)(value));
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.ToByte(((Func<TEnum, ulong>)toInt)(value));
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.ToByte(((Func<TEnum, sbyte>)toInt)(value));
                case TypeCode.Byte:
                    return EnumsCache<byte>.ToByte(((Func<TEnum, byte>)toInt)(value));
                case TypeCode.Int16:
                    return EnumsCache<short>.ToByte(((Func<TEnum, short>)toInt)(value));
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.ToByte(((Func<TEnum, ushort>)toInt)(value));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return 0;
        }

        /// <summary>
        /// Converts <paramref name="value"/> to an <see cref="short"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="OverflowException"><paramref name="value"/> cannot fit within <see cref="short"/>'s value range without overflowing</exception>
        [Pure]
        public static short ToInt16<[EnumConstraint] TEnum>(this TEnum value)
            where TEnum : struct
        {
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.ToInt16(((Func<TEnum, int>)toInt)(value));
                case TypeCode.UInt32:
                    return EnumsCache<uint>.ToInt16(((Func<TEnum, uint>)toInt)(value));
                case TypeCode.Int64:
                    return EnumsCache<long>.ToInt16(((Func<TEnum, long>)toInt)(value));
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.ToInt16(((Func<TEnum, ulong>)toInt)(value));
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.ToInt16(((Func<TEnum, sbyte>)toInt)(value));
                case TypeCode.Byte:
                    return EnumsCache<byte>.ToInt16(((Func<TEnum, byte>)toInt)(value));
                case TypeCode.Int16:
                    return EnumsCache<short>.ToInt16(((Func<TEnum, short>)toInt)(value));
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.ToInt16(((Func<TEnum, ushort>)toInt)(value));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return 0;
        }

        /// <summary>
        /// Converts <paramref name="value"/> to a <see cref="ushort"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="OverflowException"><paramref name="value"/> cannot fit within <see cref="ushort"/>'s value range without overflowing</exception>
        [Pure]
        [CLSCompliant(false)]
        public static ushort ToUInt16<[EnumConstraint] TEnum>(this TEnum value)
            where TEnum : struct
        {
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.ToUInt16(((Func<TEnum, int>)toInt)(value));
                case TypeCode.UInt32:
                    return EnumsCache<uint>.ToUInt16(((Func<TEnum, uint>)toInt)(value));
                case TypeCode.Int64:
                    return EnumsCache<long>.ToUInt16(((Func<TEnum, long>)toInt)(value));
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.ToUInt16(((Func<TEnum, ulong>)toInt)(value));
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.ToUInt16(((Func<TEnum, sbyte>)toInt)(value));
                case TypeCode.Byte:
                    return EnumsCache<byte>.ToUInt16(((Func<TEnum, byte>)toInt)(value));
                case TypeCode.Int16:
                    return EnumsCache<short>.ToUInt16(((Func<TEnum, short>)toInt)(value));
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.ToUInt16(((Func<TEnum, ushort>)toInt)(value));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return 0;
        }

        /// <summary>
        /// Converts <paramref name="value"/> to an <see cref="int"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="OverflowException"><paramref name="value"/> cannot fit within <see cref="int"/>'s value range without overflowing</exception>
        [Pure]
        public static int ToInt32<[EnumConstraint] TEnum>(this TEnum value)
            where TEnum : struct
        {
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.ToInt32(((Func<TEnum, int>)toInt)(value));
                case TypeCode.UInt32:
                    return EnumsCache<uint>.ToInt32(((Func<TEnum, uint>)toInt)(value));
                case TypeCode.Int64:
                    return EnumsCache<long>.ToInt32(((Func<TEnum, long>)toInt)(value));
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.ToInt32(((Func<TEnum, ulong>)toInt)(value));
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.ToInt32(((Func<TEnum, sbyte>)toInt)(value));
                case TypeCode.Byte:
                    return EnumsCache<byte>.ToInt32(((Func<TEnum, byte>)toInt)(value));
                case TypeCode.Int16:
                    return EnumsCache<short>.ToInt32(((Func<TEnum, short>)toInt)(value));
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.ToInt32(((Func<TEnum, ushort>)toInt)(value));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return 0;
        }

        /// <summary>
        /// Converts <paramref name="value"/> to a <see cref="uint"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="OverflowException"><paramref name="value"/> cannot fit within <see cref="uint"/>'s value range without overflowing</exception>
        [Pure]
        [CLSCompliant(false)]
        public static uint ToUInt32<[EnumConstraint] TEnum>(this TEnum value)
            where TEnum : struct
        {
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.ToUInt32(((Func<TEnum, int>)toInt)(value));
                case TypeCode.UInt32:
                    return EnumsCache<uint>.ToUInt32(((Func<TEnum, uint>)toInt)(value));
                case TypeCode.Int64:
                    return EnumsCache<long>.ToUInt32(((Func<TEnum, long>)toInt)(value));
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.ToUInt32(((Func<TEnum, ulong>)toInt)(value));
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.ToUInt32(((Func<TEnum, sbyte>)toInt)(value));
                case TypeCode.Byte:
                    return EnumsCache<byte>.ToUInt32(((Func<TEnum, byte>)toInt)(value));
                case TypeCode.Int16:
                    return EnumsCache<short>.ToUInt32(((Func<TEnum, short>)toInt)(value));
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.ToUInt32(((Func<TEnum, ushort>)toInt)(value));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return 0;
        }

        /// <summary>
        /// Converts <paramref name="value"/> to an <see cref="long"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="OverflowException"><paramref name="value"/> cannot fit within <see cref="long"/>'s value range without overflowing</exception>
        [Pure]
        public static long ToInt64<[EnumConstraint] TEnum>(this TEnum value)
            where TEnum : struct
        {
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.ToInt64(((Func<TEnum, int>)toInt)(value));
                case TypeCode.UInt32:
                    return EnumsCache<uint>.ToInt64(((Func<TEnum, uint>)toInt)(value));
                case TypeCode.Int64:
                    return EnumsCache<long>.ToInt64(((Func<TEnum, long>)toInt)(value));
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.ToInt64(((Func<TEnum, ulong>)toInt)(value));
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.ToInt64(((Func<TEnum, sbyte>)toInt)(value));
                case TypeCode.Byte:
                    return EnumsCache<byte>.ToInt64(((Func<TEnum, byte>)toInt)(value));
                case TypeCode.Int16:
                    return EnumsCache<short>.ToInt64(((Func<TEnum, short>)toInt)(value));
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.ToInt64(((Func<TEnum, ushort>)toInt)(value));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return 0;
        }

        /// <summary>
        /// Converts <paramref name="value"/> to a <see cref="ulong"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="OverflowException"><paramref name="value"/> cannot fit within <see cref="ulong"/>'s value range without overflowing</exception>
        [Pure]
        [CLSCompliant(false)]
        public static ulong ToUInt64<[EnumConstraint] TEnum>(this TEnum value)
            where TEnum : struct
        {
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return EnumsCache<int>.ToUInt64(((Func<TEnum, int>)toInt)(value));
                case TypeCode.UInt32:
                    return EnumsCache<uint>.ToUInt64(((Func<TEnum, uint>)toInt)(value));
                case TypeCode.Int64:
                    return EnumsCache<long>.ToUInt64(((Func<TEnum, long>)toInt)(value));
                case TypeCode.UInt64:
                    return EnumsCache<ulong>.ToUInt64(((Func<TEnum, ulong>)toInt)(value));
                case TypeCode.SByte:
                    return EnumsCache<sbyte>.ToUInt64(((Func<TEnum, sbyte>)toInt)(value));
                case TypeCode.Byte:
                    return EnumsCache<byte>.ToUInt64(((Func<TEnum, byte>)toInt)(value));
                case TypeCode.Int16:
                    return EnumsCache<short>.ToUInt64(((Func<TEnum, short>)toInt)(value));
                case TypeCode.UInt16:
                    return EnumsCache<ushort>.ToUInt64(((Func<TEnum, ushort>)toInt)(value));
            }
            Debug.Fail("Unknown Enum TypeCode");
            return 0;
        }

        [Pure]
        public static int GetHashCode<[EnumConstraint] TEnum>(TEnum value)
            where TEnum : struct
        {
            var toInt = Enums<TEnum>.ToInt;
            switch (Enums<TEnum>.TypeCode)
            {
                case TypeCode.Int32:
                    return ((Func<TEnum, int>)toInt)(value);
                case TypeCode.UInt32:
                    return ((Func<TEnum, uint>)toInt)(value).GetHashCode();
                case TypeCode.Int64:
                    return ((Func<TEnum, long>)toInt)(value).GetHashCode();
                case TypeCode.UInt64:
                    return ((Func<TEnum, ulong>)toInt)(value).GetHashCode();
                case TypeCode.SByte:
                    return ((Func<TEnum, sbyte>)toInt)(value);
                case TypeCode.Byte:
                    return ((Func<TEnum, byte>)toInt)(value);
                case TypeCode.Int16:
                    return ((Func<TEnum, short>)toInt)(value);
                case TypeCode.UInt16:
                    return ((Func<TEnum, ushort>)toInt)(value);
            }
            Debug.Fail("Unknown Enum TypeCode");
            return 0;
        }
        #endregion

        #region Defined Values Main Methods
        [Pure]
        public static EnumMemberInfo<TEnum> GetEnumMemberInfo<[EnumConstraint] TEnum>(this TEnum value) where TEnum : struct => Enums<TEnum>.Cache.GetEnumMemberInfo(value);

        [Pure]
        public static EnumMemberInfo<TEnum> GetEnumMemberInfo<[EnumConstraint] TEnum>(string name) where TEnum : struct => Enums<TEnum>.Cache.GetEnumMemberInfo(name);

        [Pure]
        public static EnumMemberInfo<TEnum> GetEnumMemberInfo<[EnumConstraint] TEnum>(string name, bool ignoreCase) where TEnum : struct => Enums<TEnum>.Cache.GetEnumMemberInfo(name, ignoreCase);

        /// <summary>
        /// Retrieves the name of the constant in <typeparamref name="TEnum"/> that has the specified <paramref name="value"/>. If <paramref name="value"/>
        /// is not defined null is returned.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Name of the constant in <typeparamref name="TEnum"/> that has the specified <paramref name="value"/>. If <paramref name="value"/>
        /// is not defined null is returned.</returns>
        [Pure]
        public static string GetName<[EnumConstraint] TEnum>(this TEnum value) where TEnum : struct => Enums<TEnum>.Cache.GetName(value);

        /// <summary>
        /// Retrieves the description of the constant in the enumeration that has the specified <paramref name="value"/>. If <paramref name="value"/>
        /// is not defined or no associated <see cref="DescriptionAttribute"/> is found then null is returned.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns>Description of the constant in the enumeration that has the specified <paramref name="value"/>. If <paramref name="value"/>
        /// is not defined or no associated <see cref="DescriptionAttribute"/> is found then null is returned.</returns>
        [Pure]
        public static string GetDescription<[EnumConstraint] TEnum>(this TEnum value) where TEnum : struct => Enums<TEnum>.Cache.GetDescription(value);

        [Pure]
        public static string GetDescriptionOrName<[EnumConstraint] TEnum>(this TEnum value) where TEnum : struct => Enums<TEnum>.Cache.GetDescriptionOrName(value);

        public static string GetDescriptionOrName<[EnumConstraint] TEnum>(this TEnum value, Func<string, string> nameFormatter) where TEnum : struct => Enums<TEnum>.Cache.GetDescriptionOrName(value, nameFormatter);
#endregion

        #region Attributes
        /// <summary>
        /// Indicates if the enumerated constant with the specified <paramref name="value"/> has a <typeparamref name="TAttribute"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="value"></param>
        /// <returns>Indication if the enumerated constant with the specified <paramref name="value"/> has a <typeparamref name="TAttribute"/>.</returns>
        [Pure]
        public static bool HasAttribute<[EnumConstraint] TEnum, TAttribute>(this TEnum value)
            where TAttribute : Attribute where TEnum : struct => Enums<TEnum>.Cache.HasAttribute<TAttribute>(value);

        /// <summary>
        /// Retrieves the <typeparamref name="TAttribute"/> if it exists of the enumerated constant with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="value"></param>
        /// <returns><typeparamref name="TAttribute"/> of the enumerated constant with the specified <paramref name="value"/> if defined and has attribute, else null</returns>
        [Pure]
        public static TAttribute GetAttribute<[EnumConstraint] TEnum, TAttribute>(this TEnum value)
            where TAttribute : Attribute where TEnum : struct => Enums<TEnum>.Cache.GetAttribute<TAttribute>(value);

        /// <summary>
        /// Retrieves the <typeparamref name="TAttribute"/> if it exists of the enumerated constant with the specified <paramref name="value"/>
        /// and then applies the <paramref name="selector"/> else it returns <paramref name="defaultValue"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="value"></param>
        /// <param name="selector"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null</exception>
        [Pure]
        public static TResult GetAttributeSelect<[EnumConstraint] TEnum, TAttribute, TResult>(this TEnum value, Func<TAttribute, TResult> selector, TResult defaultValue = default(TResult))
            where TAttribute : Attribute where TEnum : struct => Enums<TEnum>.Cache.GetAttributeSelect(value, selector, defaultValue);

        [Pure]
        public static bool TryGetAttributeSelect<[EnumConstraint] TEnum, TAttribute, TResult>(this TEnum value, Func<TAttribute, TResult> selector, out TResult result)
            where TAttribute : Attribute where TEnum : struct => Enums<TEnum>.Cache.TryGetAttributeSelect(value, selector, out result);

        /// <summary>
        /// Retrieves an array of <typeparamref name="TAttribute"/>'s of the constant in the enumeration that has the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="value"></param>
        /// <returns><typeparamref name="TAttribute"/> array</returns>
        [Pure]
        public static IEnumerable<TAttribute> GetAttributes<[EnumConstraint] TEnum, TAttribute>(this TEnum value)
            where TAttribute : Attribute where TEnum : struct => Enums<TEnum>.Cache.GetAttributes<TAttribute>(value);

        /// <summary>
        /// Retrieves an array of all the <see cref="Attribute"/>'s of the constant in the enumeration that has the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns><see cref="Attribute"/> array if value is defined, else null</returns>
        [Pure]
        public static Attribute[] GetAllAttributes<[EnumConstraint] TEnum>(this TEnum value) where TEnum : struct => Enums<TEnum>.Cache.GetAllAttributes(value);
#endregion

        #region Parsing
        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated constants
        /// to an equivalent enumerated object.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is either an empty string or only contains white space.
        /// -or-
        /// <paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying type of <typeparamref name="TEnum"/></exception>
        [Pure]
        public static TEnum Parse<[EnumConstraint] TEnum>(string value) where TEnum : struct => Enums<TEnum>.Cache.Parse(value);

        /// <summary>
        /// Converts the string representation of an enumerated constant using the given <paramref name="parseFormatOrder"/>.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="parseFormatOrder"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is either an empty string or only contains white space.
        /// -or-
        /// <paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying type of <typeparamref name="TEnum"/></exception>
        [Pure]
        public static TEnum Parse<[EnumConstraint] TEnum>(string value, params EnumFormat[] parseFormatOrder) where TEnum : struct => Enums<TEnum>.Cache.Parse(value, parseFormatOrder);

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated constants
        /// to an equivalent enumerated object. A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is either an empty string or only contains white space.
        /// -or-
        /// <paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying type of <typeparamref name="TEnum"/></exception>
        [Pure]
        public static TEnum Parse<[EnumConstraint] TEnum>(string value, bool ignoreCase) where TEnum : struct => Enums<TEnum>.Cache.Parse(value, ignoreCase);

        /// <summary>
        /// Converts the string representation of an enumerated constant using the given <paramref name="parseFormatOrder"/>.
        /// A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="ignoreCase"></param>
        /// <param name="parseFormatOrder"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is either an empty string or only contains white space.
        /// -or-
        /// <paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.</exception>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying type of <typeparamref name="TEnum"/></exception>
        [Pure]
        public static TEnum Parse<[EnumConstraint] TEnum>(string value, bool ignoreCase, params EnumFormat[] parseFormatOrder) where TEnum : struct => Enums<TEnum>.Cache.Parse(value, ignoreCase, parseFormatOrder);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated
        /// constants to an equivalent enumerated object but if it fails returns the specified default enumerated value.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="defaultEnum"></param>
        /// <returns></returns>
        [Pure]
        public static TEnum ParseOrDefault<[EnumConstraint] TEnum>(string value, TEnum defaultEnum) where TEnum : struct => Enums<TEnum>.Cache.ParseOrDefault(value, defaultEnum);

        /// <summary>
        /// Tries to convert the string representation of an enumerated constant using the given <paramref name="parseFormatOrder"/>
        /// but if it fails returns the specified default enumerated value.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="defaultEnum"></param>
        /// <param name="parseFormatOrder"></param>
        /// <returns></returns>
        [Pure]
        public static TEnum ParseOrDefault<[EnumConstraint] TEnum>(string value, TEnum defaultEnum, params EnumFormat[] parseFormatOrder) where TEnum : struct => Enums<TEnum>.Cache.ParseOrDefault(value, defaultEnum, parseFormatOrder);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated
        /// constants to an equivalent enumerated object but if it fails returns the specified default enumerated value.
        /// A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="ignoreCase"></param>
        /// <param name="defaultEnum"></param>
        /// <returns></returns>
        [Pure]
        public static TEnum ParseOrDefault<[EnumConstraint] TEnum>(string value, bool ignoreCase, TEnum defaultEnum) where TEnum : struct => Enums<TEnum>.Cache.ParseOrDefault(value, ignoreCase, defaultEnum);

        /// <summary>
        /// Tries to convert the string representation of an enumerated constant using the given <paramref name="parseFormatOrder"/>
        /// but if it fails returns the specified default enumerated value. A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="ignoreCase"></param>
        /// <param name="defaultEnum"></param>
        /// <param name="parseFormatOrder"></param>
        /// <returns></returns>
        [Pure]
        public static TEnum ParseOrDefault<[EnumConstraint] TEnum>(string value, bool ignoreCase, TEnum defaultEnum, params EnumFormat[] parseFormatOrder) where TEnum : struct => Enums<TEnum>.Cache.ParseOrDefault(value, ignoreCase, defaultEnum, parseFormatOrder);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated
        /// constants to an equivalent enumerated object. The return value indicates whether the conversion succeeded.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [Pure]
        public static bool TryParse<[EnumConstraint] TEnum>(string value, out TEnum result) where TEnum : struct => Enums<TEnum>.Cache.TryParse(value, out result);

        /// <summary>
        /// Tries to convert the string representation of an enumerated constant using the given <paramref name="parseFormatOrder"/>.
        /// The return value indicates whether the conversion succeeded.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <param name="parseFormatOrder"></param>
        /// <returns></returns>
        [Pure]
        public static bool TryParse<[EnumConstraint] TEnum>(string value, out TEnum result, params EnumFormat[] parseFormatOrder) where TEnum : struct => Enums<TEnum>.Cache.TryParse(value, out result, parseFormatOrder);

        /// <summary>
        /// Tries to convert the string representation of the name or numeric value of one or more enumerated
        /// constants to an equivalent enumerated object. The return value indicates whether the conversion succeeded.
        /// A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="ignoreCase"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [Pure]
        public static bool TryParse<[EnumConstraint] TEnum>(string value, bool ignoreCase, out TEnum result) where TEnum : struct => Enums<TEnum>.Cache.TryParse(value, ignoreCase, out result);

        /// <summary>
        /// Tries to convert the string representation of an enumerated constant using the given <paramref name="parseFormatOrder"/>.
        /// The return value indicates whether the conversion succeeded. A parameter specifies whether the operation is case-insensitive.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value"></param>
        /// <param name="ignoreCase"></param>
        /// <param name="result"></param>
        /// <param name="parseFormatOrder"></param>
        /// <returns></returns>
        [Pure]
        public static bool TryParse<[EnumConstraint] TEnum>(string value, bool ignoreCase, out TEnum result, params EnumFormat[] parseFormatOrder) where TEnum : struct => Enums<TEnum>.Cache.TryParse(value, ignoreCase, out result, parseFormatOrder);
#endregion

        #region Internal Methods
        internal static string DescriptionEnumFormatter(IClsEnumMemberInfo info) => info.Description;

        internal static string GetDescription(Attribute[] attributes)
        {
            return attributes.Length > 0 ? (attributes[0] as DescriptionAttribute)?.Description : null;
        }

        internal static TAttribute GetAttribute<TAttribute>(Attribute[] attributes)
            where TAttribute : Attribute
        {
            foreach (var attribute in attributes)
            {
                var castedAttr = attribute as TAttribute;
                if (castedAttr != null)
                {
                    return castedAttr;
                }
            }
            return null;
        }

        internal static IEnumerable<TAttribute> GetAttributes<TAttribute>(Attribute[] attributes)
            where TAttribute : Attribute
        {
            foreach (var attribute in attributes)
            {
                var castedAttr = attribute as TAttribute;
                if (castedAttr != null)
                {
                    yield return castedAttr;
                }
            }
        }

        internal static bool IsNumeric(string value)
        {
            var firstChar = value[0];
            return char.IsDigit(firstChar) || firstChar == '-' || firstChar == '+';
        }

        internal static OverflowException GetOverflowException() => new OverflowException("value is outside the underlying type's value range");
#endregion
    }

    internal static class Enums<TEnum>
    {
        internal static readonly object Cache;

        internal static readonly Delegate ToEnum;

        internal static readonly Delegate ToInt;

        internal static readonly TypeCode TypeCode;

        static Enums()
        {
            var enumType = typeof(TEnum);
            Debug.Assert(enumType.IsEnum);
            var underlyingType = Enum.GetUnderlyingType(enumType);
            TypeCode = Type.GetTypeCode(underlyingType);

#if NET20 || USE_EMIT
            var toInt = new DynamicMethod(enumType.Name + "_ToInt",
                                       underlyingType,
                                       new[] { enumType },
                                       enumType, true);
            var toIntGenerator = toInt.GetILGenerator();
            toIntGenerator.DeclareLocal(underlyingType);
            toIntGenerator.Emit(OpCodes.Ldarg_0);
            toIntGenerator.Emit(OpCodes.Stloc_0);
            toIntGenerator.Emit(OpCodes.Ldloc_0);
            toIntGenerator.Emit(OpCodes.Ret);
            ToInt = toInt.CreateDelegate(typeof(Func<,>).MakeGenericType(enumType, underlyingType));

            var toEnum = new DynamicMethod(underlyingType.Name + "_ToEnum",
                                       enumType,
                                       new[] { underlyingType },
                                       underlyingType, true);
            var toEnumGenerator = toEnum.GetILGenerator();
            toEnumGenerator.DeclareLocal(enumType);
            toEnumGenerator.Emit(OpCodes.Ldarg_0);
            toEnumGenerator.Emit(OpCodes.Stloc_0);
            toEnumGenerator.Emit(OpCodes.Ldloc_0);
            toEnumGenerator.Emit(OpCodes.Ret);
            ToEnum = toEnum.CreateDelegate(typeof(Func<,>).MakeGenericType(underlyingType, enumType));
#else
            var enumParam = Expression.Parameter(enumType, "x");
            var enumParamConvert = Expression.Convert(enumParam, underlyingType);
            ToInt = Expression.Lambda(enumParamConvert, enumParam).Compile();
            var intParam = Expression.Parameter(underlyingType, "y");
            var intParamConvert = Expression.Convert(intParam, enumType);
            ToEnum = Expression.Lambda(intParamConvert, intParam).Compile();
#endif

            switch (TypeCode)
            {
                case TypeCode.Int32:
                    Cache = new EnumsCache<int>(enumType);
                    break;
                case TypeCode.UInt32:
                    Cache = new EnumsCache<uint>(enumType);
                    break;
                case TypeCode.Int64:
                    Cache = new EnumsCache<long>(enumType);
                    break;
                case TypeCode.UInt64:
                    Cache = new EnumsCache<ulong>(enumType);
                    break;
                case TypeCode.SByte:
                    Cache = new EnumsCache<sbyte>(enumType);
                    break;
                case TypeCode.Byte:
                    Cache = new EnumsCache<byte>(enumType);
                    break;
                case TypeCode.Int16:
                    Cache = new EnumsCache<short>(enumType);
                    break;
                case TypeCode.UInt16:
                    Cache = new EnumsCache<ushort>(enumType);
                    break;
            }
        }
    }
}