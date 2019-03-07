using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;

namespace DependenciesBot
{
    class Program
    {
        // The packages to update in aspnet/Extensions
        static readonly HashSet<string> _extensionsPackageNames = new HashSet<string>()
        {
            "InternalAspNetCoreAnalyzersPackageVersion",
            "MicrosoftAspNetCoreAnalyzerTestingPackageVersion",
            "MicrosoftAspNetCoreBenchmarkRunnerSourcesPackageVersion",
            "MicrosoftAspNetCoreCertificatesGenerationSourcesPackageVersion",
            "MicrosoftAspNetCoreTestingPackageVersion",
            "MicrosoftExtensionsActivatorUtilitiesSourcesPackageVersion",
            "MicrosoftExtensionsCachingAbstractionsPackageVersion",
            "MicrosoftExtensionsCachingMemoryPackageVersion",
            "MicrosoftExtensionsCachingSqlServerPackageVersion",
            "MicrosoftExtensionsCachingStackExchangeRedisPackageVersion",
            "MicrosoftExtensionsClosedGenericMatcherSourcesPackageVersion",
            "MicrosoftExtensionsCommandLineUtilsSourcesPackageVersion",
            "MicrosoftExtensionsConfigurationAbstractionsPackageVersion",
            "MicrosoftExtensionsConfigurationAzureKeyVaultPackageVersion",
            "MicrosoftExtensionsConfigurationBinderPackageVersion",
            "MicrosoftExtensionsConfigurationCommandLinePackageVersion",
            "MicrosoftExtensionsConfigurationEnvironmentVariablesPackageVersion",
            "MicrosoftExtensionsConfigurationFileExtensionsPackageVersion",
            "MicrosoftExtensionsConfigurationIniPackageVersion",
            "MicrosoftExtensionsConfigurationJsonPackageVersion",
            "MicrosoftExtensionsConfigurationKeyPerFilePackageVersion",
            "MicrosoftExtensionsConfigurationPackageVersion",
            "MicrosoftExtensionsConfigurationUserSecretsPackageVersion",
            "MicrosoftExtensionsConfigurationXmlPackageVersion",
            "MicrosoftExtensionsCopyOnWriteDictionarySourcesPackageVersion",
            "MicrosoftExtensionsDependencyInjectionAbstractionsPackageVersion",
            "MicrosoftExtensionsDependencyInjectionPackageVersion",
            "MicrosoftExtensionsDependencyInjectionSpecificationTestsPackageVersion",
            "MicrosoftExtensionsDiagnosticAdapterPackageVersion",
            "MicrosoftExtensionsDiagnosticsHealthChecksAbstractionsPackageVersion",
            "MicrosoftExtensionsDiagnosticsHealthChecksPackageVersion",
            "MicrosoftExtensionsFileProvidersAbstractionsPackageVersion",
            "MicrosoftExtensionsFileProvidersCompositePackageVersion",
            "MicrosoftExtensionsFileProvidersEmbeddedPackageVersion",
            "MicrosoftExtensionsFileProvidersPhysicalPackageVersion",
            "MicrosoftExtensionsFileSystemGlobbingPackageVersion",
            "MicrosoftExtensionsHashCodeCombinerSourcesPackageVersion",
            "MicrosoftExtensionsHostingAbstractionsPackageVersion",
            "MicrosoftExtensionsHostingPackageVersion",
            "MicrosoftExtensionsHttpPackageVersion",
            "MicrosoftExtensionsLocalizationAbstractionsPackageVersion",
            "MicrosoftExtensionsLocalizationPackageVersion",
            "MicrosoftExtensionsLoggingAbstractionsPackageVersion",
            "MicrosoftExtensionsLoggingAzureAppServicesPackageVersion",
            "MicrosoftExtensionsLoggingConfigurationPackageVersion",
            "MicrosoftExtensionsLoggingConsolePackageVersion",
            "MicrosoftExtensionsLoggingDebugPackageVersion",
            "MicrosoftExtensionsLoggingEventSourcePackageVersion",
            "MicrosoftExtensionsLoggingPackageVersion",
            "MicrosoftExtensionsLoggingTestingPackageVersion",
            "MicrosoftExtensionsLoggingTraceSourcePackageVersion",
            "MicrosoftExtensionsNonCapturingTimerSourcesPackageVersion",
            "MicrosoftExtensionsObjectMethodExecutorSourcesPackageVersion",
            "MicrosoftExtensionsObjectPoolPackageVersion",
            "MicrosoftExtensionsOptionsConfigurationExtensionsPackageVersion",
            "MicrosoftExtensionsOptionsDataAnnotationsPackageVersion",
            "MicrosoftExtensionsOptionsPackageVersion",
            "MicrosoftExtensionsParameterDefaultValueSourcesPackageVersion",
            "MicrosoftExtensionsPrimitivesPackageVersion",
            "MicrosoftExtensionsProcessSourcesPackageVersion",
            "MicrosoftExtensionsPropertyActivatorSourcesPackageVersion",
            "MicrosoftExtensionsPropertyHelperSourcesPackageVersion",
            "MicrosoftExtensionsRazorViewsSourcesPackageVersion",
            "MicrosoftExtensionsSecurityHelperSourcesPackageVersion",
            "MicrosoftExtensionsStackTraceSourcesPackageVersion",
            "MicrosoftExtensionsTypeNameHelperSourcesPackageVersion",
            "MicrosoftExtensionsValueStopwatchSourcesPackageVersion",
            "MicrosoftExtensionsWebEncodersPackageVersion",
            "MicrosoftExtensionsWebEncodersSourcesPackageVersion",
        };

        static readonly string _netCoreUrlPrefix = "https://dotnetcli.azureedge.net/dotnet/Runtime/{0}/dotnet-runtime-{0}-win-x64.zip";

        static readonly string _extensionsPackageId = "Microsoft.Extensions.Caching.Memory";
        static readonly string _extensionsVersionPrefix = "3.0.0-preview4.";

        static readonly string _efCorePackageId = "Microsoft.EntityFrameworkCore.Abstractions";
        static readonly string _efCoreVersionPrefix = "3.0.0-preview4.";

        // The packages to update in aspnet/EntityFrameworkCore
        static readonly HashSet<string> _efCorePackageNames = new HashSet<string>()
        {
            "MicrosoftEntityFrameworkCoreAbstractionsPackageVersion",
            "MicrosoftEntityFrameworkCoreAnalyzersPackageVersion",
            "MicrosoftEntityFrameworkCoreDesignPackageVersion",
            "MicrosoftEntityFrameworkCoreInMemoryPackageVersion",
            "MicrosoftEntityFrameworkCoreRelationalPackageVersion",
            "MicrosoftEntityFrameworkCoreSqlitePackageVersion",
            "MicrosoftEntityFrameworkCoreSqlServerPackageVersion",
            "MicrosoftEntityFrameworkCoreToolsPackageVersion",
            "MicrosoftEntityFrameworkCorePackageVersion",
        };

        // extensions/efcore/aspnetcore
        static readonly string _extensionsVersions = "https://raw.githubusercontent.com/aspnet/Extensions/master/eng/Versions.props";
        static readonly string _extensionsDetails = "https://raw.githubusercontent.com/aspnet/Extensions/master/eng/Version.Details.xml";

        static readonly string _efCoreVersions = "https://raw.githubusercontent.com/aspnet/EntityFrameworkCore/master/eng/Versions.props";
        static readonly string _efCoreDetails = "https://raw.githubusercontent.com/aspnet/EntityFrameworkCore/master/eng/Version.Details.xml";

        static readonly string _aspnetCoreDependencies = "https://github.com/aspnet/AspNetCore/blob/master/eng/Versions.props";

        static readonly string _extensionsVersionsFilename = "extensions-Versions.props";
        static readonly string _extensionsDetailsFilename = "extensions-Versions.Details.xml";

        static readonly string _efcoreVersionsFilename = "efcore-Versions.props";
        static readonly string _efcoreDetailsFilename = "efcore-Versions.Details.xml";

        static readonly string _aspnetCoreDependenciesFilename = "aspnetcore-dependencies.props";

        // core-setup/corefx
        static readonly string _latestCoreSetupPackages = "https://raw.githubusercontent.com/dotnet/versions/master/build-info/dotnet/core-setup/master/Latest_Packages.txt";
        static readonly string _latestCoreFxPackages = "https://raw.githubusercontent.com/dotnet/versions/master/build-info/dotnet/corefx/master/Latest_Packages.txt";
        static readonly string _coreSetupCoherence = "https://raw.githubusercontent.com/dotnet/core-setup/master/eng/Versions.props";

        static readonly string _coreSetupCoherenceFilename = "core-setup-dependencies.props";
        static readonly string _latestCoreSetupPackagesFilename = "core-setup-latest.txt";
        static readonly string _latestCoreFxPackagesFilename = "corefx-latest.txt";
        static readonly string _versionsFilename = "versions.txt";

        static readonly HashSet<string> _ignorePackages = new HashSet<string> ()
        {
            "SystemValueTuplePackageVersion",
            "SystemMemoryPackageVersion",
            "MicrosoftNETFrameworkCompatibilityPackageVersion",
            "SystemBuffersPackageVersion",
            "SystemIOPipesAccessControlPackageVersion",
            "SystemWindowsExtensionsPackageVersion",
        };

        static readonly HttpClient _httpClient = new HttpClient();

        static ProductHeaderValue _productHeaderValue = new ProductHeaderValue("BenchmarksBot");
        static string _accessToken;
        static string _username;
        static long _repositoryId;

        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "DEPENDENCIESBOT_")
                .AddCommandLine(args)
                .Build();

            LoadSettings(config);

            var action = args[0];

            if (action == "clean")
            {
                Clean();

                return;
            }

            if (action == "coherence")
            {
                await EnsureCoherence();

                return;
            }

            if (action == "clean-coherence")
            {
                Clean();
                await EnsureCoherence();

                return;
            }

            if (action == "extensions")
            {
                await UpdateExtensionsDependencies();

                return;
            }

            if (action == "efcore")
            {
                await UpdateEfCoreDependencies();

                return;
            }

            if (action == "aspnetcore")
            {
                await UpdateAspNetCoreDependencies();

                return;
            }

            Console.WriteLine("Expected argument: clean, coherence, extensions, efcore, aspnetcore");
        }

        private static void Clean()
        {
            File.Delete(_coreSetupCoherenceFilename);
            File.Delete(_latestCoreSetupPackagesFilename);
            File.Delete(_latestCoreFxPackagesFilename);
            File.Delete(_extensionsVersionsFilename);
            File.Delete(_extensionsDetailsFilename);
            File.Delete(_efcoreVersionsFilename);
            File.Delete(_efcoreDetailsFilename);
        }

        private static async Task<bool> EnsureCoherence()
        {
            // Ensure the corefx package versions are coherent with the version of MicrosoftNETCoreAppPackageVersion. 
            // If the corefx package versions are newer, we'll end up with extra assemblies in our shared framework.
            // We ensure that core-setup has built with new corefx packages yet:
            // In  for 
            // MicrosoftNETCoreRuntimeCoreCLRPackageVersion and MicrosoftNETCorePlatformsPackageVersion are matching the Latest built packages

            // MicrosoftNETCoreRuntimeCoreCLRPackageVersion 3.0.0-preview-27207-02 (https://raw.githubusercontent.com/dotnet/core-setup/master/Versions.props)
            // Microsoft.NETCore.App 3.0.0-preview-27206-02 (https://raw.githubusercontent.com/dotnet/versions/master/build-info/dotnet/core-setup/master/Latest_Packages.txt)

            // MicrosoftNETCorePlatformsPackageVersion 3.0.0-preview.18606.1 (https://raw.githubusercontent.com/dotnet/core-setup/master/Versions.props)
            // Microsoft.NETCore.Platforms 3.0.0-preview.18606.1 (https://raw.githubusercontent.com/dotnet/versions/master/build-info/dotnet/corefx/master/Latest_Packages.txt)

            // Delete the files from the previous workflow

            if (File.Exists(_latestCoreSetupPackagesFilename))
            {
                Log("Existing files were found. Use 'clean' before 'coherence' to start a new workflow.");

                return false;
            }

            Log("# Checking coherence");

            Log($"Saving 'Latest_Packages' for core-setup");
            await DownloadFileAsync(_latestCoreSetupPackages, _latestCoreSetupPackagesFilename);
            var latestCoreSetup = File.ReadAllText(_latestCoreSetupPackagesFilename);

            var latestNetCoreApp = new Regex(@"^Microsoft\.NETCore\.App\s+([\w\-\.]+)\s*$", RegexOptions.Multiline).Match(latestCoreSetup).Groups[1].Value;

            var versions = await GetRuntimeSha("3.0.0-preview4-27504-10");

            string coreSetupSha;
            using (var sr = new StringReader(versions))
            {
                coreSetupSha = sr.ReadLine();
            }

            Log($"Microsoft.NetCore.App: {latestNetCoreApp} / {coreSetupSha}");

            // Other option is to read Microsoft.NETCore.App.deps.json from the runtime file instead of reflecting the attribute

            var versionAttribute = await GetRuntimeAssemblyVersion(latestNetCoreApp, "System.Collections.dll");

            var coreFxVersion = new Regex(@"^[\d\w\.\-]+").Match(versionAttribute).Value;
            var coreFxSha = new Regex(@"[\d\w\-]{40}").Match(versionAttribute).Value;

            Log($"CoreFX: {coreFxVersion} / {coreFxSha}");

            var version = new Version
            {
                CoreFxVersion = coreFxVersion,
                CoreFxSha = coreFxSha,
                MicrosoftNetCoreAppVersion = latestNetCoreApp,
                CoreSetupSha = coreSetupSha
            };

            File.WriteAllText(_versionsFilename, JsonConvert.SerializeObject(version));

            // Search the Latest_Packages for corefx file that corresponds to this version
            var commitVersion = "master";

            for(;;)
            {
                Log($"Seeking core-fx Latest_Packages.txt in '{commitVersion}'");

                var contentUrl = $"https://raw.githubusercontent.com/dotnet/versions/{commitVersion}/build-info/dotnet/corefx/master/Latest_Packages.txt";
                var fileContent = await DownloadContentAsync(contentUrl);

                if (fileContent.Contains(coreFxVersion))
                {
                    Log($"Found matching version !");
                    File.WriteAllText(_latestCoreFxPackagesFilename, fileContent);
                    break;
                }

                // Find parent commit version
                
                var commitUrl = $"https://api.github.com/repos/dotnet/versions/commits/{commitVersion}";
                using (var message = new HttpRequestMessage(HttpMethod.Get, commitUrl))
                {
                    message.Headers.Add("Accept", "application/vnd.github.v3+json");
                    message.Headers.Add("User-Agent", "Build-Scripts");

                    var response = await _httpClient.SendAsync(message);

                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var dom = JObject.Parse(content);

                    commitVersion = dom["parents"][0]["sha"].ToString();
                }

                // Rate limit requests to GH API
                await Task.Delay(2000);
            }

            return true;
        }

        private static void Log(string text)
        {
            Console.WriteLine("[{0}] {1}", DateTime.UtcNow.ToShortTimeString(), text);
        }

        private static async Task<string> PatchVersionsPropsCoreSetupCoreFxVersionAsync(string deps)
        {
            // Any tag after this section should not be updated
            var manualIndex = deps.IndexOf("<PropertyGroup Label=\"Manual\">");

            if (manualIndex < 0)
            {
                manualIndex = deps.Length;
            }

            foreach (var source in new[] { _latestCoreSetupPackagesFilename, _latestCoreFxPackagesFilename })
            {
                // Load latest dependencies from core-setup
                var latestPackages = await File.ReadAllTextAsync(source);

                using (var sr = new StringReader(latestPackages))
                {
                    var line = sr.ReadLine();

                    while (!String.IsNullOrEmpty(line))
                    {
                        var parts = line.Split(' ');

                        if (parts.Length != 2)
                        {
                            throw new ApplicationException($"Expected 2 parts in latest core-setup packages: {line}");
                        }

                        var packageName = parts[0];
                        var packageVersion = parts[1];

                        var normalizedPackageName = packageName.Replace(".", "") + "PackageVersion";

                        // Performance is not a concern
                        var oldDependency = new Regex($@"\<{normalizedPackageName}\>([\w\-\.]+)\</{normalizedPackageName}\>");

                        // Search for this package in the existing dependencies
                        var match = oldDependency.Match(deps);

                        if (match.Success && match.Index < manualIndex)
                        {
                            var oldPackageVersion = match.Groups[1].Value;

                            if (oldPackageVersion != packageVersion)
                            {
                                if (!_ignorePackages.Contains(normalizedPackageName))
                                {
                                    if (oldPackageVersion != packageVersion)
                                    {
                                        Console.WriteLine($"[Core-Setup/CoreFx] Updated {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                                        deps = deps.Replace(
                                            $"<{normalizedPackageName}>{oldPackageVersion}</{normalizedPackageName}>",
                                            $"<{normalizedPackageName}>{packageVersion}</{normalizedPackageName}>");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Skipped {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                                }
                            }
                        }

                        line = sr.ReadLine();
                    }
                }
            }

            return deps;
        }

        private static async Task<string> PatchVersionsDetailsCoreSetupCoreFxVersionAsync(string deps)
        {
            var doc = XDocument.Parse(deps);

            foreach (var source in new[] { _latestCoreSetupPackagesFilename, _latestCoreFxPackagesFilename })
            {
                foreach (var node in doc.Root.Elements().SelectMany(x => x.Elements("Dependency")))
                {
                    // Load latest dependencies from core-setup
                    var latestPackages = await File.ReadAllTextAsync(source);

                    using (var sr = new StringReader(latestPackages))
                    {
                        var line = sr.ReadLine();

                        while (!String.IsNullOrEmpty(line))
                        {
                            var parts = line.Split(' ');

                            if (parts.Length != 2)
                            {
                                throw new ApplicationException($"Expected 2 parts in latest core-setup packages: {line}");
                            }

                            var packageName = parts[0];
                            var packageVersion = parts[1];

                            var normalizedPackageName = packageName.Replace(".", "") + "PackageVersion";

                            if (node.Attribute("Name").Value == packageName)
                            {
                                var oldPackageVersion = node.Attribute("Version").Value;

                                if (oldPackageVersion != packageVersion)
                                {
                                    if (!_ignorePackages.Contains(normalizedPackageName))
                                    {
                                        if (oldPackageVersion != packageVersion)
                                        {
                                            Console.WriteLine($"[Core-Setup/CoreFx] Updated {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                                            deps = deps.Replace(
                                                $"<Dependency Name=\"{packageName}\" Version=\"{oldPackageVersion}\"",
                                                $"<Dependency Name=\"{packageName}\" Version=\"{packageVersion}\""
                                            );
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Skipped {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                                    }
                                }
                            }

                            line = sr.ReadLine();
                        }
                    }
                }
            }

            return deps;
        }

        private static async Task UpdateAspNetCoreDependencies()
        {
            if (!File.Exists(_efcoreVersionsFilename))
            {
                Log($"{_efcoreVersionsFilename} not found. Use 'efcore' before 'aspnetcore'.");

                return;
            }

            // Load existing dependencies 
            var source = await DownloadContentAsync(_aspnetCoreDependencies);

            // Apply version changes from core-setup and corefx
            var deps = await PatchVersionsPropsCoreSetupCoreFxVersionAsync(source);

            // The versions in aspnet/Extensions are updated at that point to overwrite any from core-setup/corefx
            // This is done such that AspNetCore and Extensions use the same versions

            var extensionsDeps = await File.ReadAllTextAsync(_extensionsVersionsFilename);

            var extensionsDoc = XDocument.Parse(extensionsDeps);

            var allPackageElements = extensionsDoc.Root.Elements("PropertyGroup").SelectMany(x => x.Elements());

            foreach (var el in allPackageElements)
            {
                var normalizedPackageName = el.Name.ToString();
                var packageVersion = el.Value;

                // Performance is not a concern
                var oldDependency = new Regex($@"\<{normalizedPackageName}\>([\w\-\.]+)\</{normalizedPackageName}\>");

                // Search for this package in the existing dependencies
                var match = oldDependency.Match(deps);

                if (match.Success)
                {
                    var oldPackageVersion = match.Groups[1].Value;

                    if (!_ignorePackages.Contains(normalizedPackageName))
                    {
                        if (oldPackageVersion != packageVersion)
                        {
                            Console.WriteLine($"[Extensions deps] Updated {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                            deps = deps.Replace(
                                $"<{normalizedPackageName}>{oldPackageVersion}</{normalizedPackageName}>",
                                $"<{normalizedPackageName}>{packageVersion}</{normalizedPackageName}>");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Skipped {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                    }
                }
            }

            // Seek the latest built Extension packages
            var extensionsMygetVersion = await GetLatestAspNetCoreMygetVersion(_extensionsPackageId, _extensionsVersionPrefix);

            foreach (var normalizedPackageName in _extensionsPackageNames)
            {
                // Performance is not a concern
                var oldDependency = new Regex($@"\<{normalizedPackageName}\>([\w\-\.]+)\</{normalizedPackageName}\>");

                // Search for this package in the existing dependencies
                var match = oldDependency.Match(deps);

                if (match.Success)
                {
                    var oldPackageVersion = match.Groups[1].Value;

                    if (!_ignorePackages.Contains(normalizedPackageName))
                    {
                        if (oldPackageVersion != extensionsMygetVersion)
                        {
                            Console.WriteLine($"[Extensions package] Updated {normalizedPackageName} {oldPackageVersion} -> {extensionsMygetVersion}");
                            deps = deps.Replace(
                                $"<{normalizedPackageName}>{oldPackageVersion}</{normalizedPackageName}>",
                                $"<{normalizedPackageName}>{extensionsMygetVersion}</{normalizedPackageName}>");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Skipped {normalizedPackageName} {oldPackageVersion} -> {extensionsMygetVersion}");
                    }
                }
            }

            // The versions in aspnet/EntityFramework are updated at that point to overwrite any from core-setup/corefx
            // This is done such that AspNetCore and Extensions use the same versions

            var efCoreDeps = await File.ReadAllTextAsync(_efcoreVersionsFilename);

            var efCoreDoc = XDocument.Parse(efCoreDeps);

            var allEfCorePackageElements = efCoreDoc.Root.Elements("PropertyGroup").SelectMany(x => x.Elements());

            foreach (var el in allEfCorePackageElements)
            {
                var normalizedPackageName = el.Name.ToString();
                var packageVersion = el.Value;

                // Performance is not a concern
                var oldDependency = new Regex($@"\<{normalizedPackageName}\>([\w\-\.]+)\</{normalizedPackageName}\>");

                // Search for this package in the existing dependencies
                var match = oldDependency.Match(deps);

                if (match.Success)
                {
                    var oldPackageVersion = match.Groups[1].Value;

                    if (!_ignorePackages.Contains(normalizedPackageName))
                    {
                        if (oldPackageVersion != packageVersion)
                        {
                            if (oldPackageVersion != packageVersion)
                            {
                                Console.WriteLine($"[EntityFrameworkCore deps] Updated {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                                deps = deps.Replace(
                                    $"<{normalizedPackageName}>{oldPackageVersion}</{normalizedPackageName}>",
                                    $"<{normalizedPackageName}>{packageVersion}</{normalizedPackageName}>");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Skipped {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                    }
                }
            }

            // Seek the latest built EntityFramework packages

            var efCoreMygetVersion = await GetLatestAspNetCoreMygetVersion(_efCorePackageId, _efCoreVersionPrefix);

            foreach (var normalizedPackageName in _efCorePackageNames)
            {
                // Performance is not a concern
                var oldDependency = new Regex($@"\<{normalizedPackageName}\>([\w\-\.]+)\</{normalizedPackageName}\>");

                // Search for this package in the existing dependencies
                var match = oldDependency.Match(deps);

                if (match.Success)
                {
                    var oldPackageVersion = match.Groups[1].Value;

                    if (!_ignorePackages.Contains(normalizedPackageName))
                    {
                        if (oldPackageVersion != efCoreMygetVersion)
                        {
                            Console.WriteLine($"[EntityFrameworkCore package] Updated {normalizedPackageName} {oldPackageVersion} -> {efCoreMygetVersion}");
                            deps = deps.Replace(
                                $"<{normalizedPackageName}>{oldPackageVersion}</{normalizedPackageName}>",
                                $"<{normalizedPackageName}>{efCoreMygetVersion}</{normalizedPackageName}>");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Skipped {normalizedPackageName} {oldPackageVersion} -> {efCoreMygetVersion}");
                    }
                }
            }

            File.WriteAllText(_aspnetCoreDependenciesFilename, deps);
        }

        private static async Task UpdateEfCoreDependencies()
        {
            if (!File.Exists(_extensionsVersionsFilename))
            {
                Log($"{_extensionsVersionsFilename} not found. Use 'extensions' before 'efcore'.");

                return;
            }

            // Load existing dependencies file from aspnet/Extensions
            await DownloadFileAsync(_efCoreVersions, _efcoreVersionsFilename);
            await DownloadFileAsync(_efCoreDetails, _efcoreDetailsFilename);

            var efCoreVersionsSource = await File.ReadAllTextAsync(_efcoreVersionsFilename);
            var efCoreDetailsSource = await File.ReadAllTextAsync(_efcoreDetailsFilename);

            // Apply version changes from core-setup and corefx
            efCoreVersionsSource = await PatchVersionsPropsCoreSetupCoreFxVersionAsync(efCoreVersionsSource);

            // Apply versions from the locally built extensions dependencies
            (efCoreVersionsSource, efCoreDetailsSource) = await ApplyDetailsFiles(_extensionsDetailsFilename, efCoreVersionsSource, efCoreDetailsSource);

            // Seek the latest built Extension packages
            var extensionsMygetVersion = await GetLatestAspNetCoreMygetVersion(_extensionsPackageId, _extensionsVersionPrefix);
            var extensionsSha = await GetPackageSha(_extensionsPackageId, extensionsMygetVersion);

            //Update the versions file with extensions version
            var versions = JsonConvert.DeserializeObject<Version>(File.ReadAllText(_versionsFilename));
            versions.ExtensionsVersion = extensionsMygetVersion;
            versions.ExtensionsSha = extensionsSha;
            File.WriteAllText(_versionsFilename, JsonConvert.SerializeObject(versions));

            // Apply versions from from myget version
            (efCoreVersionsSource, efCoreDetailsSource) = await ApplySpecificVersion(extensionsMygetVersion, _extensionsPackageNames, efCoreVersionsSource, efCoreDetailsSource);

            // Apply shas
            efCoreDetailsSource = PatchVersionShas(efCoreDetailsSource);

            File.WriteAllText(_efcoreVersionsFilename, efCoreVersionsSource);
            File.WriteAllText(_efcoreDetailsFilename, efCoreDetailsSource);
        }

        private static async Task<(string versions, string details)> ApplyDetailsFiles(string detailsFilename, string sourceVersions, string sourceDetails)
        {
            // The versions in aspnet/Extensions are updated at that point to overwrite any from core-setup/corefx

            var extensionsDetails = await File.ReadAllTextAsync(detailsFilename);

            var extensionsDoc = XDocument.Parse(extensionsDetails);

            var extensionsDependencies = extensionsDoc.Root.Elements("ProductDependencies").SelectMany(x => x.Elements());

            foreach (var el in extensionsDependencies)
            {
                var packageName = el.Attribute("Name").Value;
                var normalizedPackageName = packageName.Replace(".", "") + "PackageVersion";
                var packageVersion = el.Attribute("Version").Value;

                // Performance is not a concern
                var oldDependency = new Regex($@"\<{normalizedPackageName}\>([\w\-\.]+)\</{normalizedPackageName}\>");

                // Search for this package in the existing dependencies
                var match = oldDependency.Match(sourceVersions);

                if (match.Success)
                {
                    var oldPackageVersion = match.Groups[1].Value;

                    if (!_ignorePackages.Contains(normalizedPackageName))
                    {
                        if (oldPackageVersion != packageVersion)
                        {
                            Console.WriteLine($"[Extensions deps] Updated {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                            sourceVersions = sourceVersions.Replace(
                                $"<{normalizedPackageName}>{oldPackageVersion}</{normalizedPackageName}>",
                                $"<{normalizedPackageName}>{packageVersion}</{normalizedPackageName}>");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Skipped {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                    }
                }
            }

            var detailsDoc = XDocument.Parse(sourceDetails);

            foreach (var el in extensionsDependencies)
            {
                var packageName = el.Attribute("Name").Value;
                var normalizedPackageName = packageName.Replace(".", "") + "PackageVersion";
                var packageVersion = el.Attribute("Version").Value;

                foreach (var node in detailsDoc.Root.Elements().SelectMany(x => x.Elements("Dependency")))
                {
                    if (node.Attribute("Name").Value == packageName)
                    {
                        var oldPackageVersion = node.Attribute("Version").Value;

                        if (!_ignorePackages.Contains(normalizedPackageName))
                        {
                            if (oldPackageVersion != packageVersion)
                            {
                                Console.WriteLine($"[Extensions deps] Updated {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                                sourceDetails = sourceDetails.Replace(
                                    $"<Dependency Name=\"{packageName}\" Version=\"{oldPackageVersion}\"",
                                    $"<Dependency Name=\"{packageName}\" Version=\"{packageVersion}\""
                                );
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Skipped {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                        }
                    }
                }
            }

            return (sourceVersions, sourceDetails);
        }

        private static async Task<(string versions, string details)> ApplySpecificVersion(string packageVersion, IEnumerable<string> packageNames, string sourceVersions, string sourceDetails)
        {
            var detailsDoc = XDocument.Parse(sourceDetails);

            foreach (var normalizedPackageName in packageNames)
            {
                // Performance is not a concern
                var oldDependency = new Regex($@"\<{normalizedPackageName}\>([\w\-\.]+)\</{normalizedPackageName}\>");

                // Search for this package in the existing dependencies
                var match = oldDependency.Match(sourceVersions);

                if (match.Success)
                {
                    var oldPackageVersion = match.Groups[1].Value;

                    if (!_ignorePackages.Contains(normalizedPackageName))
                    {
                        if (oldPackageVersion != packageVersion)
                        {
                            Console.WriteLine($"Updated {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                            sourceVersions = sourceVersions.Replace(
                                $"<{normalizedPackageName}>{oldPackageVersion}</{normalizedPackageName}>",
                                $"<{normalizedPackageName}>{packageVersion}</{normalizedPackageName}>");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Skipped {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                    }
                }

                // DETAILS FILE

                var depdendencies = detailsDoc.Root.Elements("ProductDependencies").SelectMany(x => x.Elements());

                foreach (var node in depdendencies)
                {
                    var packageName = node.Attribute("Name").Value;

                    if (normalizedPackageName != packageName.Replace(".", "") + "PackageVersion")
                    {
                        continue;
                    }

                    var oldPackageVersion = node.Attribute("Version").Value;

                    if (!_ignorePackages.Contains(normalizedPackageName))
                    {
                        if (oldPackageVersion != packageVersion)
                        {
                            Console.WriteLine($"[Extensions deps] Updated {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                            sourceDetails = sourceDetails.Replace(
                                $"<Dependency Name=\"{packageName}\" Version=\"{oldPackageVersion}\"",
                                $"<Dependency Name=\"{packageName}\" Version=\"{packageVersion}\""
                            );
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Skipped {normalizedPackageName} {oldPackageVersion} -> {packageVersion}");
                    }
                }
            }

            return (sourceVersions, sourceDetails);
        }

        private static async Task UpdateExtensionsDependencies()
        {
            if (!File.Exists(_latestCoreSetupPackagesFilename))
            {
                Log($"{_latestCoreSetupPackagesFilename} not found. Use 'coherence' before 'extensions'.");

                return;
            }

            // Load existing dependencies file from aspnet/Extensions
            await DownloadFileAsync(_extensionsVersions, _extensionsVersionsFilename);
            await DownloadFileAsync(_extensionsDetails, _extensionsDetailsFilename);

            var versions = await File.ReadAllTextAsync(_extensionsVersionsFilename);

            Log("UPDATING Extensions/Versions.props");

            versions = await PatchVersionsPropsCoreSetupCoreFxVersionAsync(versions);
            File.WriteAllText(_extensionsVersionsFilename, versions);

            var details = await File.ReadAllTextAsync(_extensionsDetailsFilename);

            Log("UPDATING Extensions/Version.Details.props");

            details = await PatchVersionsDetailsCoreSetupCoreFxVersionAsync(details);
            details = PatchVersionShas(details);

            File.WriteAllText(_extensionsDetailsFilename, details);

            Log("SUCCESS");
        }

        private static string PatchVersionShas(string source)
        {
            var versions = JsonConvert.DeserializeObject<Version>(File.ReadAllText(_versionsFilename));
            var doc = XDocument.Parse(source);

            foreach (var node in doc.Root.Elements().SelectMany(x => x.Elements("Dependency")))
            {
                switch(node.Element("Uri").Value)
                {
                    case "https://github.com/dotnet/corefx":
                        var oldCoreFxSha = node.Element("Sha").Value;
                        source = source.Replace($"<Sha>{oldCoreFxSha}</Sha>", $"<Sha>{versions.CoreFxSha}</Sha>");
                        break;

                    case "https://github.com/dotnet/core-setup":
                        var oldCoreSetupSha = node.Element("Sha").Value;
                        source = source.Replace($"<Sha>{oldCoreSetupSha}</Sha>", $"<Sha>{versions.CoreSetupSha}</Sha>");
                        break;

                    case "https://github.com/aspnet/Extensions":
                        var oldExtensionsSha = node.Element("Sha").Value;
                        source = source.Replace($"<Sha>{oldExtensionsSha}</Sha>", $"<Sha>{versions.ExtensionsSha}</Sha>");
                        break;
                }
            }

            return source;
        }

        private static void LoadSettings(IConfiguration config)
        {
            // Tip: The repository id van be found using this endpoint: https://api.github.com/repos/aspnet/Benchmarks

            long.TryParse(config["RepositoryId"], out _repositoryId);
            _accessToken = config["AccessToken"];
            _username = config["Username"];

            if (String.IsNullOrEmpty(_accessToken))
            {
                throw new ArgumentException("AccessToken argument is missing");
            }

            if (String.IsNullOrEmpty(_username))
            {
                throw new ArgumentException("BotUsername argument is missing");
            }
        }

        private static async Task<string> GetLatestAspNetCoreMygetVersion(string packageId, string versionPrefix)
        {
            var index = JObject.Parse(await DownloadContentAsync($"https://dotnet.myget.org/F/aspnetcore-dev/api/v3/registration1/{packageId}/index.json"));

            var compatiblePages = index["items"].Where(t => ((string)t["lower"]).StartsWith(versionPrefix)).ToArray();

            // All versions might be comprised in a single page, with lower and upper bounds not matching the prefix
            if (!compatiblePages.Any())
            {
                compatiblePages = index["items"].ToArray();
            }

            foreach(var page in compatiblePages.Reverse())
            {
                var lastPageUrl = (string)page["@id"];

                var lastPage = JObject.Parse(await DownloadContentAsync(lastPageUrl));

                // Extract the highest version
                var lastEntry = lastPage["items"]
                    .Where(t => ((string)t["catalogEntry"]["version"]).StartsWith(versionPrefix)).LastOrDefault();

                if (lastEntry != null)
                {
                    return (string)lastEntry["catalogEntry"]["version"];
                }
            }

            return null;
        }

        //private static async Task CreateIssue(IEnumerable<Regression> regressions)
        //{
        //    if (regressions == null || !regressions.Any())
        //    {
        //        return;
        //    }

        //    var client = new GitHubClient(_productHeaderValue);
        //    client.Credentials = new Credentials(_accessToken);

        //    var body = new StringBuilder();
        //    body.Append("A performance regression has been detected for the following scenarios:");

        //    foreach (var r in regressions.OrderBy(x => x.Scenario).ThenBy(x => x.DateTimeUtc))
        //    {
        //        body.AppendLine();
        //        body.AppendLine();
        //        body.AppendLine("| Scenario | Environment | Date | Old RPS | New RPS | Change | Deviation |");
        //        body.AppendLine("| -------- | ----------- | ---- | ------- | ------- | ------ | --------- |");

        //        var prevRPS = r.Values.Skip(2).First();
        //        var rps = r.Values.Last();
        //        var change = Math.Round((double)(rps - prevRPS) / prevRPS * 100, 2);
        //        var deviation = Math.Round((double)(rps - prevRPS) / r.Stdev, 2);

        //        body.AppendLine($"| {r.Scenario} | {r.OperatingSystem}, {r.Scheme}, {r.WebHost} | {r.DateTimeUtc.ToString("u")} | {prevRPS.ToString("n0")} | {rps.ToString("n0")} | {change} % | {deviation} σ |");


        //        body.AppendLine();
        //        body.AppendLine("Before versions:");

        //        body.AppendLine($"Microsoft.AspNetCore.App __{r.PreviousAspNetCoreVersion}__");
        //        body.AppendLine($"Microsoft.NetCore.App __{r.PreviousRuntimeVersion}__");

        //        body.AppendLine();
        //        body.AppendLine("After versions:");

        //        body.AppendLine($"Microsoft.AspNetCore.App __{r.CurrentAspNetCoreVersion}__");
        //        body.AppendLine($"Microsoft.NetCore.App __{r.CurrentRuntimeVersion}__");

        //        var aspNetChanged = r.PreviousAspNetCoreVersion != r.CurrentAspNetCoreVersion;
        //        var runtimeChanged = r.PreviousRuntimeVersion != r.CurrentRuntimeVersion;

        //        if (aspNetChanged || runtimeChanged)
        //        {
        //            body.AppendLine();
        //            body.AppendLine("Commits:");

        //            if (aspNetChanged)
        //            {
        //                if (r.AspNetCoreHashes != null && r.AspNetCoreHashes.Length == 2 && r.AspNetCoreHashes[0] != null && r.AspNetCoreHashes[1] != null)
        //                {
        //                    body.AppendLine();
        //                    body.AppendLine("__Microsoft.AspNetCore.App__");
        //                    body.AppendLine($"https://github.com/aspnet/AspNetCore/compare/{r.AspNetCoreHashes[0]}...{r.AspNetCoreHashes[1]}");
        //                }
        //            }

        //            if (runtimeChanged)
        //            {
        //                if (r.CoreFxHashes != null && r.CoreFxHashes.Length == 2 && r.CoreFxHashes[0] != null && r.CoreFxHashes[1] != null)
        //                {
        //                    body.AppendLine();
        //                    body.AppendLine("__Microsoft.NetCore.App / Core FX__");
        //                    body.AppendLine($"https://github.com/dotnet/corefx/compare/{r.CoreFxHashes[0]}...{r.CoreFxHashes[1]}");
        //                }

        //                if (r.CoreClrHashes != null && r.CoreClrHashes.Length == 2 && r.CoreClrHashes[0] != null && r.CoreClrHashes[1] != null)
        //                {
        //                    body.AppendLine();
        //                    body.AppendLine("__Microsoft.NetCore.App / Core CLR__");
        //                    body.AppendLine($"https://github.com/dotnet/coreclr/compare/{r.CoreClrHashes[0]}...{r.CoreClrHashes[1]}");
        //                }
        //            }
        //        }
        //    }

        //    var title = "Performance regression: " + String.Join(", ", regressions.Select(x => x.Scenario).Take(5));

        //    if (regressions.Count() > 5)
        //    {
        //        title += " ...";
        //    }

        //    var createIssue = new NewIssue(title)
        //    {
        //        Body = body.ToString()
        //    };

        //    createIssue.Labels.Add("perf-regression");

        //    Console.Write(createIssue.Body);

        //    var issue = await client.Issue.Create(_repositoryId, createIssue);
        //}

        private static async Task<bool> DownloadFileAsync(string url, string outputPath, int maxRetries = 3, int timeout = 5)
        {
            //Log($"Downloading '{url}'");

            for (var i = 0; i < maxRetries; ++i)
            {
                try
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
                    var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead, cts.Token);
                    response.EnsureSuccessStatusCode();

                    // This probably won't use async IO on windows since the stream
                    // needs to created with the right flags
                    using (var stream = File.Create(outputPath))
                    {
                        // Copy the response stream directly to the file stream
                        await response.Content.CopyToAsync(stream);
                    }

                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: Failed downloading '{url}':");
                    Console.WriteLine(e);
                }
            }

            return false;
        }

        private static async Task<string> DownloadContentAsync(string url, int maxRetries = 3, int timeout = 5)
        {
            for (var i = 0; i < maxRetries; ++i)
            {
                try
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
                    return await _httpClient.GetStringAsync(url);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error while downloading {url}:");
                    Console.WriteLine(e);
                }
            }

            throw new ApplicationException($"Error while downloading {url} after {maxRetries} attempts");
        }

        private static async Task<string> GetRuntimeAssemblyVersion(string netCoreAppVersion, string assemblyName)
        {
            var packagePath = Path.GetTempFileName();

            try
            {
                // Download the runtime

                var netCoreAppUrl = String.Format(_netCoreUrlPrefix, netCoreAppVersion);
                if (!await DownloadFileAsync(netCoreAppUrl, packagePath))
                {
                    return null;
                }

                // Extract the .nuspec file

                using (var archive = ZipFile.OpenRead(packagePath))
                {
                    var versionAssemblyPath = Path.GetTempFileName();

                    try
                    {
                        var entry = archive.GetEntry($@"shared\Microsoft.NETCore.App\{netCoreAppVersion}\{assemblyName}");
                        if (entry == null)
                        {
                            entry = archive.GetEntry($@"shared/Microsoft.NETCore.App/{netCoreAppVersion}/{assemblyName}");
                        }

                        entry.ExtractToFile(versionAssemblyPath, true);

                        using (var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(versionAssemblyPath))
                        {
                            var informationalVersionAttribute = assembly.CustomAttributes.Where(x => x.AttributeType.Name == "AssemblyInformationalVersionAttribute").FirstOrDefault();
                            var argumentValule = informationalVersionAttribute.ConstructorArguments[0].Value.ToString();

                            return argumentValule;
                        }
                    }
                    finally
                    {
                        try
                        {
                            File.Delete(versionAssemblyPath);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    File.Delete(packagePath);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: Failed to delete file {packagePath}");
                    Console.WriteLine(e);
                }
            }
        }

        private static async Task<string> GetPackageSha(string packageId, string packageVersion)
        {
            var packagePath = Path.GetTempFileName();

            try
            {
                // Download the runtime

                var packageUrl = $"https://dotnet.myget.org/F/aspnetcore-dev/api/v2/package/{packageId}/{packageVersion}";
                if (!await DownloadFileAsync(packageUrl, packagePath))
                {
                    return null;
                }

                // Extract the .nuspec file

                using (var archive = ZipFile.OpenRead(packagePath))
                {
                    var nuspecPath = Path.GetTempFileName();

                    try
                    {
                        var entry = archive.GetEntry($"{packageId}.nuspec");
                        entry.ExtractToFile(nuspecPath, true);
                        var root = XDocument.Parse(File.ReadAllText(nuspecPath)).Root;

                        XNamespace xmlns = root.Attribute("xmlns").Value;
                        return root
                            .Element(xmlns + "metadata")
                            .Element(xmlns + "repository")
                            .Attribute("commit").Value;
                    }
                    finally
                    {
                        try
                        {
                            File.Delete(nuspecPath);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    File.Delete(packagePath);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: Failed to delete file {packagePath}");
                    Console.WriteLine(e);
                }
            }
        }

        private static async Task<string> GetRuntimeSha(string netCoreAppVersion)
        {
            var packagePath = Path.GetTempFileName();

            try
            {
                // Download the runtime

                var netCoreAppUrl = String.Format(_netCoreUrlPrefix, netCoreAppVersion);
                if (!await DownloadFileAsync(netCoreAppUrl, packagePath))
                {
                    return null;
                }

                // Extract the .nuspec file

                using (var archive = ZipFile.OpenRead(packagePath))
                {
                    var versionAssemblyPath = Path.GetTempFileName();

                    try
                    {
                        var entry = archive.GetEntry($@"shared\Microsoft.NETCore.App\{netCoreAppVersion}\.version")
                            ?? archive.GetEntry($@"shared/Microsoft.NETCore.App/{netCoreAppVersion}/.version");

                        entry.ExtractToFile(versionAssemblyPath, true);
                        return File.ReadAllText(versionAssemblyPath);
                    }
                    finally
                    {
                        try
                        {
                            File.Delete(versionAssemblyPath);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    File.Delete(packagePath);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"ERROR: Failed to delete file {packagePath}");
                    Console.WriteLine(e);
                }
            }
        }

        private class Version
        {
            public string CoreFxVersion { get; set; }
            public string CoreFxSha { get; set; }
            public string MicrosoftNetCoreAppVersion { get; set; }
            public string CoreSetupSha { get; set; }
            public string ExtensionsVersion { get; set; }
            public string ExtensionsSha { get; set; }
        }
    }
}
