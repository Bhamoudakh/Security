// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Authentication
{
    public class AuthenticationHandlerFacts
    {
        [Fact]
        public async Task ShouldHandleSchemeAreDeterminedOnlyByMatchingAuthenticationScheme()
        {
            var handler = await TestHandler.Create("Alpha");
            var passiveNoMatch = handler.ShouldHandleScheme("Beta", handleAutomatic: false);

            handler = await TestHandler.Create("Alpha");
            var passiveWithMatch = handler.ShouldHandleScheme("Alpha", handleAutomatic: false);

            Assert.False(passiveNoMatch);
            Assert.True(passiveWithMatch);
        }

        [Fact]
        public async Task AutomaticHandlerInAutomaticModeHandlesEmptyChallenges()
        {
            var handler = await TestAutoHandler.Create("ignored", true);
            Assert.True(handler.ShouldHandleScheme(AuthenticationManager.AutomaticScheme, handleAutomatic: true));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("        ")]
        [InlineData("notmatched")]
        public async Task AutomaticHandlerDoesNotHandleSchemes(string scheme)
        {
            var handler = await TestAutoHandler.Create("ignored", true);
            Assert.False(handler.ShouldHandleScheme(scheme, handleAutomatic: true));
        }

        [Fact]
        public async Task AutomaticHandlerShouldHandleSchemeWhenSchemeMatches()
        {
            var handler = await TestAutoHandler.Create("Alpha", true);
            Assert.True(handler.ShouldHandleScheme("Alpha", handleAutomatic: true));
        }

        [Fact]
        public async Task AutomaticHandlerShouldNotHandleChallengeWhenSchemesNotEmpty()
        {
            var handler = await TestAutoHandler.Create(null, true);
            Assert.False(handler.ShouldHandleScheme("Alpha", handleAutomatic: true));
        }

        [Theory]
        [InlineData("Alpha")]
        [InlineData("Automatic")]
        public async Task AuthHandlerAuthenticateCachesTicket(string scheme)
        {
            var handler = await CountHandler.Create(scheme);
            var context = new AuthenticateContext(scheme);
            await handler.AuthenticateAsync(context);
            await handler.AuthenticateAsync(context);
            Assert.Equal(1, handler.AuthCount);
        }

        [Theory]
        [InlineData("Alpha", false)]
        [InlineData("Bravo", true)]
        public async Task AuthHandlerChallengeCallsPriorHandlerIfNotHandled(string challenge, bool passedThrough)
        {
            var handler = await TestHandler.Create("Alpha");
            var previous = new PreviousHandler();

            handler.PriorHandler = previous;
            await handler.ChallengeAsync(new ChallengeContext(challenge));
            Assert.Equal(passedThrough, previous.ChallengeCalled);
        }

        private class PreviousHandler : IAuthenticationHandler
        {
            public bool ChallengeCalled = false;

            public Task AuthenticateAsync(AuthenticateContext context)
            {
                throw new NotImplementedException();
            }

            public Task ChallengeAsync(ChallengeContext context)
            {
                ChallengeCalled = true;
                return Task.FromResult(0);
            }

            public void GetDescriptions(DescribeSchemesContext context)
            {
                throw new NotImplementedException();
            }

            public Task SignInAsync(SignInContext context)
            {
                throw new NotImplementedException();
            }

            public Task SignOutAsync(SignOutContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class CountOptions : AuthenticationOptions { }

        private class CountHandler : AuthenticationHandler<CountOptions>
        {
            public int AuthCount = 0;

            private CountHandler() { }

            public static async Task<CountHandler> Create(string scheme)
            {
                var handler = new CountHandler();
                var context = new DefaultHttpContext();
                context.Features.Set<IHttpResponseFeature>(new TestResponse());
                await handler.InitializeAsync(
                    new CountOptions(), context,
                    new LoggerFactory().CreateLogger("CountHandler"),
                    UrlEncoder.Default);
                handler.Options.AuthenticationScheme = scheme;
                handler.Options.AutomaticAuthenticate = true;
                return handler;
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                AuthCount++;
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), new AuthenticationProperties(), "whatever")));
            }

        }

        private class TestHandler : AuthenticationHandler<TestOptions>
        {
            private TestHandler() { }

            public AuthenticateResult Result = AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), new AuthenticationProperties(), "whatever"));

            public static async Task<TestHandler> Create(string scheme)
            {
                var handler = new TestHandler();
                var context = new DefaultHttpContext();
                context.Features.Set<IHttpResponseFeature>(new TestResponse());
                await handler.InitializeAsync(
                    new TestOptions(), context,
                    new LoggerFactory().CreateLogger("TestHandler"),
                    UrlEncoder.Default);
                handler.Options.AuthenticationScheme = scheme;
                return handler;
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                return Task.FromResult(Result);
            }
        }

        private class TestOptions : AuthenticationOptions { }

        private class TestAutoOptions : AuthenticationOptions
        {
            public TestAutoOptions()
            {
                AutomaticAuthenticate = true;
            }
        }

        private class TestAutoHandler : AuthenticationHandler<TestAutoOptions>
        {
            private TestAutoHandler() { }

            public static async Task<TestAutoHandler> Create(string scheme, bool auto)
            {
                var handler = new TestAutoHandler();
                var context = new DefaultHttpContext();
                context.Features.Set<IHttpResponseFeature>(new TestResponse());
                await handler.InitializeAsync(
                    new TestAutoOptions(), context,
                    new LoggerFactory().CreateLogger("TestAutoHandler"),
                    UrlEncoder.Default);
                handler.Options.AuthenticationScheme = scheme;
                handler.Options.AutomaticAuthenticate = auto;
                return handler;
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), new AuthenticationProperties(), "whatever")));
            }
        }

        private class TestResponse : IHttpResponseFeature
        {
            public Stream Body
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public bool HasStarted
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public IHeaderDictionary Headers
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public string ReasonPhrase
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public int StatusCode
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                }
            }

            public void OnCompleted(Func<object, Task> callback, object state)
            {
                throw new NotImplementedException();
            }

            public void OnStarting(Func<object, Task> callback, object state)
            {
            }
        }
    }
}
