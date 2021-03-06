﻿using System;
using DemgelRedis.Interfaces;
using StackExchange.Redis;

namespace DemgelRedis.Converters
{
    public class FloatConverter : ITypeConverter
    {
        public RedisValue ToWrite(object prop)
        {
            return (float) prop;
        }

        public object OnRead(RedisValue obj)
        {
            float value;
            if (float.TryParse(obj, out value))
            {
                return value;
            }

            throw new InvalidCastException("obj is not a Float value");
        }
    }
}