// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance;
using Cratis.Concepts;
using Cratis.Schemas;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sample.Customers;

namespace Sample
{
    [Route("/api/compliance")]
    public class ComplianceTesting : Controller
    {
        readonly IJsonSchemaGenerator _schemaGenerator;
        readonly IJsonComplianceManager _complianceManager;
        readonly IEncryption _encryption;
        readonly IEncryptionKeyStore _encryptionKeyStore;

        public ComplianceTesting(IJsonSchemaGenerator schemaGenerator, IJsonComplianceManager complianceManager, IEncryption encryption, IEncryptionKeyStore encryptionKeyStore)
        {
            _schemaGenerator = schemaGenerator;
            _complianceManager = complianceManager;
            _encryption = encryption;
            _encryptionKeyStore = encryptionKeyStore;
        }

        [HttpGet]
        public async Task<JObject> Test()
        {
            const string identifier = "6d96c760-ce66-48af-9751-c206bde791d8";

            // var key = _encryption.GenerateKey();
            // await _encryptionKeyStore.SaveFor(identifier, key);

            var schema = _schemaGenerator.Generate(typeof(CustomerSignedUp));

            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new ConceptAsJsonConverter(),
                    new ConceptAsDictionaryJsonConverter()
                }
            };

            var @event = new CustomerSignedUp("20107543776", "Einar", "Ingebrigtsen");

            var eventAsJson = JsonConvert.SerializeObject(@event, serializerSettings);
            var eventAsJObject = JObject.Parse(eventAsJson);

            var result = await _complianceManager.Apply(schema, identifier, eventAsJObject);
            result = await _complianceManager.Release(schema, identifier, result);
            return result;
        }

    }
}
