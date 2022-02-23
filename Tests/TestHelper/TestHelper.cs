using System;
using System.Linq;
using System.Reflection;

namespace TestHelper
{
    public static class Helper
    {
        public static int GetRandomNumber()
        {
            return new Random((int)DateTime.Now.Ticks).Next();
        }

        public static bool GetRandomBoolean()
        {
            return GetRandomNumber() % 200 > 100;
        }

        public static T GetFieldValueFromInstance<T>(string fieldName, bool isPublic, bool isStatic, object instance)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, (isPublic ? BindingFlags.Public : BindingFlags.NonPublic)
                | (isStatic ? BindingFlags.Static : BindingFlags.Instance));

            try
            {
                return (T)Convert.ChangeType(field.GetValue(instance), typeof(T));
            }
            catch
            {
                return (T)field.GetValue(instance);
            }
        }

        public static void SetFieldValueToInstance<T>(string fieldName, bool isPublic, bool isStatic, object instance, object value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, (isPublic ? BindingFlags.Public : BindingFlags.NonPublic)
                | (isStatic ? BindingFlags.Static : BindingFlags.Instance));

            field.SetValue(instance, value);
        }

        public static T GetPropertyValueFromInstance<T>(string propertyName, bool isPublic, bool isStatic, object instance)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, (isPublic ? BindingFlags.Public : BindingFlags.NonPublic)
                | (isStatic ? BindingFlags.Static : BindingFlags.Instance));

            try
            {
                return (T)Convert.ChangeType(property.GetValue(instance), typeof(T));
            }
            catch
            {
                return (T)property.GetValue(instance);
            }
        }

        public static void SetPropertyValueToInstance<T>(string propertyName, bool isPublic, bool isStatic, object instance, object value)
        {
            PropertyInfo field = instance.GetType().GetProperty(propertyName, (isPublic ? BindingFlags.Public : BindingFlags.NonPublic)
                | (isStatic ? BindingFlags.Static : BindingFlags.Instance));

            field.SetValue(instance, value);
        }

        public static void SetPrivateRuntimeProperty(string propertyName, object instance, object value)
        {
            var privateProperty = instance.GetType().GetRuntimeProperties().Where(x => x.Name == propertyName).First();
            if (privateProperty != null)
            {
                privateProperty.SetValue(instance, value);
            }
        }

        public static void CallPrivateMethod<T>(string methodName, object instance, out T result, object[] parameters = null)
        {
            if (parameters == null)
            {
                parameters = new object[] { };
            }

            var privateMethod = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (privateMethod == null)
            {
                result = default(T);
            }
            else
            {
                result = (T)privateMethod.Invoke(instance, parameters);
            }
        }

        public static void CallPrivateMethodVoid(string methodName, object instance, object[] parameters = null)
        {
            if (parameters == null)
            {
                parameters = new object[] { };
            }
            var privateMethod = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            privateMethod?.Invoke(instance, parameters);
        }
    }
}
