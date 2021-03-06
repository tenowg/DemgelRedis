﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Castle.Core.Internal;
using Castle.DynamicProxy;
using DemgelRedis.Common;
using DemgelRedis.Extensions;
using DemgelRedis.Interfaces;
using DemgelRedis.ObjectManager.Attributes;
using DemgelRedis.ObjectManager.Proxy;
using DemgelRedis.ObjectManager.Proxy.RedisObjectInterceptor;
using DemgelRedis.ObjectManager.Proxy.Selectors;
using StackExchange.Redis;

namespace DemgelRedis.ObjectManager.Handlers
{
    public class RedisObjectHandler : RedisHandler
    {
        private readonly RedisObjectSelector _redisObjectSelector;

        public RedisObjectHandler(RedisObjectManager manager)
            : base(manager)
        {
            _redisObjectSelector = new RedisObjectSelector();
        }

        public override bool CanHandle(object obj)
        {
            return obj is IRedisObject;
        }

        public override object Read(object obj, Type objType, IDatabase redisDatabase, string id, PropertyInfo basePropertyInfo, LimitObject limits = null)
        {
            if (id == null)
            {
                throw new Exception("Id can't be null");
            }
            var redisKey = new RedisKeyObject(objType, id);

            RedisObjectManager.RedisBackup?.RestoreHash(redisDatabase, redisKey);
            var ret = redisDatabase.HashGetAll(redisKey.RedisKey);

            // Attempt to set all given properties
            obj = RedisObjectManager.ConvertToObject(obj, ret);

            // Do we continue here if it is a base system class?
            if (!(obj is IRedisObject)) return obj;

            var props = obj.GetType().GetProperties();

            foreach (var prop in props)
            {
                // If value is virtual assume it is lazy
                if (!prop.GetMethod.IsVirtual) continue;
                // Create proxies here
                if (prop.PropertyType.IsSealed) continue;
                if (!prop.PropertyType.IsClass && !prop.PropertyType.IsInterface) continue;
                try
                {
                    

                    // If the target is an IRedisObject we need to get the ID differently
                    string objectKey = null;
                    //RedisValue propKey = new RedisValue();
                    if (prop.PropertyType.GetInterfaces().Any(x => x == typeof(IRedisObject)))
                    {
                        // Try to get the property value from ret
                        RedisValue propKey;
                        if (ret.ToDictionary().TryGetValue(prop.Name, out propKey))
                        {
                            objectKey = propKey.ParseKey();
                            if (prop.GetValue(obj, null) == null)
                            {
                                var pr = RedisObjectManager.GetRedisObjectWithType(redisDatabase, (string)propKey, objectKey);
                                obj.GetType().GetProperty(prop.Name).SetValue(obj, pr);
                            }
                        }
                        else
                        {
                            if (prop.PropertyType.IsInterface)
                            {
                                throw new Exception("Properties of type Interface need to be populated first");
                            }
                            else
                            {
                                object baseObject = prop.GetValue(obj, null) ?? Activator.CreateInstance(prop.PropertyType);
                                var key = new RedisKeyObject(baseObject.GetType(), string.Empty);
                                redisDatabase.GenerateId(key, baseObject, null);
                                objectKey = key.Id;

                                if (!(baseObject is IProxyTargetAccessor))
                                {
                                    var pr = RedisObjectManager.RetrieveObjectProxy(prop.PropertyType, objectKey, redisDatabase,
                                        baseObject, obj);
                                    obj.GetType().GetProperty(prop.Name).SetValue(obj, pr);
                                }
                            }
                        }
                    }
                    else
                    {
                        // Here we try to handle NON redisObjects
                        object baseObject = prop.GetValue(obj, null) ?? Activator.CreateInstance(prop.PropertyType);
                        
                        objectKey = id;

                        if (!(baseObject is IProxyTargetAccessor))
                        {
                            var pr = RedisObjectManager.RetrieveObjectProxy(prop.PropertyType, objectKey, redisDatabase,
                                baseObject, obj);
                            obj.GetType().GetProperty(prop.Name).SetValue(obj, pr);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
            
            return obj;
        }

        public override bool Save(object obj, 
            Type objType, 
            IDatabase redisDatabase, 
            string id, 
            PropertyInfo basePropertyInfo = null)
        {
            var redisKey = new RedisKeyObject(objType, id);

            CommonData data = null;
            if (obj is IProxyTargetAccessor)
            {
                data = ((IProxyTargetAccessor) obj).GetCommonData();
                data.Processing = true;
            }

            var hashList = RedisObjectManager.ConvertToRedisHash(obj).ToArray();

            if (data != null)
            {
                data.Processing = false;
            }

            RedisObjectManager.RedisBackup?.UpdateHash(hashList, redisKey);
            RedisObjectManager.RedisBackup?.RestoreHash(redisDatabase, redisKey);
            redisDatabase.HashSet(redisKey.RedisKey, hashList);

            return true;
        }

        public override bool Delete(object obj, Type objType, IDatabase redisDatabase, string id, PropertyInfo basePropertyInfo = null)
        {
            // TODO make this delete sub objects too, as long as they dont have RedisCascdeDelete(false)
            if (obj is IRedisObject)
            {
                ((IRedisObject)obj).DeleteRedisObject();
            }
            return true;
        }

        public override object BuildProxy(ProxyGenerator generator, Type objType, CommonData data, object baseObj)
        {
            object proxy;
            if (baseObj == null)
            {
                proxy = generator.CreateClassProxyWithTarget(objType,
                    new ProxyGenerationOptions(new GeneralProxyGenerationHook())
                    {
                        Selector = _redisObjectSelector
                    },
                    new GeneralGetInterceptor(data), new RedisObjectSetInterceptor(data));
            }
            else
            {
                proxy = generator.CreateClassProxyWithTarget(objType, baseObj,
                    new ProxyGenerationOptions(new GeneralProxyGenerationHook())
                    {
                        Selector = _redisObjectSelector
                    },
                    new GeneralGetInterceptor(data), new RedisObjectSetInterceptor(data));
            }

            return proxy;
        }
    }
}