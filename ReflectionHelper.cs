using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using MergeTelemetry.Attributes;

namespace MergeTelemetry
{
    public static class ReflectionHelper
    {
        [NotNull] private static readonly Dictionary<Type, object> DefaultDictionary = new Dictionary<Type, object>();
        [NotNull] private static readonly MethodInfo MethodInfo = typeof(ReflectionHelper).GetMethod("GetDefaultInternal", BindingFlags.NonPublic | BindingFlags.Static);

        public static void Merge<T>([NotNull] object source, [NotNull] T target, List<PropertyInfo> propertyInfos) where T : class
        {
            if(source == null) throw new ArgumentNullException(nameof(source));
            if(target == null) throw new ArgumentNullException(nameof(target));
            
            foreach(PropertyInfo propertyInfo in propertyInfos)
            {
                if (propertyInfo.GetCustomAttribute<MergeIgnoreAttribute>() != null) continue;
                object targetValue = propertyInfo.GetValue(target);
                object targetPropertyTypeDefaultValue = GetDefault(propertyInfo.PropertyType);

                if(!(targetValue?.Equals(targetPropertyTypeDefaultValue) ?? targetPropertyTypeDefaultValue == null)) continue;

                object sourceValue = propertyInfo.GetValue(source);
                propertyInfo.SetValue(target, sourceValue);
            }
        }

        private static T GetDefaultInternal<T>() => default;
        private static object GetDefault(Type type)
        {
            if (DefaultDictionary.ContainsKey(type)) return DefaultDictionary[type];

            DefaultDictionary.Add(type, MethodInfo.MakeGenericMethod(type).Invoke(null, null));

            return DefaultDictionary[type];
        }
    }
}
