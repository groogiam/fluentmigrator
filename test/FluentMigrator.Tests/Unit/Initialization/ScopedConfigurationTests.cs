#region License
//
// Copyright (c) 2018, Fluent Migrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System.Collections.Generic;
using System.IO;
using System.Threading;

using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using NUnit.Framework;

namespace FluentMigrator.Tests.Unit.Initialization
{
    [Parallelizable(ParallelScope.All)]
    [Category("Initialization")]
    public class ScopedConfigurationTests
    {
        [Test]
        public void TestConfiguredProcessorOptions()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string>()
                    {
                        ["ProcessorOptions:ConnectionString"] = "Data Source=:memory:"
                    })
                .Build();

            using (var serviceProvider = ServiceCollectionExtensions
                .CreateServices(false)
                .Configure<ProcessorOptions>(config.GetSection("ProcessorOptions"))
                .BuildServiceProvider(true))
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var accessor = scope.ServiceProvider.GetRequiredService<IConnectionStringAccessor>();
                    Assert.AreEqual("Data Source=:memory:", accessor.ConnectionString);
                }
            }
        }

        [Test]
        public void TestReconfiguredProcessorOptions()
        {
            var jsonFileName = Path.GetTempFileName();

            try
            {
                var customConfig = new CustomConfig()
                {
                    ProcessorOptions = new ProcessorOptions()
                    {
                        ConnectionString = "Data Source=:memory:",
                    }
                };

                SaveConfigFile(jsonFileName, customConfig);

                var config = new ConfigurationBuilder()
                    .AddJsonFile(
                        source =>
                        {
                            source.Path = jsonFileName;
                            source.ReloadOnChange = true;
                            source.ReloadDelay = 50;
                            source.ResolveFileProvider();
                        })
                    .Build();

                using (var serviceProvider = ServiceCollectionExtensions
                    .CreateServices(false)
                    .Configure<ProcessorOptions>(config.GetSection("ProcessorOptions"))
                    .BuildServiceProvider(true))
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var accessor = scope.ServiceProvider.GetRequiredService<IConnectionStringAccessor>();
                        Assert.AreEqual("Data Source=:memory:", accessor.ConnectionString);
                    }

                    customConfig.ProcessorOptions.ConnectionString = "Data Source=test.db";
                    SaveConfigFile(jsonFileName, customConfig);

                    Thread.Sleep(millisecondsTimeout: 250);

                    using (var scope = serviceProvider.CreateScope())
                    {
                        var accessor = scope.ServiceProvider.GetRequiredService<IConnectionStringAccessor>();
                        Assert.AreEqual("Data Source=test.db", accessor.ConnectionString);
                    }
                }
            }
            finally
            {
                File.Delete(jsonFileName);
            }
        }

        [Test]
        public void TestConfiguredProcessorOptionsUsingConnectionName()
        {
            var jsonFileName = Path.GetTempFileName();

            try
            {
                var customConfig = new CustomConfig()
                {
                    ProcessorOptions = new ProcessorOptions()
                    {
                        ConnectionString = "SQLite",
                    },
                    ConnectionStrings = new Dictionary<string, string>()
                    {
                        ["SQLite"] = "Data Source=:memory:",
                    }
                };

                SaveConfigFile(jsonFileName, customConfig);

                var config = new ConfigurationBuilder()
                    .AddJsonFile(
                        source =>
                        {
                            source.Path = jsonFileName;
                            source.ReloadOnChange = true;
                            source.ReloadDelay = 50;
                            source.ResolveFileProvider();
                        })
                    .Build();

                using (var serviceProvider = ServiceCollectionExtensions
                    .CreateServices(false)
                    .Configure<ProcessorOptions>(config.GetSection("ProcessorOptions"))
                    .AddSingleton<IConfiguration>(config)
                    .BuildServiceProvider(true))
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var accessor = scope.ServiceProvider.GetRequiredService<IConnectionStringAccessor>();
                        Assert.AreEqual("Data Source=:memory:", accessor.ConnectionString);
                    }
                }
            }
            finally
            {
                File.Delete(jsonFileName);
            }
        }

        [Test]
        public void TestReconfiguredProcessorOptionsUsingConnectionName()
        {
            var jsonFileName = Path.GetTempFileName();

            try
            {
                var customConfig = new CustomConfig()
                {
                    ProcessorOptions = new ProcessorOptions()
                    {
                        ConnectionString = "SQLite",
                    },
                    ConnectionStrings = new Dictionary<string, string>()
                    {
                        ["SQLite"] = "Data Source=:memory:",
                    }
                };

                SaveConfigFile(jsonFileName, customConfig);

                var config = new ConfigurationBuilder()
                    .AddJsonFile(
                        source =>
                        {
                            source.Path = jsonFileName;
                            source.ReloadOnChange = true;
                            source.ReloadDelay = 50;
                            source.ResolveFileProvider();
                        })
                    .Build();

                using (var serviceProvider = ServiceCollectionExtensions
                    .CreateServices(false)
                    .Configure<ProcessorOptions>(config.GetSection("ProcessorOptions"))
                    .AddSingleton<IConfiguration>(config)
                    .BuildServiceProvider(true))
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var accessor = scope.ServiceProvider.GetRequiredService<IConnectionStringAccessor>();
                        Assert.AreEqual("Data Source=:memory:", accessor.ConnectionString);
                    }

                    customConfig.ConnectionStrings["SQLite"] = "Data Source=test.db";
                    SaveConfigFile(jsonFileName, customConfig);

                    Thread.Sleep(millisecondsTimeout: 250);

                    using (var scope = serviceProvider.CreateScope())
                    {
                        var accessor = scope.ServiceProvider.GetRequiredService<IConnectionStringAccessor>();
                        Assert.AreEqual("Data Source=test.db", accessor.ConnectionString);
                    }
                }
            }
            finally
            {
                File.Delete(jsonFileName);
            }
        }

        [Test]
        public void TestReconfiguredProcessorId()
        {
            var jsonFileName = Path.GetTempFileName();

            try
            {
                var customConfig = new CustomConfig()
                {
                    ProcessorSelectorOptions = new SelectingProcessorAccessorOptions()
                    {
                        ProcessorId = "SQLite",
                    },
                    ConnectionStrings = new Dictionary<string, string>()
                    {
                        ["SQLite"] = "Data Source=:memory:",
                        ["SQLAnywhere16"] = "Data Source=test.db",
                    }
                };

                SaveConfigFile(jsonFileName, customConfig);

                var config = new ConfigurationBuilder()
                    .AddJsonFile(
                        source =>
                        {
                            source.Path = jsonFileName;
                            source.ReloadOnChange = true;
                            source.ReloadDelay = 50;
                            source.ResolveFileProvider();
                        })
                    .Build();

                using (var serviceProvider = ServiceCollectionExtensions
                    .CreateServices(false)
                    .Configure<ProcessorOptions>(config.GetSection("ProcessorOptions"))
                    .Configure<SelectingProcessorAccessorOptions>(config.GetSection("ProcessorSelectorOptions"))
                    .AddSingleton<IConfiguration>(config)
                    .BuildServiceProvider(true))
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var accessor = scope.ServiceProvider.GetRequiredService<IConnectionStringAccessor>();
                        Assert.AreEqual("Data Source=:memory:", accessor.ConnectionString);
                    }

                    customConfig.ProcessorSelectorOptions.ProcessorId = "SqlAnywhere16";
                    SaveConfigFile(jsonFileName, customConfig);

                    Thread.Sleep(millisecondsTimeout: 250);

                    using (var scope = serviceProvider.CreateScope())
                    {
                        var accessor = scope.ServiceProvider.GetRequiredService<IConnectionStringAccessor>();
                        Assert.AreEqual("Data Source=test.db", accessor.ConnectionString);
                    }
                }
            }
            finally
            {
                File.Delete(jsonFileName);
            }
        }

        private static void SaveConfigFile(string jsonFileName, CustomConfig config)
        {
            var serializer = new JsonSerializer()
            {
                NullValueHandling = NullValueHandling.Ignore,
            };

            using (var output = new StreamWriter(jsonFileName))
            {
                using (var writer = new JsonTextWriter(output))
                {
                    serializer.Serialize(writer, config);
                    writer.Flush();
                }
            }
        }

        private class CustomConfig
        {
            public ProcessorOptions ProcessorOptions { get; set; }
            public SelectingProcessorAccessorOptions ProcessorSelectorOptions { get; set; }
            // ReSharper disable once CollectionNeverQueried.Local
            public IDictionary<string, string> ConnectionStrings { get; set; }
        }
    }
}