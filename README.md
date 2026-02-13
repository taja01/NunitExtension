# DeepCompare.NUnitExtension

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A powerful NUnit extension that provides deep, recursive comparison of complex object graphs with detailed, actionable assertion failure messages.

## Why DeepCompare?

When testing complex object trees, NUnit's built-in `Assert.AreEqual()` provides minimal diagnostic information. This extension delivers:

- **Clear difference reporting** - See exactly which properties differ, with their paths and values
- **Deep recursive comparison** - Automatically traverses nested objects, collections, and dictionaries
- **Flexible configuration** - Skip properties, set DateTime tolerances, limit diff output
- **Circular reference detection** - Safely handles object graphs with cycles
- **Type-flexible collection comparison** - Compares arrays, lists, and other collection types seamlessly

## Installation

dotnet add package DeepCompare.NUnitExtension

## Quick Start

### Basic Usage
```
using DeepCompare.NUnitExtension;

var response1 = new ResponseBody 
{ 
    IsSuccess = true, 
    Message = "Accepted",
    Method = Method.POST,
    StatusCode = 202, 
    InnerMessage = new InnerMessage { Message = "Test" }, 
    Numbers = [1, 2, 3, 4, 5, 6] 
};

var response2 = new ResponseBody 
{ 
    IsSuccess = true, 
    Message = "Accepted", 
    Method = Method.POST, 
    StatusCode = 200, 
    InnerMessage = new InnerMessage { Message = "Dev" }, 
    Numbers = [7, 6, 5, 4, 3, 2, 1] 
};

Assert.That(response1, Matches.DeeplyWith(response2));
// Differences found: 3. The details are as follows:
// Property 'StatusCode' mismatch: Expected '200', but was '202'.
// Property 'Numbers.Count' mismatch: Expected 'Count 7', but was 'Count 6'.
// Property 'InnerMessage.Message' mismatch: Expected 'Dev', but was 'Test'.
```
## Features

### 1. Skip Properties

Ignore specific properties during comparison using fluent syntax:
```
var actual = new ResponseBody 
{ 
    StatusCode = 200,
    Method = Method.GET,
    InnerMessage = new InnerMessage { Message = "Test" }
};

var expected = new ResponseBody 
{
    StatusCode = 200,
    Method = Method.POST,
    InnerMessage = new InnerMessage { Message = "Dev" }
};

// Skip the Method property - test passes 
Assert.That(actual, Matches.DeeplyWith(expected).Skip("Method"));

// Skip nested properties 
Assert.That(actual, Matches.DeeplyWith(expected).Skip("InnerMessage.Message"));
```
### 2. DateTime Tolerance

Handle DateTime comparisons with configurable tolerance:
```
var now = DateTime.UtcNow; 
var actual = new Event { Timestamp = now }; 
var expected = new Event { Timestamp = now.AddMilliseconds(500) };

// Global tolerance for all DateTime properties 
Assert.That(actual, Matches.DeeplyWith(expected).WithGlobalDateTimeTolerance(TimeSpan.FromSeconds(1)));

// Per-property tolerance (overrides global) 
Assert.That(actual, Matches.DeeplyWith(expected).WithDateTimeTolerance("Timestamp", TimeSpan.FromSeconds(2)));
```
Works with `DateTime`, `DateTimeOffset`, `TimeSpan`, and their nullable variants.

### 3. Limit Differences Output

Control the maximum number of differences reported to avoid overwhelming output:
```
var options = new DeepCompareOptions(); 
options.WithMaxDifferences(5); // Stop after finding 5 differences

Assert.That(actual, Matches.DeeplyWith(expected, opt => opt.WithMaxDifferences(5)));

// Or using fluent syntax 
var constraint = Matches.DeeplyWith(expected).WithOptions(opt => opt.WithMaxDifferences(5));
Assert.That(actual, constraint);
```
### 4. Collection Comparison

Automatically handles various collection types:
```
// Lists 
var expectedList = new List<string> { "a", "b", "c" }; 
var actualList = new List<string> { "a", "b", "x" }; 

Assert.That(actualList, Matches.DeeplyWith(expectedList)); // Property '[2]' mismatch: Expected 'c', but was 'x'.

// Arrays 
var expected = new[] { 1, 2, 3 }; 
var actual = new[] { 1, 2, 3, 4 }; 

Assert.That(actual, Matches.DeeplyWith(expected)); // Property 'Count' mismatch: Expected 'Count 3', but was 'Count 4'.

// Mixed collection types (array vs list) 
var expectedArray = new[] { 1, 2, 3 };
var actualList = new List<int> { 1, 2, 3 }; 

Assert.That(actualList, Matches.DeeplyWith(expectedArray)); // Passes!

// Nullable elements 
var expectedWithNull = new List<int?> { 1, 2, null };
var actualWithNull = new List<int?> { 1, 2, null }; 

Assert.That(actualWithNull, Matches.DeeplyWith(expectedWithNull)); // Passes!
```
### 5. Dictionary Comparison

Deep comparison of dictionaries with clear diff reporting:
```
var expected = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }; 
var actual = new Dictionary<string, int> { ["a"] = 1, ["b"] = 3 };
Assert.That(actual, Matches.DeeplyWith(expected)); // Property '["b"]' mismatch: Expected '2', but was '3'.
```
### 6. Nested Object Graphs

Recursively compares nested objects and reports the full property path:
```
var expected = new Order 
{
    Id = 1,
    Customer = new Customer
    {
        Name = "John", 
        Address = new Address { City = "NYC" }
    }
};
var actual = new Order 
{ 
    Id = 1, 
    Customer = new Customer 
    {
        Name = "John", 
        Address = new Address { City = "LA" }
    } 
};
Assert.That(actual, Matches.DeeplyWith(expected)); // Property 'Customer.Address.City' mismatch: Expected 'NYC', but was 'LA'.
```
### 7. Circular Reference Detection

Safely handles circular references without infinite recursion:
```
var node1 = new Node { Value = 1 }; 
var node2 = new Node { Value = 2 };

node1.Next = node2;
node2.Next = node1; // Circular reference
var otherNode1 = new Node { Value = 1 };
var otherNode2 = new Node { Value = 2 };

otherNode1.Next = otherNode2; 
otherNode2.Next = otherNode1;

Assert.That(node1, Matches.DeeplyWith(otherNode1)); // Handles gracefully
```
## Advanced Configuration

### Fluent Builder Pattern

Chain multiple configuration options:

Assert.That(actual, Matches.DeeplyWith(expected) .Skip("Id") .Skip("CreatedAt") .WithGlobalDateTimeTolerance(TimeSpan.FromSeconds(1)) .WithDateTimeTolerance("UpdatedAt", TimeSpan.FromMinutes(5)) .WithOptions(opt => opt.WithMaxDifferences(10)));

### Reusable Constraints

Store and reuse configured constraints:
```
var constraint = Matches.DeeplyWith(expected)
                                 .Skip("Id")
                                 .Skip("Timestamp") 
                                 .WithGlobalDateTimeTolerance(TimeSpan.FromSeconds(1));
Assert.That(actual1, constraint); 
Assert.That(actual2, constraint); 
Assert.That(actual3, constraint);
```
## Comparison Features

| Feature | Supported |
|---------|-----------|
| Primitive types (int, string, bool, etc.) | ✅ |
| Nullable types | ✅ |
| DateTime/DateTimeOffset with tolerance | ✅ |
| TimeSpan with tolerance | ✅ |
| Collections (List, Array, IEnumerable) | ✅ |
| Dictionaries | ✅ |
| Nested objects | ✅ |
| Mixed collection types | ✅ |
| Circular references | ✅ |
| Property path skipping | ✅ |
| null vs empty string detection | ✅ |
| Configurable diff limit | ✅ |

## API Reference

### `Matches.DeeplyWith(object expected, Action<DeepCompareOptions>? configure = null)`

Creates a deep comparison constraint.

**Parameters:**
- `expected` - The expected object to compare against
- `configure` - Optional callback to configure comparison options

**Returns:** `DeeplyEqualConstraint` - A constraint that can be used with `Assert.That()`

### Fluent Methods

- `.Skip(string propertyPath)` - Skip a property from comparison
- `.WithGlobalDateTimeTolerance(TimeSpan tolerance)` - Set global DateTime tolerance
- `.WithDateTimeTolerance(string propertyPath, TimeSpan tolerance)` - Set per-property DateTime tolerance
- `.WithOptions(Action<DeepCompareOptions> configure)` - Advanced configuration

### DeepCompareOptions

- `SkippedProperties` - Collection of property paths to skip (case-insensitive)
- `GlobalDateTimeTolerance` - Global tolerance for DateTime comparisons
- `DateTimeTolerances` - Per-property DateTime tolerances
- `MaxDifferences` - Maximum number of differences to collect (default: 100)

## Contributing

Contributions are welcome! Feel free to:
- Report bugs or issues
- Suggest new features
- Submit pull requests

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Repository

[https://github.com/taja01/NunitExtension](https://github.com/taja01/NunitExtension)