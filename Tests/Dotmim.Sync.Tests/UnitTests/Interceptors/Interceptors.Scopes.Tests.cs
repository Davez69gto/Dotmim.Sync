﻿using Dotmim.Sync.Builders;
using Dotmim.Sync.Enumerations;
using Dotmim.Sync.SqlServer;
using Dotmim.Sync.Tests.Core;
using Dotmim.Sync.Tests.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Dotmim.Sync.Tests.UnitTests
{
    public partial class InterceptorsTests
    {
        [Fact]
        public async Task LocalOrchestrator_Scope()
        {
            var dbName = HelperDatabase.GetRandomName("tcp_lo_");
            await HelperDatabase.CreateDatabaseAsync(ProviderType.Sql, dbName, true);
            var cs = HelperDatabase.GetConnectionString(ProviderType.Sql, dbName);
            var sqlProvider = new SqlSyncProvider(cs);
            var ctx = new AdventureWorksContext((dbName, ProviderType.Sql, sqlProvider), true, false);
            await ctx.Database.EnsureCreatedAsync();
            var scopeName = "scope";

            var options = new SyncOptions();
            var setup = new SyncSetup(this.Tables);

            var localOrchestrator = new LocalOrchestrator(sqlProvider, options);


            var scopeTableCreating = 0;
            var scopeTableCreated = 0;
            var scopeLoading = 0;
            var scopeLoaded = 0;
            var scopeSaving = 0;
            var scopeSaved = 0;

            localOrchestrator.OnScopeSaving(ssa =>
            {
                Assert.NotNull(ssa.Command);
                scopeSaving++;
            });

            localOrchestrator.OnScopeSaved(ssa => scopeSaved++);

            localOrchestrator.OnScopeTableCreating(stca =>
            {
                Assert.NotNull(stca.Command);
                scopeTableCreating++;
            });

            localOrchestrator.OnScopeTableCreated(stca =>
            {
                scopeTableCreated++;
            });

            localOrchestrator.OnClientScopeInfoLoading(args =>
            {
                Assert.NotNull(args.Command);
                Assert.Equal(SyncStage.ScopeLoading, args.Context.SyncStage);
                Assert.Equal(scopeName, args.Context.ScopeName);
                Assert.Equal(scopeName, args.ScopeName);
                Assert.NotNull(args.Connection);
                Assert.NotNull(args.Transaction);
                Assert.Equal(ConnectionState.Open, args.Connection.State);
                scopeLoading++;
            });

            localOrchestrator.OnClientScopeInfoLoaded(args =>
            {
                Assert.Equal(SyncStage.ScopeLoading, args.Context.SyncStage);
                Assert.Equal(scopeName, args.Context.ScopeName);
                Assert.NotNull(args.Connection);
                Assert.NotNull(args.Transaction);
                scopeLoaded++;
            });

            var localScopeInfo = await localOrchestrator.GetClientScopeInfoAsync(scopeName);


            Assert.Equal(1, scopeTableCreating);
            Assert.Equal(1, scopeTableCreated);
            Assert.Equal(2, scopeLoading);
            Assert.Equal(2, scopeLoaded);
            Assert.Equal(1, scopeSaving);
            Assert.Equal(1, scopeSaved);

            scopeTableCreating = 0;
            scopeTableCreated = 0;
            scopeLoading = 0;
            scopeLoaded = 0;
            scopeSaving = 0;
            scopeSaved = 0;

            localScopeInfo.Version = "2.0";

            await localOrchestrator.SaveClientScopeInfoAsync(localScopeInfo);

            Assert.Equal(0, scopeTableCreating);
            Assert.Equal(0, scopeTableCreated);
            Assert.Equal(0, scopeLoading);
            Assert.Equal(0, scopeLoaded);
            Assert.Equal(1, scopeSaving);
            Assert.Equal(1, scopeSaved);

            HelperDatabase.DropDatabase(ProviderType.Sql, dbName);
        }
        [Fact]
        public async Task RemoteOrchestrator_Scope()
        {
            var dbName = HelperDatabase.GetRandomName("tcp_lo_");
            await HelperDatabase.CreateDatabaseAsync(ProviderType.Sql, dbName, true);
            var cs = HelperDatabase.GetConnectionString(ProviderType.Sql, dbName);
            var sqlProvider = new SqlSyncProvider(cs);
            var ctx = new AdventureWorksContext((dbName, ProviderType.Sql, sqlProvider), true, false);
            await ctx.Database.EnsureCreatedAsync();
            var scopeName = "scope";

            var options = new SyncOptions();
            var setup = new SyncSetup(this.Tables);

            var remoteOrchestrator = new RemoteOrchestrator(sqlProvider, options);

            var scopeTableCreating = 0;
            var scopeTableCreated = 0;
            var scopeLoading = 0;
            var scopeLoaded = 0;
            var scopeSaving = 0;
            var scopeSaved = 0;

            remoteOrchestrator.OnScopeSaving(ssa =>
            {
                Assert.NotNull(ssa.Command);
                scopeSaving++;
            });

            remoteOrchestrator.OnScopeSaved(ssa => scopeSaved++);

            remoteOrchestrator.OnScopeTableCreating(stca =>
            {
                Assert.NotNull(stca.Command);
                scopeTableCreating++;
            });

            remoteOrchestrator.OnScopeTableCreated(stca =>
            {
                scopeTableCreated++;
            });

            remoteOrchestrator.OnServerScopeInfoLoading(args =>
            {
                Assert.NotNull(args.Command);
                Assert.Equal(scopeName, args.Context.ScopeName);
                Assert.Equal(scopeName, args.ScopeName);
                Assert.NotNull(args.Connection);
                Assert.NotNull(args.Transaction);
                Assert.Equal(ConnectionState.Open, args.Connection.State);
                scopeLoading++;
            });

            remoteOrchestrator.OnServerScopeInfoLoaded(args =>
            {
                Assert.Equal(scopeName, args.Context.ScopeName);
                Assert.NotNull(args.Connection);
                Assert.NotNull(args.Transaction);
                scopeLoaded++;
            });

            var serverScopeInfo = await remoteOrchestrator.GetServerScopeInfoAsync(scopeName, setup);
            // TODO : if serverScope.Schema is null, should we Provision here ?

            serverScopeInfo.Version = "2.0";

            await remoteOrchestrator.SaveServerScopeInfoAsync(serverScopeInfo);

            Assert.Equal(2, scopeTableCreating);
            Assert.Equal(2, scopeTableCreated);
            Assert.Equal(2, scopeLoading);
            Assert.Equal(3, scopeLoaded);
            Assert.Equal(3, scopeSaving);
            Assert.Equal(3, scopeSaved);

            HelperDatabase.DropDatabase(ProviderType.Sql, dbName);
        }
    }
}
