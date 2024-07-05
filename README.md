# Kameleoon OpenFeature provider for .NET

The Kameleoon OpenFeature provider for .NET allows you to connect your OpenFeature .NET implementation to Kameleoon without installing the C#  Kameleoon SDK.

> [!WARNING]
> This is a beta version. Breaking changes may be introduced before general release.

## Supported .NET versions

This version of the SDK is built for the following targets:

* .NET Framework 4.6.2: runs on .NET Framework 4.6.2 and above.
* .NET Standard 2.0: runs in any project that is targeted to .NET Standard 2.x rather than to a specific runtime platform.

## Get started

This section explains how to install, configure, and customize the Kameleoon OpenFeature provider.

### Install dependencies

First, choose your preferred dependency manager from the following options and install the required dependencies in your application.

#### .NET CLI

```shell
dotnet add package Kameleoon.OpenFeature
dotnet add package KameleoonClient
dotnet add package OpenFeature
```
#### Package Manager

```shell
NuGet\Install-Package Kameleoon.OpenFeature
NuGet\Install-Package KameleoonClient
NuGet\Install-Package OpenFeature
```
#### Package Reference

```xml
<PackageReference Include="Kameleoon.OpenFeature" />
<PackageReference Include="KameleoonClient" />
<PackageReference Include="OpenFeature" />
```
#### Packet CLI

```shell
paket add Kameleoon.OpenFeature
paket add KameleoonClient
paket add OpenFeature
```

#### Cake

```shell
// Install Kameleoon.OpenFeature as a Cake Addin
#addin nuget:?package=Kameleoon.OpenFeature
// Install KameleoonClient as a Cake Addin
#addin nuget:?package=KameleoonClient&version=4.4.1
// Install OpenFeature as a Cake Addin
#addin nuget:?package=OpenFeature&version=1.5.0

// Install Kameleoon.OpenFeature as a Cake Tool
#tool nuget:?package=Kameleoon.OpenFeature
// Install KameleoonClient as a Cake Tool
#tool nuget:?package=KameleoonClient&version=4.4.1
// Install OpenFeature as a Cake Tool
#tool nuget:?package=OpenFeature&version=1.5.0
```

### Usage

The following example shows how to use the Kameleoon provider with the OpenFeature SDK.

```csharp
using Kameleoon.OpenFeature;

namespace Kameleoon.OpenFeature.App
{
    class App {
        static void Main(string[] args) {
            var config = new KameleoonClientConfig(clientId: clientId, clientSecret: clientSecret);
            var provider = new KameleoonProvider(siteCode, config);

            OpenFeature.Api.Instance.SetProvider(provider);

            var client = OpenFeature.Api.Instance.GetClient();

            var context = EvaluationContext.Builder()
                .SetTargetingKey("visitorCode")
                .Build();
            var isFeatureEnabled = client.GetBooleanValue("featureKey", false, context);

            if(isFeatureEnabled)
                runNewFeatureMethod();
            else
                runOriginVariationMethod();
        }
    }
}
```

#### Customize the Kameleoon provider

You can customize the Kameleoon provider by changing the `KameleoonClientConfig` object that you passed to the constructor above. For example:

```csharp
var config = new KameleoonClientConfig(
    clientId: "<clientId>", // mandatory
    clientSecret: "<clientSecret>", // mandatory
    refreshIntervalMinute: 1, // optional
    sessionDurationMinute: 60 // optional
);

var kameleoonProvider = new KameleoonProvider("<YOUR-SITE-CODE>", config);
```
> [!NOTE]
> For additional configuration options, see the [Kameleoon documentation](https://developers.kameleoon.com/feature-management-and-experimentation/web-sdks/csharp-sdk/#example-code).

## EvaluationContext and Kameleoon Data

Kameleoon uses the concept of associating `Data` to users, while the OpenFeature SDK uses the concept of an `EvaluationContext`, which is a dictionary of string keys and values. The Kameleoon provider maps the `EvaluationContext` to the Kameleoon `Data`.

> [!NOTE]
> To get the evaluation for a specific visitor, set the `TargetingKey` value for the `EvaluationContext` to the visitor code (user ID). If the value is not provided, then the `defaultValue` parameter will be returned.

```csharp
var context = EvaluationContext.Builder().SetTargetingKey("userId").Build();
```

The Kameleoon provider provides a few predefined parameters that you can use to target a visitor from a specific audience and track each conversion. These are:

| Parameter | Description |
|-----------|-------------|
| `Data.Type.CustomData` | The parameter is used to set [`CustomData`](https://developers.kameleoon.com/feature-management-and-experimentation/web-sdks/csharp-sdk/#customdata) for a visitor. |
| `Data.Type.Conversion` | The parameter is used to track a [`Conversion`](https://developers.kameleoon.com/feature-management-and-experimentation/web-sdks/csharp-sdk/#conversion) for a visitor. |

### Data.CustomData

Use `Data.CustomData` to set [`CustomData`](https://developers.kameleoon.com/feature-management-and-experimentation/web-sdks/csharp-sdk/#customdata) for a visitor. The `Data.CustomData` field has the following parameters:

| Parameter | Type | Description |
|-----------| ---- | ----------- |
| `Data.CustomDataType.Index` | int | Index or ID of the custom data to store. This field is mandatory. |
| `Data.CustomDataType.Values` | string |Value of the custom data to store. This field is mandatory. |

#### Example

```csharp
var context = EvaluationContext.Builder()
    .SetTargetingKey("userId")
    .Set(Data.Type.CustomData, new Structure(
        new Dictionary<string, Value> {
            { Data.CustomDataType.Index, new Value(1) },
            { Data.CustomDataType.Values, new Value("10").ToList() }
        })
    )
    .Build();
```

### Data.Conversion

Use `Data.Conversion` to track a [`Conversion`](https://developers.kameleoon.com/feature-management-and-experimentation/web-sdks/csharp-sdk/#conversion) for a visitor. The `Data.Conversion` field has the following parameters:

| Parameter | Type | Description |
|-----------| ---- | ----------- |
| `Data.ConversionType.GoalId` | int | Identifier of the goal. This field is mandatory. |
| `Data.ConversionType.Revenue` | float | Revenue associated with the conversion. This field is optional. |

#### Example
```csharp
var context = EvaluationContext.Builder()
    .SetTargetingKey("userId")
    .Set(Data.Type.Conversion, new Structure(
        new Dictionary<string, Value> {
            { Data.ConversionType.GoalId, new Value(1) },
            { Data.ConversionType.Revenue, new Value(200) },
        })
    )
    .Build();
```

### Use multiple Kameleoon Data types

You can provide many different kinds of Kameleoon data within a single `EvaluationContext` instance.

For example, the following code provides one `Data.Conversion` instance and two `Data.CustomData` instances.

```csharp
var context = EvaluationContext.Builder()
    .SetTargetingKey("userId")
    .Set(Data.Type.Conversion, new Structure(
        new Dictionary<string, Value> {
            { Data.ConversionType.GoalId, new Value(1) },
            { Data.ConversionType.Revenue, new Value(200) },
        })
    )
    .Set(Data.Type.CustomData, new Value(new Value[] {
        new(new Structure(
            new Dictionary<string, Value> {
                { Data.CustomDataType.Index, new Value(1) },
                { Data.CustomDataType.Values, new Value(new List<Value> { new("10"), new("30") }) }
            }
        )),
        new(new Structure(
            new Dictionary<string, Value> {
                { Data.CustomDataType.Index, new Value(2) },
                { Data.CustomDataType.Values, new Value("20") }
            }
        )),
    }))
    .Build();
```
