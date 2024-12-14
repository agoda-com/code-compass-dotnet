using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Agoda.CodeCompass.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Agoda.CodeCompass.Data;

public static class TechDebtMetadata
{
    private static readonly ConcurrentDictionary<string, TechDebtInfo> AnalyzerMetadata = new();

    internal static readonly Dictionary<string, TechDebtInfo> PredefinedMetadata = new()
    {
        // Agoda Rules
        ["AG0002"] = new TechDebtInfo { Minutes = 15, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow Agoda's implementation guidelines" },
        ["AG0003"] = new TechDebtInfo { Minutes = 20, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow Agoda's implementation guidelines" },
        ["AG0005"] = new TechDebtInfo { Minutes = 1, Category = "AgodaSpecific", Priority = "High", Rationale = "Agoda-specific architecture violation", Recommendation = "Restructure according to architecture guidelines" },
        ["AG0009"] = new TechDebtInfo { Minutes = 15, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific naming violation", Recommendation = "Follow naming conventions" },
        ["AG0010"] = new TechDebtInfo { Minutes = 20, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },
        ["AG0011"] = new TechDebtInfo { Minutes = 15, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },
        ["AG0012"] = new TechDebtInfo { Minutes = 20, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific naming violation", Recommendation = "Follow naming conventions" },
        ["AG0013"] = new TechDebtInfo { Minutes = 25, Category = "AgodaSpecific", Priority = "High", Rationale = "Agoda-specific architecture violation", Recommendation = "Restructure according to architecture guidelines" },
        ["AG0018"] = new TechDebtInfo { Minutes = 20, Category = "AgodaSpecific", Priority = "High", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },
        ["AG0019"] = new TechDebtInfo { Minutes = 15, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },
        ["AG0020"] = new TechDebtInfo { Minutes = 20, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },
        ["AG0021"] = new TechDebtInfo { Minutes = 25, Category = "AgodaSpecific", Priority = "High", Rationale = "Agoda-specific architecture violation", Recommendation = "Restructure according to architecture guidelines" },
        ["AG0022"] = new TechDebtInfo { Minutes = 20, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },
        ["AG0023"] = new TechDebtInfo { Minutes = 15, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },
        ["AG0024"] = new TechDebtInfo { Minutes = 20, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },
        ["AG0026"] = new TechDebtInfo { Minutes = 25, Category = "AgodaSpecific", Priority = "High", Rationale = "Agoda-specific architecture violation", Recommendation = "Restructure according to architecture guidelines" },
        ["AG0032"] = new TechDebtInfo { Minutes = 20, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },
        ["AG0033"] = new TechDebtInfo { Minutes = 15, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },
        ["AG0035"] = new TechDebtInfo { Minutes = 20, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },
        ["AG0038"] = new TechDebtInfo { Minutes = 25, Category = "AgodaSpecific", Priority = "High", Rationale = "Agoda-specific architecture violation", Recommendation = "Restructure according to architecture guidelines" },
        ["AG0041"] = new TechDebtInfo { Minutes = 20, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },
        ["AG0042"] = new TechDebtInfo { Minutes = 15, Category = "AgodaSpecific", Priority = "Medium", Rationale = "Agoda-specific implementation issue", Recommendation = "Follow implementation guidelines" },

        // Compiler Diagnostics
        ["CS0105"] = new TechDebtInfo { Minutes = 5, Category = "Compiler", Priority = "Low", Rationale = "Duplicate using directive", Recommendation = "Remove duplicate using directive" },
        ["CS0108"] = new TechDebtInfo { Minutes = 15, Category = "Compiler", Priority = "Medium", Rationale = "Missing new keyword on hiding member", Recommendation = "Add new keyword or rename member" },
        ["CS0109"] = new TechDebtInfo { Minutes = 10, Category = "Compiler", Priority = "Low", Rationale = "Unnecessary new keyword", Recommendation = "Remove unnecessary new keyword" },
        ["CS0162"] = new TechDebtInfo { Minutes = 10, Category = "Compiler", Priority = "Low", Rationale = "Unreachable code detected", Recommendation = "Remove or fix unreachable code" },
        ["CS0164"] = new TechDebtInfo { Minutes = 5, Category = "Compiler", Priority = "Low", Rationale = "Label not referenced", Recommendation = "Remove unused label" },
        ["CS0168"] = new TechDebtInfo { Minutes = 5, Category = "Compiler", Priority = "Low", Rationale = "Variable declared but not used", Recommendation = "Remove unused variable" },
        ["CS0169"] = new TechDebtInfo { Minutes = 5, Category = "Compiler", Priority = "Low", Rationale = "Field never used", Recommendation = "Remove unused field" },
        ["CS0219"] = new TechDebtInfo { Minutes = 5, Category = "Compiler", Priority = "Low", Rationale = "Variable assigned but not used", Recommendation = "Remove unused variable assignment" },
        ["CS0414"] = new TechDebtInfo { Minutes = 5, Category = "Compiler", Priority = "Low", Rationale = "Field assigned but not used", Recommendation = "Remove unused field assignment" },
        ["CS0472"] = new TechDebtInfo { Minutes = 15, Category = "Compiler", Priority = "Medium", Rationale = "Result of expression always same", Recommendation = "Fix or simplify expression" },
        ["CS0612"] = new TechDebtInfo { Minutes = 20, Category = "Compiler", Priority = "High", Rationale = "Obsolete type or member usage", Recommendation = "Update to non-obsolete alternative" },
        ["CS0618"] = new TechDebtInfo { Minutes = 20, Category = "Compiler", Priority = "High", Rationale = "Obsolete type or member usage", Recommendation = "Update to non-obsolete alternative" },
        ["CS0628"] = new TechDebtInfo { Minutes = 15, Category = "Compiler", Priority = "Medium", Rationale = "New protected member in sealed class", Recommendation = "Change protection level or unseal class" },
        ["CS0649"] = new TechDebtInfo { Minutes = 10, Category = "Compiler", Priority = "Low", Rationale = "Field never assigned", Recommendation = "Initialize field or remove if unused" },
        ["CS0659"] = new TechDebtInfo { Minutes = 30, Category = "Compiler", Priority = "High", Rationale = "Overriding Equals requires GetHashCode override", Recommendation = "Implement proper GetHashCode override" },
        ["CS1030"] = new TechDebtInfo { Minutes = 5, Category = "Compiler", Priority = "Low", Rationale = "Directive not valid", Recommendation = "Fix or remove invalid directive" },
        ["CS1572"] = new TechDebtInfo { Minutes = 10, Category = "Compiler", Priority = "Low", Rationale = "XML comment has param tag for non-existent parameter", Recommendation = "Update XML documentation" },
        ["CS1573"] = new TechDebtInfo { Minutes = 10, Category = "Compiler", Priority = "Low", Rationale = "Missing XML comment for parameter", Recommendation = "Add parameter documentation" },
        ["CS1634"] = new TechDebtInfo { Minutes = 15, Category = "Compiler", Priority = "Medium", Rationale = "Assembly reference issue", Recommendation = "Fix assembly reference" },
        ["CS1696"] = new TechDebtInfo { Minutes = 20, Category = "Compiler", Priority = "Medium", Rationale = "Single-file compilation issue", Recommendation = "Fix single-file compilation setup" },
        ["CS4014"] = new TechDebtInfo { Minutes = 20, Category = "AsyncAwait", Priority = "High", Rationale = "Async method called without await", Recommendation = "Add await or handle Task explicitly" },
        ["CS8073"] = new TechDebtInfo { Minutes = 15, Category = "Compiler", Priority = "Medium", Rationale = "The result of the expression is always null", Recommendation = "Fix expression logic" },
        ["CS8600"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Converting null literal or possible null value", Recommendation = "Add null check or use appropriate null-handling" },
        ["CS8601"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Possible null reference assignment", Recommendation = "Add null check or ensure non-null value" },
        ["CS8602"] = new TechDebtInfo { Minutes = 20, Category = "NullableReference", Priority = "High", Rationale = "Dereference of possible null reference", Recommendation = "Add null check before dereferencing" },
        ["CS8603"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Possible null reference return", Recommendation = "Ensure non-null return or change return type" },
        ["CS8604"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Possible null reference argument", Recommendation = "Ensure non-null argument or add null check" },
        ["CS8605"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Unboxing possibly null value", Recommendation = "Add null check before unboxing" },
        ["CS8609"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Nullability mismatch in interface implementation", Recommendation = "Fix nullability annotations" },
        ["CS8613"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Nullability mismatch in method override", Recommendation = "Fix nullability annotations" },
        ["CS8619"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Nullability mismatch in generic type usage", Recommendation = "Fix generic type constraints or usage" },
        ["CS8620"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Generic type argument nullability mismatch", Recommendation = "Fix generic type argument nullability" },
        ["CS8622"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Nullability mismatch in delegate creation", Recommendation = "Fix delegate nullability annotations" },
        ["CS8625"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Cannot convert null literal to non-nullable type", Recommendation = "Use non-null value or change type to nullable" },
        ["CS8629"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Nullable value type may be null", Recommendation = "Add null check before value type usage" },
        ["CS8632"] = new TechDebtInfo { Minutes = 10, Category = "NullableReference", Priority = "Low", Rationale = "Nullable annotation context mismatch", Recommendation = "Fix nullable annotation context" },
        ["CS8669"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Nullability attribute mismatch", Recommendation = "Fix nullability attributes" },
        ["CS8714"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Generic type constraint nullability mismatch", Recommendation = "Fix generic type constraints" },
        ["CS8767"] = new TechDebtInfo { Minutes = 15, Category = "NullableReference", Priority = "Medium", Rationale = "Nullability mismatch in interface implementation", Recommendation = "Fix interface implementation nullability" },
        ["CS8981"] = new TechDebtInfo { Minutes = 10, Category = "Compiler", Priority = "Low", Rationale = "Type name only contains lower-cased ascii characters", Recommendation = "Use PascalCase for type names" },
        ["CS9107"] = new TechDebtInfo { Minutes = 15, Category = "Compiler", Priority = "Medium", Rationale = "Pattern matching issue", Recommendation = "Fix pattern matching logic" },
        ["CS9113"] = new TechDebtInfo { Minutes = 15, Category = "Compiler", Priority = "Medium", Rationale = "Parameter issue in primary constructor", Recommendation = "Fix primary constructor parameter" },

        // Code Analysis Rules
        ["CA1000"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "Do not declare static members on generic types", Recommendation = "Move static members to non-generic base class" },
        ["CA1001"] = new TechDebtInfo { Minutes = 25, Category = "Reliability", Priority = "High", Rationale = "Types that own disposable fields should be disposable", Recommendation = "Implement IDisposable pattern" },
        ["CA1002"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "Do not expose generic lists", Recommendation = "Use IReadOnlyList or IList interface instead" },
        ["CA1008"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "Enums should have zero value", Recommendation = "Add zero value to enum" },
        ["CA1010"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "Collections should implement generic interface", Recommendation = "Implement generic collection interface" },
        ["CA1018"] = new TechDebtInfo { Minutes = 10, Category = "Design", Priority = "Low", Rationale = "Mark attributes with AttributeUsageAttribute", Recommendation = "Add AttributeUsage attribute" },
        ["CA1019"] = new TechDebtInfo { Minutes = 15, Category = "Design", Priority = "Medium", Rationale = "Define accessors for attribute arguments", Recommendation = "Add property accessors for attribute arguments" },
        ["CA1024"] = new TechDebtInfo { Minutes = 15, Category = "Design", Priority = "Medium", Rationale = "Use properties where appropriate", Recommendation = "Convert method to property if it's a simple getter" },
        ["CA1027"] = new TechDebtInfo { Minutes = 15, Category = "Design", Priority = "Medium", Rationale = "Mark enums with FlagsAttribute", Recommendation = "Add Flags attribute to bitfield enums" },
        ["CA1032"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "Implement standard exception constructors", Recommendation = "Add standard exception constructors" },
        ["CA1033"] = new TechDebtInfo { Minutes = 25, Category = "Design", Priority = "Medium", Rationale = "Interface methods should be callable by child types", Recommendation = "Make interface methods accessible to child types" },
        ["CA1034"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "Nested types should not be visible", Recommendation = "Move nested type to top level" },
        ["CA1040"] = new TechDebtInfo { Minutes = 15, Category = "Design", Priority = "Low", Rationale = "Avoid empty interfaces", Recommendation = "Add members or remove interface" },
        ["CA1041"] = new TechDebtInfo { Minutes = 15, Category = "Design", Priority = "Medium", Rationale = "Provide ObsoleteAttribute message", Recommendation = "Add message to Obsolete attribute" },
        ["CA1044"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "Properties should not be write only", Recommendation = "Add getter or convert to method" },
        ["CA1050"] = new TechDebtInfo { Minutes = 10, Category = "Design", Priority = "Low", Rationale = "Declare types in namespaces", Recommendation = "Move type into a namespace" },
        ["CA1051"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "Do not declare visible instance fields", Recommendation = "Convert field to property" },
        ["CA1052"] = new TechDebtInfo { Minutes = 15, Category = "Design", Priority = "Medium", Rationale = "Static holder types should be Static or NotInheritable", Recommendation = "Make type static or sealed" },
        ["CA1054"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "URI parameters should not be strings", Recommendation = "Use Uri type for URI parameters" },
        ["CA1055"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "URI return values should not be strings", Recommendation = "Return Uri type instead of string" },
        ["CA1056"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "URI properties should not be strings", Recommendation = "Use Uri type for URI properties" },
        ["CA1063"] = new TechDebtInfo { Minutes = 30, Category = "Design", Priority = "High", Rationale = "Implement IDisposable correctly", Recommendation = "Follow dispose pattern guidelines" },
        ["CA1064"] = new TechDebtInfo { Minutes = 15, Category = "Design", Priority = "Medium", Rationale = "Exceptions should be public", Recommendation = "Make exception type public" },
        ["CA1066"] = new TechDebtInfo { Minutes = 25, Category = "Design", Priority = "Medium", Rationale = "Implement IEquatable when overriding Equals", Recommendation = "Implement IEquatable<T>" },
        ["CA1068"] = new TechDebtInfo { Minutes = 15, Category = "Design", Priority = "Medium", Rationale = "CancellationToken parameters should come last", Recommendation = "Move CancellationToken to last parameter" },

        // Globalization Rules
        ["CA1303"] = new TechDebtInfo { Minutes = 25, Category = "Globalization", Priority = "Medium", Rationale = "Do not pass literals as localized parameters", Recommendation = "Use resource strings" },
        ["CA1304"] = new TechDebtInfo { Minutes = 20, Category = "Globalization", Priority = "Medium", Rationale = "Specify CultureInfo", Recommendation = "Add explicit culture info" },
        ["CA1305"] = new TechDebtInfo { Minutes = 20, Category = "Globalization", Priority = "Medium", Rationale = "Specify IFormatProvider", Recommendation = "Add explicit format provider" },
        ["CA1307"] = new TechDebtInfo { Minutes = 20, Category = "Globalization", Priority = "Medium", Rationale = "Specify StringComparison for clarity", Recommendation = "Add explicit string comparison" },
        ["CA1308"] = new TechDebtInfo { Minutes = 15, Category = "Globalization", Priority = "Medium", Rationale = "Normalize strings to uppercase", Recommendation = "Use ToUpperInvariant instead of ToLowerInvariant" },
        ["CA1309"] = new TechDebtInfo { Minutes = 15, Category = "Globalization", Priority = "Medium", Rationale = "Use ordinal string comparison", Recommendation = "Use StringComparison.Ordinal" },
        ["CA1310"] = new TechDebtInfo { Minutes = 15, Category = "Globalization", Priority = "Medium", Rationale = "Specify StringComparison for correctness", Recommendation = "Add explicit string comparison" },
        ["CA1311"] = new TechDebtInfo { Minutes = 15, Category = "Globalization", Priority = "Medium", Rationale = "Specify a culture or use invariant culture", Recommendation = "Add culture specification" },

        // Performance Rules
        ["CA1802"] = new TechDebtInfo { Minutes = 10, Category = "Performance", Priority = "Medium", Rationale = "Use literals where appropriate", Recommendation = "Use literal instead of computed value" },
        ["CA1805"] = new TechDebtInfo { Minutes = 10, Category = "Performance", Priority = "Low", Rationale = "Do not initialize unnecessarily", Recommendation = "Remove unnecessary initialization" },
        ["CA1806"] = new TechDebtInfo { Minutes = 15, Category = "Performance", Priority = "Medium", Rationale = "Do not ignore method results", Recommendation = "Use or check method results" },
        ["CA1810"] = new TechDebtInfo { Minutes = 20, Category = "Performance", Priority = "Medium", Rationale = "Initialize reference type static fields inline", Recommendation = "Move initialization inline" },
        ["CA1812"] = new TechDebtInfo { Minutes = 15, Category = "Performance", Priority = "Low", Rationale = "Avoid uninstantiated internal classes", Recommendation = "Remove unused class or add usage" },
        ["CA1813"] = new TechDebtInfo { Minutes = 15, Category = "Performance", Priority = "Medium", Rationale = "Avoid unsealed attributes", Recommendation = "Seal attribute classes" },
        ["CA1815"] = new TechDebtInfo { Minutes = 20, Category = "Performance", Priority = "Medium", Rationale = "Override equals and operator equals on value types", Recommendation = "Implement equality members" },
        ["CA1816"] = new TechDebtInfo { Minutes = 25, Category = "Performance", Priority = "High", Rationale = "Call GC.SuppressFinalize correctly", Recommendation = "Add or fix GC.SuppressFinalize call" },
        ["CA1819"] = new TechDebtInfo { Minutes = 25, Category = "Performance", Priority = "Medium", Rationale = "Properties should not return arrays", Recommendation = "Return array copy or use different type" },
        ["CA1820"] = new TechDebtInfo { Minutes = 15, Category = "Performance", Priority = "Medium", Rationale = "Test for empty strings using string length", Recommendation = "Use Length instead of comparison" },
        ["CA1821"] = new TechDebtInfo { Minutes = 20, Category = "Performance", Priority = "Medium", Rationale = "Remove empty finalizers", Recommendation = "Remove unnecessary finalizer" },
        ["CA1822"] = new TechDebtInfo { Minutes = 15, Category = "Performance", Priority = "Medium", Rationale = "Mark members as static", Recommendation = "Add static modifier where appropriate" },
        ["CA1823"] = new TechDebtInfo { Minutes = 15, Category = "Performance", Priority = "Medium", Rationale = "Avoid unused private fields", Recommendation = "Remove unused private fields" },
        ["CA1824"] = new TechDebtInfo { Minutes = 15, Category = "Performance", Priority = "Medium", Rationale = "Mark assemblies with NeutralResourcesLanguageAttribute", Recommendation = "Add NeutralResourcesLanguage attribute" },
        ["CA1825"] = new TechDebtInfo { Minutes = 15, Category = "Performance", Priority = "Medium", Rationale = "Avoid zero-length array allocations", Recommendation = "Use Array.Empty<T>()" },

        // Naming Rules
        ["CA1707"] = new TechDebtInfo { Minutes = 10, Category = "Naming", Priority = "Low", Rationale = "Remove underscores from member names", Recommendation = "Use PascalCase naming" },
        ["CA1708"] = new TechDebtInfo { Minutes = 15, Category = "Naming", Priority = "Medium", Rationale = "Names should differ by more than case", Recommendation = "Rename to avoid confusion" },
        ["CA1710"] = new TechDebtInfo { Minutes = 15, Category = "Naming", Priority = "Low", Rationale = "Identifiers should have correct suffix", Recommendation = "Add appropriate suffix" },
        ["CA1711"] = new TechDebtInfo { Minutes = 15, Category = "Naming", Priority = "Low", Rationale = "Identifiers should not have incorrect suffix", Recommendation = "Remove or change incorrect suffix" },
        ["CA1715"] = new TechDebtInfo { Minutes = 15, Category = "Naming", Priority = "Low", Rationale = "Identifiers should have correct prefix", Recommendation = "Add appropriate prefix" },
        ["CA1716"] = new TechDebtInfo { Minutes = 20, Category = "Naming", Priority = "Medium", Rationale = "Identifiers should not match keywords", Recommendation = "Rename to avoid keyword conflict" },
        ["CA1720"] = new TechDebtInfo { Minutes = 15, Category = "Naming", Priority = "Low", Rationale = "Identifiers should not contain type names", Recommendation = "Remove type name from identifier" },
        ["CA1724"] = new TechDebtInfo { Minutes = 20, Category = "Naming", Priority = "Medium", Rationale = "Type names should not match namespaces", Recommendation = "Rename type or use different namespace" },
        ["CA1725"] = new TechDebtInfo { Minutes = 15, Category = "Naming", Priority = "Medium", Rationale = "Parameter names should match base declaration", Recommendation = "Match parameter names with base class" },

        // NUnit Rules
        ["NUnit1001"] = new TechDebtInfo { Minutes = 15, Category = "Testing", Priority = "Medium", Rationale = "Test class missing test attribute", Recommendation = "Add TestFixture attribute" },
        ["NUnit1002"] = new TechDebtInfo { Minutes = 10, Category = "Testing", Priority = "Low", Rationale = "Test method missing test attribute", Recommendation = "Add Test attribute" },
        ["NUnit1028"] = new TechDebtInfo { Minutes = 15, Category = "Testing", Priority = "Medium", Rationale = "Test method parameter issue", Recommendation = "Fix test method parameters" },
        ["NUnit1032"] = new TechDebtInfo { Minutes = 15, Category = "Testing", Priority = "Medium", Rationale = "Test attribute usage issue", Recommendation = "Fix test attribute usage" },
        ["NUnit2001"] = new TechDebtInfo { Minutes = 10, Category = "Testing", Priority = "Low", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2002"] = new TechDebtInfo { Minutes = 10, Category = "Testing", Priority = "Low", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2003"] = new TechDebtInfo { Minutes = 10, Category = "Testing", Priority = "Low", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2004"] = new TechDebtInfo { Minutes = 10, Category = "Testing", Priority = "Low", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2005"] = new TechDebtInfo { Minutes = 10, Category = "Testing", Priority = "Low", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2007"] = new TechDebtInfo { Minutes = 10, Category = "Testing", Priority = "Low", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2009"] = new TechDebtInfo { Minutes = 15, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2010"] = new TechDebtInfo { Minutes = 15, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2011"] = new TechDebtInfo { Minutes = 15, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2012"] = new TechDebtInfo { Minutes = 15, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2014"] = new TechDebtInfo { Minutes = 15, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2016"] = new TechDebtInfo { Minutes = 15, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2017"] = new TechDebtInfo { Minutes = 15, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2018"] = new TechDebtInfo { Minutes = 15, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2019"] = new TechDebtInfo { Minutes = 15, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2027"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2030"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2035"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2036"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2037"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2038"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2039"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },
        ["NUnit2043"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Use ComparisonConstraint for better assertions", Recommendation = "Convert to constraint-based assertion" },
        ["NUnit2045"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Use Assert.That with constraints", Recommendation = "Convert to constraint-based assertion" },
        ["NUnit2046"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Use Assert.That with constraints", Recommendation = "Convert to constraint-based assertion" },
        ["NUnit2049"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Consider using Assert.That", Recommendation = "Convert to Assert.That usage" },

        // SonarQube Rules
        ["S101"] = new TechDebtInfo { Minutes = 10, Category = "Convention", Priority = "Low", Rationale = "Class naming convention violation", Recommendation = "Rename class to match conventions" },
        ["S108"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Empty block should be documented", Recommendation = "Add comment explaining empty block" },
        ["S112"] = new TechDebtInfo { Minutes = 20, Category = "ErrorHandling", Priority = "High", Rationale = "General exceptions should not be thrown", Recommendation = "Use more specific exception type" },
        ["S1066"] = new TechDebtInfo { Minutes = 15, Category = "CodeStyle", Priority = "Low", Rationale = "Merge collapsible if statements", Recommendation = "Combine if statements" },
        ["S1104"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "Public fields should be properties", Recommendation = "Convert field to property" },
        ["S1116"] = new TechDebtInfo { Minutes = 5, Category = "CodeStyle", Priority = "Low", Rationale = "Empty statements should be removed", Recommendation = "Remove empty statement" },
        ["S1117"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Variable shadowing should be avoided", Recommendation = "Rename shadowing variable" },
        ["S1118"] = new TechDebtInfo { Minutes = 15, Category = "Design", Priority = "Medium", Rationale = "Utility classes should not be instantiable", Recommendation = "Make constructor private" },
        ["S1121"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Assignments should not be made in conditions", Recommendation = "Move assignment outside condition" },
        ["S1123"] = new TechDebtInfo { Minutes = 15, Category = "Documentation", Priority = "Low", Rationale = "Obsolete attribute should have explanation", Recommendation = "Add explanation to Obsolete attribute" },
        ["S1125"] = new TechDebtInfo { Minutes = 10, Category = "CodeStyle", Priority = "Low", Rationale = "Boolean literals should not be redundant", Recommendation = "Remove redundant boolean literal" },
        ["S1133"] = new TechDebtInfo { Minutes = 15, Category = "Documentation", Priority = "Low", Rationale = "Deprecated code should be removed", Recommendation = "Remove or update deprecated code" },
        ["S1144"] = new TechDebtInfo { Minutes = 15, Category = "Dead Code", Priority = "Low", Rationale = "Remove unused private types or members", Recommendation = "Remove unused private code" },
        ["S1155"] = new TechDebtInfo { Minutes = 15, Category = "Performance", Priority = "Low", Rationale = "Use Count property instead of Any()", Recommendation = "Replace Any() with Count check" },
        ["S1168"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Return empty collection instead of null", Recommendation = "Return empty collection" },
        ["S1172"] = new TechDebtInfo { Minutes = 15, Category = "Dead Code", Priority = "Low", Rationale = "Remove unused method parameters", Recommendation = "Remove unused parameters" },
        ["S1186"] = new TechDebtInfo { Minutes = 15, Category = "Documentation", Priority = "Low", Rationale = "Empty methods should be documented", Recommendation = "Add documentation for empty method" },
        ["S1199"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "Nested code blocks should be extracted", Recommendation = "Extract nested code into methods" },
        ["S1206"] = new TechDebtInfo { Minutes = 25, Category = "BestPractices", Priority = "Medium", Rationale = "Property getter should be pure", Recommendation = "Remove side effects from getter" },
        ["S1450"] = new TechDebtInfo { Minutes = 15, Category = "Dead Code", Priority = "Low", Rationale = "Private fields only used in initialization", Recommendation = "Convert field to local variable" },
        ["S1481"] = new TechDebtInfo { Minutes = 10, Category = "Dead Code", Priority = "Low", Rationale = "Remove unused local variables", Recommendation = "Remove unused variables" },
        ["S1607"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Test method should contain assertion", Recommendation = "Add test assertions" },
        ["S1751"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Jump statements should not be redundant", Recommendation = "Remove redundant jump statements" },
        ["S1854"] = new TechDebtInfo { Minutes = 15, Category = "Dead Code", Priority = "Low", Rationale = "Remove dead stores", Recommendation = "Remove unused assignments" },
        ["S1905"] = new TechDebtInfo { Minutes = 15, Category = "CodeStyle", Priority = "Low", Rationale = "Redundant cast should be removed", Recommendation = "Remove unnecessary cast" },
        ["S1939"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Inheritance list should not be redundant", Recommendation = "Remove redundant interface implementations" },
        ["S1940"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Use ISerializable appropriately", Recommendation = "Fix serialization implementation" },
        ["S2094"] = new TechDebtInfo { Minutes = 10, Category = "Dead Code", Priority = "Low", Rationale = "Remove empty class", Recommendation = "Remove or implement empty class" },
        ["S2178"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Short-circuit logic should be used", Recommendation = "Use && or || instead of & or |" },
        ["S2187"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Test classes should contain tests", Recommendation = "Add tests or remove test class" },
        ["S2201"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Return value should not be ignored", Recommendation = "Use or remove return value" },
        ["S2223"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Change non-readonly to readonly", Recommendation = "Add readonly modifier" },
        ["S2251"] = new TechDebtInfo { Minutes = 25, Category = "BestPractices", Priority = "Medium", Rationale = "For loop increment clause should modify loop counter", Recommendation = "Fix loop increment" },
        ["S2259"] = new TechDebtInfo { Minutes = 25, Category = "BugRisk", Priority = "High", Rationale = "Null pointer dereference", Recommendation = "Add null check" },
        ["S2292"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Trivial properties should be auto-implemented", Recommendation = "Convert to auto-property" },
        ["S2344"] = new TechDebtInfo { Minutes = 15, Category = "Convention", Priority = "Low", Rationale = "Enumeration type naming convention", Recommendation = "Rename enumeration" },
        ["S2365"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Properties should not be recursive", Recommendation = "Remove property recursion" },
        ["S2386"] = new TechDebtInfo { Minutes = 25, Category = "Security", Priority = "High", Rationale = "Public mutable fields should not be public", Recommendation = "Encapsulate public fields" },
        ["S2479"] = new TechDebtInfo { Minutes = 15, Category = "Convention", Priority = "Low", Rationale = "Whitespace and control characters in string", Recommendation = "Remove control characters" },
        ["S2486"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Generic exceptions should not be caught", Recommendation = "Catch more specific exceptions" },
        ["S2583"] = new TechDebtInfo { Minutes = 20, Category = "BugRisk", Priority = "High", Rationale = "Conditionally executed code should be possible", Recommendation = "Fix impossible condition" },
        ["S2589"] = new TechDebtInfo { Minutes = 15, Category = "CodeStyle", Priority = "Low", Rationale = "Boolean expressions should not be gratuitous", Recommendation = "Simplify boolean expression" },
        ["S2699"] = new TechDebtInfo { Minutes = 20, Category = "Testing", Priority = "Medium", Rationale = "Tests should include assertions", Recommendation = "Add assertions to test" },
        ["S2925"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Thread.Sleep should not be used in tests", Recommendation = "Use proper test synchronization" },
        ["S2933"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Fields should be initialized inline", Recommendation = "Move initialization inline" },
        ["S2971"] = new TechDebtInfo { Minutes = 20, Category = "Performance", Priority = "Medium", Rationale = "IEnumerable LINQs should be simplified", Recommendation = "Simplify LINQ query" },
        ["S3010"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Static fields should be initialized statically", Recommendation = "Move initialization to static constructor" },
        ["S3063"] = new TechDebtInfo { Minutes = 20, Category = "Design", Priority = "Medium", Rationale = "Single responsibility principle violation", Recommendation = "Split class responsibilities" },
        ["S3218"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Inner class members should be simplified", Recommendation = "Simplify inner class members" },
        ["S3260"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Convert to auto-implemented property", Recommendation = "Use auto-property" },
        ["S3267"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Loop should be simplified", Recommendation = "Simplify loop structure" },
        ["S3358"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Ternary operator should not be nested", Recommendation = "Extract nested ternary" },
        ["S3376"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Attribute instances should be replaceable", Recommendation = "Make attributes replaceable" },
        ["S3400"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Constants should be defined", Recommendation = "Extract magic numbers to constants" },
        ["S3415"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Assertion arguments should be passed in correct order", Recommendation = "Fix assertion argument order" },
        ["S3427"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Method overloads should be grouped", Recommendation = "Group method overloads" },
        ["S3459"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Unassigned members should be removed", Recommendation = "Remove unassigned members" },
        ["S3604"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Member initializer values should not be redundant", Recommendation = "Remove redundant initialization" },
        ["S3626"] = new TechDebtInfo { Minutes = 15, Category = "CodeStyle", Priority = "Low", Rationale = "Jump statements should not be redundant", Recommendation = "Remove redundant jumps" },
        ["S3655"] = new TechDebtInfo { Minutes = 25, Category = "BugRisk", Priority = "High", Rationale = "Empty alternative branches should be removed", Recommendation = "Remove empty branches" },
        ["S3871"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Exception types should be public", Recommendation = "Make exception public" },
        ["S3878"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Arrays should not be created for params parameters", Recommendation = "Use params directly" },
        ["S3881"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Fix sonar issue", Recommendation = "Follow sonar recommendation" },
        ["S3887"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Fix sonar issue", Recommendation = "Follow sonar recommendation" },
        ["S3903"] = new TechDebtInfo { Minutes = 15, Category = "Convention", Priority = "Low", Rationale = "Types should be in named namespaces", Recommendation = "Move type to named namespace" },
        ["S3925"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Implement ISerializable correctly", Recommendation = "Fix serialization implementation" },
        ["S3928"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Parameter names should match base declaration", Recommendation = "Fix parameter names" },
        ["S3963"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "String interpolation should be used", Recommendation = "Use string interpolation" },
        ["S4035"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Classes implementing IEquatable<T> should be sealed", Recommendation = "Seal class or remove IEquatable" },
        ["S4136"] = new TechDebtInfo { Minutes = 15, Category = "CodeStyle", Priority = "Low", Rationale = "Method overloads should be grouped together", Recommendation = "Reorder method declarations" },
        ["S4144"] = new TechDebtInfo { Minutes = 25, Category = "CodeStyle", Priority = "Medium", Rationale = "Methods should not have identical implementations", Recommendation = "Remove duplicate method or extract common code" },
        ["S4158"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Empty collections should be empty, not null", Recommendation = "Return empty collection instead of null" },
        ["S4201"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Null checks should not be used with is", Recommendation = "Remove redundant null check" },
        ["S4487"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Unread 'private' fields should be removed", Recommendation = "Remove unused private fields" },
        ["S4581"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "new Guid() should not be used", Recommendation = "Use Guid.Empty or Guid.NewGuid()" },
        ["S4586"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Non-async method names should not end with 'Async'", Recommendation = "Rename method or make async" },
        ["S4663"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Disposable types should implement IDisposable", Recommendation = "Implement IDisposable" },
        ["S6561"] = new TechDebtInfo { Minutes = 25, Category = "Performance", Priority = "High", Rationale = "DateTimeKind should be specified", Recommendation = "Specify DateTimeKind" },
        ["S6562"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Always set DateTimeKind", Recommendation = "Specify DateTimeKind" },
        ["S6580"] = new TechDebtInfo { Minutes = 15, Category = "Performance", Priority = "Medium", Rationale = "Use spans for string operations", Recommendation = "Convert to span operations" },
        ["S6602"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Fix sonar issue", Recommendation = "Follow sonar recommendation" },
        ["S6603"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Fix sonar issue", Recommendation = "Follow sonar recommendation" },
        ["S6605"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Fix sonar issue", Recommendation = "Follow sonar recommendation" },
        ["S6608"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Fix sonar issue", Recommendation = "Follow sonar recommendation" },
        ["S6610"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Fix sonar issue", Recommendation = "Follow sonar recommendation" },
        ["S6617"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Fix sonar issue", Recommendation = "Follow sonar recommendation" },

        // IDE Rules
        ["IDE0028"] = new TechDebtInfo { Minutes = 5, Category = "CodeStyle", Priority = "Low", Rationale = "Collection initialization can be simplified", Recommendation = "Use collection initializer" },
        ["IDE0037"] = new TechDebtInfo { Minutes = 5, Category = "CodeStyle", Priority = "Low", Rationale = "Use inferred member name", Recommendation = "Simplify member name" },
        ["IDE0051"] = new TechDebtInfo { Minutes = 10, Category = "CodeStyle", Priority = "Low", Rationale = "Remove unused private member", Recommendation = "Remove unused member" },
        ["IDE0052"] = new TechDebtInfo { Minutes = 10, Category = "CodeStyle", Priority = "Low", Rationale = "Remove unread private member", Recommendation = "Remove unread member" },

        // ASP.NET Rules
        ["ASP0001"] = new TechDebtInfo { Minutes = 20, Category = "Security", Priority = "High", Rationale = "Parameter may be vulnerable to XSS", Recommendation = "Encode output" },
        ["ASP0015"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Suggest async operations", Recommendation = "Convert to async" },
        ["ASP0018"] = new TechDebtInfo { Minutes = 20, Category = "Performance", Priority = "Medium", Rationale = "Avoid sync operations", Recommendation = "Use async alternatives" },
        ["ASP0019"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Fix ASP.NET issue", Recommendation = "Follow ASP.NET guidelines" },
        ["ASP0025"] = new TechDebtInfo { Minutes = 20, Category = "BestPractices", Priority = "Medium", Rationale = "Fix ASP.NET issue", Recommendation = "Follow ASP.NET guidelines" },

        // StyleCop Rules
        ["SA1106"] = new TechDebtInfo { Minutes = 5, Category = "CodeStyle", Priority = "Low", Rationale = "Code should declare access modifier", Recommendation = "Add access modifier" },
        ["SA1107"] = new TechDebtInfo { Minutes = 5, Category = "CodeStyle", Priority = "Low", Rationale = "Code should not have multiple statements on one line", Recommendation = "Split into multiple lines" },
        ["SA1123"] = new TechDebtInfo { Minutes = 5, Category = "CodeStyle", Priority = "Low", Rationale = "Region should not be located within a code element", Recommendation = "Move region" },

        // SysLib Rules
        ["SYSLIB0012"] = new TechDebtInfo { Minutes = 25, Category = "Obsolescence", Priority = "High", Rationale = "Type or member is obsolete", Recommendation = "Update to supported alternative" },
        ["SYSLIB0014"] = new TechDebtInfo { Minutes = 25, Category = "Obsolescence", Priority = "High", Rationale = "Type or member is obsolete", Recommendation = "Update to supported alternative" },
        ["SYSLIB0021"] = new TechDebtInfo { Minutes = 25, Category = "Obsolescence", Priority = "High", Rationale = "Type or member is obsolete", Recommendation = "Update to supported alternative" },
        ["SYSLIB0022"] = new TechDebtInfo { Minutes = 25, Category = "Obsolescence", Priority = "High", Rationale = "Type or member is obsolete", Recommendation = "Update to supported alternative" },
        ["SYSLIB0023"] = new TechDebtInfo { Minutes = 25, Category = "Obsolescence", Priority = "High", Rationale = "Type or member is obsolete", Recommendation = "Update to supported alternative" },
        ["SYSLIB0041"] = new TechDebtInfo { Minutes = 25, Category = "Obsolescence", Priority = "High", Rationale = "Type or member is obsolete", Recommendation = "Update to supported alternative" },
        ["SYSLIB0045"] = new TechDebtInfo { Minutes = 25, Category = "Obsolescence", Priority = "High", Rationale = "Type or member is obsolete", Recommendation = "Update to supported alternative" },
        ["SYSLIB0051"] = new TechDebtInfo { Minutes = 25, Category = "Obsolescence", Priority = "High", Rationale = "Type or member is obsolete", Recommendation = "Update to supported alternative" },

        // Suppyl Specific Rules
        ["SUP001"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Follow Supply guideline", Recommendation = "Use Switch Case Instead Of multiple ifelse" },
        ["SUP002"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Follow Supply guideline", Recommendation = "Always use a Security Attribute on public controllers" },
        ["SUP003"] = new TechDebtInfo { Minutes = 15, Category = "BestPractices", Priority = "Medium", Rationale = "Follow Supply guideline", Recommendation = "No Direct HttpContext Access, go via our Library" }
    };



    private static string GetPriorityFromSeverity(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => "High",
            DiagnosticSeverity.Warning => "Medium",
            _ => "Low"
        };
    }

    public static TechDebtInfo? GetTechDebtInfo(string ruleId)
    {
        // Check predefined first, then analyzer metadata
        if (PredefinedMetadata.TryGetValue(ruleId, out var predefinedInfo))
        {
            return predefinedInfo;
        }

        if (AnalyzerMetadata.TryGetValue(ruleId, out var analyzerInfo))
        {
            return analyzerInfo;
        }

        return null;
    }
    
    public static void UpdateMetadataFromDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            var ruleId = diagnostic.Id;
            var descriptor = diagnostic.Descriptor;

            // Skip if we already have predefined metadata for this rule
            if (PredefinedMetadata.ContainsKey(ruleId))
                continue;

            var properties = diagnostic.Properties ?? ImmutableDictionary<string, string>.Empty;

            // Try to get tech debt minutes from properties
            int minutes = 15; // Default value
            if (properties.TryGetValue("TechDebtInMinutes", out var techDebtMinutesStr))
            {
                if (!int.TryParse(techDebtMinutesStr, out minutes))
                {
                    minutes = 15; // Fallback to default if parsing fails
                }
            }

            // Determine category based on descriptor or fallback to a default
            string category = "BestPractices"; // Default category
            if (properties.TryGetValue("Category", out var categoryFromProps))
            {
                category = categoryFromProps;
            }
            else if (descriptor.Category != null)
            {
                category = descriptor.Category;
            }

            // Determine priority based on diagnostic severity
            string priority = GetPriorityFromSeverity(descriptor.DefaultSeverity);

            // Create rationale from diagnostic description
            string rationale = descriptor.Description.ToString();
            if (properties.TryGetValue("Rationale", out var rationaleFromProps))
            {
                rationale = rationaleFromProps;
            }

            // Get recommendation from help link or message
            string recommendation = descriptor.HelpLinkUri ?? descriptor.MessageFormat.ToString();
            if (properties.TryGetValue("Recommendation", out var recommendationFromProps))
            {
                recommendation = recommendationFromProps;
            }

            // Create or update the tech debt info
            var techDebtInfo = new TechDebtInfo
            {
                Minutes = minutes,
                Category = category,
                Priority = priority,
                Rationale = rationale,
                Recommendation = recommendation
            };

            // Add to analyzer metadata if not already in predefined
            AnalyzerMetadata.TryAdd(ruleId, techDebtInfo);
        }
    }
}