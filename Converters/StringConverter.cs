﻿using DemgelRedis.Interfaces;
using StackExchange.Redis;

namespace DemgelRedis.Converters
{
    public class StringConverter : ITypeConverter
    {
        public RedisValue ToWrite(object prop)
        {
            return (string)prop;
        }

        public object OnRead(RedisValue obj)
        {
            return (string)obj;
        }
    }
}