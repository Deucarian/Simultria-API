using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace Deucarian.Simultria.API
{
    internal static class SimultriaRequestValues
    {
        internal static string Format(object value)
        {
            if (value is bool boolean) return boolean ? "true" : "false";
            if (value is JToken token) return token.ToString(Formatting.None);
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        internal static void Require(object value, string name)
        {
            if (value == null || value is string text && string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("A value is required for " + name + ".", name);
            }
        }

        internal static void RequirePositiveId(int? value, string name)
        {
            Require(value, name);
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(name, "A positive Simultria resource ID is required.");
            }
        }

        internal static void AddFormField(List<IMultipartFormSection> form, string name, object value)
        {
            if (value is SimultriaUpload upload)
            {
                form.Add(new MultipartFormFileSection(name, upload.Data, upload.FileName, upload.ContentType));
                return;
            }

            form.Add(new MultipartFormDataSection(name, value == null ? string.Empty : Format(value)));
        }
    }
}
