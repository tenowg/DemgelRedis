﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using DemgelRedis.Common;
using DemgelRedis.Interfaces;
using DemgelRedis.ObjectManager.Attributes;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace DemgelRedis.Tests
{

    internal class TestClass : IRedisObject
    {
        public virtual Guid TestGuid { get; set; }
        public virtual string TestString { get; set; }
        public virtual int TestInt { get; set; }
        public virtual float TestFloat { get; set; }
        public virtual double TestDouble { get; set; }
        public virtual DateTime TestDateTime { get; set; }
    }

    //[RedisPrefix(Key = "testcase")]
    internal class TestConvertClass : IRedisObject
    {
        [RedisIdKey]
        public Guid Id { get; set; }
        public virtual string TestValue { get; set; }
    }

    internal class TestConvertClass2 : IRedisObject
    {
        [RedisIdKey]
        public virtual string Id { get; set; }
        public virtual string TestValue { get; set; }
        public string TestNonVirtualValue { get; set; }
    }

    internal class TestConvertClassSubSuffix : IRedisObject
    {
        [RedisIdKey]
        public virtual string Id { get; set; }
        public virtual string test { get; set; }
        public virtual TestConvertClassSub subTest { get; set; }
        //[RedisSuffix(Key = "testlist")]
        public virtual IList<RedisValue> SomeStrings { get; set; } = new List<RedisValue>();
        public virtual IList<string> NewSomeStrings { get; set; } = new List<string>();
        //[RedisPrefix(Key = "guidtest")]
        public virtual IList<TestConvertClass2> SomeIntegers { get; set; } = new List<TestConvertClass2>(); 
    }

    internal class TestConvertClassSubSuffix2 : IRedisObject
    {
        [RedisIdKey]
        public virtual string Id { get; set; }
        [RedisDeleteCascade(Cascade = false)]
        public virtual TestConvertClassSub subTest { get; set; }
    }

    internal class TestConvertClassSub : IRedisObject
    {
        [RedisIdKey]
        public virtual string Id { get; set; }
        public virtual string test { get; set; }
        public virtual TestConvertClassSubSuffix TestInitite { get; set; }
    }

    internal class TestDictionaryClass : IRedisObject
    {
        [RedisIdKey]
        public virtual string Id { get; set; }
        public virtual IDictionary<string, RedisValue> TestDictionary { get; set; } = new Dictionary<string, RedisValue>();
        public virtual IDictionary<int, string> TestDictionaryWithInt { get; set; } = new Dictionary<int, string>();
        [RedisDeleteCascade(Cascade = true)]
        internal virtual IDictionary<string, TestConvertClass2> TestConvertClasses { get; set; } = new Dictionary<string, TestConvertClass2>();
        public virtual IDictionary<RedisValue, ITestInterface> TestingInterface { get; set; } = new Dictionary<RedisValue, ITestInterface>();
    }

    internal interface ITestInterface : IRedisObject
    {
        string Id { get; set; }
        string test { get; set; }
    }
    internal class TestInterface : ITestInterface
    {
        [RedisIdKey]
        public virtual string Id { get; set; }
        public virtual string test { get; set; }
    }

    internal class TestInterfaceClass : IRedisObject
    {
        [RedisIdKey]
        public virtual string Id { get; set; }
        public virtual ITestInterface Interface { get; set; }
    }

    internal class TestSetOpertions : IRedisObject
    {
        [RedisIdKey]
        public virtual string Id { get; set; }
        public virtual ISet<TestSet> TestSet { get; set; } = new RedisSortedSet<TestSet>(); 
    }

    internal class TestSet : IRedisObject
    {
        [RedisIdKey]
        public virtual string Id { get; set; }
        public virtual string SomeString { get; set; }
        [RedisSetOrderKey]
        public virtual DateTime SomeDate { get; set; }
    }
}