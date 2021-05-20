// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BasicTestApp;
using BasicTestApp.Reconnection;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit;
using Microsoft.AspNetCore.Components.Lifetime;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class CircuitTests : ServerTestBase<BasicTestAppServerSiteFixture<ServerStartup>>
    {
        public CircuitTests(
            BrowserFixture browserFixture,
            BasicTestAppServerSiteFixture<ServerStartup> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: false);
        }

        [Theory]
        [InlineData("constructor-throw")]
        [InlineData("attach-throw")]
        [InlineData("setparameters-sync-throw")]
        [InlineData("setparameters-async-throw")]
        [InlineData("render-throw")]
        [InlineData("afterrender-sync-throw")]
        [InlineData("afterrender-async-throw")]
        public void ComponentLifecycleMethodThrowsExceptionTerminatesTheCircuit(string id)
        {
            Browser.MountTestComponent<ReliabilityComponent>();
            Browser.Exists(By.Id("thecounter"));

            var targetButton = Browser.Exists(By.Id(id));
            targetButton.Click();

            // Triggering an error will show the exception UI
            Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"));

            // Clicking the button again will trigger a server disconnect
            targetButton.Click();

            AssertLogContains("Connection disconnected.");
        }

        [Fact]
        public void ComponentDisposeMethodThrowsExceptionTerminatesTheCircuit()
        {
            Browser.MountTestComponent<ReliabilityComponent>();
            Browser.Exists(By.Id("thecounter"));
            
            // Arrange
            var targetButton = Browser.Exists(By.Id("dispose-throw"));

            // Clicking the button sets a boolean that renders the component
            targetButton.Click();
            // Clicking it again hides the component and invokes the rethrow which triggers the exception
            targetButton.Click();
            Browser.Exists(By.CssSelector("#blazor-error-ui[style='display: block;']"));

            // Clicking it again causes the circuit to disconnect
            targetButton.Click();
            AssertLogContains("Connection disconnected.");
        }

        [Fact]
        public void OnLocationChanged_ReportsErrorForExceptionInUserCode()
        {
            Browser.MountTestComponent<NavigationFailureComponent>(); 
            var targetButton = Browser.Exists(By.Id("navigate-to-page"));

            targetButton.Click();

            var expectedError = "There was an unhandled exception on the current circuit, so this circuit will be terminated. " +
                "For more details turn on detailed exceptions by setting 'DetailedErrors: true' in 'appSettings.Development.json' or set 'CircuitOptions.DetailedErrors'. " +
                "Location change failed.";

            AssertLogContains(expectedError);
        }

        void AssertLogContains(params string[] messages)
        {
            var log = Browser.Manage().Logs.GetLog(LogType.Browser);
            foreach (var message in messages)
            {
                Assert.Contains(log, entry =>
                {
                    return entry.Level == LogLevel.Info
                    && entry.Message.Contains(message);
                });
            }
        }
    }
}