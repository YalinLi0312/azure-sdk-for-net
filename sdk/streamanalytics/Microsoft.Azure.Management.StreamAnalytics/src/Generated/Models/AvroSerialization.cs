// <auto-generated>
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.
//
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Microsoft.Azure.Management.StreamAnalytics.Models
{
    using Newtonsoft.Json;
    using System.Linq;

    /// <summary>
    /// Describes how data from an input is serialized or how data is
    /// serialized when written to an output in Avro format.
    /// </summary>
    [Newtonsoft.Json.JsonObject("Avro")]
    public partial class AvroSerialization : Serialization
    {
        /// <summary>
        /// Initializes a new instance of the AvroSerialization class.
        /// </summary>
        public AvroSerialization()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the AvroSerialization class.
        /// </summary>
        /// <param name="properties">The properties that are associated with
        /// the Avro serialization type. Required on PUT (CreateOrReplace)
        /// requests.</param>
        public AvroSerialization(object properties = default(object))
        {
            Properties = properties;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets the properties that are associated with the Avro
        /// serialization type. Required on PUT (CreateOrReplace) requests.
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public object Properties { get; set; }

    }
}
