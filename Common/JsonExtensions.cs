using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace Common
{
    public static class JsonExtensions
    {
        /// <summary>
        /// Serializes an object to JSON
        /// </summary>
        /// <param name="self">Object to serialize.</param>
        /// <param name="camelCasingMembers">Whether the members of JSON object should be in camel casing or pascal casing</param>
        /// <param name="swallowException">if set to <c>true</c> swallow exception thrown during serialization and returns null.</param>
        public static string ToJson(
            this object self,
            bool camelCasingMembers = false,
            bool swallowException = false)
        {
            var setting = new JsonSerializerSettings
            {
                ContractResolver = camelCasingMembers ? new CamelCasePropertyNamesContractResolver() : new DefaultContractResolver()
            };

            try
            {
                return JsonConvert.SerializeObject(self, setting);
            }
            catch (Exception)
            {
                if (!swallowException)
                    throw;
                else
                    return null;
            }
        }

        /// <summary>
        /// De-serializes an object from JSON
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="self">JSON string.</param>
        /// <param name="swallowException">if set to <c>true</c> swallow exception thrown during de-serialization and returns null.</param>
        public static T FromJson<T>(this string self, bool swallowException = false)
            where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(self);
            }
            catch (Exception)
            {
                if (!swallowException)
                    throw;
                else
                    return null;
            }
        }
    }
}
